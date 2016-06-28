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

        public override bool Build()
        {
            TimeStamp = File.TimeStamp;
            return true;
        }
    }

    public interface IHaveFileNodes
    {
        string Name { get; }
        FileNode[] FileNodes { get; }
    }

    public class SourceFilesInDirectoryNode : Node, IHaveFileNodes
    {
        private readonly NPath _directory;
        public FileNode[] FileNodes { get; private set; }

        public SourceFilesInDirectoryNode(NPath directory) : base(directory + "|filesindir")
        {
            _directory = directory;
        }

        public override bool Build()
        {
            FileNodes = _directory.Files("*.cpp").Select(f => new SourceFileNode(f)).ToArray<FileNode>();
            return true;
        }
    }

    public interface IHaveObjectNodes
    {
        ObjectNode[] ObjectNodes { get; }
    }

    public class ObjectsNode : Node, IHaveObjectNodes
    {
        public SourceFilesInDirectoryNode SourceFilesInDirectoryNode { get; }
        public ObjectNode[] ObjectNodes { get; private set; }

        public ObjectsNode(SourceFilesInDirectoryNode sourceFilesInDirectoryNode) : base(sourceFilesInDirectoryNode.Name+"|objectlist")
        {
            SourceFilesInDirectoryNode = sourceFilesInDirectoryNode;
        }

        public override void SetupDynamicDependencies()
        {
            ObjectNodes = SourceFilesInDirectoryNode.FileNodes.Select(f => new ObjectNode(f, f.File.ChangeExtension(".obj"))).ToArray();
        }

        public override IEnumerable<Node> StaticDependencies => new[] { SourceFilesInDirectoryNode};
        public override IEnumerable<Node> DynamicDependencies => ObjectNodes;
    }
}