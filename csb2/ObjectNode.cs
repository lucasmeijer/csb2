using System.Collections.Generic;
using System.Text;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
    public class ObjectNode : GeneratedFileNode
    {
        private readonly SourceFileNode _cppFile;

        public ObjectNode(SourceFileNode cppFile, NPath objectFile) : base(objectFile)
        {
            _cppFile = cppFile;
        }

        public override IEnumerable<Node> Dependencies
        {
            get { yield return _cppFile; }
        }

        protected override bool BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();
            foreach (var includeDir in MsvcInstallation.GetLatestInstalled().GetIncludeDirectories())
                includeArguments.Append("-I" + includeDir.InQuotes()+" ");
            
            var args = new Shell.ExecuteArgs
            {
                Arguments = includeArguments+ _cppFile.File.InQuotes() + " /Fo:" + File.InQuotes() + " -c",
                Executable = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "cl.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }
    }

    public class UpdateReason
    {
        private readonly string _message;

        public UpdateReason(string message)
        {
            _message = message;
        }

        public override string ToString()
        {
            return _message;
        }
    }

    class AliasNode : Node
    {
        private readonly Node[] _dependencies;

        public AliasNode(string name, Node[] dependencies) : base(name)
        {
            _dependencies = dependencies;
        }

        public override UpdateReason DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return null;
        }

        public override IEnumerable<Node> Dependencies => _dependencies;
    }
}