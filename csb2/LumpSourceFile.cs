using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;

namespace csb2
{
    class LumpSourceFile : GeneratedFileNode
    {
        public IEnumerable<FileNode> CppFiles { get; }

        public LumpSourceFile(IEnumerable<FileNode> cppFiles, NPath file) : base(file)
        {
            CppFiles = cppFiles;
        }

        public override string NodeTypeIdentifier=> "LMP";

        protected override JobResult BuildGeneratedFile()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// generated lump file by buildsystem");
            foreach (var cppFile in CppFiles)
            {
                var relativeTo = cppFile.File.RelativeTo(new NPath("c:/unity2"));
                sb.AppendLine($"#include \"{relativeTo}\"");
            }
            File.WriteAllText(sb.ToString());

            return new JobResult()
            {
                BuildInfo = new PreviousBuildsDatabase.Entry() {File = File.ToString(), InputsSummary = InputsSummary, TimeStamp = File.TimeStamp},
                Input = "",
                Node = this,
                Output = "",
                ResultState = State.Built,
                Source = "local"
            };
        }
        
        protected override InputsSumary CalculateInputsSummary()
        {
            return new InputsSumary() {CommandLine = CppFiles.Select(c => c.File).ConcatAll(), Dependencies = new FileSummary[0]};
        }
    }
}
