using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using GPLVM.Numerical;
using GPLVM;
using ILNumerics;
using FactorGraph.FactorNodes;
using FactorGraph.DataNodes;

namespace FactorGraph.Core
{
    [DataContract(IsReference = true)]
    [KnownType(typeof(GPLVMNode))]
    [KnownType(typeof(AccelerationDynamicsNode))]
    [KnownType(typeof(StyleAccelerationDynamicsNode))]
    [KnownType(typeof(PTCBackConstraintNode))]
    [KnownType(typeof(KBRBackConstraintNode))]
    [KnownType(typeof(DataNodeWithSegments))]
    [KnownType(typeof(StyleDataNode))]
    [KnownType(typeof(CompoundMatrixDataNode))]
    [KnownType(typeof(CompoundMatrixDataNodeWithSegments))]
    public class Graph : NamedEntity, IFunctionWithGradient
    {
        [DataMember()]
        protected MatrixDataNodeList lDataNodes = new MatrixDataNodeList();
        [DataMember()]
        protected FactorNodeList lFactorNodes = new FactorNodeList();

        public Graph(string sName)
            : base(sName)
        {
        }
       
        public MatrixDataNodeList DataNodes
        {
            get { return lDataNodes; }
        }

        public FactorNodeList FactorNodes
        {
            get { return lFactorNodes; }
        }

        /// IFunctionWithGradient implementation
        public int NumParameters
        {
            get
            {
                int n = 0;
                foreach (IMatrixDataNode dataNode in lDataNodes)
                    n += dataNode.GetOptimizingSize();
                return n;
            }
        }

        public double Value()
        {
            // Factor graph is operating in log scale, return sum of factors instead of product
            foreach (IMatrixDataNode dataNode in lDataNodes)
                dataNode.PushParametersToFactorNodes();
            double sum = 0;
            foreach (FactorNode factorNode in lFactorNodes)
                sum += factorNode.FunctionValue();
            return -sum;
        }

        public ILArray<double> Gradient()
        {
            ILArray<double> fullGradient = ILMath.empty();
            foreach (IMatrixDataNode dataNode in lDataNodes)
                dataNode.PullGradientsFormFactorNodes();
            foreach (IMatrixDataNode dataNode in lDataNodes)
                fullGradient.a = Util.Concatenate<double>(fullGradient, dataNode.GetParametersVectorGradient());
            return -fullGradient.T;
        }

        public ILArray<double> Parameters
        {
            get
            {
                ILArray<double> fullParameters = ILMath.empty();
                foreach (IMatrixDataNode dataNode in lDataNodes)
                    fullParameters.a = Util.Concatenate<double>(fullParameters, dataNode.GetParametersVector());
                return fullParameters.T;
            }
            set
            {
                int done = 0;
                foreach (IMatrixDataNode dataNode in lDataNodes)
                {
                    dataNode.SetParametersVector(value[ILMath.r(done, done + dataNode.GetOptimizingSize() - 1)]);
                    done += dataNode.GetOptimizingSize();
                }
                ComputeGradients();               
            }
        }

        public void ComputeGradients()
        {
            // Notify all factors about new data to perform internal computations
            foreach (FactorNode factorNode in lFactorNodes)
                factorNode.BeforeOnDataNodesChanged();
            foreach (FactorNode factorNode in lFactorNodes)
                factorNode.OnDataNodesChanged();
            foreach (FactorNode factorNode in lFactorNodes)
                factorNode.AfterOnDataNodesChanged();
        }

        public IMatrixDataNode FindDataNodeByName(string name)
        {
            IMatrixDataNode res = lDataNodes.Find(
                delegate(IMatrixDataNode dataNode)
                {
                    return dataNode.Name.Equals(name);
                }
            );
            if (res != null)
                return res;
            foreach (FactorNode factorNode in lFactorNodes)
            {
                if (factorNode != this)
                    res = factorNode.FindDataNodeByName(name);
                if (res != null)
                    return res;
            }
            return null;
        } 

        [OnDeserialized()]
        public void OnDeserializedMethod(StreamingContext context)
        {
            // Restore dependent data in nodes
            foreach (IMatrixDataNode dataNode in lDataNodes)
                dataNode.OnDeserializedMethod();
            foreach (FactorNode factorNode in lFactorNodes)
                factorNode.OnDeserializedMethod();
        }
    }
}
