using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using FactorGraph.Core;

namespace FactorGraph.DataNodes
{
    [DataContract(IsReference = true)]
    [KnownType(typeof(DataNodeWithSegments))]
    [KnownType(typeof(StyleDataNode))]
    [KnownType(typeof(MatrixDataNode))]
    [KnownType(typeof(CompoundMatrixDataNode))]
    [KnownType(typeof(CompoundMatrixDataNodeWithSegments))]
    public class CompoundMatrixDataNode : NamedEntity, IMatrixDataNode
    {
        [DataContract()]
        public class DCDN
        {
            [DataMember()]
            public DataConnector DataConnector;
            [DataMember()]
            public IMatrixDataNode DataNode;
            public DCDN(DataConnector dataConnector, IMatrixDataNode dataNode)
            {
                DataConnector = dataConnector;
                DataNode = dataNode;
            }
        }
        public class DCDNList : List<DCDN>
        {
            public DCDN FindByDataNodeName(string name)
            {
                return this.Find(
                    delegate(DCDN dcdn)
                    {
                        return dcdn.DataNode.Name.Equals(name);
                    }
                );
            }
        }
        protected ILArray<double> aValuesGradientDummy = ILMath.localMember<double>(); // Gradient for every value
        [DataMember()]
        protected DataConnectorList lDataConnectors = new DataConnectorList();
        [DataMember()]
        protected DCDNList DCDNs = new DCDNList();

        public event ValuesSizeChangedEventHandler ValuesSizeChanged;
        
        public CompoundMatrixDataNode(string sName)
            : base(sName)
        {
        }

        public ILSize GetValuesSize()
        {
                int innerDimsSum = 0;
                foreach (DCDN dcdn in DCDNs)
                    innerDimsSum += dcdn.DataNode.GetValuesSize()[1];
                return new ILSize(DCDNs.First().DataNode.GetValuesSize()[0], innerDimsSum); 
        }

        public void SetValuesSize(ILSize newSize)
        {
            throw new GraphException("Can't set ValuesSize for CompoundMatrixDatanode");
        }
        

        public void AddInnerDataNode(IMatrixDataNode newInnerDataNode)
        {
            var newInnerDataConnector = new DataConnector(newInnerDataNode.Name + " Inner");
            newInnerDataConnector.PullingGradients += PullGradientsFormFactorNodes;
            DCDNs.Add(new DCDN(newInnerDataConnector, newInnerDataNode));
            newInnerDataConnector.ConnectDataNode(newInnerDataNode);
            //newInnerDataNode.ConnectDataConnector(newInnerDataConnector);
            newInnerDataNode.ValuesSizeChanged += this.OnValuesSizeChanged;
        }

        protected void OnValuesSizeChanged()
        {
            aValuesGradientDummy.a = ILMath.zeros<double>(GetValuesSize());
            SetOptimizingMaskAll();
            foreach (DataConnector dataConnector in lDataConnectors)
            {
                dataConnector.SetGradient(aValuesGradientDummy);
            }
            if (ValuesSizeChanged != null)
                ValuesSizeChanged();
        }

        public virtual int GetOptimizingSize()
        {
            int res = 0;
            foreach (DCDN dcdn in DCDNs)
                res += dcdn.DataNode.GetOptimizingSize();
            return res;
        }

        public ILRetArray<int> GetOptimizingMask()
        {
            using (ILScope.Enter())
            {
                ILArray<int> res = ILMath.empty<int>();
                int done = 0;
                foreach (DCDN dcdn in DCDNs)
                {
                    res.a = Util.Concatenate<int>(res, dcdn.DataNode.GetOptimizingMask() + done);
                    done += dcdn.DataNode.GetValuesSize().NumberOfElements;
                }
                return res;
            }
        }

        public void SetOptimizingMask(ILInArray<int> inMask)
        {
            using (ILScope.Enter())
            {
                ILArray<int> mask = ILMath.check<int>(inMask);
                int done = 0;
                foreach (DCDN dcdn in DCDNs)
                {
                    int numValues = dcdn.DataNode.GetValuesSize().NumberOfElements;
                    ILArray<int> maskPart = mask[ILMath.and((mask >= done), (mask < numValues + done))];
                    dcdn.DataNode.SetOptimizingMask(maskPart - done);
                    done += numValues;
                }
            }
        }

        public void SetOptimizingMaskAll()
        {
            using (ILScope.Enter())
            {
                SetOptimizingMask(ILMath.counter<int>(0, 1, GetValuesSize())[ILMath.full]);
            }
        }

        public void SetOptimizingMaskNone()
        {
            using (ILScope.Enter())
            {
                SetOptimizingMask(ILMath.empty<int>());
            }
        }

