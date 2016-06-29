using System.Collections.Generic;
using NiceIO;

namespace csb2
{
    public abstract class GeneratedFileNode : FileNode
    {
        protected GeneratedFileNode(NPath file) : base(file)
        {
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

            if (file.TimeStamp > e.TimeStamp)
                return new UpdateReason($"Previous build that we made had a timestamp of {e.TimeStamp}, but the generated file on disk has a timestamp of {file.TimeStamp}");

            foreach (var dep in AllDependencies)
            {
                var fileDep = dep as FileNode;

                if (fileDep != null && fileDep.TimeStamp > e.TimeStamp)
                    return new UpdateReason($"Dependency {dep} has a timestamp ({fileDep.TimeStamp}) newer than the timestamp of the generated file we had previously built ({e.TimeStamp}");
            }
            
            return null;
        }

        public sealed override bool Build()
        {
            File.Parent.EnsureDirectoryExists();

            return BuildGeneratedFile();
        }

        protected abstract bool BuildGeneratedFile();
    }
}