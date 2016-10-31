using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using GPLVM.Kernel;
using GPLVM.Dynamics.Topology;
using GPLVM.GPLVM;
using FactorGraph.Core;
using FactorGraph.DataConnectors;
using FactorGraph.DataNodes;
using FactorGraph.FactorNodes.Common;

namespace FactorGraph.FactorNodes
{
    [DataContract(IsReference = true)]
    public class StyleAccelerationDynamicsNode : FactorNodeWithStyles
    {
        [DataMember()]
        protected List<GPPredictionStruct> predictionStruct = new List<GPPredictionStruct>();

        [DataMember()]
        public static string CP_STATE_SPACE = "State space";
        [DataMember()]
        public static string CP_BETA = "Beta";

        [DataMember()]
        protected DataConnector dcX;
        [DataMember()]
        protected DataConnector dcBeta;

        [DataMember()]
        private int nInducing = 200;   // number of inducing inputs

        [DataMember()]
        private ApproximationType pApproxType;
        [DataMember()]
        private int nD;                     // dimension of the data
        [DataMember()]
        private int nN;                     // number of data points
        [DataMember()]
        private int nDyn;                   // dimensions of the dynamics channels (nD as default, equals to aDynamicsIndexes.Length)

        [DataMember()]
        private ILArray<int> aDynamicsIndexes = ILMath.localMember<int>();    // indexes of the dynamics part. Full as default, part of X in case of coupled dynamics
        //[DataMember()]
        private ILArray<double> aXIn = ILMath.localMember<double>();        // Nxq matrix of latent points
        //[DataMember()]
        private ILArray<double> aXOut = ILMath.localMember<double>();       // Nxq matrix of data points
        //[DataMember()]
        private ILArray<double> aXInducing = ILMath.localMember<double>();  // nInducing-x-nQ matrix of latent inducing inputs
        [DataMember()]
        private ILArray<double> aScale = ILMath.localMember<double>();     // 1xD vector of weights for the data
        [DataMember()]
        private GPTopology pTopology;       // Topology of segments connectivity
        //[DataMember()]
        private ILArray<int> aMapOrder3 = ILMath.localMember<int>();    // [X-2, X-1] -> [X] mapping indexes for _X
        //[DataMember()]
        private ILArray<int> aMapStart2 = ILMath.localMember<int>();    // [X, X+1] map where [X] does not have parents
        //[DataMember()]
        private ILArray<int> aMapStart1 = ILMath.localMember<int>();    // [X] map where [X] does not have parents

        //[DataMember()]
        private ILArray<double> aKuf = ILMath.localMember<double>();       // NxN kernel covariance matrix
        //[DataMember()]
        private ILArray<double> aA = ILMath.localMember<double>();         // help matrix for rearranging log-likelihood
        //[DataMember()]
        private ILArray<double> aAInv = ILMath.localMember<double>();
        //[DataMember()]
        private double fLogDetA;

        //[DataMember()]
        private ILArray<double> aDDiag = ILMath.localMember<double>();     // help diagonal of the kernel for rearranging fitc log-likelihood
        //[DataMember()]
        private ILArray<double> aDInv = ILMath.localMember<double>();
        //[DataMember()]
        private ILArray<double> aDetDiff = ILMath.localMember<double>();

        //[DataMember()]
        private ILArray<double> aK = ILMath.localMember<double>();         // NxN kernel covariance matrix
        //[DataMember()]
        private ILArray<double> aKInv = ILMath.localMember<double>();      // NxN inverse kernel covariance matrix
        //[DataMember()]
        private double fKLogDet;            // log determinant of K
        //[DataMember()]
        private ILArray<double> aAlpha = ILMath.localMember<double>();     // part needed for mean prediction

