using System;

namespace GPLVM.Graph
{
    public class Node
    {
        public Guid key = Guid.NewGuid();

        protected NodeList _nodes;

        public NodeList Nodes
        {
            get { return _nodes; }
            set { _nodes = value; }
        }

        protected bool _isChild;
        public bool isChild
        {
            get
            {
                return _isChild;
            }
            set
            {
                _isChild = value;
            }
        }

        public Node()
        {
            _nodes = new NodeList();
            _isChild = false;
        }

        public void AddNode(Node newNode)
        {
            newNode.isChild = true;
            _nodes.Add(newNode);
        }

        public Node GetNode(string key)
        {
            foreach (Node node in _nodes)
            {
                if (node.key.ToString() == key)
                    return node;
            }

            return null;
        }
    }
}
