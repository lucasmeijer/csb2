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
        public virtual IEnumerable<Node> ProvideStaticDependencies() { yield break;}
        private Node[] _dynamicDependencies;
        private Node[] _staticDependencies;
        public Node[] DynamicDependencies
        {
            get
            {
                if (State < State.StaticDependenciesReady)
                    throw new InvalidOperationException();

                return _dynamicDependencies ?? (_dynamicDependencies = ProvideDynamicDependencies().ToArray());
            }
        }

        public Node[] StaticDependencies => _staticDependencies ?? (_staticDependencies = ProvideStaticDependencies().ToArray());

        public virtual bool NeverBuilds => false;

        public void SetStaticDependencies(params Node[] deps)
        {
            if (_staticDependencies != null)
                throw new InvalidOperationException();
            _staticDependencies = deps;
        }

        public IEnumerable<Node> AllDependencies => _staticDependencies.Concat(_dynamicDependencies);
        public abstract string NodeTypeIdentifier { get; }
        public int EstimatedCost => 1;

        protected Node(string name)
        {
            Name = name;
        }

        public virtual UpdateReason DetermineNeedToBuild(PreviousBuildsDatabase db)
        {
            return null;
//            return new UpdateReason($"Nodes of type {GetType()} always rebuild");
        }

        public virtual JobResult Build()
        {
            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            return Name;
        }

        public void SetUpdateReason(UpdateReason updateReason)
        {
            UpdateReason = updateReason;
        }

        public virtual IEnumerable<Node> ProvideDynamicDependencies()
        {
            yield break;
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
        Built,
        Failed,
        Processing
    }
}