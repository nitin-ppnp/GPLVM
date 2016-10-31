using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicalModel.Factors;
using GPLVM.Kernel;

namespace GraphicalModel.Kernels
{
    public class ProductKernel : CompoundKernel
    {
        public ProductKernel(KernelDesc desc, Factor containerFactor)
            : base(desc, containerFactor)
        {
            // Reusing the GPLVM library kernels
            GPLVMKernel = new GPLVM.Kernel.TensorKern();
        }

        public override void AddChildKernel(Kernel childKernel)
        {
            base.AddChildKernel(childKernel);
            // Reusing the GPLVM library kernels
            ((GPLVM.Kernel.CompoundKern)GPLVMKernel).AddKern(childKernel.GPLVMKernel);
        }
    }

    public class ProductKernelBuilder : KernelBuilder
    {
        public ProductKernelBuilder()
        {
            Type = "Product kernel";
        }

        public override Kernel BuildKernel(KernelDesc desc, Factor containerFactor)
        {
            return new ProductKernel(desc, containerFactor);
        }

        public override KernelDesc BuildDesc(FactorDesc ownerFactorDesc)
        {
            var desc = new CompoundKernelDesc("none", Type);
            return desc;
        }
    }
}

