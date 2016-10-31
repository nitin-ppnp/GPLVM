using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace FactorGraph.Core
{
    [DataContract(IsReference = true)]
    public abstract class FactorNode : Graph // FactorNode can be compound, containing other nodes
    {
        [DataMember()]
        private DataConnectorList lDataConnectors = new DataConnectorList();

        public FactorNode(string sName)
            : base(sName)
        {
            lFactorNodes.Add(this);
        }

        public virtual void Initialize()
        {
            foreach (DataConnector dc in lDataConnectors)
            {
                if (dc.ConnectedDataNode == null)
                    throw new GraphException("Data connector \"" + Name + "->" + dc.Name + "\" is not connected");
            }
        }

        public DataConnectorList DataConnectors
        {
            get { return lDataConnectors; }
        }

        public abstract void ComputeAllGradients();

        public abstract double FunctionValue(); // Operaing in log scale, return 0 = log(1)

        public virtual void PullDataFromDataNodes()
        {
        }

        public virtual void BeforeOnDataNodesChanged()
        {
        }

        public virtual void OnDataNodesChanged()
        {
            PullDataFromDataNodes();
            ComputeAllGradients();
        }

        public virtual void AfterOnDataNodesChanged()
        {
        }

        public virtual void OnDeserializedMethod()
        {
            foreach (DataConnector dataConnector in lDataConnectors)
                dataConnector.OnDeserializedMethod();
        }
    }

    public class FactorNodeList : List<FactorNode>
    {
        public FactorNode FindByName(string name)
        {
            return this.Find(
                delegate(FactorNode factorNode)
                {
                    return factorNode.Name.Equals(name);
                }
            );
        }
    }
    
}
