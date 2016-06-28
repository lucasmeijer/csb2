using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;

namespace csb2
{
    class Builder
    {
        private readonly PreviousBuildsDatabase _previousBuildsDatabase;
        private readonly Queue<Node> m_Jobs = new Queue<Node>();

        public Builder(PreviousBuildsDatabase previousBuildsDatabase)
        {
            _previousBuildsDatabase = previousBuildsDatabase;
        }

        public void Build(Node nodeToBuild)
        {
            while (true)
            {
                if (DoPass(nodeToBuild))
                    return;

                PumpJobs();
            }
        }

        private void PumpJobs()
        {
            if (!m_Jobs.Any())
                return;
            var job = m_Jobs.Dequeue();

            Console.WriteLine("Building: "+job);
            if (!job.Build())
            {
                job.SetState(State.Failed);
                throw new BuildFailedException("Failed building " + job);
            }
            job.SetState(State.UpToDate);

            _previousBuildsDatabase.SetInfoFor(job.Name, new PreviousBuildsDatabase.Entry() {TimeStamp = new NPath(job.Name).TimeStamp});
        }

        private bool DoPass(Node nodeToBuild)
        {
            if (nodeToBuild.GetState() == State.UpToDate)
                return true;

            if (nodeToBuild.GetState() == State.NotProcessed)
            {
                bool allUpToDate = true;
                foreach (var dep in nodeToBuild.Dependencies)
                {
                    var upToDate = DoPass(dep);
                    if (!upToDate)
                        allUpToDate = false;
                }
                if (allUpToDate)
                    nodeToBuild.SetState(State.DependenciesReady);
                else
                    return false;
            }

            if (nodeToBuild.GetState() == State.DependenciesReady)
            {
                var needToBuild = nodeToBuild.DetermineNeedToBuild(_previousBuildsDatabase);
                if (!needToBuild)
                {
                    nodeToBuild.SetState(State.UpToDate);
                    return true;
                }
                nodeToBuild.SetState(State.Building);
                QueueJob(nodeToBuild);
                return false;
            }

            throw new NotSupportedException();
        }

        private void QueueJob(Node nodeToBuild)
        {
            m_Jobs.Enqueue(nodeToBuild);
        }
    }
}