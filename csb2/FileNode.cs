using System;
using NiceIO;

namespace csb2
{
    public abstract class FileNode : Node
    {
        public NPath File { get; }
        public DateTime TimeStamp { get; protected set; }

        protected FileNode(NPath file) : base(file.ToString())
        {
            File = file;
        }
    }
}