using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    class Builder
    {
        private readonly PreviousBuildsDatabase _previousBuildsDatabase;
        private readonly Queue<Node> m_Jobs = new Queue<Node>();
        
        public FileHashProvider FileHashProvider { get; } = new FileHashProvider(new NPath("c:/test/hashdatabase"));

        private int _totalEstimatedCost = 1;
        private int _remainingEstimatedCost = 0;


        public readonly AutoResetEvent _mainThreadEvent = new AutoResetEvent(false);
        readonly AutoResetEvent _workedThreadsEvent = new AutoResetEvent(false);


        public bool _workerThreadsShouldExit = false;
        private readonly object _jobsMutex = new object();

        private readonly object _consoleLock = new object();
        private CachingClient _cachingClient;


        public Builder(PreviousBuildsDatabase previousBuildsDatabase)
        {
            _previousBuildsDatabase = previousBuildsDatabase;
        }

        public void Build(Node nodeToBuild)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var workedThreads = new List<Thread>();
            for (int i=0; i!=5;i++)
            {
                var thread = new Thread(WorkerThread);
                thread.Start(i);
                thread.Name = "WorkedThread" + i;
                workedThreads.Add(thread);
            }
           
            _cachingClient = new CachingClient(this);

            while (true)
            {
                int costRemaining;
                bool doPass;
                int i = 0;
                using (TinyProfiler.Section("DoPass"+i))
                    doPass = DoPass(nodeToBuild, out costRemaining);
                if (doPass)
                {
                    i++;
                    _workerThreadsShouldExit = true;

                    Console.WriteLine();
                    Console.WriteLine($"CSB: Build Finished");
                    var elapsed = stopWatch.Elapsed;
                    Console.WriteLine($"Time: {elapsed.Minutes}m{elapsed.Seconds}.{elapsed.Milliseconds}s");

                    using (TinyProfiler.Section("Waiting For WorkerThread Shutdown"))
                        foreach (var thread in workedThreads)
                             thread.Join();
                    return;
                }
                _remainingEstimatedCost = costRemaining;
                using (TinyProfiler.Section("Waiting for completion of a job"))
                    _mainThreadEvent.WaitOne(1000);
            }
        }
        
        void WorkerThread(object indexObject)
        {
            int workedThreadIndex = (int) indexObject;
            while (true)
            {
                using (TinyProfiler.Section("Waiting for task"))
                    _workedThreadsEvent.WaitOne(200);

                if (_workerThreadsShouldExit)
                    return;

                while (true)
                {
                    Node job = null;
                    lock (_jobsMutex)
                    {
                        if (m_Jobs.Count == 0)
                            break;

                        job = m_Jobs.Dequeue();
                    }

                    using (TinyProfiler.Section("Build: " + job.Name))
                        BuildNode(job, "Local" + workedThreadIndex);
                    
                   

                    _mainThreadEvent.Set();
                }
            }    
        }

        public void LogBuild(Node job, string workerIdentifier)
        {
            lock (_consoleLock)
            {
                Console.Write("\r                                                           \r");

                var nodeIdentifier = $" [{job.NodeTypeIdentifier}]".PadRight(9);
                var paddedWorkerIdentifier = $"{workerIdentifier.PadRight(7)}>";
                Console.Write(paddedWorkerIdentifier);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(nodeIdentifier);
                Console.ResetColor();
                Console.WriteLine(job.Name);
                //flush
                var percentage = 100 - (int) (100.0f*_remainingEstimatedCost/_totalEstimatedCost);

                var bar = new StringBuilder();
                int maxBarLength = 10;
                for (int i = 0; i != maxBarLength; i++)
                {
                    if (100*((float) i/maxBarLength) < percentage)
                        bar.Append("*");
                    else
                        bar.Append("-");
                }

                Console.Write($"\r[{bar}] {percentage}%");

                //Console.Out.Flush();
            }
        }

        private void BuildNode(Node job, string source)
        {
            if (!job.Build())
            {
                job.State = State.Failed;
                throw new BuildFailedException("Failed building " + job);
            }
            LogBuild(job, source);
            job.State = State.UpToDate;
            
        }

        private bool DoPass(Node nodeToBuild, out int remaininCost)
        {
            remaininCost = 0;

            if (nodeToBuild.State == State.UpToDate)
                return true;

            remaininCost += nodeToBuild.EstimatedCost;

            if (nodeToBuild.State == State.Building)
                return false;

            if (nodeToBuild.State == State.NotProcessed)
            {
                int costOfRemainingDependencies = 0;
                //using (TinyProfiler.Section("RecurseIntoStaticDeps: " + nodeToBuild.Name))
                {
                    if (!RecurseIntoDependencies(nodeToBuild.StaticDependencies, out costOfRemainingDependencies))
                    {
                        remaininCost += costOfRemainingDependencies;
                        return false;
                    }
                }
                nodeToBuild.State = State.StaticDependenciesReady;
            }

            if (nodeToBuild.State == State.StaticDependenciesReady)
            {
                int costOfRemainingDependencies = 0;
                if (!RecurseIntoDependencies(nodeToBuild.DynamicDependencies, out costOfRemainingDependencies))
                {
                    remaininCost += costOfRemainingDependencies;
                    return false;
                }
                nodeToBuild.State = State.AllDependenciesReady;
            }
            
            if (nodeToBuild.State == State.AllDependenciesReady)
            {
                UpdateReason updateReason;
                using (TinyProfiler.Section("DetermineNeedToBuild: " + nodeToBuild.Name))
                    updateReason = nodeToBuild.DetermineNeedToBuild(_previousBuildsDatabase);
                if (updateReason == null)
                {
                    //Console.WriteLine("Already UpToDate: "+nodeToBuild);
                    nodeToBuild.State = State.UpToDate;
                    return true;
                }
                _totalEstimatedCost += nodeToBuild.EstimatedCost;
                nodeToBuild.SetUpdateReason(updateReason);
                nodeToBuild.State = State.Building;
                QueueJob(nodeToBuild);
                return false;
            }
            
            throw new NotSupportedException();
        }

        private bool RecurseIntoDependencies(IEnumerable<Node> dependencies, out int costOfRemainingDependencies)
        {
            bool allUpToDate = true;
            costOfRemainingDependencies = 0;
            foreach (var dep in dependencies)
            {
                int cost;
                bool upToDate;
                //using (TinyProfiler.Section("DoPass "+dep.Name))
                    upToDate = DoPass(dep, out cost);
                costOfRemainingDependencies += cost;
                if (!upToDate)
                    allUpToDate = false;
            }
            return allUpToDate;
        }

        private void QueueJob(Node nodeToBuild)
        {
            var generatedFileNode = nodeToBuild as GeneratedFileNode;
            if (_cachingClient.Enabled && generatedFileNode != null && generatedFileNode.SupportsNetworkCache)
            {
                _cachingClient.Queue(generatedFileNode);
            }
            else
            {
                QueueJobNoCaching(nodeToBuild);
            }
        }

        public void QueueJobNoCaching(Node nodeToBuild)
        {
            lock (_jobsMutex)
                m_Jobs.Enqueue(nodeToBuild);
            _workedThreadsEvent.Set();
        }
    }
}
 