        [DataMember()]
        private ILArray<int> aInd = ILMath.localMember<int>();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// GPVelocity is a continius second order non-linear autoregressiv model provided by a Gaussian process. The actual time step x_n 
        /// depends on the two time steps before x_n-1, x_n-2 via non-linear mapping function g([x_n-1, x_n-2]) drawn from a Gaussian 
        /// process and some noise e (x_n = g([x_n-1, x_n-2]) + e; g([x_n-1, x_n-2]) ~ GP(m([x_n-1, x_n-2]),k([x_n-1, x_n-2],x')); 
        /// where m(x_n-1) is the mean function and k([x_n-1, x_n-2],x') the kernel function).
        /// This class provides the full fit (FTC), deterministic training conditional (DTC) and fully independent training conditional 
        /// (FITC) of the data.
        /// </remarks>
        /// <param name="aType">The approximation type.</param>
        public StyleAccelerationDynamicsNode(string sName, IKernel iKernel, ApproximationType aType = ApproximationType.ftc)
            : base(sName, iKernel)
        {
            this.pApproxType = aType;

            dcX = new DataConnector(CP_STATE_SPACE);
            DataConnectors.Add(dcX);
            if (UseInducing())
            {
                dcBeta = new LogDataConnector(CP_BETA);
                DataConnectors.Add(dcBeta);
            }

            pTopology = new GPTopology();

            lFactorDescs.Add(new FactorDesc(dcX));
        }

        /// Public data connectors
        public DataConnector DataConnectorX
        {
            get { return dcX; }
        }

        public DataConnector DataConnectorBeta
        {
            get { return dcBeta; }
        }

        public List<GPPredictionStruct> PredictionStructs
        {
            get { return predictionStruct; }
        }

        /// <summary>
        /// Predicate that indicates whether the BETA and inducing variables are used
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
        }

        protected double Beta
        {
            get { return (double)dcBeta.Values; } 
            set { dcBeta.Values = new double[] { value }; }
        }

        protected ILArray<int> Segments
        {
            get
            {
                if (dcX.ConnectedDataNode is IDataNodeWithSegments)
                {
                    return ((IDataNodeWithSegments)dcX.ConnectedDataNode).Segments;
                }
                else
                {
                    return new int[] { 0 };
                }
            }
        }

        public ILRetArray<int> DynamicsIndexes
        {
            get
            {
                using (ILScope.Enter())
                {
                    if (aDynamicsIndexes.IsEmpty)
                    {
                        aDynamicsIndexes.a = ILMath.counter<int>(0, 1, FullLatent.S[1]);
                    }
                    return aDynamicsIndexes;
                }
            }
            set
            {
                using (ILScope.Enter())
                {
                    aDynamicsIndexes.a = value.C;
                }
            }
        }

        /// <summary>
        /// Number of inducing variables
        /// </summary>
        public int NumInducingMax
        {
            get
            {
                return nInducing;
            }
            set
            {
                nInducing = value;
            }
        }

        protected int NumInducing
        {
            get { return Math.Min(NumInducingMax, X.S[0] - Segments.Length * 2); }
        }

        /// <summary>
        /// Initialize the object. 
        /// </summary>
        public override void Initialize()
        {
            // Initialize kernel
            base.Initialize();

            nN = FullLatent.Size[0];
            nD = FullLatent.Size[1];
            nDyn = DynamicsIndexes.Length;

            ILArray<int> segmentSize = ILMath.toint32(Segments[ILMath.r(1, ILMath.end)] - Segments[ILMath.r(0, ILMath.end - 1)]);
            segmentSize.a = Util.Concatenate<int>(segmentSize, ILMath.toint32(nN - Segments[ILMath.end]));
            pTopology = new GPTopology();
            for (int i = 0; i < Segments.Length; i++)
            {
                pTopology.AddSegment((int)segmentSize[i]);
            }

            //InitializeKernelIndexes();
            CreateInOutMaps();

            aScale.a = ILMath.ones(1, FullLatent.Size[1]);

            aInd.a = ILMath.empty<int>();
            switch (pApproxType)
            {
                case ApproximationType.ftc:
                    aXInducing.a = ILMath.empty();
                    nInducing  = 0;
                    break;
                case ApproximationType.dtc:
                    ILMath.sort(ILMath.rand(1, nN - Segments.Length * 2), aInd);
                    aInd.a = aInd[ILMath.r(0, NumInducing - 1)];
                    break;
                case ApproximationType.fitc:
                    ILMath.sort(ILMath.rand(1, nN - Segments.Length * 2), aInd);
                    aInd.a = aInd[ILMath.r(0, NumInducing - 1)];
                    break;
            }
            if (UseInducing())
            {
                Beta = 1e3;
            }

            ComputeAllGradients();
        }

