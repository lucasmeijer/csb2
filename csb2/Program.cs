using NiceIO;

namespace csb2
{
    class Program
    {
        static void Main(string[] args)
        {
            var graph = new NodeGraph();

            /*
            var fileNode = new SourceFileNode(new NPath("c:/test/test.cpp"));
            var objectNode = new ObjectNode(fileNode, new NPath("c:/test/test.obj"));

            var fileNode2 = new SourceFileNode(new NPath("c:/test/test - Copy.cpp"));
            var objectNode2 = new ObjectNode(fileNode2, new NPath("c:/test/test - Copy.obj"));
            */

            var filesNode = new SourceFilesInDirectoryNode(new NPath("c:/test/"));
            var objectsNode = new ObjectsNode(filesNode);

            var exeNode = new ExeNode(new NPath("c:/test/program.exe"), objectsNode);

            var aliasNode = new AliasNode("all", new[] {exeNode});
            /*
           graph.AddNode(fileNode);
        graph.AddNode(objectNode);
        graph.AddNode(exeNode);
            graph.Add
            */

            var db = new PreviousBuildsDatabase(new NPath("c:/test/database"));
            var builder = new Builder(db);
            builder.Build(aliasNode);
            db.Save();
        }
    }
}


