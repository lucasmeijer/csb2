﻿using System.Collections.Generic;
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
using Unity.TinyProfiling;

namespace csb2
{
    public class ObjectNode : GeneratedFileNode
    {
        private readonly FileNode _cppFile;
        private string _codeGenArguments = "-O2";

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
            
            var tmp = NPath.SystemTemp.Combine("csb2").EnsureDirectoryExists().Combine(Hashing.CalculateHash(_cppFile.File.ToString()).ToString()).ChangeExtension("cpp");
            var cl = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "cl.exe").ToString();

            var preprocessorargs = new Shell.ExecuteArgs {Arguments = includeArguments + _cppFile.File.InQuotes() + "  /P /Fi:" + tmp, Executable = cl};

            using (TinyProfiler.Section("PreProcessor " + _cppFile.File))
                Shell.ExecuteAndCaptureOutput(preprocessorargs);

            var task = Task.Run(() => FindIncludedFilesInPreprocessorOutput(tmp));

            var args = new Shell.ExecuteArgs {Arguments = tmp.InQuotes() + _codeGenArguments+ " /Fo:" + File.InQuotes() + " -c", Executable = cl};

            using (TinyProfiler.Section("Compile " + _cppFile.File))
                Shell.ExecuteAndCaptureOutput(args);
            /*

      var fullArgs = new Shell.ExecuteArgs { Arguments = includeArguments + _cppFile.File.InQuotes() + " /Fo:" + File.InQuotes() + " -c", Executable = cl };

      using (TinyProfiler.Section("CompileFull " + _cppFile.File))
          Shell.ExecuteAndCaptureOutput(fullArgs);
      var showIncludes = new Shell.ExecuteArgs { Arguments = includeArguments + _cppFile.File.InQuotes() + " /showIncludes -c", Executable = cl };

      using (TinyProfiler.Section("showIncludes " + _cppFile.File))
          Shell.ExecuteAndCaptureOutput(showIncludes);

*/

            using (TinyProfiler.Section("WaitForIncludes " + _cppFile.File))
                task.Wait();
            var result = task.Result;

            tmp.Delete();
            return new PreviousBuildsDatabase.Entry() {Name = Name, OutOfGraphDependencies = result.Select(r=>new PreviousBuildsDatabase.OutOfGraphDependency() {Name = r, TimeStamp = new NPath(r).TimeStamp}).ToArray(), TimeStamp = File.TimeStamp, CacheKey = InputsHash};

        }

        protected override PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            return new PreviousBuildsDatabase.Entry() { Name = Name, OutOfGraphDependencies = new PreviousBuildsDatabase.OutOfGraphDependency[0], TimeStamp = File.TimeStamp, CacheKey = InputsHash };
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
                {
                    var value = match.Groups["file"].Value;
                    if (!value.Contains("Microsoft Visual Studio") && !value.Contains("Windows Kits"))
                        result.Add(value);
                }
                return result.ToArray();
            }
        }


        public override bool SupportsNetworkCache => true;

        public override string InputsHash => Hashing.CalculateHash(Hashing.CalculateHash(_cppFile.File) + _codeGenArguments).ToString();

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
