using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;
using ILNumerics;
using FactorGraph.Core;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Kernel;
using FactorGraph.DataConnectors;

namespace FactorGraph.FactorNodes
{
    [DataContract(IsReference = true)]
    public class StyleGPLVMNode : FactorNodeWithStyles
    {
        public static string CP_LATENT = "Latent";
        public static string CP_OBSERVED = "Observed";
        public static string CP_SCALE = "Log scale";
        public static string CP_INDUCING = "Inducing";
        public static string CP_BETA = "Beta";
        
        [DataMember()]
        private ApproximationType pApproxType;
        [DataMember()]
        protected DataConnector dcX;
        [DataMember()]
        protected DataConnector dcY;
        [DataMember()]
        protected DataConnector dcLogScale;
        [DataMember()]
        protected DataConnector dcInducing;
        [DataMember()]
        protected DataConnector dcBeta;
        [DataMember()]
        protected int nN; // size of the data set
        [DataMember()]
        protected int nD; // observed space dimantionality
        [DataMember()]
        protected int nQ; // latent space dimentionality
        [DataMember()]
        protected int nInducing = 2;           // number of inducing inputs
        //[DataMember()]
        private ILArray<double> aInnerProducts = ILMath.localMember<double>();
        //[DataMember()]
        private ILArray<double> aBias = ILMath.localMember<double>();      // bias (mean) of the Y data matrix
        //[DataMember()]
        private ILArray<double> aYCentered = ILMath.localMember<double>(); // bias-corrected Y data matrix
        //[DataMember()]
        private ILArray<double> aK = ILMath.localMember<double>();         // NxN kernel covariance matrix
        //[DataMember()]
        private ILArray<double> aKuf = ILMath.localMember<double>();       // NxN kernel covariance matrix
        //[DataMember()]
        private ILArray<double> aKInv = ILMath.localMember<double>();      // NxN inverse kernel covariance matrix
        //[DataMember()]
        private ILArray<double> aAlpha = ILMath.localMember<double>();     // part needed for mean prediction
        //[DataMember()]
        private double fLogDetK;            // log determinant of K
        //[DataMember()]
        protected ILArray<double> aA = ILMath.localMember<double>();       // help matrix for rearranging log-likelihood
        //[DataMember()]
        protected ILArray<double> aAInv = ILMath.localMember<double>();
        //[DataMember()]
        protected double fLogDetA;

        //[DataMember()]
        protected ILArray<double> aKDiag = ILMath.localMember<double>();     // diagonal of the kernel given _X
        //[DataMember()]
        protected ILArray<double> aDDiag = ILMath.localMember<double>();     // help diagonal of the kernel for rearranging fitc log-likelihood
        //[DataMember()]
        protected ILArray<double> aDInv = ILMath.localMember<double>();
        //[DataMember()]
        protected ILArray<double> aDetDiff = ILMath.localMember<double>();


        public StyleGPLVMNode(string sName, IKernel iKernel, ApproximationType aType = ApproximationType.ftc)
            : base(sName, iKernel)
        {
            this.pApproxType = aType;

            dcX = new DataConnector(CP_LATENT);
            DataConnectors.Add(dcX);
            dcY = new DataConnector(CP_OBSERVED);
            DataConnectors.Add(dcY);
            dcLogScale = new LogDataConnector(CP_SCALE);
            DataConnectors.Add(dcLogScale);
            dcInducing = null;
            if (UseInducing())
            {
                dcInducing = new DataConnector(CP_INDUCING);
                DataConnectors.Add(dcInducing);
                dcBeta = new LogDataConnector(CP_BETA);
                DataConnectors.Add(dcBeta);
            }
            lFactorDescs.Add(new FactorDesc(dcX, dcInducing));
        }

        /// Public data connectors
        public DataConnector DataConnectorX
        {
            get { return dcX; }
        }

        public DataConnector DataConnectorY
        {
            get { return dcY; }
        }

