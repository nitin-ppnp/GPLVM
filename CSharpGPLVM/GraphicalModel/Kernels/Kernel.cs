using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Factors;
using GPLVM.Kernel;

namespace GraphicalModel.Kernels
{
    public abstract class KernelBuilder
    {
        public string Type;
        public abstract Kernel BuildKernel(KernelDesc desc, Factor containerFactor);
        public abstract KernelDesc BuildDesc(FactorDesc ownerFactorDesc);
    }

    public abstract class KernelDesc : GraphicModelElement
    {
        protected string sKernelType;

        public KernelDesc(string name, string kernelType)
            : base(name)
        {
            sKernelType = kernelType;
        }
        
        public string KernelType
        {
            get { return sKernelType; }
        }

        public abstract FactorConnectionPointDescList ConnectionPoints
        {
            get;
        }
    }

    public abstract class Kernel
    {
        public KernelDesc Desc; // back reference to the description
        public Factor ContainerFactor; // reference to the container factor
        protected FactorConnectionPointList lConnectionPoints;
        public IKernel GPLVMKernel; // reuse the GPLVM library implemenation

        public Kernel(KernelDesc desc, Factor containerFactor)
        {
            Desc = desc;
            ContainerFactor = containerFactor;
            lConnectionPoints = new FactorConnectionPointList();

            // Find the connection points in the container factor
            foreach (var connectionPointDesc in desc.ConnectionPoints)
                lConnectionPoints.Add(ContainerFactor.ConnectionPoints.FindByName(connectionPointDesc.Name));

            // Create the connection points
            //foreach (var connectionPointDesc in desc.ConnectionPoints)
            //    lConnectionPoints.Add(new FactorConnectionPoint(connectionPointDesc, ContainerFactor));
        }

        public abstract FactorConnectionPointList ConnectionPoints
        {
            get;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Gradients calculations and other abstract methods
        ////////////////////////////////////////////////////////////////////////////////

        // TODO: change to abstract
        public virtual double LogLikelihood()
        {
            return 0;
        }

        public virtual ILArray<double> K
        {
            get { return GPLVMKernel.K; }
            //set { GPLVMKernel.K = value; }
        }

        public virtual int NumParameter
        {
            get { return GPLVMKernel.NumParameter; }
        }

        public virtual ILRetArray<double> ComputeKernelMatrix(ILInArray<double> inX1, ILInArray<double> inX2)
        {
            return GPLVMKernel.ComputeKernelMatrix(inX1, inX2);
        }

        public virtual ILRetArray<double> LogLikGradientX(ILInArray<double> inX, ILInArray<double> indL_dK)
        {
            return GPLVMKernel.LogLikGradientX(inX, indL_dK);
        }

        public virtual ILRetArray<double> LogLikGradientParam(ILInArray<double> indL_dK)
        {
            return GPLVMKernel.LogLikGradientParam(indL_dK);
        }

        public virtual ILRetArray<double> GradX(ILInArray<double> inX, int q)
        {
            return GPLVMKernel.GradX(inX, inX, q);
        }

        // Returns number of parameters set
        public abstract int SetParametersGradient(ILInArray<double> inParamsGrad);

        protected virtual void DataExpandedHandler(object source)
        {
        }

        public virtual void Initialise()
        {
        }
    }
}
