using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GraphicalModel;

namespace GraphicalModel.Factors.DynamicsFactors
{
    public class FirstOrderMarkovDynamicsFactor : Factor
    {
        public static string CP_TYPE = "Markov";
        protected DataExpander deX; // X is nN*dQ dimensions
        protected int nQ; // latent space dimentionality
        protected int nN; // size of the data set
        private double fLogDetKx;            // log determinant of Kx
        private ILArray<double> aKx;         // NxN kernel covariance matrix
        private ILArray<double> aInvKx;      // NxN inverse kernel covariance matrix

        public FirstOrderMarkovDynamicsFactor(FactorDesc desc)
            : base(desc)
        {
        }

        /// <summary>
        /// Initialized the data expanders(with the partial gradients)
        /// </summary>
        public override void Initialize()
        {
            base.Initialize(); // Initializes kernel

            // Find the corresponding connection points and data expanders
            deX = (DataExpander)(lConnectionPoints.FindByName(CP_TYPE).ConnectedDataObject);
            deX.Expand();

            //ASK WHETHER THE No. OF OBSERVATIONS ALWAYS SAME AS LATENT PTS?
            nN = deX.ExpandedData.Size[0];
            nQ = deX.ExpandedData.Size[1];

            ComputeAllGradients();
            //deX.Unexpand();
        }

        /// <summary>
        /// Calculates the Log Likelihood of the Dynamics Prior
        /// </summary>
        /// <returns> Log Likelihood Value</returns>
        public double LogLikelihood()
        {
            double L = 0;

            //Prior on x1
            L += (double)((nQ / 2 * ILMath.log(2 * ILMath.pi)) - (0.5 * ILMath.multiply(
                    deX.ExpandedData[0, ILMath.full], deX.ExpandedData[0, ILMath.full].T)));

            // log normalisation term
            L -= (double)(nQ * (nN - 1) / 2 * ILMath.log(2 * ILMath.pi) + nQ / 2 * fLogDetKx);

            // exponent term of the likelihood
            for (int i = 0; i < nQ; i++)
                L -= (double)(0.5 * ILMath.multiply(ILMath.multiply(deX.ExpandedData["1:end", i].T,
                        aInvKx), deX.ExpandedData["1:end", i]));

            return L;
        }

        /// <summary>
        /// Recalculates the kernal values for the new data and updates the Dynamics kernal matrix
        /// </summary>
        private void UpdateKernelMatrix()
        {
            if (KernelObject != null)
            {
                aKx = KernelObject.ComputeKernelMatrix(deX.ExpandedData[ILMath.r(0, ILMath.end - 1), ILMath.full],
                        deX.ExpandedData[ILMath.r(0, ILMath.end - 1), ILMath.full]);
                aInvKx = GPLVM.Util.pdinverse(aKx);
                fLogDetKx = GPLVM.Util.logdet(aKx);

            }
            else
                System.Console.WriteLine("No kernel function found! Please add a kernel object!");
        }

        /// <summary>
        /// Computes the Gradients of the Dynamics Likelihood wrt the parameters and latent variables
        /// </summary>
        public override void ComputeAllGradients()
        {
            UpdateKernelMatrix();

            // Derivative of log likelihood w.r.t Kx
            ILArray<double> dL_dKx = -nQ / 2 * aInvKx + .5 * ILMath.multiply(ILMath.multiply(
                                    ILMath.multiply(aInvKx, deX.ExpandedData["1:end", ILMath.full]),
                                    deX.ExpandedData["1:end", ILMath.full].T), aInvKx);

            //////TODO{

            ///////////////////////////////////////////
            // Kernel parameters gradient.
            // It is computed using the full kernel object and then 
            // set to corresponding data nodes of subkernels
            ILArray<double> gParams = KernelObject.LogLikGradientParam(dL_dKx);
            KernelObject.SetParametersGradient(gParams);

            ///////////////////////////////////////////
            // X (latent data) gradient
            ILArray<double> gX = KernelObject.LogLikGradientX(deX.ExpandedData[ILMath.r(0, ILMath.end - 1), ILMath.full], dL_dKx);
            ILArray<double> tmp = new double[3] { 0, 0, 0 };
            deX.Gradient = gX.Concat(tmp.T, 0);
            ////} end TODO

        }

        /// <summary>
        /// Returns the value of Log Likelihood of the Dynamics Factor.
        /// </summary>
        /// <returns></returns>
        public override double FunctionValue()
        {
            return LogLikelihood();
        }

        //COMPLETE    --COPIED DMYTRO's
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

    }
}
