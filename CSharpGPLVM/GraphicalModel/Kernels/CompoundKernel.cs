using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Factors;

namespace GraphicalModel.Kernels
{
    public class CompoundKernelDesc : KernelDesc
    {
        protected List<KernelDesc> lChildKernels = new List<KernelDesc>();

        public CompoundKernelDesc(string name, string kernelType)
            : base(name, kernelType)
        {
        }

        public List<KernelDesc> ChildKernels
        {
            get { return lChildKernels; }
        }

        public override FactorConnectionPointDescList ConnectionPoints
        {
            get
            {
                var res = new FactorConnectionPointDescList();
                // Combine all child kernels connection points
                foreach (var kernel in lChildKernels)
                    res.AddRange(kernel.ConnectionPoints);
                return res;
            }
        }
    }

    public abstract class CompoundKernel : Kernel
    {
        protected List<Kernel> lChildKernels;

        public CompoundKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
            lChildKernels = new List<Kernel>();
        }

        public virtual void AddChildKernel(Kernel childKernel)
        {
            lChildKernels.Add(childKernel);
        }

        public override FactorConnectionPointList ConnectionPoints
        {
            get
            {
                var res = new FactorConnectionPointList();
                res.AddRange(lConnectionPoints);
                // Combine all child kernels connection points
                foreach (var kernel in lChildKernels)
                    res.AddRange(kernel.ConnectionPoints);
                return res;
            }
        }

        public override int SetParametersGradient(ILInArray<double> inParamsGrad)
        {
            using (ILScope.Enter(inParamsGrad))
            {
                ILArray<double> paramsGrad = ILMath.check(inParamsGrad);
                int done = 0;
                // TODO:
                //if (lConnectionPoints.Count == 1)
                //    done += ((DataExpander)(lConnectionPoints.First().ConnectedDataObject)).Gradient = paramsGrad[ILMath.r(0, GPLVMKernel.NumParameter)];
                foreach (var kernel in lChildKernels)
                    done += kernel.SetParametersGradient(paramsGrad[ILMath.r(done, done + kernel.NumParameter - 1)]);
                return done;
            }
        }

        public override void Initialise()
        {
            foreach (var kernel in lChildKernels)
                kernel.Initialise();
        }
    }
}
