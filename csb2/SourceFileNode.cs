using NiceIO;

namespace csb2
{
    public class SourceFileNode : FileNode
    {
        public SourceFileNode(NPath file) : base(file)
        {
        }

        public override bool Build()
        {
            TimeStamp = File.TimeStamp;
            return true;
        }
    }
}