using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
    public class ExeNode : GeneratedFileNode
    {
        private readonly ObjectNode[] _objectNodes;

        public ExeNode(NPath exeFile, ObjectNode[] objectNodes) : base(exeFile)
        {
            _objectNodes = objectNodes;
            SetStaticDependencies(objectNodes);
        }

        protected override PreviousBuildsDatabase.Entry BuildGeneratedFile()
        {
            var libPaths = MsvcInstallation.GetLatestInstalled().GetLibDirectories(new x86Architecture()).InQuotes().Select(s => "/LIBPATH:" + s).SeperateWithSpace();
                
            var args = new Shell.ExecuteArgs
            {
                Arguments = libPaths +" "+ _objectNodes.Select(o=>o.File).InQuotes().SeperateWithSpace() + " /OUT:" + File.InQuotes(),
                Executable = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "link.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);

            return new PreviousBuildsDatabase.Entry()
            {
                Name = Name,
                TimeStamp = File.TimeStamp,
                OutOfGraphDependencies = _objectNodes.Select(o => new PreviousBuildsDatabase.OutOfGraphDependency() {Name = o.File.ToString(), TimeStamp = o.File.TimeStamp}).ToArray()
            };
        }

        public override string NodeTypeIdentifier => "Exe";
    }

    
}