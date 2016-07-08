using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using csb2.Caching;
using NiceIO;
using ServiceStack;
using Unity.TinyProfiling;

namespace csb2
{
    class Builder
    {
        private readonly PreviousBuildsDatabase _previousBuildsDatabase;
        private readonly ConcurrentQueue<Node> m_Jobs = new ConcurrentQueue<Node>();
        
        public FileHashProvider FileHashProvider { get; } 

        private int _totalEstimatedCost = 1;
        private int _remainingEstimatedCost = 0;


        public readonly AutoResetEvent _mainThreadEvent = new AutoResetEvent(false);
        readonly AutoResetEvent _workedThreadsEvent = new AutoResetEvent(false);


        public bool _workerThreadsShouldExit = false;


        public bool _quit = false;
        private CachingClient _cachingClient;

        public ConcurrentQueue<JobResult> _completedJobs = new ConcurrentQueue<JobResult>();
     

        public Builder(PreviousBuildsDatabase previousBuildsDatabase, FileHashProvider _fileHashProvider)
        {
            FileHashProvider = _fileHashProvider;
            _previousBuildsDatabase = previousBuildsDatabase;
        }

        public void Build(Node nodeToBuild)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.CancelKeyPress += new ConsoleCancelEventHandler(MyControlCHandler);

            var workerThreads = new List<Thread>();
            for (int i=0; i!=13;i++)
            {
                var thread = new Thread(WorkerThread);
                thread.Start(i);
                thread.Name = "WorkerThread" + i;
                workerThreads.Add(thread);
            }
           
            _cachingClient = new CachingClient(this);
            
            while (true)
            {
                int costRemaining;
                ProgressState state;
                int i = 0;
                using (TinyProfiler.Section("DoPass"+i))
                    state = DoPass(nodeToBuild, out costRemaining);
                if ((state == ProgressState.Failed || state == ProgressState.Ready) && !m_Jobs.Any())
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

                using (TinyProfiler.Section("ProcessCompleteJobs"))
                    if (ProcessCompletedJobs())
                        continue;

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

        private bool ProcessCompletedJobs()
        {
            bool processedAny = false;
            while (!_quit)
            {
                JobResult jobResult;

                using (TinyProfiler.Section("TryDequeue"))
                {
                    if (!_completedJobs.TryDequeue(out jobResult))
                        return processedAny;
                }

                processedAny = true;

                if (jobResult.ResultState != State.UpToDate)
                    using (TinyProfiler.Section("LogJobResult"))
                        LogJobResult(jobResult);
                jobResult.Node.State = jobResult.ResultState;

                if (jobResult.ResultState == State.Built)
                    using (TinyProfiler.Section("SetInfoFor"))
                        _previousBuildsDatabase.SetInfoFor(jobResult.BuildInfo);
            }
            return processedAny;
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

                    if (!m_Jobs.TryDequeue(out job))
                        break;

                    UpdateReason updateReason;
                    var nodeToBuild = job;


                    switch (nodeToBuild.State)
                    {
                        case State.Processing:
                            using (TinyProfiler.Section("DetermineNeedToBuild", nodeToBuild.Name))
                                updateReason = nodeToBuild.DetermineNeedToBuild(_previousBuildsDatabase);
                            if (updateReason == null)
                            {
                                var jobResult = new JobResult() {ResultState = State.UpToDate, Node = nodeToBuild};
                                CompletedJob(jobResult);
                            }
                            else
                            {
                                nodeToBuild.SetUpdateReason(updateReason);
                                var generatedFileNode = nodeToBuild as GeneratedFileNode;
                                if (generatedFileNode != null && generatedFileNode.SupportsNetworkCache && CachingClient.CacheMode.HasFlag(CacheMode.Read))
                                {
                                    //calculate summary on workerthread;
                                    var summary = generatedFileNode.InputsSummary;
                                    _cachingClient.Queue(generatedFileNode);
                                }
                                else
                                {
                                    if (generatedFileNode.CanDistribute && RemoteJobsCounter.Value < 5)
                                    {
                                        DistributeNode(generatedFileNode);
                                    }
                                    else
                                        BuildNode(nodeToBuild, job, workedThreadIndex);
                                }
                            }
                            break;
                        case State.CacheLoadFailed:
                            BuildNode(nodeToBuild, job, workedThreadIndex);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }


                }
            }    
        }

        class RemoteJobsCounter : IDisposable
        {
            private static int counter;

            public static int Value => counter;
           
            public RemoteJobsCounter()
            {
                Interlocked.Increment(ref counter);
            }

            public void Dispose()
            {
                Interlocked.Decrement(ref counter);
            }
        }

        private void DistributeNode(GeneratedFileNode generatedFileNode)
        {
            CompilationRequest remoteRequest = generatedFileNode.MakeDistributionRequest();

            if (remoteRequest == null)
            {
                QueueJob(generatedFileNode);
                return;
            }
            var jsonClient = new JsonServiceClient(Program.UseRemoteCompilationService);
            JobResult jobResult;

            using (TinyProfiler.Section("Distribute",generatedFileNode.File.FileName))
            using (new RemoteJobsCounter())
            { 
                try
                {
                    var response = jsonClient.Post(remoteRequest);
                    jobResult = generatedFileNode.ProcessDistributionResponse(response);

                }
                catch (Exception e)
                {
                    if (!(e is WebServiceException))
                        Console.WriteLine("Caught application while processing distribution response: " + e.ToString());
                    generatedFileNode.CanDistribute = false;
                    QueueJob(generatedFileNode);
                    return;
                }
            }
            CompletedJob(jobResult);
        }

