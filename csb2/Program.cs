using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using csb2.Caching;
using Mono.Options;
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
                
                bool runCacheServer = false;
                CacheMode cacheMode = CacheMode.None;
                var cacheDirectory = new NPath("c:/test2/cache");

                // thses are the available options, not that they set the variables
                var options = new OptionSet
                {
                    {"runCacheServer", "Run a cache server", v => runCacheServer = v != null},
                    {"cacheDirectory", "Directory for the cacheserver to store its cache", s => cacheDirectory = new NPath(s) },
                    {
                        "cacheMode", "Sets cachemode. valid options: r,w,rw,n", (v) =>
                        {
                            switch (v)
                            {
                                case "n":
                                    cacheMode = CacheMode.None;
                                    break;
                                case "r":
                                    cacheMode = CacheMode.Read;
                                    break;
                                case "w":
                                    cacheMode = CacheMode.Write;
                                    break;
                                case "rw":
                                    cacheMode = CacheMode.Read | CacheMode.Write;
                                    break;
                                default:
                                    throw new ArgumentException("Only valid values for cacheMode are r/w/rw/n");
                            }
                        }
                    }
                };

                options.Parse(args);

                CachingClient.CacheMode = cacheMode;

                if (runCacheServer)
                {
                        using (TinyProfiler.Section("Start CacheServer"))
                            new CachingServer().Start(cacheDirectory);

                        while(true)
                            System.Threading.Thread.Sleep(TimeSpan.FromDays(100));
                }

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


