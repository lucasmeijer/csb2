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
    [Route("/cache/{key}", "GET")]
    public class CacheRequest : IReturn<CacheResponse>
    {
        public string Name { get; set; }
        public string Key { get; set; }
    }

    [Route("/cache", "POST")]
    public class CacheStore : IReturn<CacheResponse>
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Output { get; set; }
        public List<FilePayLoad> Files { get; set; }
    }

    public class FilePayLoad
    {
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }

    internal class CacheResponse
    {
        public List<FilePayLoad> Files { get; set; } = new List<FilePayLoad>();
        public string Output { get; set; }
    }

    public class CachingService : Service
    {
        public static NPath CacheDirectory { get; set; }
            
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
            return CacheDirectory.Combine(key.Substring(0,3), key);
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
}
