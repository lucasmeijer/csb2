using System;
using System.Collections.Generic;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    public abstract class GeneratedFileNode : FileNode
    {
        protected GeneratedFileNode(NPath file) : base(file)
        {
        }

        public void Process()
        {
            CalculateInputHash();

            //Check option against PreviousDatabase
            //if match -> state = UpToDate, return.

            //if caching-read-enabled
            //queue with CacheClientThread & return

            //if distribution enabled && availablity
            //  queue with distribution thread & return
            
            //build right here
        }

        private void CalculateInputHash()
        {
            throw new NotImplementedException();
        }


        public virtual bool SupportsNetworkCache => false;
        public abstract string InputsHash { get; }

        public override UpdateReason DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            NPath file = File;

            if (!file.FileExists())
                return new UpdateReason("No previously built file exists");

            PreviousBuildsDatabase.Entry e = null;
            db.TryGetInfoFor(Name, out e);
            if (e == null)
                return new UpdateReason("No entry of a previous possibly recyclable build in the database");

            if (file.TimeStamp != e.TimeStamp)
                return new UpdateReason($"Previous build that we made had a timestamp of {e.TimeStamp}, but the generated file on disk has a timestamp of {file.TimeStamp}");
  
            if (e.InputsHash != InputsHash)
                return new UpdateReason("CacheKey of potentially recyclable object is different from current one");
  
            return null;
        }

        public sealed override JobResult Build()
        {
            File.Parent.EnsureDirectoryExists();

            var jobResult = BuildGeneratedFile();
            if (!jobResult.Success)
                return jobResult;

            PreviousBuildsDatabase.Instance.SetInfoFor(jobResult.BuildInfo);
            
//            if (SupportsNetworkCache)
  //              CachingClient.Store(InputsHash, File, jobResult.Output);

            return jobResult;
        }
        
        protected virtual PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            throw new InvalidOperationException();
        }

        protected abstract JobResult BuildGeneratedFile();

    }
}