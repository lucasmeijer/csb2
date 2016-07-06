using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;
using ServiceStack;
using Unity.TinyProfiling;

namespace csb2.Caching
{
    [Route("/cache/{key}")]
    class CacheRequest : IReturn<CacheResponse>
    {
        public string Name { get; set; }
        public string Key { get; set; }
    }

    [Route("/cachestore")]
    class CacheStore : IReturn<CacheResponse>
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Output { get; set; }
        public List<FilePayLoad> Files { get; set; }
    }

    class FilePayLoad
    {
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }

    internal class CacheResponse
    {
        public List<FilePayLoad> Files { get; set; } = new List<FilePayLoad>();
        public string Output { get; set; }
    }
    
    class CachingServer
    {
        //Define the Web Services AppHost
        public class AppHost : AppSelfHostBase
        {
            public AppHost()
              : base("HttpListener Self-Host", typeof(CachingServer).Assembly)
            { }

            public override void Configure(Funq.Container container)
            {
                
            }
        }


        public class HelloService : Service
        {
            public static NPath _cachePath;
            
            public object Any(CacheRequest request)
            {
                //using (TinyProfiler.Section("CacheServer " + request.Name))
                {
                    var result = new CacheResponse();
                    var cacheEntryDir = CacheEntryDirFor(request.Key);
                    if (!cacheEntryDir.DirectoryExists())
                        return result;

                    foreach (var file in cacheEntryDir.Files())
                        result.Files.Add(new FilePayLoad() {Name = file.FileName, Content = file.ReadAllBytes()});

                    return result;
                }
            }

            private static NPath CacheEntryDirFor(string key)
            {
                return _cachePath.Combine(key.Substring(0,3), key);
            }

            public object Any(CacheStore storeRequest)
            {
                //using (TinyProfiler.Section("CacheServerStore " + storeRequest.Name))
                {
                    var cacheEntryDir = CacheEntryDirFor(storeRequest.Key);
                    if (cacheEntryDir.Exists()) 
                    {
                        Console.WriteLine("Getting a store request that we already have. very fishy! "+storeRequest.Name);
                        return null;
                    }

                    cacheEntryDir.EnsureDirectoryExists();

                    foreach (var file in storeRequest.Files)
                        cacheEntryDir.Combine(file.Name).WriteAllBytes(file.Content);
                    return null;
                }
            }
        }


        public void Start(NPath nPath)
        {
            HelloService._cachePath = nPath.EnsureDirectoryExists();

            new AppHost()
                .Init()
                .Start(Url);

        }

        public static string Url => "http://localhost:1337/";
    }
    
}
