using System;
using NiceIO;

namespace csb2
{
    public abstract class FileNode : Node
    {
        public NPath File { get; }

        public DateTime TimeStamp => File.TimeStamp;

        protected FileNode(NPath file) : base(file.ToString())
        {
            File = file;
        }
    }
}