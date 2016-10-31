using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using FactorGraph;
using GPLVM.Kernel;
using FactorGraph.Core;

namespace FactorGraph.FactorNodes
{
    [DataContract(IsReference = true)]
    [KnownType(typeof(BiasKern))]
    [KnownType(typeof(CompoundKern))]
    [KnownType(typeof(LinearAccelerationKern))]
    [KnownType(typeof(LinearKern))]
    [KnownType(typeof(RBFAccelerationKern))]
    [KnownType(typeof(RBFKern))]
    [KnownType(typeof(RBFKernBack))]
    [KnownType(typeof(StyleKern))]
    [KnownType(typeof(StyleAccelerationKern))]
    [KnownType(typeof(TensorKern))]
    [KnownType(typeof(WhiteKern))]
    [KnownType(typeof(ModulationKern))]
    [KnownType(typeof(DirectionKern))]
    [KnownType(typeof(PGEKern))]
    public abstract class FactorNodeWithKernel : FactorNode
    {
        [DataMember()]
        public static string CP_KERNEL = "Kernel";
        [DataMember()]
        public IKernel KernelObject;
        [DataMember()]
        protected DataConnector dcK;
        [DataMember()]
        protected IMatrixDataNode dnK;

        public FactorNodeWithKernel(string sName, IKernel iKernel)
            : base(sName)
        {
            KernelObject = iKernel;
            dcK = new DataConnector(CP_KERNEL);
            DataConnectors.Add(dcK);
        }

        public override void Initialize()
        {
            base.Initialize();

            dnK = dcK.ConnectedDataNode;
            dnK.SetValuesSize(new ILSize(new int[] {KernelObject.NumParameter}));
            dnK.SetOptimizingMaskAll();
            dnK.SetValues(KernelObject.LogParameter);
        }

        public DataConnector DataConnectorKernel
        {
            get { return dcK; }
        }

        protected void PushKernelParamsGradientsToDataConnector(ILInArray<double> inParamsGradient)
        {
            using (ILScope.Enter(inParamsGradient))
            {
                ILArray<double> paramsGradient = ILMath.check(inParamsGradient);
                dcK.SetGradient(paramsGradient);
            }
        }

        protected void PullKernelParamsFromDataConnector()
        {
            using (ILScope.Enter())
            {
                KernelObject.LogParameter = dnK.GetValues();
            }
        }

        protected void PushKernelParamsToDataConnector()
        {
            using (ILScope.Enter())
            {
                dnK.SetValues(KernelObject.LogParameter);
            }
        }

        public override void PullDataFromDataNodes()
        {
            base.PullDataFromDataNodes();
            PullKernelParamsFromDataConnector();
            PushKernelParamsToDataConnector();
        }

        public virtual double LogLikelihood()
        {
            // prior of kernel parameters
            return -(double)(ILMath.sum(KernelObject.LogParameter));
        }

        public override double FunctionValue()
        {
            return LogLikelihood();
        }

    }
}
