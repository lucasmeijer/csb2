using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;
using Unity.TinyProfiling;

namespace csb2
{
    public class ObjectNode : GeneratedFileNode
    {
        public FileNode CppFile { get; }
        public NPath[] IncludeDirs { get; }
        public string[] Defines { get; }
        public string[] Flags { get; }

        public ObjectNode(FileNode cppFile, NPath objectFile, NPath[] includeDirs, string[] defines, string[] flags) : base(objectFile)
        {
            CppFile = cppFile;
            IncludeDirs = includeDirs;
            Defines = defines;
            Flags = flags;
            SetStaticDependencies(CppFile);
        }

        protected override JobResult BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();

            foreach (var includeDir in AllIncludeDirectories)
                includeArguments.Append("-I" + includeDir.InQuotes() + " ");

            var cl = MsvcInstallation.GetInstallation(new Version(10, 0)).GetVSToolPath(new x64Architecture(), "cl.exe").ToString();

            var fullArgs = new Shell.ExecuteArgs {Arguments = includeArguments + " " + DefineAndFlagArguments() + " /nologo /Fo" + File.InQuotes(SlashMode.Forward) + " " + CppFile.File.InQuotes(SlashMode.Forward), Executable = cl};

            Shell.ExecuteResult executeResult;
            using (TinyProfiler.Section("CompileFull " + CppFile.File))
                executeResult = Shell.Execute(fullArgs);

            var output = executeResult.StdOut + executeResult.StdErr;

            if (output.StartsWith(CppFile.File.FileName))
                output = output.Substring(File.FileName.Length).Trim();

            return new JobResult()
            {
                BuildInfo = new PreviousBuildsDatabase.Entry() {File = Name, TimeStamp = File.TimeStamp, InputsSummary = InputsSummary},
                ResultState =  executeResult.ExitCode == 0 ? State.Built : State.Failed,
                Output = output,
                Input = fullArgs.Arguments,
                Node = this
            };

        }

        private string DefineAndFlagArguments()
        {
            var sb = new StringBuilder();

            foreach (var define in Defines)
                sb.Append("-D" + define+" ");
            foreach (var flag in Flags)
                sb.Append(flag + " ");
            sb.Append(" /c ");
            return sb.ToString();
        }

        private IEnumerable<NPath> AllIncludeDirectories => IncludeDirs.Concat(ToolChainIncludeDirectories);

        private static IEnumerable<NPath> ToolChainIncludeDirectories => MsvcInstallation.GetInstallation(new Version(10,0)).GetIncludeDirectories();


        public override bool SupportsNetworkCache => true;

        private string _inputsHash = null;
        private static IncludeParser _parser = new IncludeParser();

        protected override InputsSumary CalculateInputsSummary()
        {
            using (TinyProfiler.Section("CalculateInputsHash " + CppFile.File))
            {
                var includeFiles = _parser.FindIncludedFiles(CppFile.File, IncludeDirs).ToArray();

                var dependentfiles = includeFiles.Concat(new[] {CppFile.File});
                return new InputsSumary()
                {
                    TargetFileName = File.ToString(),
                    CommandLine = DefineAndFlagArguments(),
                    Dependencies = dependentfiles.Select(o => new FileSummary() {FileName = o.ToString(), Hash = FileHashProvider.Instance.HashFor(o), TimeStamp = o.TimeStamp}).ToArray()
                };
            }
        }

        public override string NodeTypeIdentifier => "Obj";

        public override IEnumerable<Node> ProvideStaticDependencies()
        {
            yield return CppFile;
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
