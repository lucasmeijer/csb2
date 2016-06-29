using System.Linq;
using NiceIO;

namespace csb2
{
    class CppProgram : Node
    {
        public CppProgram(string name, NPath file, NPath directory) : base(name)
        {
            var objectNodes =
                directory.Files("*.cpp").Select(sourceFile => new ObjectNode(new SourceFileNode(sourceFile), Artifacts.Combine(sourceFile.RelativeTo(directory).ChangeExtension(".obj")))).ToArray();

            var exeNode = new ExeNode(file, objectNodes);
            SetStaticDependencies(exeNode);
        }

        public NPath Artifacts => new NPath("c:/test/artifacts/" + Name);
        public override string NodeTypeIdentifier => "CppProgram";
    }
}