        protected void CreateInOutMaps()
        {
            aMapOrder3.a = pTopology.GetInOutMap(3);
            aMapStart2.a = pTopology.GetStartingPointsMap(2);
            aMapStart1.a = pTopology.GetStartingPointsMap(1);
        }

        /// <summary>
        /// Computes the negative log likelihood of the object. 
        /// </summary>
        /// <returns>Negative log likelihood of the object.</returns>
        public override double LogLikelihood()
        {
            using (ILScope.Enter())
            {
                double L = 0;
                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        L -= (double)(nDyn * nN / 2 * ILMath.log(2 * ILMath.pi) + nDyn / 2 * fKLogDet);
                        for (int d = 0; d < nDyn; d++)
                            L -= (double)(0.5 * ILMath.multiply(ILMath.multiply(aXOut[ILMath.full, d].T, aKInv), aXOut[ILMath.full, d]));
                        break;

                    case ApproximationType.dtc:
                        ILArray<double> KufM = ILMath.multiply(aKuf, aXOut);
                        ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);

                        L -= (double)(.5 * (nDyn * (-(nN - NumInducing) * ILMath.log(Beta) - fKLogDet + fLogDetA)
                            - (ILMath.sum(ILMath.sum(ILMath.multiplyElem(aAInv, KufMKufM)))
                            - ILMath.sum(ILMath.sum(ILMath.multiplyElem(aXOut, aXOut)))) * Beta));
                        break;

                    case ApproximationType.fitc:
                        L -= (double)(.5 * nN * nDyn * ILMath.log(2 * ILMath.pi));
                        ILArray<double> DinvM = ILMath.multiplyElem(ILMath.repmat(aDInv, 1, nDyn), aXOut);
                        ILArray<double> KufDinvM = ILMath.multiply(aKuf, DinvM);

                        L -= (double)(.5 * (nDyn * (ILMath.sum(ILMath.log(aDDiag))
                            - (nN - NumInducing) * ILMath.log(Beta) + aDetDiff) + (ILMath.sum(ILMath.sum(ILMath.multiplyElem(DinvM, aXOut)))
                            - ILMath.sum(ILMath.sum(ILMath.multiplyElem(ILMath.multiply(aAInv, KufDinvM), KufDinvM)))) * Beta));

                        break;
                }

                // Correlation between [0] and [1] points for every segment (as they are not in the Markow chain prediction)
                ILArray<double> XDiff = FullLatent[aMapStart2[ILMath.full, 1], ILMath.full] - FullLatent[aMapStart2[ILMath.full, 0], ILMath.full];
                L -= (double)(aMapStart2.S[0] * nDyn / 2 * ILMath.log(2 * ILMath.pi) - 0.5 * ILMath.sum(ILMath.sum(ILMath.multiplyElem(XDiff, XDiff))));

                // Gaussian prior likelihood for [0] point of every segment
                L -= (double)(aMapStart1.S[0] * nDyn / 2 * ILMath.log(2 * ILMath.pi) -
                    0.5 * ILMath.sum(ILMath.sum(ILMath.multiplyElem(FullLatent[aMapStart1, ILMath.full], FullLatent[aMapStart1, ILMath.full]))));

                L -= (double)(ILMath.sum(KernelObject.LogParameter));

