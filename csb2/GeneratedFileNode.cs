using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    public abstract class GeneratedFileNode : FileNode
    {
        private InputsSumary _inputsSummary;

        protected GeneratedFileNode(NPath file) : base(file)
        {
        }


        public InputsSumary InputsSummary
        {
            get
            {
                if (_inputsSummary != null)
                    return _inputsSummary;

                _inputsSummary = CalculateInputsSummary();
                return _inputsSummary;
            }
        }

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

            string difference;
            if (e.InputsSummary.Matches(InputsSummary, out difference))
                return null;
                
           // Console.WriteLine("difference: "+difference);
            return new UpdateReason(difference);
        }

        public sealed override JobResult Build()
        {
            File.Parent.EnsureDirectoryExists();

            var jobResult = BuildGeneratedFile();

            if (SupportsNetworkCache && jobResult.ResultState == State.Built && CachingClient.Enabled)
                CachingClient.Store(InputsSummary.Hash, File, jobResult.Output);

            return jobResult;
        }
        
        protected virtual PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            throw new InvalidOperationException();
        }

        protected abstract JobResult BuildGeneratedFile();

        protected abstract InputsSumary CalculateInputsSummary();
    }

}