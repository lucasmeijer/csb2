using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
    public class ObjectNode : GeneratedFileNode
    {
        private readonly FileNode _cppFile;

        public ObjectNode(FileNode cppFile, NPath objectFile) : base(objectFile)
        {
            _cppFile = cppFile;
            SetStaticDependencies(_cppFile);
        }
        
        protected override bool BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();
            foreach (var includeDir in MsvcInstallation.GetLatestInstalled().GetIncludeDirectories())
                includeArguments.Append("-I" + includeDir.InQuotes() + " ");

            var args = new Shell.ExecuteArgs
            {
                Arguments = includeArguments + _cppFile.File.InQuotes() + " /Fo:" + File.InQuotes() + " -c",
                Executable = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "cl.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }

        public ObjectNode[] ObjectNodes => new[] {this};
        public override string NodeTypeIdentifier => "Obj";
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
        public AliasNode(string name, Node[] staticDependencies) : base(name)
        {
            SetStaticDependencies(staticDependencies);
        }

        public override string NodeTypeIdentifier => "Alias";

        public override UpdateReason DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return null;
        }
    }
}
