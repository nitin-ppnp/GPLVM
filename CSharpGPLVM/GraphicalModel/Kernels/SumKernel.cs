using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicalModel.Factors;
using GPLVM.Kernel;

namespace GraphicalModel.Kernels
{
    public class SumKernel : CompoundKernel
    {
        public SumKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
            // Reuse the GPLVM librarry implemenation
            GPLVMKernel = new GPLVM.Kernel.CompoundKern();
        }

        public override void AddChildKernel(Kernel childKernel)            
        {
            base.AddChildKernel(childKernel);
            // Reusing the GPLVM library kernels
            ((GPLVM.Kernel.CompoundKern)GPLVMKernel).AddKern(childKernel.GPLVMKernel);
        }
    }

    public class SumKernelBuilder : KernelBuilder
    {
        public SumKernelBuilder()
        {
            Type = "Sum kernel";
        }

        public override Kernel BuildKernel(KernelDesc desc, Factor containerFactor)
        {
            return new SumKernel(desc, containerFactor);
        }

        public override KernelDesc BuildDesc(FactorDesc ownerFactorDesc)
        {
            var desc = new CompoundKernelDesc("none", Type);
            return desc;
        }
    }
}
