using System;
using System.Collections.Generic;
using NiceIO;

namespace csb2
{
    public abstract class Node
    {
        public string Name { get; }
        public State State { get; set; }
        public UpdateReason UpdateReason { get; private set; }
        public virtual IEnumerable<Node> Dependencies { get{ yield break;}}

        protected Node(string name)
        {
            Name = name;
        }

        public virtual UpdateReason DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return new UpdateReason($"Nodes of type {GetType()} always rebuild");
        }

        public virtual bool Build()
        {
            return true;
        }

        public override string ToString()
        {
            return Name;
        }

        public void SetUpdateReason(UpdateReason updateReason)
        {
            UpdateReason = updateReason;
        }
    }

    class NodeGraph
    {
        private List<Node> _nodes = new List<Node>();

        public void AddNode(Node n)
        {
            _nodes.Add(n);
        }
    }

    internal class BuildFailedException : Exception
    {
        public BuildFailedException(string message) : base(message)
        {
        }
    }

    public enum State
    {
        NotProcessed,
        UpToDate,
        DependenciesReady,
        Building,
        Failed
    }
}