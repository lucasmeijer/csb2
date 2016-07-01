using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using csb2.Caching;
using NiceIO;
using ServiceStack;
using Unity.TinyProfiling;

namespace csb2
{
    class CachingClient
    {
        private readonly Builder _builder;
        readonly AutoResetEvent _cacheThreadEvent = new AutoResetEvent(false);
        private readonly object _cacheJobLock = new object();
        private readonly Queue<GeneratedFileNode> m_CacheJobs = new Queue<GeneratedFileNode>();
        private int _errors;

        public CachingClient(Builder builder)
        {
            _builder = builder;

            var cacheThread = new Thread(CacheThread) { Name = "CacheThread" };
            cacheThread.Start();
        }

        public bool Enabled { get; set; } = true;


        public void Queue(GeneratedFileNode node)
        {
            lock (_cacheJobLock)
                m_CacheJobs.Enqueue(node);
            _cacheThreadEvent.Set();
        }

        void CacheThread()
        {
            try
            {
                CacheThreadLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine("CacheThreadException: " + e);
            }
        }

        private void CacheThreadLoop()
        {
            while (true)
            {
                _cacheThreadEvent.WaitOne(500);
                if (_builder._workerThreadsShouldExit)
                    return;

                while (true)
                {
                    GeneratedFileNode job = null;
                    lock (_cacheJobLock)
                    {
                        if (m_CacheJobs.Count == 0)
                            break;
                        job = m_CacheJobs.Dequeue();
                    }
                    Task.Run(() => HandleCacheJob(job));
                }
            }
        }

        private  void HandleCacheJob(GeneratedFileNode job)
        {
            using (TinyProfiler.Section("CacheClient " + job.Name))
            {
                var cacheKey = job.NetworkCacheKey;
                var client = new JsonServiceClient(CachingServer.Url);

                CacheResponse result = new CacheResponse() {Files = new List<FilePayLoad>()};
                try
                {
                    using (TinyProfiler.Section("Get " + job.Name))
                        result = client.Get(new CacheRequest() {Key = cacheKey, Name = job.Name});
                }
                catch (WebException)
                {
                    if (Enabled)
                    {
                        _errors++;
                        if (_errors > 2)
                            ShutDown();
                    }
                }

                if (result.Files.Count == 0)
                {
                    //result did not exist in the cacheserver
                    _builder.QueueJobNoCaching(job);
                    return;
                }

                var filePayLoad = result.Files.Single();
                if (filePayLoad.Name != job.File.FileName)
                    throw new InvalidOperationException();

                job.File.WriteAllBytes(filePayLoad.Content);

                job.State = State.UpToDate;

                _builder.LogBuild(job, "Cache");

                _builder._mainThreadEvent.Set();
            }
        }

        private void ShutDown()
        {
            Enabled = false;
            GeneratedFileNode[] jobs;
            lock (_cacheJobLock)
            {
                jobs = m_CacheJobs.ToArray();
                m_CacheJobs.Clear();
            }
            
            foreach (var job in jobs)
                _builder.QueueJobNoCaching(job);
        }

        public static void Store(string networkCacheKey, NPath file)
        {
            var bytes = file.ReadAllBytes();
            Task.Run(() =>
            {
                var cacheStore = new CacheStore() {Key = networkCacheKey, Files = new List<FilePayLoad>() {new FilePayLoad() {Name = file.FileName, Content = bytes}}};
                new JsonServiceClient(CachingServer.Url).Post(cacheStore);
            });
        }
    }
}