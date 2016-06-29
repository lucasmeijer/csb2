using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        
        protected override PreviousBuildsDatabase.Entry BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();
            foreach (var includeDir in MsvcInstallation.GetLatestInstalled().GetIncludeDirectories())
                includeArguments.Append("-I" + includeDir.InQuotes() + " ");
            
            var tmp = NPath.SystemTemp.Combine("csb2").EnsureDirectoryExists().Combine(CalculateHash(_cppFile.File.ToString()).ToString()).ChangeExtension("cpp");
            var cl = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "cl.exe").ToString();

            var preprocessorargs = new Shell.ExecuteArgs {Arguments = includeArguments + _cppFile.File.InQuotes() + "  /P /Fi:" + tmp, Executable = cl};

            Shell.ExecuteAndCaptureOutput(preprocessorargs);

            var task = Task.Run(() => FindIncludedFilesInPreprocessorOutput(tmp));

            var args = new Shell.ExecuteArgs {Arguments = tmp.InQuotes() + " /Fo:" + File.InQuotes() + " -c", Executable = cl};

            Shell.ExecuteAndCaptureOutput(args);

            task.Wait();
            var result = task.Result;

            tmp.Delete();

            return new PreviousBuildsDatabase.Entry() {Name = Name, OutOfGraphDependencies = result.Select(r=>new PreviousBuildsDatabase.OutOfGraphDependency() {Name = r, TimeStamp = new NPath(r).TimeStamp}).ToArray(), TimeStamp = File.TimeStamp};

        }

        string[] FindIncludedFilesInPreprocessorOutput(NPath preprocessorOutput)
        {
            var regex = new Regex(@"^#line \d+ ""(?<file>.+)""", RegexOptions.Multiline);
            using (StreamReader reader = new StreamReader(preprocessorOutput.ToString()))
            {
                var all = reader.ReadToEnd();
                var matches = regex.Matches(all);
                var result = new SortedSet<string>();

                foreach (Match match in matches)
                    result.Add(match.Groups["file"].Value);
                return result.ToArray();
            }
        }


        public ObjectNode[] ObjectNodes => new[] {this};
        public override string NodeTypeIdentifier => "Obj";

        static UInt64 CalculateHash(string read)
        {
            UInt64 hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
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
