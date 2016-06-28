using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
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
                Executable = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "link.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }
    }
}