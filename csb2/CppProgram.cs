using System.Linq;
using NiceIO;

namespace csb2
{
    class CppProgram : Node
    {
        public CppProgram(string name, NPath file, NPath directory) : base(name)
        {
            var objectNodes =
                directory.Files("*.cpp").Select(sourceFile => new ObjectNode(new SourceFileNode(sourceFile), Artifacts.Combine(sourceFile.RelativeTo(directory).ChangeExtension(".obj")), new NPath[0], new string[0], new [] {"-O2"})).ToArray();

            var exeNode = new ExeNode(file, objectNodes, new NPath[0], new string[0]);
            SetStaticDependencies(exeNode);
        }

        public NPath Artifacts => new NPath("c:/test/artifacts/" + Name);
        public override string NodeTypeIdentifier => "CppProgram";
    }
}