        public void CompletedJob(JobResult jobResult)
        {
            _completedJobs.Enqueue(jobResult);
            _mainThreadEvent.Set();
        }

        private void BuildNode(Node nodeToBuild, Node job, int workedThreadIndex)
        {
            nodeToBuild.State = State.Building;
            using (TinyProfiler.Section("Build", job.Name))
            {
                var jobResult = job.Build();
                jobResult.Source = "Local" + workedThreadIndex;
                CompletedJob(jobResult);
            }
        }

        public void LogJobResult(JobResult jobResult)
        {
            //Console.Write("\r                                                           \r");

            var nodeIdentifier = $" [{jobResult.Node.NodeTypeIdentifier}]".PadRight(9);
            var paddedWorkerIdentifier = $"{jobResult.Source.PadRight(7)}>";
            Console.Write(paddedWorkerIdentifier);

            Console.ForegroundColor = jobResult.ResultState == State.Built ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(nodeIdentifier);
            Console.ResetColor();
            Console.WriteLine(jobResult.Node.Name);
            if (jobResult.ResultState == State.Failed)
                Console.WriteLine(jobResult.Input);
            if (!string.IsNullOrEmpty(jobResult.Output))
                Console.WriteLine(jobResult.Output.Length > 4000 ? jobResult.Output.Substring(0, 2000) + "\n\n<<SNIPPED>>\n\n" + jobResult.Output.Substring(jobResult.Output.Length-2000) : jobResult.Output);

            /*
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

            Console.Write($"\r[{bar}] {percentage}%");*/
        }

        private ProgressState DoPass(Node nodeToBuild, out int remaininCost)
        {
            remaininCost = 0;

            if (nodeToBuild.NeverBuilds)
                return ProgressState.Ready;
            
            switch (nodeToBuild.State)
            {
                case State.UpToDate:
                case State.Built:
                    return ProgressState.Ready;

                case State.Building:
                case State.Processing:
                case State.CacheLoadFailed:
                    return ProgressState.StillWorking;
                case State.Failed:
                    return ProgressState.Failed;

                case State.NotProcessed:
                    int costOfRemainingDependencies = 0;
                    //using (TinyProfiler.Section("RecurseIntoStaticDeps: " + nodeToBuild.Name))

                    var dependenciesState = RecurseIntoDependencies(nodeToBuild.StaticDependencies, out costOfRemainingDependencies);
                    switch (dependenciesState)
                    {
                        case ProgressState.StillWorking:
                            remaininCost += costOfRemainingDependencies;
                            return dependenciesState;
                        case ProgressState.Failed:
                            return ProgressState.Failed;
                    }
                    nodeToBuild.State = State.StaticDependenciesReady;
                    break;
            }

            if (nodeToBuild.State == State.StaticDependenciesReady)
            {
                int costOfRemainingDependencies = 0;
                var dependenciesState = RecurseIntoDependencies(nodeToBuild.DynamicDependencies, out costOfRemainingDependencies);
                switch (dependenciesState)
                {
                    case ProgressState.StillWorking:
                        remaininCost += costOfRemainingDependencies;
                        return dependenciesState;
                    case ProgressState.Failed:
                        return ProgressState.Failed;
                }
                nodeToBuild.State = State.AllDependenciesReady;
            }
            
            if (nodeToBuild.State == State.AllDependenciesReady)
            {
                nodeToBuild.State = State.Processing;
                QueueJob(nodeToBuild);
                return ProgressState.StillWorking;
            }


            throw new NotSupportedException();
        }

        enum ProgressState
        {
            StillWorking,
            Ready,
            Failed
        }

        private ProgressState RecurseIntoDependencies(IEnumerable<Node> dependencies, out int costOfRemainingDependencies)
        {
            List<ProgressState> allState = new List<ProgressState>();
            costOfRemainingDependencies = 0;
            
            foreach (var dep in dependencies)
            {
                int cost;
                State state;
                //using (TinyProfiler.Section("DoPass "+dep.Name))

                allState.Add(DoPass(dep, out cost));
            }

            if (allState.Contains(ProgressState.Failed))
                return ProgressState.Failed;
            if (allState.All(s => s == ProgressState.Ready))
                return ProgressState.Ready;

            return ProgressState.StillWorking;
        }

        public void QueueJob(Node nodeToBuild)
        {
            m_Jobs.Enqueue(nodeToBuild);
            _workedThreadsEvent.Set();
        }
    }

    public class JobResult
    {
        public Node Node { get; set; }
        public PreviousBuildsDatabase.Entry BuildInfo { get; set; }
        public string Source { get; set; }
        public string Output { get; set; }
        public string Input { get; set; }
        public State ResultState { get; set; }
    }
}
 