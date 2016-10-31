using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GraphicalModel;

namespace GraphicalModel.Factors
{
    public class GPLVMFactor : Factor
    {
        public static string CP_LATENT    = "Latent";
        public static string CP_OBSERVED  = "Observed";

        protected DataExpander deX;
        protected DataExpander deY;
        protected int nN; // size of the data set
        protected int nD; // observed space dimantionality
        protected int nQ; // latent space dimentionality
        private ILArray<double> aScale;     // 1xD vector of weights for the data
        private ILArray<double> aInnerProducts;
        private ILArray<double> aBias;      // bias (mean) of the Y data matrix
        private ILArray<double> aK;         // NxN kernel covariance matrix
        private ILArray<double> aInvK;      // NxN inverse kernel covariance matrix
        private double fLogDetK;            // log determinant of K
        private ILArray<double> aAlpha;     // part needed for mean prediction

        public GPLVMFactor(FactorDesc desc)
            : base(desc)
        {
        }

        public override void Initialize()
        {
            base.Initialize(); // Initializes kernel

            // Find the corresponding connection points and data expanders
            deX = (DataExpander)(lConnectionPoints.FindByName(CP_LATENT).ConnectedDataObject);
            deY = (DataExpander)(lConnectionPoints.FindByName(CP_OBSERVED).ConnectedDataObject);
            deX.Expand();
            deY.Expand();

            nN = deY.ExpandedData.Size[0];
            nD = deY.ExpandedData.Size[1];
            nQ = deX.ExpandedData.Size[1];

            // TODO: Should the bias correction be done in data nodes???
            aBias = ILMath.mean(deY.ExpandedData);
            for (int i = 0; i < aBias.Length; i++)
                deY.ExpandedData[ILMath.full, i] = deY.ExpandedData[ILMath.full, i] - aBias[i];

            deX.ExpandedData.a = EstimateX(deY.ExpandedData);

            //_latentgX = ILMath.zeros(_X.Size);
            aScale = ILMath.ones(1, nD);
            aInnerProducts = ILMath.zeros(1, nD);
            ComputeAllGradients();
            deX.Unexpand();
            deY.Unexpand();
        }

        /// <summary>
        /// Estimates latent points through PCA. 
        /// </summary>
        /// <param name="data">The data Y.</param>
        private ILRetArray<double> EstimateX(ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);

                if (!ILMath.any(ILMath.any(ILMath.isnan(data))))
                {
                    ILArray<double> u = ILMath.empty();
                    ILArray<double> v = ILMath.empty();
                    v = GPLVM.Util.EigDec(ILMath.cov(data.T).T, data.Size[1], u);

                    v[ILMath.find(v < 0)] = 0;
                    ILArray<double> tmp = u[ILMath.full, ILMath.r(0, nQ - 1)];
                    tmp = ILMath.multiply(ILMath.multiply(data, tmp), ILMath.diag(1 / ILMath.sqrt(v[ILMath.r(0, nQ - 1)])));
                    return tmp;
                }
                else
                    return 0;
            }
        }

        public double LogLikelihood()
        {
            double L = 0;

            // log normalisation term
            L -= (double)(nD * nN / 2 * ILMath.log(2 * ILMath.pi) + nD / 2 * fLogDetK);

            // log of the exponent; likelihood for each dimension of Y
            for (int i = 0; i < nD; i++)
                L -= (double)(0.5 * ILMath.pow(aScale[i], 2) * 
                    ILMath.multiply(ILMath.multiply(deY.ExpandedData[ILMath.full, i].T, aInvK), 
                    deY.ExpandedData[ILMath.full, i]));

            // Prior of the weights
            L -= (double)(nN * ILMath.sum(ILMath.log(aScale)));

            return L;
        }

        private void UpdateKernelMatrix()
        {
            if (KernelObject != null)
            {
                aK = KernelObject.ComputeKernelMatrix(deX.ExpandedData, deX.ExpandedData);
                aInvK = GPLVM.Util.pdinverse(aK);
                fLogDetK = GPLVM.Util.logdet(aK);

                for (int i = 0; i < nD; i++)
                    aInnerProducts[i] = ILMath.multiply(ILMath.multiply(deY.ExpandedData[ILMath.full, i].T, aInvK), deY.ExpandedData[ILMath.full, i]);

                aAlpha = ILMath.multiply(aInvK, deY.ExpandedData);
            }
            else
                System.Console.WriteLine("No kernel function found! Please add a kernel object!");
        }

        public override void ComputeAllGradients()
        {
            UpdateKernelMatrix();
            
            //KernelObject.ComputeKernelMatrix(deX.ExpandedData, deX.ExpandedData);

            // Precalculate Y scale
            ILArray<double> scaleY = ILMath.zeros(deY.ExpandedData.Size);
            for (int i = 0; i < nD; i++)
                scaleY[ILMath.full, i] = aScale[i] * deY.ExpandedData[ILMath.full, i];
            
            // Derivative of log likelihood w.r.t K
            ILArray<double> dL_dK = -nD / 2 * aInvK + .5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(aInvK, scaleY), scaleY.T), aInvK);
            
            ///////////////////////////////////////////
            // Kernel parameters gradient.
            // It is computed using the full kernel object and then 
            // set to corresponding data nodes of subkernels
            ILArray<double> gParams = KernelObject.LogLikGradientParam(dL_dK);
            KernelObject.SetParametersGradient(gParams);

            ///////////////////////////////////////////
            // X (latent data) gradient
            ILArray<double> gX = KernelObject.LogLikGradientX(deX.ExpandedData, dL_dK);
            deX.Gradient = gX;

            ///////////////////////////////////////////
            // Y ("observed" data) gradient (if needed)
            if (deY.DataNode.DataDesc.Mode != EVariableMode.Observed)
            {
                ILArray<double> gY = ILMath.zeros(deX.ExpandedData.Length, aScale.Length);
                for (int i = 0; i < nQ; i++)
                    gY[ILMath.full, i] = -ILMath.pow(aScale[i], 2) * ILMath.multiply(aInvK, deY.ExpandedData[ILMath.full, i]);
                deY.Gradient = gY;
            }
        }

        public override double FunctionValue()
        {
            return LogLikelihood();
        }
    }

    public class GPLVMFactorBuilder : FactorBuilder
    {
        public GPLVMFactorBuilder()
        {
            Type = "GPLVM";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new GPLVMFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GPLVMFactor.CP_LATENT, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GPLVMFactor.CP_OBSERVED, desc));
            return desc;
        }
    }
}

