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

        public sealed override bool Build()
        {
            File.Parent.EnsureDirectoryExists();

            var entry = BuildGeneratedFile();

            PreviousBuildsDatabase.Instance.SetInfoFor(entry);

            if (SupportsNetworkCache)
                CachingClient.Store(InputsHash, File);

            return entry != null;
        }


        public void ResolvedFromCache()
        {
            var entry = EntryForResultFromCache();

            PreviousBuildsDatabase.Instance.SetInfoFor(entry);
        }

        protected virtual PreviousBuildsDatabase.Entry EntryForResultFromCache()
        {
            throw new InvalidOperationException();
        }

        protected abstract PreviousBuildsDatabase.Entry BuildGeneratedFile();

    }
}