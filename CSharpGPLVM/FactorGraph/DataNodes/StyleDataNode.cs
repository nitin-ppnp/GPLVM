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
    public class StyleDataNode : MatrixDataNode
    {
        [DataMember()]
        public List<string> Substyles;
        [DataMember()]
        protected ILArray<double> aIndexes = ILMath.localMember<double>();
        [DataMember()]
        protected ILArray<double> aParameters = ILMath.localMember<double>(); // SxS matrix of syle parameters

        public StyleDataNode(string sName)
            : base(sName)
        {
            Substyles = new List<string>();
        }

        public void AddData(string sSubstyle, int nSize)
        {
            using (ILScope.Enter())
            {
                if (!Substyles.Contains(sSubstyle))
                    Substyles.Add(sSubstyle);
                int k = Substyles.IndexOf(sSubstyle);
                aIndexes.a = Util.Concatenate<double>(aIndexes, k + ILMath.zeros(nSize));
                CreateParametersMatrix();
                CreateValuesMatrix();
            }
        }

        protected void CreateParametersMatrix()
        {
            using (ILScope.Enter())
            {
                aParameters.a = ILMath.eye(Substyles.Count, Substyles.Count); // eye means identity, no clue why
            }
        }

        protected void CreateValuesMatrix()
        {
            using (ILScope.Enter())
            {
                aValues.a = aParameters[aIndexes, ILMath.full];
                OnValuesSizeChanged();
            }
        }

        public override int GetOptimizingSize()
        {
            // TODO: add optimization mask
            return aParameters.S.NumberOfElements;// Util.NumElements<int>(aParameters);
        }

        // Accessed by the graph minimizing routine
        public override ILRetArray<double> GetParametersVector()
        {
            return aParameters[ILMath.full];// aValues[aOptimizingMask][ILMath.full];
        }

        // Accessed by the graph minimizing routine
        public override void SetParametersVector(ILInArray<double> inValue)
        {
            using (ILScope.Enter(inValue))
            {
                ILArray<double> value = ILMath.check(inValue);
                aParameters[ILMath.full] = value;
                base.SetParametersVector(aParameters[aIndexes, ILMath.full]);
            }
        }

        // Accessed by the graph minimizing routine
        public override ILRetArray<double> GetParametersVectorGradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> gParams = ILMath.zeros<double>(aParameters.S);
                // TODO: add optimization mask
                for (int k = 0; k < Substyles.Count; k++)
                    gParams[k, ILMath.full] = gParams[k, ILMath.full] + ILMath.sum(aValuesGradient[aIndexes == k, ILMath.full], 0);
                return gParams[ILMath.full];
            }
        }

        public override void CreateInducing(IMatrixDataNode matrixDataNode, ILArray<int> indexes)
        {
            using (ILScope.Enter())
            {
                if (!(matrixDataNode is StyleDataNode))
                    throw new GraphException("Data node " + matrixDataNode.Name + " is not StyleDataNode!");
                StyleDataNode orig = matrixDataNode as StyleDataNode;
                Substyles = orig.Substyles;
                CreateParametersMatrix();
                aIndexes.a = orig.aIndexes[indexes];
                CreateValuesMatrix();
            }
        }
    }
}
