using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NiceIO;

namespace csb2
{
    public class SourceFileNode : FileNode
    {
        public SourceFileNode(NPath file) : base(file)
        {
        }

        public override string NodeTypeIdentifier => "SrcFile";

        public override UpdateReason DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return null;
        }
    }
    
}