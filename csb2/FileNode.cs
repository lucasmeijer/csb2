using NiceIO;

namespace csb2
{
    public class FileNode : Node
    {
        public NPath File { get; private set; }

        public FileNode(NPath file) : base(file.ToString())
        {
            File = file;
        }

        public override bool DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return true;
        }
    }
}