        // Accessed by factor nodes
        public ILRetArray<double> GetValues()
        {
            using (ILScope.Enter())
            {
                ILArray<double> res = ILMath.empty();
                foreach (DCDN dcdn in DCDNs)
                    res.a = Util.Concatenate<double>(res, dcdn.DataNode.GetValues(), 1);
                return res;
            }
        }

        // Accessed by factor nodes
        public void SetValues(ILInArray<double> inNewValues)
        {
            throw new GraphException("Can't set Values for CompoundMatrixDatanode");
        }

        // Accessed by factor nodes
        public ILRetArray<double> GetValuesGradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> valuesGradient = ILMath.empty();
                foreach (DCDN dcdn in DCDNs)
                {
                    valuesGradient.a = Util.Concatenate<double>(valuesGradient, dcdn.DataNode.GetValuesGradient());
                }
                return valuesGradient;
            }
        }

        // Accessed by the graph minimizing routine
        public virtual ILRetArray<double> GetParametersVector()
        {
            using (ILScope.Enter())
            {
                return GetValues()[GetOptimizingMask()];
            }
        }

        // Accessed by the graph minimizing routine
        public virtual void SetParametersVector(ILInArray<double> inValue)
        {
            using (ILScope.Enter(inValue))
            {
                ILArray<double> value = ILMath.check(inValue);
                int nDone = 0;
                foreach (DCDN dcdn in DCDNs)
                {
                    int optimizingSize = dcdn.DataNode.GetOptimizingSize();
                    dcdn.DataNode.SetParametersVector(value[ILMath.r(nDone, nDone + optimizingSize-1)]);
                    nDone += optimizingSize;
                }
                PushParametersToFactorNodes();
            }
        }

        // Accessed by the graph minimizing routine
        public virtual ILRetArray<double> GetParametersVectorGradient()
        {
            using (ILScope.Enter())
            {
                // All gradients are stored in the internal data nodes. Iterate and collect them
                ILArray<double> res = ILMath.empty();
                foreach (DCDN dcdn in DCDNs)
                {
                    int optimizingSize = dcdn.DataNode.GetOptimizingSize();
                    res.a = Util.Concatenate<double>(res, dcdn.DataNode.GetParametersVectorGradient());
                }
                return res;
            }
        }

        public void ConnectDataConnector(DataConnector dataConnector)
        {
            lDataConnectors.Add(dataConnector);
        }

        public void PushParametersToFactorNodes()
        {
            foreach (DataConnector dataConnector in lDataConnectors)
            {
                foreach (DCDN dcdn in DCDNs)
                    dcdn.DataNode.PushParametersToFactorNodes();
                dataConnector.PushParametesToFactorNode();
            }
        }

        protected bool pullingGradient = false;
        public void PullGradientsFormFactorNodes()
        {
            using (ILScope.Enter())
            {
                if (pullingGradient || GetOptimizingMask().IsEmpty)
                {
                    return;
                }
                pullingGradient = true;
                if (lDataConnectors.Count > 0)
                {
                    ILArray<double> externalGradients = ILMath.zeros<double>(lDataConnectors.First().GetGradient().S);
                    // Pull gradients from external data nodes
                    foreach (DataConnector dataConnector in lDataConnectors)
                    {
                        dataConnector.PullGradientsFromFactorNode();
                        externalGradients.a = externalGradients + dataConnector.GetGradient();
                    }
                    // Push external gradients to internal data connectors
                    int nColumnsDone = 0;
                    foreach (DCDN dcdn in DCDNs)
                    {
                        int optimizingSize = dcdn.DataNode.GetOptimizingSize();
                        int q = optimizingSize / externalGradients.S[0];
                        dcdn.DataConnector.SetGradient(externalGradients[ILMath.full, ILMath.r(nColumnsDone, nColumnsDone + q - 1)]);
                        nColumnsDone += q;
                    }
                }
                // Call internal data nodes gradients collection
                foreach (DCDN dcdn in DCDNs)
                    dcdn.DataNode.PullGradientsFormFactorNodes();
                
                pullingGradient = false;
            }
        }

        public virtual void CreateInducing(IMatrixDataNode matrixDataNode, ILArray<int> indexes)
        {
            throw new GraphException("Can't CreateInducing for CompoundMatrixDatanode");
        }

        public virtual void OnDeserializedMethod()
        {
            using (ILScope.Enter())
            {
                foreach (DCDN dcdn in DCDNs)
                {
                    dcdn.DataNode.OnDeserializedMethod();
                    dcdn.DataNode.ValuesSizeChanged += this.OnValuesSizeChanged;
                    dcdn.DataConnector.OnDeserializedMethod();
                    dcdn.DataConnector.PullingGradients += PullGradientsFormFactorNodes;
                }
                pullingGradient = false;
            }
        }
    }
}
