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

            Console.WriteLine("Building: "+job+ " Reason: "+job.UpdateReason);
            if (!job.Build())
            {
                job.State = State.Failed;
                throw new BuildFailedException("Failed building " + job);
            }
            job.State = State.UpToDate;

            var generatedFileNode = job as GeneratedFileNode;
            if (generatedFileNode != null)
                _previousBuildsDatabase.SetInfoFor(new PreviousBuildsDatabase.Entry() {Name = job.Name, TimeStamp = generatedFileNode.TimeStamp});
        }

        private bool DoPass(Node nodeToBuild)
        {
            if (nodeToBuild.State == State.UpToDate)
                return true;

            if (nodeToBuild.State == State.Building)
                return false;

            if (nodeToBuild.State == State.NotProcessed)
            {
                bool allUpToDate = true;
                foreach (var dep in nodeToBuild.Dependencies)
                {
                    var upToDate = DoPass(dep);
                    if (!upToDate)
                        allUpToDate = false;
                }
                if (allUpToDate)
                    nodeToBuild.State = State.DependenciesReady;
                else
                    return false;
            }

            if (nodeToBuild.State == State.DependenciesReady)
            {
                var updateReason = nodeToBuild.DetermineNeedToBuild(_previousBuildsDatabase);
                if (updateReason == null)
                {
                    Console.WriteLine("Already UpToDate: "+nodeToBuild);
                    nodeToBuild.State = State.UpToDate;
                    return true;
                }
                nodeToBuild.SetUpdateReason(updateReason);
                nodeToBuild.State = State.Building;
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