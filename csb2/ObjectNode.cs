using System.Collections.Generic;
using System.Text;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
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
                Executable = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "link.exe").ToString()
            };

            Shell.ExecuteAndCaptureOutput(args);
            return true;
        }
    }
}