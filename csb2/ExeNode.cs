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
        private readonly ObjectsNode _objectsNode;
        private ObjectNode[] _objectNodes;

        public ExeNode(NPath exeFile, ObjectsNode objectsNode) : base(exeFile)
        {
            _objectsNode = objectsNode;
        }

        public override IEnumerable<Node> StaticDependencies
        {
            get { yield return _objectsNode; }
        }

        public override void SetupDynamicDependencies()
        {
            _objectNodes = _objectsNode.ObjectNodes;
        }

        public override IEnumerable<Node> DynamicDependencies => _objectNodes;

        protected override bool BuildGeneratedFile()
        {
            var libPaths = MsvcInstallation.GetLatestInstalled().GetLibDirectories(new x86Architecture()).InQuotes().Select(s => "/LIBPATH:" + s).SeperateWithSpace();
                
            var args = new Shell.ExecuteArgs
            {
                Arguments = libPaths +" "+ _objectsNode.ObjectNodes.Select(o=>o.File).InQuotes().SeperateWithSpace() + " /OUT:" + File.InQuotes(),
                Executable = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "link.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }
    }

    
}