using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly string[] _defines;
        private readonly string[] _flags;

        public ObjectNode(FileNode cppFile, NPath objectFile, NPath[] includeDirs, string[] defines, string[] flags) : base(objectFile)
        {
            _cppFile = cppFile;
            _includeDirs = includeDirs;
            _defines = defines;
            _flags = flags;
            SetStaticDependencies(_cppFile);
        }

        protected override JobResult BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();

            foreach (var includeDir in AllIncludeDirectories)
                includeArguments.Append("-I" + includeDir.InQuotes() + " ");

            var cl = MsvcInstallation.GetInstallation(new Version(10, 0)).GetVSToolPath(new x64Architecture(), "cl.exe").ToString();

            var fullArgs = new Shell.ExecuteArgs {Arguments = includeArguments + " " + DefineAndFlagArguments() + " /nologo /Fo" + File.InQuotes(SlashMode.Forward) + " " + _cppFile.File.InQuotes(SlashMode.Forward), Executable = cl};

            Shell.ExecuteResult executeResult;
            using (TinyProfiler.Section("CompileFull " + _cppFile.File))
                executeResult = Shell.Execute(fullArgs);

            var output = executeResult.StdOut + executeResult.StdErr;

            if (output.StartsWith(_cppFile.File.FileName))
                output = output.Substring(File.FileName.Length).Trim();

            return new JobResult()
            {
                BuildInfo = new PreviousBuildsDatabase.Entry() {File = Name, TimeStamp = File.TimeStamp, InputsHash = InputsHash},
                Success = executeResult.ExitCode == 0,
                Output = output,
                Input = fullArgs.Arguments,
                Node = this
            };

        }

        private string DefineAndFlagArguments()
        {
            var sb = new StringBuilder();

            foreach (var define in _defines)
                sb.Append("-D" + define+" ");
            foreach (var flag in _flags)
                sb.Append(flag + " ");
            sb.Append(" /c ");
            return sb.ToString();
        }

        private IEnumerable<NPath> AllIncludeDirectories => _includeDirs.Concat(ToolChainIncludeDirectories);

        private static IEnumerable<NPath> ToolChainIncludeDirectories => MsvcInstallation.GetInstallation(new Version(10,0)).GetIncludeDirectories();

        protected override PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            return new PreviousBuildsDatabase.Entry() { File = Name, TimeStamp = File.TimeStamp, InputsHash = InputsHash };
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
