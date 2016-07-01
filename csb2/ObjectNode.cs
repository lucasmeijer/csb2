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
using Unity.TinyProfiling;

namespace csb2
{
    public class ObjectNode : GeneratedFileNode
    {
        private readonly FileNode _cppFile;
        private readonly NPath[] _includeDirs;
        private string _codeGenArguments = "-O2";

        public ObjectNode(FileNode cppFile, NPath objectFile, NPath[] includeDirs) : base(objectFile)
        {
            _cppFile = cppFile;
            _includeDirs = includeDirs;
            SetStaticDependencies(_cppFile);
        }
        
        protected override PreviousBuildsDatabase.Entry BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();

            foreach (var includeDir in AllIncludeDirectories)
                includeArguments.Append("-I" + includeDir.InQuotes() + " ");
            
            var tmp = NPath.SystemTemp.Combine("csb2").EnsureDirectoryExists().Combine(Hashing.CalculateHash(_cppFile.File.ToString()).ToString()).ChangeExtension("cpp");
            var cl = MsvcInstallation.GetLatestInstalled().GetVSToolPath(new x86Architecture(), "cl.exe").ToString();

            /*
            var preprocessorargs = new Shell.ExecuteArgs {Arguments = includeArguments + _cppFile.File.InQuotes() + "  /P /Fi:" + tmp, Executable = cl};
            
            using (TinyProfiler.Section("PreProcessor " + _cppFile.File))
                Shell.ExecuteAndCaptureOutput(preprocessorargs);
                */


      var fullArgs = new Shell.ExecuteArgs { Arguments = includeArguments + " " + _codeGenArguments + " "+_cppFile.File.InQuotes() + " /Fo:" + File.InQuotes() + " -c", Executable = cl };

      using (TinyProfiler.Section("CompileFull " + _cppFile.File))
          Shell.ExecuteAndCaptureOutput(fullArgs);
      var showIncludes = new Shell.ExecuteArgs { Arguments = includeArguments + _cppFile.File.InQuotes() + " /showIncludes -c", Executable = cl };
            

            

            return new PreviousBuildsDatabase.Entry() {Name = Name, TimeStamp = File.TimeStamp, InputsHash = InputsHash};

        }

        private IEnumerable<NPath> AllIncludeDirectories => _includeDirs.Concat(ToolChainIncludeDirectories);

        private static IEnumerable<NPath> ToolChainIncludeDirectories => MsvcInstallation.GetLatestInstalled().GetIncludeDirectories();

        protected override PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            return new PreviousBuildsDatabase.Entry() { Name = Name, TimeStamp = File.TimeStamp, InputsHash = InputsHash };
        }

        IEnumerable<NPath> FindIncludedFiles(NPath file, HashSet<NPath> alreadyProcessed = null )
        {
            if (alreadyProcessed == null)
                alreadyProcessed = new HashSet<NPath>();
            foreach (var nPath in _parser.FindIncludedFiles(file, _includeDirs, ToolChainIncludeDirectories, alreadyProcessed)) yield return nPath;
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

        private string _inputsHash = null;
        private static IncludeParser _parser = new IncludeParser();

        public override string InputsHash
        {
            get
            {
                if (_inputsHash != null)
                    return _inputsHash;

                _inputsHash = CalculateInputsHash();
                return _inputsHash;
            }
        }

        private string CalculateInputsHash()
        {
            using (TinyProfiler.Section("CalculateInputsHash "+_cppFile.File))
            {
                var includeFiles = FindIncludedFiles(_cppFile.File);
                var sb = new StringBuilder(FileHashProvider.Instance.HashFor(_cppFile.File));

                foreach (var includeFile in includeFiles)
                {
                    sb.Append(includeFile.FileName);
                    sb.Append(FileHashProvider.Instance.HashFor(includeFile));
                }
                return Hashing.CalculateHash(sb.ToString());
            }
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
