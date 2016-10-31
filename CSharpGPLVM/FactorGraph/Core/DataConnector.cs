using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using FactorGraph.DataConnectors;

namespace FactorGraph.Core
{
    // DataConnector is used to store gradints and connect to DataNode
    [DataContract(IsReference = true)]
    public class DataConnector : NamedEntity
    {
        public delegate void PullingGradientsEventHandler();
        [DataMember()]
        public IMatrixDataNode ConnectedDataNode = null;
        //[DataMember()]
        protected ILArray<double> aGradient = ILMath.localMember<double>();
        public event PullingGradientsEventHandler PullingGradients;

        public DataConnector(string sName)
            : base(sName)
        {
        }

        public void ConnectDataNode(IMatrixDataNode dataNode)
        {
            dataNode.ConnectDataConnector(this);
            ConnectedDataNode = dataNode;
        }
        
        public virtual void PushParametesToFactorNode()
        {
            // Nothing for now, may be callback to factor node
        }

        public void PullGradientsFromFactorNode()
        {
            if (PullingGradients != null)
                PullingGradients();
        }

        public ILArray<double> Values
        {
            get { return GetValues(); }
            set { SetValues(value); }
        }

        protected virtual ILRetArray<double> GetValues()
        {
            return ConnectedDataNode.GetValues();
        }

        protected virtual void SetValues(ILInArray<double> inV)
        {
            using (ILScope.Enter(inV))
            {
                ILArray<double> V = ILMath.check(inV);
                ConnectedDataNode.SetValues(V);
            }
        }

        public virtual ILRetArray<double> GetGradient()
        {
            return aGradient;
        }

        public virtual void SetGradient(ILInArray<double> inV)
        {
            using (ILScope.Enter(inV))
            {
                ILArray<double> V = ILMath.check(inV);
                aGradient.a = V;
            }
        }

        public virtual void OnDeserializedMethod()
        {
            aGradient = ILMath.localMember<double>();
            if (ConnectedDataNode != null)
            {
                aGradient.a = ILMath.zeros(ConnectedDataNode.GetValues().S);
            }
        }
    }

    [KnownType(typeof(LogDataConnector))]
    public class DataConnectorList : List<DataConnector>
    {
        public DataConnector FindByName(string name)
        {
            return this.Find(
                delegate(DataConnector dataConnector)
                {
                    return dataConnector.Name.Equals(name);
                }
            );
        }
    }
}
