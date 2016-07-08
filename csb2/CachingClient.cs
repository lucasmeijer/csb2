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
    [Flags]
    enum CacheMode
    {
        None = 0,
        Read = 1,
        Write= 2
    }

    class CachingClient
    {
        private readonly Builder _builder;
        readonly AutoResetEvent _cacheThreadEvent = new AutoResetEvent(false);
        private readonly object _cacheJobLock = new object();
        private readonly Queue<GeneratedFileNode> m_CacheJobs = new Queue<GeneratedFileNode>();
        private int _errors;
        private List<Task> _tasks = new List<Task>();

        public CachingClient(Builder builder)
        {
            _builder = builder;

            var cacheThread = new Thread(CacheThread) { Name = "CacheThread" };
            cacheThread.Start();
        }

        public static CacheMode CacheMode { get; set; } = CacheMode.Read;


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
                    _tasks.Add(Task.Run(() => HandleCacheJob(job)));
                    _tasks.RemoveAll(t => t.IsCompleted && !t.IsFaulted);
                }
            }
        }

        private  void HandleCacheJob(GeneratedFileNode job)
        {
            using (TinyProfiler.Section("CacheClient " + job.Name))
            {
                var inputsSummary = job.InputsSummary;
                var client = new JsonServiceClient(Program.CachingServerToUse);
                client.Timeout = TimeSpan.FromSeconds(30);

                CacheResponse result = new CacheResponse() {Files = new List<FilePayLoad>()};
                try
                {
                    using (TinyProfiler.Section("Get " + job.Name))
                        result = client.Get(new CacheRequest() {Key = inputsSummary.Hash, Name = job.Name});
                }
                catch (WebException)
                {
                    if (CacheMode.HasFlag(CacheMode.Read))
                    {
                        _errors++;
                        if (_errors > 2)
                            ShutDown();
                    }
                }

                if (result.Files.Count == 0)
                {
                    job.State = State.CacheLoadFailed;
                    //result did not exist in the cacheserver
                    _builder.QueueJob(job);
                    return;
                }

                var filePayLoad = result.Files.Single();
                if (filePayLoad.Name != job.File.FileName)
                    throw new InvalidOperationException();

                job.File.WriteAllBytes(filePayLoad.Content);

                _builder.CompletedJob(new JobResult()
                {
                    BuildInfo = new PreviousBuildsDatabase.Entry() {File = job.File.ToString(), InputsSummary = inputsSummary, TimeStamp = job.File.TimeStamp},
                    Node = job,
                    Output = result.Output,
                    Source = "Cache",
                    ResultState = State.Built
                });
            }
        }

        private void ShutDown()
        {
            CacheMode = CacheMode.None;
            GeneratedFileNode[] jobs;
            lock (_cacheJobLock)
            {
                jobs = m_CacheJobs.ToArray();
                m_CacheJobs.Clear();
            }

            foreach (var job in jobs)
            {
                job.State=State.CacheLoadFailed;
                _builder.QueueJob(job);
            }
        }

        public static void Store(string networkCacheKey, NPath file, string output)
        {
            if (file.FileSize > 15*1024*1024)
                return;

            var bytes = file.ReadAllBytes();
            Task.Run(() =>
            {
                var cacheStore = new CacheStore() {Key = networkCacheKey, Name = file.FileName, Output = output, Files = new List<FilePayLoad>() {new FilePayLoad() {Name = file.FileName, Content = bytes}}};
                var jsonServiceClient = new JsonServiceClient(Program.CachingServerToUse) {Timeout = TimeSpan.FromSeconds(20)};
                jsonServiceClient.Post(cacheStore);
            });
        }
    }
}