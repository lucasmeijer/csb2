using System;
using System.Collections.Concurrent;
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

        public bool _quit = false;
        private CachingClient _cachingClient;

        public ConcurrentQueue<JobResult> _completedJobs = new ConcurrentQueue<JobResult>();

        public Builder(PreviousBuildsDatabase previousBuildsDatabase)
        {
            _previousBuildsDatabase = previousBuildsDatabase;
        }

        public void Build(Node nodeToBuild)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.CancelKeyPress += new ConsoleCancelEventHandler(MyControlCHandler);

            var workerThreads = new List<Thread>();
            for (int i=0; i!=10;i++)
            {
                var thread = new Thread(WorkerThread);
                thread.Start(i);
                thread.Name = "WorkerThread" + i;
                workerThreads.Add(thread);
            }
           
            _cachingClient = new CachingClient(this);
            _cachingClient.Enabled = false;

            while (true)
            {
                int costRemaining;
                State state;
                int i = 0;
                using (TinyProfiler.Section("DoPass"+i))
                    state = DoPass(nodeToBuild, out costRemaining);
                if (state != State.Building)
                {
                    i++;
                    _workerThreadsShouldExit = true;

                    Console.WriteLine();
                    Console.WriteLine($"CSB: Build Finished:  "+state);
                    var elapsed = stopWatch.Elapsed;
                    Console.WriteLine($"Time: {elapsed.Minutes}m{elapsed.Seconds}.{elapsed.Milliseconds}s");
                    
                    using (TinyProfiler.Section("Waiting For WorkerThread Shutdown"))
                    {
                        foreach (var thread in workerThreads)
                            _workedThreadsEvent.Set();
                        foreach (var thread in workerThreads)
                             thread.Join();
                    }
                    return;
                }

                ProcessCompletedJobs();

                _remainingEstimatedCost = costRemaining;
                using (TinyProfiler.Section("Waiting for completion of a job"))
                    _mainThreadEvent.WaitOne(1000);
            }
        }

        private void MyControlCHandler(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Caught ControlC, gracefully exiting");
            _quit = true;
            _mainThreadEvent.Set();
            e.Cancel = true;
        }

        private void ProcessCompletedJobs()
        {
            while (!_quit)
            {
                JobResult jobResult;
                if (!_completedJobs.TryDequeue(out jobResult))
                    return;

                LogJobResult(jobResult);
                jobResult.Node.State = jobResult.Success ? State.UpToDate : State.Failed;

                if (!jobResult.Success)
                {
                    //_quit = true;
                    return;
                }
                _previousBuildsDatabase.SetInfoFor(jobResult.BuildInfo);
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

        public void LogJobResult(JobResult jobResult)
        {
            Console.Write("\r                                                           \r");

            var nodeIdentifier = $" [{jobResult.Node.NodeTypeIdentifier}]".PadRight(9);
            var paddedWorkerIdentifier = $"{jobResult.Source.PadRight(7)}>";
            Console.Write(paddedWorkerIdentifier);

            Console.ForegroundColor = jobResult.Success ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(nodeIdentifier);
            Console.ResetColor();
            Console.WriteLine(jobResult.Node.Name);
            if (jobResult.Success == false)
                Console.WriteLine(jobResult.Input);
            if (!string.IsNullOrEmpty(jobResult.Output))
                Console.WriteLine(jobResult.Output.Length > 4000 ? jobResult.Output.Substring(0, 2000) + "\n\n<<SNIPPED>>\n\n" + jobResult.Output.Substring(jobResult.Output.Length-2000) : jobResult.Output);


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
        }

        private void BuildNode(Node job, string source)
        {
            var jobResult = job.Build();
            jobResult.Source = source;
            _completedJobs.Enqueue(jobResult);

        }

        private State DoPass(Node nodeToBuild, out int remaininCost)
        {
            remaininCost = 0;

            if (nodeToBuild.State == State.UpToDate)
                return State.UpToDate;

            remaininCost += nodeToBuild.EstimatedCost;

            if (nodeToBuild.State == State.Building)
                return State.Building;

            if (nodeToBuild.State == State.Failed)
                return State.Failed;

            if (nodeToBuild.State == State.NotProcessed)
            {
                int costOfRemainingDependencies = 0;
                //using (TinyProfiler.Section("RecurseIntoStaticDeps: " + nodeToBuild.Name))
                {
                    var dependenciesState = RecurseIntoDependencies(nodeToBuild.StaticDependencies, out costOfRemainingDependencies);
                    if (dependenciesState == State.Building)
                    {
                        remaininCost += costOfRemainingDependencies;
                        return State.Building;
                    }
                    if (dependenciesState == State.Failed)
                        return State.Failed;

                }
                nodeToBuild.State = State.StaticDependenciesReady;
            }

            if (nodeToBuild.State == State.StaticDependenciesReady)
            {
                int costOfRemainingDependencies = 0;
                var dependenciesState = RecurseIntoDependencies(nodeToBuild.DynamicDependencies, out costOfRemainingDependencies);
                if (dependenciesState == State.Building)
                {
                    remaininCost += costOfRemainingDependencies;
                    return State.Building;
                }
                if (dependenciesState == State.Failed)
                    return State.Failed;
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
                    return State.UpToDate;
                }
                _totalEstimatedCost += nodeToBuild.EstimatedCost;
                nodeToBuild.SetUpdateReason(updateReason);
                nodeToBuild.State = State.Building;
                QueueJob(nodeToBuild);
                return State.Building;
            }


            throw new NotSupportedException();
        }

        private State RecurseIntoDependencies(IEnumerable<Node> dependencies, out int costOfRemainingDependencies)
        {
            State allState = State.UpToDate;
            costOfRemainingDependencies = 0;
            foreach (var dep in dependencies)
            {
                int cost;
                State state;
                //using (TinyProfiler.Section("DoPass "+dep.Name))
                     state = DoPass(dep, out cost);
                costOfRemainingDependencies += cost;

                if (state == State.Building)
                    allState = State.Building;
                if (state == State.Failed)
                    return State.Failed;
            }
            return allState;
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

    public class JobResult
    {
        public Node Node { get; set; }
        public bool Success { get; set; }
        public PreviousBuildsDatabase.Entry BuildInfo { get; set; }
        public string Source { get; set; }
        public string Output { get; set; }
        public string Input { get; set; }
    }
}
 