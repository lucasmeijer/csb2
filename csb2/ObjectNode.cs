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
            CanDistribute = true;
        }

        protected override JobResult BuildGeneratedFile()
        {
            var includeArguments = new StringBuilder();

            foreach (var includeDir in AllIncludeDirectories)
                includeArguments.Append("/I" + includeDir.InQuotes() + " ");

            var cl = ClExe;

            var inputAndOutputFile = new[] {$"/Fo{File.InQuotes(SlashMode.Forward)}", CppFile.File.InQuotes()};

            var exeArgs = new Shell.ExecuteArgs
            {
                Arguments = InputSignatureFlags.Concat(inputAndOutputFile).SeperateWithSpace(),
                Executable = cl,
                EnvVars = new Dictionary<string, string>() {{"INCLUDE", ToolChainIncludeDirectories.SeperateWith(";")}}
            };

            Shell.ExecuteResult executeResult;
            using (TinyProfiler.Section("CompileFull " + CppFile.File))
                executeResult = Shell.Execute(exeArgs);
            
            return new JobResult() {BuildInfo = MakeBuildInfo(), ResultState = executeResult.ExitCode == 0 ? State.Built : State.Failed, Output = TrimCompilerOutput(executeResult.StdOut + executeResult.StdErr), Input = exeArgs.Arguments, Node = this};
        }

        private string TrimCompilerOutput(string output)
        {
            if (!output.StartsWith(CppFile.File.FileName))
                return output;
            return output.Substring(File.FileName.Length).Trim();
        }

        private IEnumerable<string> InputSignatureFlags => PreprocessorArguments.Concat(CodegenArguments);

        private IEnumerable<string> CodegenArguments => Flags.Where(f=>!IsForceInclude(f)).Append("/c","/nologo");

        private static bool IsForceInclude(string flag)
        {
            return flag.StartsWith("/FI");
        }

        private IEnumerable<string> PreprocessorArguments
        {
            get { return AllIncludeDirectories.Select(i => $"/I{i.InQuotes()}").Concat(Defines.Select(d => $"/D{d}")).Append("/nologo").Concat(Flags.Where(IsForceInclude)); }
        }

        private PreviousBuildsDatabase.Entry MakeBuildInfo()
        {
            return new PreviousBuildsDatabase.Entry() {File = Name, TimeStamp = File.TimeStamp, InputsSummary = InputsSummary};
        }

        private static string ClExe => MsvcInstallation.GetInstallation(new Version(10, 0)).GetVSToolPath(new x64Architecture(), "cl.exe").ToString();

        private IEnumerable<NPath> AllIncludeDirectories => IncludeDirs.Concat(ToolChainIncludeDirectories);

        private static IEnumerable<NPath> ToolChainIncludeDirectories => MsvcInstallation.GetInstallation(new Version(10, 0)).GetIncludeDirectories();


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
                    CommandLine = PreprocessorArguments.Concat(CodegenArguments).ConcatAll(),
                    Dependencies = dependentfiles.Select(o => new FileSummary() {FileName = o.ToString(), Hash = FileHashProvider.Instance.HashFor(o), TimeStamp = o.TimeStamp}).ToArray()
                };
            }
        }

        public override string NodeTypeIdentifier => "Obj";

        public override IEnumerable<Node> ProvideStaticDependencies()
        {
            yield return CppFile;
        }

        public override CompilationRequest MakeDistributionRequest()
        {
            var preprocessOutput = NPath.SystemTemp.Combine("csb2", "p" + new System.Random().Next()).EnsureDirectoryExists().Combine(CppFile.File.FileName);

            var inputAndOutputFile = new[] {"/P", $"/Fi{preprocessOutput.InQuotes(SlashMode.Forward)}", CppFile.File.InQuotes(SlashMode.Forward)};

            var exeArgs = new Shell.ExecuteArgs {Arguments = PreprocessorArguments.Concat(inputAndOutputFile).SeperateWithSpace(), Executable = ClExe};

            Shell.ExecuteResult executeResult;
            using (TinyProfiler.Section("Preprocess: " + CppFile.File))
                executeResult = Shell.Execute(exeArgs);

            if (executeResult.ExitCode != 0)
            {
                CanDistribute = false;
                return null;
            }

            return new CompilationRequest()
            {
                Arguments = CodegenArguments.SeperateWithSpace() + $" /c {CppFile.File.FileName} /Fo\"{CppFile.File.ChangeExtension("obj").FileName}\"",
                Program = ClExe,
                Contents = preprocessOutput.ReadAllBytes(),
                FileName = CppFile.File.FileName
            };
        }

        public override JobResult ProcessDistributionResponse(CompilationResponse response)
        {
            if (response.ExitCode == 0)
                File.WriteAllBytes(response.Contents);

            return new JobResult()
            {
                BuildInfo = MakeBuildInfo(),
                Input = InputSignatureFlags.SeperateWithSpace(),
                Node = this,
                Output = TrimCompilerOutput(response.Output),
                ResultState = response.ExitCode == 0 ? State.Built : State.Failed,
                Source = response.SourceIdentifier
            };
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
