using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicalModel.Factors;
using GPLVM.Kernel;

namespace GraphicalModel.Kernels
{
    public class LinearKernel : AtomicKernel
    {
        public LinearKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
            GPLVMKernel = new LinearKern();
        }
    }

    public class LinearKernelBuilder : KernelBuilder
    {
        public LinearKernelBuilder()
        {
            Type = "Linear";
        }

        public override Kernel BuildKernel(KernelDesc desc, Factor containerFactor)
        {
            return new LinearKernel(desc, containerFactor);
        }

        public override KernelDesc BuildDesc(FactorDesc ownerFactorDesc)
        {
            var desc = new AtomicKernelDesc("none", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc("Linear kernel parameters", ownerFactorDesc));
            return desc;
        }
    }
}
