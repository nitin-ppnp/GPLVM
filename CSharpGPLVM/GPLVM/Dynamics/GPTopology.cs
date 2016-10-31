using System.Collections.Generic;
using System.Linq;
using ILNumerics;

namespace GPLVM.Dynamics.Topology
{
    public class TNode
    {
        public int ID;
        public List<TNode> Parents = new List<TNode>();
        public List<TNode> Children = new List<TNode>();
        
        public TNode(int ID)
        {
            this.ID = ID;
        }

        public static void Connect(TNode parent, TNode child)
        {
            if ((parent != null) && (child != null) && (!parent.Children.Contains(child)))
            {
                parent.Children.Add(child);
                child.Parents.Add(parent);
            }
        }
    }

    public class GPTopology
    {
        protected int nTNodes = 0;
        protected List<List<TNode>> lSegments = new List<List<TNode>>();

        public GPTopology()
        {
        }

        public int AddSegment(int size)
        {
            var newSegment = new List<TNode>();
            TNode prev = null;
            for (int i = 0; i < size; i++)
            {
                TNode newTNode = new TNode(nTNodes);
                newSegment.Add(newTNode);
                TNode.Connect(prev, newTNode);
                prev = newTNode;
                nTNodes++;
            }

            lSegments.Add(newSegment);
            return lSegments.Count - 1;
        }

        public void ConnectSegments(int segmentIn, int segmentOut)
        {
            ConnectSegments(lSegments[segmentIn], lSegments[segmentOut]);
        }

        protected void ConnectSegments(List<TNode> segmentIn, List<TNode> segmentOut)
        {
            TNode.Connect(segmentIn.Last(), segmentOut.First());
        }

        delegate void OnAncestors(int[] ancestors);

        public ILRetArray<int> GetInOutMap(int order)
        {
            ILArray<int> map = ILMath.array<int>(0, 1, order);
            List<TNode> TNodes = new List<TNode>();
            foreach (List<TNode> segment in lSegments)
            {
                TNodes.AddRange(segment);
            }
            foreach (TNode TNode in TNodes)
            {
                FillMapWithDescendants(TNode, map, order);
            }
            map = map[ILMath.r(1, ILMath.end), ILMath.full]; // remove the 0-th dummy row
            return map;
        }
       
        public ILRetArray<int> GetStartingPointsMap(int order)
        {
            ILArray<int> map = ILMath.array<int>(0, 1, order);
            List<TNode> TNodes = new List<TNode>();
            foreach (List<TNode> segment in lSegments)
            {
                TNodes.AddRange(segment);
            }
            foreach (TNode TNode in TNodes)
            {
                if (TNode.Parents.Count == 0)
                {
                    FillMapWithDescendants(TNode, map, order);
                }
            }
            map = map[ILMath.r(1, ILMath.end), ILMath.full]; // remove the 0-th dummy row
            return map;
        }

        private void FillMapWithDescendants(TNode TNode, ILOutArray<int> map, int order)
        {
            if (order == 1)
            {
                map[ILMath.end + 1, ILMath.end] = TNode.ID;
                return;
            }
            int n1 = map.S[0];
            foreach (TNode parent in TNode.Children)
            {
                FillMapWithDescendants(parent, map, order - 1);
            }
            int n2 = map.S[0];
            map[ILMath.r(n1, n2 - 1), ILMath.end - order + 1] = TNode.ID;
        }

        public static void Test()
        {
            var topology = new GPTopology();
            int s1 = topology.AddSegment(5);
            int s2 = topology.AddSegment(5);
            int s3 = topology.AddSegment(5);
            int s4 = topology.AddSegment(5);
            if (true)
            {
                topology.ConnectSegments(s1, s2);
                topology.ConnectSegments(s2, s1);
                topology.ConnectSegments(s3, s1);
                topology.ConnectSegments(s1, s4);
            }
            ILArray<int> map = topology.GetInOutMap(3);
            System.Console.WriteLine(map.ToString());
            map = topology.GetStartingPointsMap(2);
            System.Console.WriteLine(map.ToString());
        }
    }

}
