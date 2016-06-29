using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NiceIO;

namespace csb2
{
    class Builder
    {
        private readonly PreviousBuildsDatabase _previousBuildsDatabase;
        private readonly Queue<Node> m_Jobs = new Queue<Node>();

        private int _totalEstimatedCost = 1;
        private int _remainingEstimatedCost = 0;

        public Builder(PreviousBuildsDatabase previousBuildsDatabase)
        {
            _previousBuildsDatabase = previousBuildsDatabase;
        }

        public void Build(Node nodeToBuild)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var workedThreads = new List<Thread>();
            for (int i=0; i!=15;i++)
            {
                var thread = new Thread(WorkerThread);
                thread.Start(i);
                thread.Name = "WorkedThread" + i;
                workedThreads.Add(thread);
            }

            while (true)
            {
                int costRemaining;
                if (DoPass(nodeToBuild, out costRemaining))
                {
                    _workerThreadsShouldExit = true;
                    
                    Console.WriteLine($"CSB: Build Finished");
                    var elapsed = stopWatch.Elapsed;
                    Console.WriteLine($"Time: {elapsed.Minutes}m{elapsed.Seconds}.{elapsed.Milliseconds}s");

                    foreach (var thread in workedThreads)
                        thread.Join();
                    return;
                }
                _remainingEstimatedCost = costRemaining;
                _mainThreadEvent.WaitOne(1000);
            }
        }

        private void PumpJobs()
        {
            if (!m_Jobs.Any())
                return;
            var job = m_Jobs.Dequeue();
            
        }

        /*
        BOOL WINAPI WriteFile(
  _In_        HANDLE       hFile,
  _In_        LPCVOID      lpBuffer,
  _In_        DWORD        nNumberOfBytesToWrite,
  _Out_opt_   LPDWORD      lpNumberOfBytesWritten,
  _Inout_opt_ LPOVERLAPPED lpOverlapped
);

            
HANDLE WINAPI GetStdHandle(
  _In_ DWORD nStdHandle
);
*/
        [DllImport("Kernel32.dll")]
        static extern bool WriteFile(IntPtr handle, byte[] buffer, int bytestowrite, out uint byteswritten, IntPtr overlapped);

        [DllImport("Kernel32.dll")]
        static extern IntPtr GetStdHandle(int handle);
        
        readonly AutoResetEvent _mainThreadEvent = new AutoResetEvent(false);
        readonly AutoResetEvent _workedThreadsEvent = new AutoResetEvent(false);
        private bool _workerThreadsShouldExit = false;
        private readonly Mutex _jobsMutex = new Mutex();
        private readonly object _consoleLock = new object();
        void WorkerThread(object indexObject)
        {
            int workedThreadIndex = (int) indexObject;
            while (true)
            {
                _workedThreadsEvent.WaitOne(200);
                if (_workerThreadsShouldExit)
                    return;
                
                Node job = null;
                _jobsMutex.WaitOne();
                if (m_Jobs.Count > 0)
                    job = m_Jobs.Dequeue();
                _jobsMutex.ReleaseMutex();
                if (job == null)
                    continue;
                BuildNode(job);

              //  Console.WriteLine("OutType: "+Console.Out.GetType().FullName);
              

                lock (_consoleLock)
                {
                    Console.Write("\r                                                           \r");
                 
                    var nodeIdentifier = $" [{job.NodeTypeIdentifier}]".PadRight(9);
                    var workedThreadIdentifier = $"{workedThreadIndex.ToString().PadLeft(2)}>";
                    Console.Write(workedThreadIdentifier);

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

                _mainThreadEvent.Set();
            }    
        }

        private void BuildNode(Node job)
        {
            if (!job.Build())
            {
                job.State = State.Failed;
                throw new BuildFailedException("Failed building " + job);
            }
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
                if (!RecurseIntoDependencies(nodeToBuild.StaticDependencies, out costOfRemainingDependencies))
                {
                    remaininCost += costOfRemainingDependencies;
                    return false;
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
                var updateReason = nodeToBuild.DetermineNeedToBuild(_previousBuildsDatabase);
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
                var upToDate = DoPass(dep, out cost);
                costOfRemainingDependencies += cost;
                if (!upToDate)
                    allUpToDate = false;
            }
            return allUpToDate;
        }

        private void QueueJob(Node nodeToBuild)
        {
            _jobsMutex.WaitOne();
            m_Jobs.Enqueue(nodeToBuild);
            _jobsMutex.ReleaseMutex();
            _workedThreadsEvent.Set();
        }
    }
}
 