using System.Collections.Generic;
using csb2.Caching;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (TinyProfiler.Section("Root"))
            {
                TinyProfiler.ConfigureOutput(new NPath("c:/test/profiler.html"), "csb");
                var projects = new NPath("c:/test/projects");

                new CachingServer().Start(new NPath("c:/test/cache"));

                var cppPrograms = new List<CppProgram>();
                foreach (var dir in projects.Directories())
                {
                    var cppProgram = new CppProgram(dir.FileName, new NPath($"c:/test/out/{dir.FileName}.exe"), dir);
                    cppPrograms.Add(cppProgram);
                }


                PreviousBuildsDatabase db;
                using (TinyProfiler.Section("Load Database"))
                    db = new PreviousBuildsDatabase(new NPath("c:/test/database"));

                var aliasNode = new AliasNode("all", cppPrograms.ToArray());
                var builder = new Builder(db);
                using (TinyProfiler.Section("Build"))
                    builder.Build(aliasNode);
                using (TinyProfiler.Section("Save Database"))
                    db.Save();
            }
        }
    }
}