                return L;
            }
        }

        /// <summary>
        /// Calculate and push all gradients
        /// </summary>
        public void PushAllGradients()
        {
            using (ILScope.Enter())
            {
                double gBeta = 0;

                ILArray<double> dL_dK = ILMath.empty(); // derivative of log likelihood w.r.t K
                ILArray<double> dL_dKuf = ILMath.empty(); // derivative of log likelihood w.r.t Kuf

                ILArray<double> gXin = ILMath.empty(); // gradient of X
                ILArray<double> gXout = ILMath.empty(); // gradient of Xu
                ILArray<double> gKernelParam = ILMath.empty(); // gradient of the kernel parameters

                ILCell dL_dX_dL_dXuf = ILMath.cell();

                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        dL_dK = -nDyn / 2 * aKInv + 0.5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(aKInv, aXOut), aXOut.T), aKInv);
                        gXin = KernelObject.LogLikGradientX(aXIn, dL_dK);

                        gXout = ILMath.zeros(aXOut.Size);
                        for (int d = 0; d < nDyn; d++)
                            gXout[ILMath.full, d] = -ILMath.multiply(aKInv, aXOut[ILMath.full, d]);
                        gKernelParam = KernelObject.LogLikGradientParam(dL_dK);
                        break;

                    case ApproximationType.dtc:
                        ILArray<double> KufM = ILMath.multiply(aKuf, aXOut);
                        ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);
                        ILArray<double> invAKufMKufM = ILMath.multiply(aAInv, KufMKufM);
                        ILArray<double> invAKufMKufMinvA = ILMath.multiply(invAKufMKufM, aAInv);

                        dL_dK = .5 * (nDyn * (aKInv - (1 / Beta) * aAInv) - invAKufMKufMinvA);

                        ILArray<double> invAKuf = ILMath.multiply(aAInv, aKuf);

                        dL_dKuf = -nDyn * invAKuf - Beta * (ILMath.multiply(invAKufMKufM, invAKuf) - (ILMath.multiply(ILMath.multiply(aAInv, KufM), aXOut.T)));

                        dL_dX_dL_dXuf = KernelObject.LogLikGradientX(aXInducing, dL_dK, aXIn, dL_dKuf);

                        gXin = dL_dX_dL_dXuf.GetArray<double>(1);

                        ILArray<double> AinvKuf = ILMath.multiply(Util.pinverse((1 / Beta) * aK + ILMath.multiply(aKuf, aKuf.T)), aKuf);

                        gXout = ILMath.zeros(aXOut.Size);
                        for (int i = 0; i < nDyn; i++)
                        {
                            gXout[ILMath.full, i] = -Beta * aXOut[ILMath.full, i]
                                + Beta * ILMath.multiply(aKuf.T, ILMath.multiply(AinvKuf, aXOut[ILMath.full, i]));
                        }

                        gKernelParam = KernelObject.LogLikGradientParam(dL_dKuf) + KernelObject.LogLikGradientParam(dL_dK);

                        gBeta = (double)(.5 * (nDyn * ((nN - NumInducing) / Beta + ILMath.sum(ILMath.sum(ILMath.multiplyElem(aAInv, aK))) / Beta * Beta))
                            + ILMath.sum(ILMath.sum(ILMath.multiplyElem(invAKufMKufMinvA, aK))) / Beta
                            + (ILMath.trace(invAKufMKufM) - ILMath.sum(ILMath.sum(ILMath.multiplyElem(aXOut, aXOut)))));
                        gBeta *= Beta; // because of the log
                        break;

                    case ApproximationType.fitc:
                        ILArray<double> KufDinvM = ILMath.multiply(ILMath.multiplyElem(aKuf, ILMath.repmat(aDInv.T, NumInducing, 1)), aXOut);
                        ILArray<double> KufDinvMKufDinvMT = ILMath.multiply(KufDinvM, KufDinvM.T);
                        ILArray<double> AinvKufDinvM = ILMath.multiply(aAInv, KufDinvM);
                        ILArray<double> diagKufAinvKufDinvMMT = ILMath.sum(ILMath.multiplyElem(aKuf, ILMath.multiply(ILMath.multiply(aAInv, KufDinvM), aXOut.T)), 0).T;
                        ILArray<double> AinvKufDinvMKufDinvMAinv = ILMath.multiply(AinvKufDinvM, AinvKufDinvM.T);
                        ILArray<double> diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf = ILMath.sum(ILMath.multiplyElem(aKuf, ILMath.multiply(nDyn * aAInv + Beta * AinvKufDinvMKufDinvMAinv, aKuf)), 0).T;
                        ILArray<double> invKuuKuf = ILMath.multiply(aKInv, aKuf);
                        ILArray<double> invKuuKufDinv = ILMath.multiplyElem(invKuuKuf, ILMath.repmat(aDInv.T, NumInducing, 1));
                        ILArray<double> diagMMT = ILMath.sum(ILMath.multiplyElem(aXOut, aXOut), 1);

                        ILArray<double> diagQ = -nDyn * aDDiag + Beta * diagMMT + diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf - 2 * Beta * diagKufAinvKufDinvMMT;

                        dL_dK = .5 * (nDyn * (aKInv - aAInv / Beta) - AinvKufDinvMKufDinvMAinv
                            + Beta * ILMath.multiply(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, NumInducing, 1)), invKuuKufDinv.T));

                        dL_dKuf = -Beta * ILMath.multiplyElem(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, NumInducing, 1)), ILMath.repmat(aDInv.T, NumInducing, 1))
                            - nDyn * ILMath.multiplyElem(ILMath.multiply(aAInv, aKuf), ILMath.repmat(aDInv.T, NumInducing, 1))
                            - Beta * ILMath.multiplyElem(ILMath.multiply(AinvKufDinvMKufDinvMAinv, aKuf), ILMath.repmat(aDInv.T, NumInducing, 1))
                            + Beta * ILMath.multiplyElem(ILMath.multiply(ILMath.multiply(aAInv, KufDinvM), aXOut.T), ILMath.repmat(aDInv.T, NumInducing, 1));

                        ILArray<double> Kstar = ILMath.divide(.5 * diagQ * Beta, ILMath.multiplyElem(aDDiag, aDDiag));

                        dL_dX_dL_dXuf = KernelObject.LogLikGradientX(aXInducing, dL_dK, aXIn, dL_dKuf);
                        gXin = dL_dX_dL_dXuf.GetArray<double>(1);

                        ILArray<double> diagGX = KernelObject.DiagGradX(aXIn);
                        for (int i = 0; i < aXIn.S[0]; i++)
                            gXin[i, ILMath.full] += diagGX[i, ILMath.full] * Kstar[i];

                        ILArray<double> KufDinv = ILMath.multiplyElem(aKuf, ILMath.repmat(aDInv.T, NumInducing, 1));
                        ILArray<double> AinvKufDinv = ILMath.multiply(aAInv, KufDinv);

                        gXout = ILMath.zeros(aXOut.Size);
                        for (int i = 0; i < nDyn; i++)
                        {
                            gXout[ILMath.full, i] = -Beta * ILMath.multiplyElem(aDInv, aXOut[ILMath.full, i])
                                + Beta * ILMath.multiply(ILMath.multiply(KufDinv.T, AinvKufDinv), aXOut[ILMath.full, i]);
                        }

                        gKernelParam = KernelObject.LogLikGradientParam(dL_dKuf) + KernelObject.LogLikGradientParam(dL_dK);
                        gKernelParam += KernelObject.DiagGradParam(aXIn, Kstar);

                        gBeta = (double)-ILMath.sum(Kstar) / (Beta * Beta);

                        gBeta *= Beta; // because of the log
                        break;
                }

                // Deal with the fact that X appears in the *target* for the dynamics.

                int qp = gXin.Size[1];
                ILArray<double> gX = ILMath.zeros(nN, qp);

                // Setting gradients for Xin set of points ([0..end-2] for every segment)
                // As the kernel gradient is 2-nd order Markov chain gradient, combine the corresponding values to get the gradient w.r.t the latent points X
                gX = Util.IndexAccum(gXin[ILMath.full, ILMath.r(ILMath.end - nD + 1, ILMath.end)], aMapOrder3[ILMath.full, 1]);
                gX = Util.ResizeAdd(gX, Util.IndexAccum(gXin[ILMath.full, ILMath.r(0, nD - 1)], aMapOrder3[ILMath.full, 0]));

                // Setting gradients for Xout set of points ([2..end] for every segment)
                ILArray<double> gXoutFull = ILMath.zeros(gXout.S);
                gXoutFull[ILMath.full, DynamicsIndexes] = gXout; // make a full gX with gradient values only on the columns being optimized
                gX = Util.ResizeAdd(gX, Util.IndexAccum(gXoutFull, aMapOrder3[ILMath.full, ILMath.end]));

                // TODO: rewrite with Util.IndexAccum()
                // Gradient of [1] point of every segment is corrected to X difference with previous X value
                gX[aMapStart2[ILMath.full, 1], ILMath.full] -= FullLatent[aMapStart2[ILMath.full, 1], ILMath.full] - FullLatent[aMapStart2[ILMath.full, 0], ILMath.full];
                // Gradient of [0] point of every segment is corrected to X difference with next X value
                gX[aMapStart2[ILMath.full, 0], ILMath.full] += -FullLatent[aMapStart2[ILMath.full, 0], ILMath.full]
                                                               + FullLatent[aMapStart2[ILMath.full, 1], ILMath.full]
                                                               - FullLatent[aMapStart2[ILMath.full, 0], ILMath.full];

                // Push all gradients for latent variables
                foreach (FactorDesc desc in lFactorDescs)
                {
                    if (ILMath.anyall(desc.ConnectionPoint.ConnectedDataNode.GetOptimizingMask()))
                    {
                        desc.ConnectionPoint.SetGradient(gX[ILMath.full, desc.IndexesInFullData]);
                    }
                }

                if (UseInducing())
                {
                    dcBeta.SetGradient(new double[] { gBeta });
                }
                PushKernelParamsGradientsToDataConnector(gKernelParam);
            }
        }

        /// <summary>
        /// Creates a seqence of latent points. 
        /// </summary>
        /// <param name="inTestInput">Starting point.</param>
        /// <param name="numTimeSteps">Number of time steps the sequence has to be.</param>
        /// <returns>The computed sequence.</returns>-
        public ILRetArray<double> SimulateDynamics(ILInArray<double> inTestInput, int numTimeSteps)
        {
            using (ILScope.Enter(inTestInput))
            {
                ILArray<double> Xstar = ILMath.check(inTestInput);
                ILArray<double> Xpred = ILMath.zeros(numTimeSteps, nD);
                ILArray<double> XpredTmp;

                if ((nD != nDyn) && numTimeSteps > 3)
                    throw new Exception("Partial dynamics can simulate only one step at a time");

                Xpred[ILMath.r(0, 1), ILMath.full] = Xstar;

                for (int n = 2; n < numTimeSteps; n++)
                {
                    XpredTmp = Xpred[n - 1, ILMath.full];
                    XpredTmp[0, ILMath.r(ILMath.end + 1, ILMath.end + nD)] = Xpred[n - 2, ILMath.full];
                    Xpred[n, DynamicsIndexes] = PredictData(XpredTmp);
                }

                return Xpred[ILMath.full, DynamicsIndexes];
            }
        }

        /// <summary>
        /// Prediction of data points. 
        /// </summary>
        /// <param name="inTestInputs">Point wants to be start</param>
        /// <returns>The predicted data.</returns>
        public ILRetArray<double> PredictData(ILInArray<double> inTestInputs)
        {
            using (ILScope.Enter(inTestInputs))
            {
                ILArray<double> Xstar = ILMath.check(inTestInputs);

                ILArray<double> k = ILMath.empty();

                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        k = KernelObject.ComputeKernelMatrix(Xstar, aXIn, Flag.reconstruction);
                        break;
                    case ApproximationType.dtc:
                        k = KernelObject.ComputeKernelMatrix(Xstar, aXInducing, Flag.reconstruction);
                        break;
                    case ApproximationType.fitc:
                        k = KernelObject.ComputeKernelMatrix(Xstar, aXInducing, Flag.reconstruction);
                        break;
                }

                ILRetArray<double> xm = ILMath.multiply(k, aAlpha);

                return xm;
            }
        }

        private void UpdateParameter()
        {
            using (ILScope.Enter())
            {
                // Reset aFullLatent. It is laizy initialized
                aFullLatent.a = ILMath.empty();
                // Here aFullLatent is reinitialized
                nD = FullLatent.S[1];

                aXIn.a = ILMath.zeros(aMapOrder3.S[0], 2 * nD);
                aXIn[ILMath.full, ILMath.r(0, nD - 1)] = FullLatent[aMapOrder3[ILMath.full, 1], ILMath.full];
                aXIn[ILMath.full, ILMath.r(nD, ILMath.end)] = FullLatent[aMapOrder3[ILMath.full, 0], ILMath.full];

                aXOut.a = FullLatent[aMapOrder3[ILMath.full, 2], DynamicsIndexes];

                if (UseInducing() && aXInducing.IsEmpty)
                    aXInducing.a = aXIn[aInd, ILMath.full].C;

                UpdateKernelMatrix();
            }
        }

        private void UpdateKernelMatrix()
        {
            using (ILScope.Enter())
            {
                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        aK.a = KernelObject.ComputeKernelMatrix(aXIn, aXIn);
                        aKInv.a = Util.pinverse(aK);
                        fKLogDet = Util.logdet(aK);

                        aAlpha.a = ILMath.multiply(aKInv, aXOut);
                        break;

                    case ApproximationType.dtc:
                        aK.a = KernelObject.ComputeKernelMatrix(aXInducing, aXInducing);
                        aKuf.a = KernelObject.ComputeKernelMatrix(aXInducing, aXIn);
                        aKInv.a = Util.pinverse(aK);
                        fKLogDet = Util.logdet(aK);

                        aA.a = (1 / Beta) * aK + ILMath.multiply(aKuf, aKuf.T);
                        // This can become unstable when K_uf2 is low rank.
                        aAInv.a = Util.pinverse(aA);
                        fLogDetA = Util.logdet(aA);

                        aAlpha.a = ILMath.multiply(ILMath.multiply(aAInv, aKuf), aXOut);
                        break;

                    case ApproximationType.fitc:
                        aK.a = KernelObject.ComputeKernelMatrix(aXInducing, aXInducing);
                        aKuf.a = KernelObject.ComputeKernelMatrix(aXInducing, aXIn);
                        aKInv.a = Util.pinverse(aK);
                        fKLogDet = Util.logdet(aK);

                        ILArray<double> _diagK = KernelObject.ComputeDiagonal(aXIn);

                        aDDiag.a = 1 + Beta * _diagK - Beta * ILMath.sum(ILMath.multiplyElem(aKuf, ILMath.multiply(aKInv, aKuf)), 0).T;
                        aDInv.a = 1 / aDDiag;
                        ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiplyElem(aKuf, ILMath.repmat(aDInv.T, NumInducing, 1)), aKuf.T);
                        aA.a = 1 / Beta * aK + KufDinvKuf;

                        // This can become unstable when K_ufDinvK_uf is low rank.
                        aAInv.a = Util.pinverse(aA);
                        fLogDetA = Util.logdet(aA);

                        aDetDiff.a = -ILMath.log(Beta) * NumInducing + ILMath.log(ILMath.det(ILMath.eye(NumInducing, NumInducing) + Beta * ILMath.multiply(KufDinvKuf, aKInv)));

                        aAlpha.a = ILMath.multiply(ILMath.multiplyElem(ILMath.multiply(aAInv, aKuf), ILMath.repmat(aDInv.T, NumInducing, 1)), aXOut);
                        break;
                }
            }
        }

        public override void ComputeAllGradients()
        {
            UpdateParameter();
            PushAllGradients();
        }

        public override void PullDataFromDataNodes()
        {
            using (ILScope.Enter())
            {
                base.PullDataFromDataNodes();
                // Reset latents for lazy recalculation
                aFullLatent.a = ILMath.empty();
                aFullLatentInducing.a = ILMath.empty();
            }
        }

        public void CreatePredictors(int n)
        {
            predictionStruct.Clear();
            for (int i = 0; i < n; i++)
            {
                var predictor = new GPPredictionStruct();
                predictor.Kernel = (IKernel)Utils.Serializer.Clone(KernelObject, KernelObject.GetType());
                predictor.aAlpha.a = aAlpha;
                predictionStruct.Add(predictor);
            }
        }

        public void UpdatePredictors(bool bPullParameters = true)
        {
            IKernel originalKernel = KernelObject;
            foreach (GPPredictionStruct predictor in predictionStruct)
            {
                KernelObject = predictor.Kernel;
                if (bPullParameters)
                    PullKernelParamsFromDataConnector();
                UpdateKernelMatrix();
                predictor.aAlpha.a = aAlpha;
            }
            KernelObject = originalKernel;
            UpdateKernelMatrix();
        }

        public ILRetArray<double> PredictorsSimulateDynamics(int predictorIndex, ILInArray<double> inTestInput, int numTimeSteps)
        {
            using (ILScope.Enter(inTestInput))
            {
                ILArray<double> Xstar = ILMath.check(inTestInput);
                ILArray<double> Xpred = ILMath.zeros(numTimeSteps, nD);
                ILArray<double> XpredTmp;

                if ((nD != nDyn) && numTimeSteps > 3)
                    throw new Exception("Partial dynamics can simulate only one step at a time");

                Xpred[ILMath.r(0, 1), ILMath.full] = Xstar;

                if (nD == nDyn)
                {
                    for (int n = 2; n < numTimeSteps; n++)
                    {
                        XpredTmp = Xpred[n - 1, ILMath.full];
                        XpredTmp[0, ILMath.r(ILMath.end + 1, ILMath.end + nD)] = Xpred[n - 2, ILMath.full];
                        Xpred[n, ILMath.full] = PredictorsPredictData(predictorIndex, XpredTmp);
                    }
                    return Xpred;
                }
                else
                {
                    XpredTmp = Xpred[2 - 1, ILMath.full];
                    XpredTmp[0, ILMath.r(ILMath.end + 1, ILMath.end + nD)] = Xpred[2 - 2, ILMath.full];
                    return PredictorsPredictData(predictorIndex, XpredTmp);
                }
            }
        }

        public ILRetArray<double> PredictorsPredictData(int predictorIndex, ILInArray<double> inTestInputs)
        {
            using (ILScope.Enter(inTestInputs))
            {
                ILArray<double> Xstar = ILMath.check(inTestInputs);

                ILArray<double> k = ILMath.empty();

                switch (pApproxType)
                {
                    case ApproximationType.ftc:
                        k = predictionStruct[predictorIndex].Kernel.ComputeKernelMatrix(Xstar, aXIn, Flag.reconstruction);
                        break;
                    case ApproximationType.dtc:
                        k = predictionStruct[predictorIndex].Kernel.ComputeKernelMatrix(Xstar, aXInducing, Flag.reconstruction);
                        break;
                    case ApproximationType.fitc:
                        k = predictionStruct[predictorIndex].Kernel.ComputeKernelMatrix(Xstar, aXInducing, Flag.reconstruction);
                        break;
                }

                ILRetArray<double> xm = ILMath.multiply(k, predictionStruct[predictorIndex].aAlpha);

                return xm;
            }
        }

        public override void OnDeserializedMethod()
        {
            using (ILScope.Enter())
            {
                base.OnDeserializedMethod();
                aMapOrder3 = ILMath.localMember<int>();
                aMapStart2 = ILMath.localMember<int>();
                aMapStart1 = ILMath.localMember<int>();
                aXOut = ILMath.localMember<double>();
                aXIn = ILMath.localMember<double>();
                aXInducing = ILMath.localMember<double>();
                aK = ILMath.localMember<double>();
                aKInv = ILMath.localMember<double>();
                aKuf = ILMath.localMember<double>();
                aA = ILMath.localMember<double>();
                aAInv = ILMath.localMember<double>();
                aAlpha = ILMath.localMember<double>();
                aDynamicsIndexes = ILMath.localMember<int>();

                CreateInOutMaps();
                aXInducing.a = ILMath.empty();
                UpdateParameter();
                UpdatePredictors();
            }
        }
    }
}