        public DataConnector DataConnectorLogScale
        {
            get { return dcLogScale; }
        }

        public DataConnector DataConnectorBeta
        {
            get { return dcBeta; }
        }

        public DataConnector DataConnectorInducing
        {
            get { return dcInducing; }
        }

        public int NumInducingMax
        {
            get { return nInducing; }
            set { nInducing = value; }
        }

        protected int NumInducing
        {
            get { return Math.Min(NumInducingMax, X.S[0]); }
        }

        /// <summary>
        /// Predicate that indicates whether the BETA and inducing latent variables is used
        /// </summary>
        /// <returns></returns>
        public override bool UseInducing()
        {
            return (pApproxType == ApproximationType.dtc || pApproxType == ApproximationType.fitc);
        }

        /// Shortcuts to variables
        protected ILArray<double> X
        {
            get { return dcX.Values; }
            set { dcX.Values = value; }
        }

        protected ILArray<double> Y
        {
            get { return dcY.Values; }
        }

        protected ILArray<double> Scale
        {
            get { return dcLogScale.Values; }
            set { dcLogScale.Values = value; }
        }
       
        protected double Beta
        {
            get { return (double)dcBeta.Values; }
            set { dcBeta.Values = new double[] { value }; }
        }

        public override void Initialize()
        {
            // Initialize kernel
            base.Initialize();

            // Find the corresponding connection points and data expanders
            nN = Y.Size[0];
            nD = Y.Size[1];
            nQ = X.Size[1];

            dcLogScale.ConnectedDataNode.SetValuesSize(new ILSize(new int[] { 1, nD }));
            dcLogScale.Values = ILMath.ones(1, nD);
            dcLogScale.ConnectedDataNode.SetOptimizingMaskAll();
            aInnerProducts.a = ILMath.zeros(1, nD);

            InitializeKernelIndexes();

            PullYFromDataNode();
            if (OptimizingX())
            {
                X = EstimateX(aYCentered);
            }

            if (UseInducing())
            {
                dcBeta.ConnectedDataNode.SetValuesSize(new ILSize(new int[] { 1, 1 }));
                Beta = 1e3;
                dcInducing.ConnectedDataNode.SetValuesSize(new ILSize(new int[] { NumInducing, nQ }));
            }

            ILArray<int> ind = ILMath.empty<int>();
            switch (pApproxType)
            {
                case ApproximationType.ftc:
                    break;
                case ApproximationType.dtc:
                case ApproximationType.fitc:
                    ILMath.sort(ILMath.rand(1, nN), ind);
                    ind = ind[ILMath.r(0, NumInducing - 1)];
                    foreach (FactorDesc desc in lFactorDescs)
                    {
                        desc.ConnectionPointInducing.ConnectedDataNode.CreateInducing(desc.ConnectionPoint.ConnectedDataNode, ind);
                    }
                    break;                
            }

            ComputeAllGradients();
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

        public ILRetArray<double> PredictData(ILInArray<double> testInputs)
        {
            using (ILScope.Enter(testInputs))
            {
                ILArray<double> test = ILMath.check(testInputs);

                ILArray<double> k = ILMath.empty();

                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        k = KernelObject.ComputeKernelMatrix(test, FullLatent, Flag.reconstruction);
                        break;
                    case ApproximationType.dtc:
                        k = KernelObject.ComputeKernelMatrix(test, FullLatentInducing, Flag.reconstruction);
                        break;
                    case ApproximationType.fitc:
                        k = KernelObject.ComputeKernelMatrix(test, FullLatentInducing, Flag.reconstruction);
                        break;
                }

                ILArray<double> y = ILMath.multiply(k, aAlpha);

                for (int i = 0; i < aBias.Length; i++)
                    y[ILMath.full, i] = y[ILMath.full, i] * Scale[i] + aBias[i];

                return y;
            }
        }

