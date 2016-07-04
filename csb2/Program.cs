using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using csb2.Caching;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    class Program
    {
        private static PreviousBuildsDatabase _previousBuildsDatabase;
        private static AliasNode _nodeToBuild;
        private static FileHashProvider _fileHashProvider;

        static void Main(string[] args)
        {
            using (TinyProfiler.Section("Root"))
            {
                TinyProfiler.ConfigureOutput(new NPath("c:/test/profiler.svg"), "csb");
                var projects = new NPath("c:/test/projects");

                /*
                using (TinyProfiler.Section("Start CacheServer"))
                    new CachingServer().Start(new NPath("c:/test/cache"));
                    */
                List<CppProgram> cppPrograms;
                
                var loadDB = Task.Run(() =>
                {
                    using (TinyProfiler.Section("Load Database"))
                        _previousBuildsDatabase = new PreviousBuildsDatabase(new NPath("c:/test/database"));
                });


                var setupDepgraph = Task.Run(() =>
                {
                    using (TinyProfiler.Section("Setup DepGraph"))
                    {

                        cppPrograms = new List<CppProgram>();
                        foreach (var dir in projects.Directories())
                        {
                            var cppProgram = new CppProgram(dir.FileName, new NPath($"c:/test/out/{dir.FileName}.exe"), dir);
                            cppPrograms.Add(cppProgram);
                        }

                        var unityEditor = new UnityEditor();

                        _nodeToBuild = new AliasNode("all", new[] {unityEditor});
                    }
                });

                var loadFileHashProvider = Task.Run(() =>
                {
                    _fileHashProvider = new FileHashProvider(new NPath("c:/test/hashdatabase")); 
                });

                Task.WaitAll(loadDB, setupDepgraph, loadFileHashProvider);

                var builder = new Builder(_previousBuildsDatabase, _fileHashProvider);
                using (TinyProfiler.Section("Build"))
                    builder.Build(_nodeToBuild);
                using (TinyProfiler.Section("Save Database"))
                {
                    Console.WriteLine("Saving Database");
                    _previousBuildsDatabase.Save();
                }
                using (TinyProfiler.Section("Save HashDatabase"))
                {
                    Console.WriteLine("Saving HashDatabase");
                    FileHashProvider.Instance.Save();
                }

            }
        }
    }
}


