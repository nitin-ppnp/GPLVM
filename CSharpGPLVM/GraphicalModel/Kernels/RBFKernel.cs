using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicalModel.Factors;
using GPLVM.Kernel;

namespace GraphicalModel.Kernels
{
    public class RBFKernel : AtomicKernel
    {
        public RBFKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
            // Reusing the GPLVM library kernels
            GPLVMKernel = new RBFKern();
        }
    }

    public class RBFKernelBuilder : KernelBuilder
    {
        public RBFKernelBuilder()
        {
            Type = "RBF";
        }

        public override Kernel BuildKernel(KernelDesc desc, Factor containerFactor)
        {
            return new RBFKernel(desc, containerFactor);
        }

        public override KernelDesc BuildDesc(FactorDesc ownerFactorDesc)
        {
            var desc = new AtomicKernelDesc("none", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc("RBF kernel parameters", ownerFactorDesc));
            return desc;
        }
    }
}