        public override double LogLikelihood()
        {
            using (ILScope.Enter())
            {
                double L = 0;

                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        // log normalisation term
                        L -= (double)(nD * nN / 2 * ILMath.log(2 * ILMath.pi) + nD / 2 * fLogDetK);

                        // log of the exponent; likelihood for each dimension of Y
                        for (int i = 0; i < nD; i++)
                            L -= (double)(0.5 * ILMath.multiply(ILMath.multiply(aYCentered[ILMath.full, i].T, aKInv), aYCentered[ILMath.full, i]));
                        break;

                    case ApproximationType.dtc:
                        ILArray<double> KufM = ILMath.multiply(aKuf, aYCentered);
                        ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);

                        L -= (double)(0.5 * (nD * (-(nN - NumInducing) * ILMath.log(Beta) - fLogDetK + fLogDetA)
                            - (ILMath.sum(ILMath.sum(ILMath.multiplyElem(aAInv, KufMKufM)))
                            - ILMath.sum(ILMath.sum(ILMath.multiplyElem(aYCentered, aYCentered)))) * Beta));
                        break;

                    case ApproximationType.fitc:
                        L -= (double)(.5 * nN * nD * ILMath.log(2 * ILMath.pi));
                        ILArray<double> DinvM = ILMath.multiplyElem(ILMath.repmat(aDInv, 1, nD), aYCentered);
                        ILArray<double> KufDinvM = ILMath.multiply(aKuf, DinvM);

                        L -= (double)(.5 * (nD * (ILMath.sum(ILMath.log(aDDiag))
                            - (nN - NumInducing) * ILMath.log(Beta) + aDetDiff) + (ILMath.sum(ILMath.sum(ILMath.multiplyElem(DinvM, aYCentered)))
                            - ILMath.sum(ILMath.sum(ILMath.multiplyElem(ILMath.multiply(aAInv, KufDinvM), KufDinvM)))) * Beta));
                        break;
                }

                // Kernel parameters log-likelihood
                L += base.LogLikelihood();

                // Prior of the weights
                L -= (double)(ILMath.sum(ILMath.log(Scale.T)));

