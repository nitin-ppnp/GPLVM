using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Factors;

namespace GraphicalModel.Kernels
{

    public class AtomicKernelDesc : KernelDesc
    {
        protected FactorConnectionPointDescList lConnectionPoints;

        public AtomicKernelDesc(string name, string kernelType)
            : base(name, kernelType)
        {
            lConnectionPoints = new FactorConnectionPointDescList();
        }

        public override FactorConnectionPointDescList ConnectionPoints
        {
            get
            {
                var res = new FactorConnectionPointDescList();
                res.AddRange(lConnectionPoints);
                return res;
            }
        }

        public void AddConnectionPointDesc(FactorConnectionPointDesc newConnectionPoint)
        {
            lConnectionPoints.Add(newConnectionPoint);
        }

    }

    public abstract class AtomicKernel : Kernel
    {
        public AtomicKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
        }

        public override FactorConnectionPointList ConnectionPoints
        {
            get { return lConnectionPoints; }
        }

        public override int SetParametersGradient(ILInArray<double> inParamsGrad)
        {
            using (ILScope.Enter(inParamsGrad))
            {
                ILArray<double> paramsGrad = ILMath.check(inParamsGrad);
                // TODO:
                //int done = 0;
                //foreach (var cp in ConnectionPoints
                if (lConnectionPoints.Count == 1)
                    ((DataExpander)(lConnectionPoints.First().ConnectedDataObject)).Gradient = paramsGrad[ILMath.r(0, GPLVMKernel.NumParameter-1)];
                return GPLVMKernel.NumParameter;
            }
        }

        public override void Initialise()
        {
            //foreach (var cp in lConnectionPoints)
            if (lConnectionPoints.Count == 1)
                ((DataExpander)(lConnectionPoints.First().ConnectedDataObject)).OnDataExpanded += DataExpandedHandler;
            DataExpandedHandler(lConnectionPoints.First().ConnectedDataObject);
        }

        protected override void DataExpandedHandler(object source)
        {
            GPLVMKernel.LogParameter = ((DataExpander)(lConnectionPoints.First().ConnectedDataObject)).ExpandedData;
        }
    }
}
