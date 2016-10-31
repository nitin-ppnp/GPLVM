using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using FactorGraph.Core;

namespace FactorGraph.Core
{
    [DataContract(IsReference = true)]
    public class MatrixDataNode : NamedEntity, IMatrixDataNode
    {
        [DataMember()]
        protected ILArray<double> aValues = ILMath.localMember<double>(); // Full data in a single vector 
        [DataMember()]
        protected ILArray<int> aOptimizingMask = ILMath.localMember<int>(); // Indexes of variables being optimized (e.g. [0, 1, 2, 6])
        //[DataMember()]
        protected ILArray<double> aValuesGradient = ILMath.localMember<double>(); // Gradient for every value
        protected ILArray<double> aValuesGradientFactor = ILMath.localMember<double>(); 
        [DataMember()]
        protected DataConnectorList lDataConnectors = new DataConnectorList();
        
        public event ValuesSizeChangedEventHandler ValuesSizeChanged;

        public MatrixDataNode(string sName)
            : base(sName)
        {
        }

        public ILSize GetValuesSize()
        {
            return aValues.S; 
        }

        public void SetValuesSize(ILSize newSize)
        {
            using (ILScope.Enter())
            {
                aValues.a = ILMath.zeros<double>(newSize);
                OnValuesSizeChanged();
            }
        }

        protected void OnValuesSizeChanged()
        {
            using (ILScope.Enter())
            {
                aValuesGradient.a = ILMath.zeros<double>(GetValuesSize());
                SetOptimizingMaskAll();
                if (ValuesSizeChanged != null)
                    ValuesSizeChanged();
            }
        }

        public virtual int GetOptimizingSize()
        {
            return aOptimizingMask.S.NumberOfElements; 
        }

        public ILRetArray<int> GetOptimizingMask()
        {
            using (ILScope.Enter())
            {
                return aOptimizingMask.C;
            }
        }

        public void SetOptimizingMask(ILInArray<int> inMask)
        {
            using (ILScope.Enter())
            {
                ILArray<int> mask = ILMath.check<int>(inMask);
                aOptimizingMask.a = mask;                
            }
        }

        public void SetOptimizingMaskAll()
        {
            using (ILScope.Enter())
            {
                aOptimizingMask.a = ILMath.counter<int>(0, 1, GetValuesSize());
                aOptimizingMask.a = aOptimizingMask[ILMath.full];
            }
        }

        public void SetOptimizingMaskNone()
        {
            using (ILScope.Enter())
            {
                aOptimizingMask.a = ILMath.empty<int>();
            }
        }

        // Accessed by factor nodes
        public ILRetArray<double> GetValues()
        {
            return aValues.C;
        }

        // Accessed by factor nodes
        public void SetValues(ILInArray<double> inNewValues)
        {
            using (ILScope.Enter())
            {
                ILArray<double> newValues = ILMath.check(inNewValues);
                aValues.a = newValues.C;
            }
        }

        // Accessed by factor nodes
        public ILRetArray<double> GetValuesGradient()
        {
            using (ILScope.Enter())
            {
                return aValuesGradient.C;
            }
        }

        // Accessed by the graph minimizing routine
        public virtual ILRetArray<double> GetParametersVector()
        {
            using (ILScope.Enter())
            {
                return aValues[aOptimizingMask];
            }
        }

        // Accessed by the graph minimizing routine
        public virtual void SetParametersVector(ILInArray<double> inValue)
        {
            using (ILScope.Enter(inValue))
            {
                ILArray<double> value = ILMath.check(inValue);
                aValues[aOptimizingMask] = value;
                PushParametersToFactorNodes();
            }
        }

        // Accessed by the graph minimizing routine
        public virtual ILRetArray<double> GetParametersVectorGradient()
        {
            using (ILScope.Enter())
            {
                if (aValuesGradientFactor.IsEmpty)
                    return aValuesGradient[aOptimizingMask];
                else
                    return ILMath.multiplyElem(aValuesGradientFactor, aValuesGradient)[aOptimizingMask];
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
                dataConnector.PushParametesToFactorNode();
            }
        }

        protected bool pullingGradient = false;
        public void PullGradientsFormFactorNodes()
        {
            using (ILScope.Enter())
            {
                if (pullingGradient || aOptimizingMask.IsEmpty)
                {
                    return;
                }
                pullingGradient = true;
                aValuesGradient.a = ILMath.zeros<double>(aValuesGradient.S);
                foreach (DataConnector dataConnector in lDataConnectors)
                {
                    dataConnector.PullGradientsFromFactorNode();
                    aValuesGradient.a = aValuesGradient + dataConnector.GetGradient();
                }
                pullingGradient = false;
            }
        }

        public virtual void CreateInducing(IMatrixDataNode matrixDataNode, ILArray<int> indexes)
        {
            using (ILScope.Enter())
            {
                aValues.a = matrixDataNode.GetValues()[indexes, ILMath.full].C;
                OnValuesSizeChanged();
            }
        }

        public virtual void OnDeserializedMethod()
        {
            using (ILScope.Enter())
            {
                // Restore gradient size
                aValuesGradient = ILMath.localMember<double>();
                aValuesGradient.a = ILMath.zeros<double>(GetValuesSize());
                aValuesGradientFactor = ILMath.localMember<double>(); 
            }
            pullingGradient = false;
        }
    }

    public class MatrixDataNodeList : List<IMatrixDataNode>
    {
        public IMatrixDataNode FindByName(string name)
        {
            return this.Find(
                delegate(IMatrixDataNode dataNode)
                {
                    return dataNode.Name.Equals(name);
                }
            );
        }
    }
}
