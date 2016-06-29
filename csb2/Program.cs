using System.Collections.Generic;
using NiceIO;

namespace csb2
{
    class Program
    {
        static void Main(string[] args)
        {

            var projects = new NPath("c:/test/projects");

            var cppPrograms = new List<CppProgram>();
            foreach (var dir in projects.Directories())
            {
                var cppProgram = new CppProgram(dir.FileName, new NPath($"c:/test/out/{dir.FileName}.exe"), dir);
                cppPrograms.Add(cppProgram);
            }
            

            var db = new PreviousBuildsDatabase(new NPath("c:/test/database"));

            var aliasNode = new AliasNode("all", cppPrograms.ToArray());
            var builder = new Builder(db);
            builder.Build(aliasNode);
            db.Save();
        }
    }
}


