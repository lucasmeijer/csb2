using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;

namespace csb2
{
    public abstract class Node
    {
        public string Name { get; }
        public State State { get; set; }
        public UpdateReason UpdateReason { get; private set; }
        public virtual IEnumerable<Node> StaticDependencies { get{ yield break;}}
        public virtual IEnumerable<Node> DynamicDependencies {  get { yield break; } }

        public IEnumerable<Node> AllDependencies => StaticDependencies.Concat(DynamicDependencies);

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

        public virtual void SetupDynamicDependencies()
        {
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
        StaticDependenciesReady,
        AllDependenciesReady,
        Building,
        Failed
    }
}