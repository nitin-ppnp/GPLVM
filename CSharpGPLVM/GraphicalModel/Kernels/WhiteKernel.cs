using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicalModel.Factors;
using GPLVM.Kernel;

namespace GraphicalModel.Kernels
{
    public class WhiteKernel : AtomicKernel
    {
        public WhiteKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
            // Reusing the GPLVM library kernels
            GPLVMKernel = new WhiteKern();
        }
    }

    public class WhiteKernelBuilder : KernelBuilder
    {
        public WhiteKernelBuilder()
        {
            Type = "White";
        }

        public override Kernel BuildKernel(KernelDesc desc, Factor containerFactor)
        {
            return new WhiteKernel(desc, containerFactor);
        }

        public override KernelDesc BuildDesc(FactorDesc ownerFactorDesc)
        {
            var desc = new AtomicKernelDesc("none", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc("White kernel parameters", ownerFactorDesc));
            return desc;
        }
    }
}
