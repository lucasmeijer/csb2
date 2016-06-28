using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
    public abstract class Node
    {
        public string Name { get; }
        public DateTime TimeStamp { get; protected set; }
        private State m_State;
        public virtual IEnumerable<Node> Dependencies { get{ yield break;}}

        protected Node(string name)
        {
            Name = name;
        }

        public State GetState()
        {
            return m_State;
        }

        public void SetState(State state)
        {
            m_State = state;
        }

        public abstract bool DetermineNeedToBuild(PreviousBuildsDatabase db);


        public virtual bool Build()
        {
            return true;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class FileNode : Node
    {
        public NPath File { get; private set; }

        public FileNode(NPath file) : base(file.ToString())
        {
            File = file;
        }

        public override bool DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return true;
        }
    }

    public class ObjectNode : Node
    {
        private readonly FileNode _cppFile;
        private readonly string _objectFile;

        public ObjectNode(FileNode cppFile, string objectFile) : base(objectFile)
        {
            _cppFile = cppFile;
            _objectFile = objectFile;
        }

        public override bool DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            PreviousBuildsDatabase.Entry e = null;
            db.TryGetInfoFor(Name, out e);
            if (e == null)
                return true;

            foreach (var dep in Dependencies)
            {
                if (dep.TimeStamp > e.TimeStamp)
                    return true;
            }

            return false;
        }

        public override IEnumerable<Node> Dependencies
        {
            get { yield return _cppFile; }
        }

        public override bool Build()
        {
            var includeArguments = new StringBuilder();
            foreach (var includeDir in MsvcInstallation.GetLatestInstalled().GetIncludeDirectories())
                includeArguments.Append("-I" + includeDir.InQuotes()+" ");
            
            var args = new Shell.ExecuteArgs
            {
                Arguments = includeArguments+ _cppFile.File.ToString() + " /Fo:" + _objectFile + " -c",
                Executable = MSVC.Base.Combine("bin/cl.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }
    }

    static class MSVC
    {
        public static NPath Base { get; } = new NPath(@"C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC");
    }
    public class ExeNode : Node
    {
        private readonly string _exeFile;
        private readonly ObjectNode[] _objectNodes;

        public ExeNode(string exeFile, ObjectNode[] objectNodes) : base(exeFile)
        {
            _exeFile = exeFile;
            _objectNodes = objectNodes;
        }

        public override bool DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return true;
        }

        public override IEnumerable<Node> Dependencies => _objectNodes;

      

        public override bool Build()
        {
            var libPaths = MsvcInstallation.GetLatestInstalled().GetLibDirectories(new x86Architecture()).InQuotes().Select(s => "/LIBPATH:" + s).SeperateWithSpace();
                
            var args = new Shell.ExecuteArgs
            {
                Arguments = libPaths +" "+ _objectNodes.Single() + " /OUT:" + _exeFile,
                Executable = MSVC.Base.Combine("bin/link.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }
    }

    class NodeGraph
    {
        private List<Node> _nodes = new List<Node>();

        public void AddNode(Node n)
        {
            _nodes.Add(n);
        }
    }

    class Builder
    {
        private readonly PreviousBuildsDatabase _previousBuildsDatabase;
        private readonly Queue<Node> m_Jobs = new Queue<Node>();

        public Builder(PreviousBuildsDatabase previousBuildsDatabase)
        {
            _previousBuildsDatabase = previousBuildsDatabase;
        }

        public void Build(Node nodeToBuild)
        {
            while (true)
            {
                if (DoPass(nodeToBuild))
                    return;

                PumpJobs();
            }
        }

        private void PumpJobs()
        {
            if (!m_Jobs.Any())
                return;
            var job = m_Jobs.Dequeue();

            if (!job.Build())
            {
                job.SetState(State.Failed);
                throw new BuildFailedException("Failed building " + job);
            }
            job.SetState(State.UpToDate);

            _previousBuildsDatabase.SetInfoFor(job.Name, new PreviousBuildsDatabase.Entry() {TimeStamp = new NPath(job.Name).TimeStamp});
        }

        private bool DoPass(Node nodeToBuild)
        {
            if (nodeToBuild.GetState() == State.UpToDate)
                return true;

            if (nodeToBuild.GetState() == State.NotProcessed)
            {
                bool allUpToDate = true;
                foreach (var dep in nodeToBuild.Dependencies)
                {
                    var upToDate = DoPass(dep);
                    if (!upToDate)
                        allUpToDate = false;
                }
                if (allUpToDate)
                    nodeToBuild.SetState(State.DependenciesReady);
                else
                    return false;
            }

            if (nodeToBuild.GetState() == State.DependenciesReady)
            {
                var needToBuild = nodeToBuild.DetermineNeedToBuild(_previousBuildsDatabase);
                if (!needToBuild)
                {
                    nodeToBuild.SetState(State.UpToDate);
                    return true;
                }
                nodeToBuild.SetState(State.Building);
                QueueJob(nodeToBuild);
                return false;
            }

            throw new NotSupportedException();
        }

        private void QueueJob(Node nodeToBuild)
        {
            m_Jobs.Enqueue(nodeToBuild);
        }
    }

    internal class BuildFailedException : Exception
    {
        public BuildFailedException(string message) : base(message)
        {
        }
    }

    public enum State
    {
        NotProcessed,
        UpToDate,
        DependenciesReady,
        Building,
        Failed
    }
}