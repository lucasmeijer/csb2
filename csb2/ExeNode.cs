using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;

namespace csb2
{
    public class ExeNode : GeneratedFileNode
    {
        private readonly ObjectNode[] _objectNodes;
        private readonly NPath[] _staticLibs;
        private readonly string[] _linkFlags;

        public ExeNode(NPath exeFile, ObjectNode[] objectNodes, NPath[] staticLibs, string[] linkFlags) : base(exeFile)
        {
            _objectNodes = objectNodes;
            _staticLibs = staticLibs;
            _linkFlags = linkFlags;
            SetStaticDependencies(objectNodes);
        }

        protected override JobResult BuildGeneratedFile()
        {
            //var libPaths = MsvcInstallation.GetInstallation(new Version(10,0)).GetLibDirectories(new x64Architecture()).InQuotes().Select(s => "/LIBPATH:" + s).SeperateWithSpace();

            var rsp = NPath.SystemTemp.Combine("csb2").EnsureDirectoryExists().Combine("responseFile" + (new System.Random().Next()));
            rsp.WriteAllText(LinkerArguments);
            
            var args = new Shell.ExecuteArgs
            {
                Arguments = $"@\"{rsp}\"",
                Executable = LinkExe.ToString(),
                EnvVars = new Dictionary<string, string>() { { "PATH", @"C:/Program Files (x86)/Microsoft Visual Studio 10.0/vc/bin/amd64/;C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE" } }
            };

            var executeResult = Shell.Execute(args);
           
            return new JobResult
            {
                BuildInfo = new PreviousBuildsDatabase.Entry() {File = Name, InputsSummary = InputsSummary, TimeStamp = File.TimeStamp,},
                ResultState = executeResult.ExitCode == 0 ? State.Built : State.Failed,
                Output = executeResult.StdOut + executeResult.StdErr,
                Input = args.Arguments,
                Node = this
            };
        }

        private static NPath LinkExe => MsvcInstallation.GetInstallation(new Version(10, 0)).GetVSToolPath(new x64Architecture(), "link.exe");

        private string LinkerArguments
        {
            get
            {
                var libPaths = new[]
                {
                    "/LIBPATH:C:/unity2", "/LIBPATH:C:/Program Files (x86)/Microsoft Visual Studio 10.0/vc/atlmfc/lib/amd64", "/LIBPATH:C:/Program Files (x86)/Microsoft Visual Studio 10.0/vc/lib/amd64",
                    @"/LIBPATH:C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\/Lib/x64"
                };
                var arguments = new StringBuilder(libPaths.InQuotes().SeperateWithSpace() + " ");
                foreach (var o in _objectNodes)
                    arguments.AppendLine(o.File.InQuotes());
                foreach (var l in _staticLibs.InQuotes())
                    arguments.AppendLine(l);
                foreach (var linkflag in _linkFlags)
                    arguments.AppendLine(linkflag);
                arguments.AppendLine(" /OUT:" + File.InQuotes());
                return arguments.ToString();
            }
        }

        public override bool SupportsNetworkCache => true;

        protected override InputsSumary CalculateInputsSummary()
        {
            return new InputsSumary() {TargetFileName = File.FileName, CommandLine=LinkerArguments, Dependencies = _objectNodes.Select(o => new FileSummary() { FileName = o.File.FileName, Hash = FileHashProvider.Instance.HashFor(o.File), TimeStamp = o.File.TimeStamp}).ToArray()};
        }

        public override string NodeTypeIdentifier => "Exe";
      
    }

    
}