using NiceIO;

namespace csb2
{
  class Program
  {
    static void Main(string[] args)
    {
        var graph = new NodeGraph();
        var fileNode = new FileNode(new NPath("c:/test/test.cpp"));
        var objectNode = new ObjectNode(fileNode, "c:/test/test.obj");
        var exeNode = new ExeNode("c:/test/program.exe", new[] {objectNode});

        graph.AddNode(fileNode);
        graph.AddNode(objectNode);
        graph.AddNode(exeNode);

        var db = new PreviousBuildsDatabase();
        var builder = new Builder(db);
        builder.Build(exeNode);
    }
  }
}