                return L;
            }
        }

        private void UpdateKernelMatrix()
        {
            using (ILScope.Enter())
            {
                if (KernelObject != null)
                {
                    switch (pApproxType)
                    {
                        case ApproximationType.ftc:
                            aK.a = KernelObject.ComputeKernelMatrix(FullLatent, FullLatent);
                            aKInv.a = GPLVM.Util.pinverse(aK);
                            fLogDetK = GPLVM.Util.logdet(aK);

                            for (int i = 0; i < nD; i++)
                                aInnerProducts[i] = ILMath.multiply(ILMath.multiply(aYCentered[ILMath.full, i].T, aKInv), aYCentered[ILMath.full, i]);

                            aAlpha.a = ILMath.multiply(aKInv, aYCentered);
                            break;

                        case ApproximationType.dtc:
                            aK.a = KernelObject.ComputeKernelMatrix(FullLatentInducing, FullLatentInducing);
                            aKuf.a = KernelObject.ComputeKernelMatrix(FullLatentInducing, FullLatent);
                            aKInv.a = Util.pinverse(aK);
                            fLogDetK = Util.logdet(aK);

                            aA.a = (1 / Beta) * aK + ILMath.multiply(aKuf, aKuf.T);
                            // This can become unstable when K_uf2 is low rank.
                            aAInv.a = Util.pinverse(aA);
                            fLogDetA = Util.logdet(aA);

                            for (int i = 0; i < nD; i++)
                                aInnerProducts[i] = Beta * (ILMath.multiply(aYCentered[ILMath.full, i].T, aYCentered[ILMath.full, i])
                                    - ILMath.multiply(ILMath.multiply(ILMath.multiply(aKuf, aYCentered[ILMath.full, i]).T, aAInv), ILMath.multiply(aKuf, aYCentered[ILMath.full, i])));

                            aAlpha.a = ILMath.multiply(ILMath.multiply(aAInv, aKuf), aYCentered);
                            break;

                        case ApproximationType.fitc:
                            aK.a = KernelObject.ComputeKernelMatrix(FullLatentInducing, FullLatentInducing);
                            aKuf.a = KernelObject.ComputeKernelMatrix(FullLatentInducing, FullLatent);
                            aKInv.a = Util.pinverse(aK);
                            fLogDetK = Util.logdet(aK);

                            aKDiag.a = KernelObject.ComputeDiagonal(FullLatent);

                            aDDiag.a = 1 + Beta * aKDiag - Beta * ILMath.sum(ILMath.multiplyElem(aKuf, ILMath.multiply(aKInv, aKuf)), 0).T;
                            aDInv.a = 1 / aDDiag; //ILMath.diag(1 / _diagD);
                            //ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiply(_Kuf, _invD), _Kuf.T);
                            ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiplyElem(aKuf, ILMath.repmat(aDInv.T, NumInducing, 1)), aKuf.T);
                            aA.a = (1 / Beta) * aK + KufDinvKuf;

                            // This can become unstable when K_ufDinvK_uf is low rank.
                            aAInv.a = Util.pinverse(aA);
                            fLogDetA = Util.logdet(aA);

                            aDetDiff.a = -ILMath.log(Beta) * NumInducing + ILMath.log(ILMath.det(ILMath.eye(NumInducing, NumInducing) + Beta * ILMath.multiply(KufDinvKuf, aKInv)));

                            for (int i = 0; i < nD; i++)
                            {
                                ILArray<double> DinvM = ILMath.multiplyElem(aDInv, aYCentered[ILMath.full, i]);
                                ILArray<double> KufDinvM = ILMath.multiply(aKuf, DinvM);
                                aInnerProducts[i] = Beta * ILMath.multiply(DinvM.T, aYCentered[ILMath.full, i]) - ILMath.multiply(ILMath.multiply(KufDinvM.T, aAInv), KufDinvM);
                            }

                            aAlpha.a = ILMath.multiply(ILMath.multiplyElem(ILMath.multiply(aAInv, aKuf), ILMath.repmat(aDInv.T, NumInducing, 1)), aYCentered);
                            break;
                    }
                }
                else
                    System.Console.WriteLine("No kernel function found! Please add a kernel object!");
            }
        }

        protected bool OptimizingX()
        {
            return ILMath.anyall(dcX.ConnectedDataNode.GetOptimizingMask());
        }

        protected bool OptimizingY()
        {
            return ILMath.anyall(dcY.ConnectedDataNode.GetOptimizingMask());
        }

        public override void ComputeAllGradients()
        {
            using (ILScope.Enter())
            {
                UpdateKernelMatrix();

                ILArray<double> dL_dK = ILMath.empty(); // derivative of log likelihood w.r.t K
                ILArray<double> dL_dKuf = ILMath.empty(); // derivative of log likelihood w.r.t Kuf

                double gBeta = 0;

                ILArray<double> gKernelParams = ILMath.empty(); // gradient of the kernel parameters
                ILArray<double> gX = ILMath.empty(); // gradient of X
                ILArray<double> gXu = ILMath.empty(); // gradient of Xu

                ILCell dL_dX_dL_dXuf = ILMath.cell();

                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        ///////////////////////////////////////////
                        // Derivative of log likelihood w.r.t K
                        dL_dK = -nD / 2 * aKInv + 0.5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(aKInv, aYCentered), aYCentered.T), aKInv);

                        ///////////////////////////////////////////
                        // X (latent data) gradient
                        if (OptimizingX())
                        {
                            gX = KernelObject.LogLikGradientX(FullLatent.C, dL_dK);
                        }

                        ///////////////////////////////////////////
                        // Kernel parameters gradient.
                        // It is computed using the full kernel object and then 
                        // set to corresponding data nodes of subkernels
                        gKernelParams = KernelObject.LogLikGradientParam(dL_dK);
                        break;

                    case ApproximationType.dtc:
                        ILArray<double> KufM = ILMath.multiply(aKuf, aYCentered);
                        ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);
                        ILArray<double> invAKufMKufM = ILMath.multiply(aAInv, KufMKufM);
                        ILArray<double> invAKufMKufMinvA = ILMath.multiply(invAKufMKufM, aAInv);

                        dL_dK = .5 * (nD * (aKInv - (1 / Beta) * aAInv) - invAKufMKufMinvA);

                        ILArray<double> invAKuf = ILMath.multiply(aAInv, aKuf);

                        dL_dKuf = -nD * invAKuf - Beta * (ILMath.multiply(invAKufMKufM, invAKuf) - (ILMath.multiply(ILMath.multiply(aAInv, KufM), aYCentered.T)));

                        dL_dX_dL_dXuf = KernelObject.LogLikGradientX(FullLatentInducing, dL_dK, FullLatent, dL_dKuf);
                        gXu = dL_dX_dL_dXuf.GetArray<double>(0);
                        gX = dL_dX_dL_dXuf.GetArray<double>(1);

                        gKernelParams = KernelObject.LogLikGradientParam(dL_dKuf) + KernelObject.LogLikGradientParam(dL_dK);

                        gBeta = (double)(.5 * (nD * ((nN - NumInducing) / Beta + ILMath.sum(ILMath.sum(ILMath.multiplyElem(aAInv, aK))) / (Beta * Beta))
                            + ILMath.sum(ILMath.sum(ILMath.multiplyElem(invAKufMKufMinvA, aK))) / Beta
                            + (ILMath.trace(invAKufMKufM) - ILMath.sum(ILMath.sum(ILMath.multiplyElem(aYCentered, aYCentered))))));

                        gBeta *= Beta; // because of the log
                        break;

                    case ApproximationType.fitc:
                        ILArray<double> KufDinvM = ILMath.multiply(ILMath.multiplyElem(aKuf, ILMath.repmat(aDInv.T, NumInducing, 1)), aYCentered);
                        ILArray<double> AinvKufDinvM = ILMath.multiply(aAInv, KufDinvM);
                        ILArray<double> diagKufAinvKufDinvMMT = ILMath.sum(ILMath.multiplyElem(aKuf, ILMath.multiply(ILMath.multiply(aAInv, KufDinvM), aYCentered.T)), 0).T;
                        ILArray<double> AinvKufDinvMKufDinvMAinv = ILMath.multiply(AinvKufDinvM, AinvKufDinvM.T);
                        ILArray<double> diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf = ILMath.sum(ILMath.multiplyElem(aKuf, ILMath.multiply(nD * aAInv + Beta * AinvKufDinvMKufDinvMAinv, aKuf)), 0).T;
                        ILArray<double> invKuuKuf = ILMath.multiply(aKInv, aKuf);
                        ILArray<double> invKuuKufDinv = ILMath.multiplyElem(invKuuKuf, ILMath.repmat(aDInv.T, NumInducing, 1));
                        ILArray<double> diagMMT = ILMath.sum(ILMath.multiplyElem(aYCentered, aYCentered), 1);

                        ILArray<double> diagQ = -nD * aDDiag + Beta * diagMMT + diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf - 2 * Beta * diagKufAinvKufDinvMMT;

                        dL_dK = .5 * (nD * (aKInv - aAInv / Beta) - AinvKufDinvMKufDinvMAinv
                            + Beta * ILMath.multiply(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, NumInducing, 1)), invKuuKufDinv.T));

                        dL_dKuf = -Beta * ILMath.multiplyElem(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, NumInducing, 1)), ILMath.repmat(aDInv.T, NumInducing, 1))
                            - nD * ILMath.multiplyElem(ILMath.multiply(aAInv, aKuf), ILMath.repmat(aDInv.T, NumInducing, 1))
                            - Beta * ILMath.multiplyElem(ILMath.multiply(AinvKufDinvMKufDinvMAinv, aKuf), ILMath.repmat(aDInv.T, NumInducing, 1))
                            + Beta * ILMath.multiplyElem(ILMath.multiply(ILMath.multiply(aAInv, KufDinvM), aYCentered.T), ILMath.repmat(aDInv.T, NumInducing, 1));

                        ILArray<double> Kstar = ILMath.divide(.5 * diagQ * Beta, ILMath.multiplyElem(aDDiag, aDDiag));

                        dL_dX_dL_dXuf = KernelObject.LogLikGradientX(FullLatentInducing, dL_dK, FullLatent, dL_dKuf);
                        gXu = dL_dX_dL_dXuf.GetArray<double>(0);
                        gX = dL_dX_dL_dXuf.GetArray<double>(1);

                        ILArray<double> diagGX = KernelObject.DiagGradX(FullLatent);
                        for (int i = 0; i < nN; i++)
                            gX[i, ILMath.full] += diagGX[i, ILMath.full] * Kstar[i];

                        gKernelParams = KernelObject.LogLikGradientParam(dL_dKuf) + KernelObject.LogLikGradientParam(dL_dK);
                        gKernelParams += KernelObject.DiagGradParam(FullLatent, Kstar);

                        gBeta = (double)-ILMath.sum(Kstar) / (Beta * Beta);

                        gBeta *= Beta; // because of the log
                        break;
                }

                // Gradient of the weights
                ILArray<double> gSc = 1 / ILMath.multiplyElem(Scale, (aInnerProducts - 1));
                gSc = ILMath.multiplyElem(Scale, gSc); // because of the log
                dcLogScale.SetGradient(gSc);

                // Push all gradients for latent variables
                foreach (FactorDesc desc in lFactorDescs)
                {
                    if (ILMath.anyall(desc.ConnectionPoint.ConnectedDataNode.GetOptimizingMask()))
                    {
                        desc.ConnectionPoint.SetGradient(gX[ILMath.full, desc.IndexesInFullData]);
                        if (UseInducing())
                        {
                            desc.ConnectionPointInducing.SetGradient(gXu[ILMath.full, desc.IndexesInFullData]);
                        }
                    }
                }

                // Inducing variables gradient
                if (UseInducing())
                {
                    dcBeta.SetGradient(new double[] { gBeta });
                }

                // Y ("observed" data) gradient (if needed)
                if (OptimizingY())
                {
                    ILArray<double> gY = ILMath.zeros(FullLatent.S[0], Scale.Length);
                    for (int i = 0; i < nQ; i++)
                        gY[ILMath.full, i] = -ILMath.pow(Scale[i], 2) * ILMath.multiply(aKInv, aYCentered[ILMath.full, i]);
                    dcY.SetGradient(gY);
                }

                // Kernel parameters gradient
                this.PushKernelParamsGradientsToDataConnector(gKernelParams);
            }
        }

        protected void PullYFromDataNode()
        {
            using (ILScope.Enter())
            {
                aBias.a = ILMath.mean(Y);
                aYCentered.a = ILMath.zeros(Y.Size);
                for (int i = 0; i < aBias.Length; i++)
                    aYCentered[ILMath.full, i] = (Y[ILMath.full, i] - aBias[i]) / Scale[i];
            }
        }

        public override void PullDataFromDataNodes()
        {
            using (ILScope.Enter())
            {
                base.PullDataFromDataNodes();
                PullYFromDataNode();
                // Reset latents for lazy recalculation
                aFullLatent.a = ILMath.empty();
                aFullLatentInducing.a = ILMath.empty();
            }
        }

        public override void OnDeserializedMethod()
        {
            using (ILScope.Enter())
            {
                base.OnDeserializedMethod();
                PullDataFromDataNodes();
                aInnerProducts.a = ILMath.zeros(1, nD);
                UpdateKernelMatrix();
            }
        }
    }
}
