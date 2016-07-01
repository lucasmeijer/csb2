using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            return MakeDBEntry();
        }

        private PreviousBuildsDatabase.Entry MakeDBEntry()
        {
            return new PreviousBuildsDatabase.Entry()
            {
                Name = Name,
                InputsHash= InputsHash,
                TimeStamp = File.TimeStamp,
            };
        }

        public override bool SupportsNetworkCache => true;


        protected override PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            return MakeDBEntry();
        }

        public override string InputsHash
        {
            get
            {
                var sb = new StringBuilder(File.FileName);
                foreach (var o in _objectNodes)
                {
                    if (o.File.ToString().Contains("Project4"))
                        System.GC.Collect();
                    if (o.File.ToString() == @"c:\test\artifacts\Project4\File4.obj")
                        System.GC.Collect();
                    sb.Append(FileHashProvider.Instance.HashFor(o.File));
                }
                return Hashing.CalculateHash(sb.ToString());
            }
        }

        public override string NodeTypeIdentifier => "Exe";
    }

    
}