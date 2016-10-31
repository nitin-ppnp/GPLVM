using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using GPLVM.Kernel;
using GPLVM.Embeddings;
using FactorGraph.Core;
using FactorGraph.FactorNodes;
using FactorGraph.DataNodes;

namespace FactorGraph.FactorNodes
{
    /// <summary>
    /// Kernel-based regression back-constraint.
    /// Provides functional mapping from Observed (Y) to Latent (X) space.
    /// </summary>
    /// 
    [DataContract(IsReference = true)]
    public class KBRBackConstraintNode : FactorNodeWithKernel
    {
        public static string CP_ARGUMENT    = "Argument";
        public static string CP_FUNCTION    = "Function";
        public static string CP_MAP         = "Map";

        private int nN;
        private int nQ;
        private int nD;

        [DataMember()]
        protected DataConnector dcX;
        [DataMember()]
        protected DataConnector dcY;
        [DataMember()]
        protected DataConnector dcA;

        private ILArray<double> aX = ILMath.localMember<double>();
        private ILArray<double> aYNorm = ILMath.localMember<double>();

        private ILArray<double> aK = ILMath.localMember<double>();
        private ILArray<double> aKInv = ILMath.localMember<double>();

        public KBRBackConstraintNode(string sName, IKernel iKernel, IMatrixDataNode dnX, IMatrixDataNode dnY)
            : base(sName, iKernel)
        {
            dcX = new DataConnector(CP_FUNCTION);
            DataConnectors.Add(dcX);
            dcX.ConnectDataNode(dnX);
            dcY = new DataConnector(CP_ARGUMENT);
            DataConnectors.Add(dcY);
            dcY.ConnectDataNode(dnY);
            dcA = new DataConnector(CP_MAP);
            DataConnectors.Add(dcA);

            // X data node is managed internally.
            // Remove X from the external graph and add to the inner one
            this.lDataNodes.Add(dnX);
        }

        /// Arguments data connectors
        public DataConnector DataConnectorX
        {
            get { return dcX; }
        }

        // Function values data connector
        public DataConnector DataConnectorY
        {
            get { return dcY; }
        }

        // Mappind data connector
        public DataConnector DataConnectorA
        {
            get { return dcA; }
        }

        // Arguments
        protected ILArray<double> X
        {
            //get { return dcX.Values; }
            set { dcX.Values = value; }
        }

        // Function values
        protected ILArray<double> Y
        {
            get { return dcY.Values; }
        }

        // Functional mapping parameters
        protected ILArray<double> A
        {
            get { return dcA.Values; }
            set { dcA.Values = value; }
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

        protected ILRetArray<int> GetSegmentsSizes()
        {
            using (ILScope.Enter())
            {
                ILArray<int> res = ILMath.zeros<int>(Segments.S[0], 2);
                res[ILMath.full, 0] = Segments;
                res[ILMath.r(0, ILMath.end - 1), 1] = Segments[ILMath.r(1, ILMath.end)];
                res[ILMath.end, 1] = Y.S[0];
                return res;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            dnK.SetOptimizingMaskNone(); // For now kernel is not optimized
            dcA.ConnectedDataNode.SetValuesSize(dcX.Values.S);
            dcA.ConnectedDataNode.SetOptimizingMaskAll();
            dcA.SetGradient(ILMath.zeros(dcX.Values.S));
            dcX.SetGradient(ILMath.zeros(dcX.Values.S));

            aX.a = dcX.Values;

            //if (Segments.Length > 1)
                aX.a = NormalizeSegments();

                InitializeFromX(aX);
        }

        public void InitializeFromX(ILInArray<double> inXProposed)
        {
            using (ILScope.Enter(inXProposed))
            {
                ILArray<double> XProposed = ILMath.check(inXProposed);
                aX.a = XProposed;
                UpdateKernelMatrix();
                A = ILMath.multiply(aKInv, aX);
                nD = Y.Size[1];
                nN = A.Size[0];
                nQ = A.Size[1];

                UpdateParameter();
            }
        }

        public ILRetArray<double> GetFunctionalMap()
        {
            return aX.C;
        }

        private void UpdateParameter()
        {
            using (ILScope.Enter())
            {
                //UpdateKernelMatrix();  // So far the kernel is not being updated
                aX.a = ILMath.multiply(aK, A);
            }
        }

        private void UpdateKernelMatrix()
        {
            using (ILScope.Enter())
            {
                if (KernelObject != null)
                {
                    aK.a = KernelObject.ComputeKernelMatrix(aYNorm, aYNorm);
                    aKInv.a = Util.pinverse(aK);
                }
            }
        }

        private ILRetArray<double> NormalizeSegments()
        {
            using (ILScope.Enter())
            {
                ILArray<double> curY = ILMath.empty();
                ILArray<double> XNorm = ILMath.zeros(aX.Size);
                aYNorm.a = Y;

                ILArray<int> segmentsSizes = GetSegmentsSizes();
                for (int i = 0; i < segmentsSizes.S[0]; i++)
                {
                    curY = Y[ILMath.r(segmentsSizes[i, 0], segmentsSizes[i, 1] - 1), ILMath.full];
                    curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

                    aYNorm[ILMath.r(segmentsSizes[i, 0], segmentsSizes[i, 1] - 1), ILMath.full] = curY;
                    //XNorm[ILMath.r(segmentsSizes[i, 0], segmentsSizes[i, 1] - 1), ILMath.full] = EstimateX(curY);
                }
                XNorm = EstimateX(aYNorm);


                return XNorm;
            }
        }

        /// <summary>
        /// Estimates latent points through PCA. 
        /// </summary>
        /// <param name="data">The data Y.</param>
        private ILRetArray<double> EstimateX(ILInArray<double> inCurY)
        {
            using (ILScope.Enter(inCurY))
            {
                ILArray<double> curY = ILMath.check(inCurY);
                ILArray<double> XEst = ILMath.empty();

                //switch (_initX)
                //{
                //    case XInit.pca:
                XEst = Embed.PCA(curY, aX.Size[1]);
                //        break;
                //    case XInit.kernelPCA:
                //        X = Embed.KernelPCA(curY, _X.Size[1]);
                //        break;
                //    case XInit.lle:
                //XEst = Embed.LLE(curY, 50, aX.Size[1]);
                //        break;
                //    case XInit.smallRand:
                //XEst = Embed.SmallRand(curY, aX.Size[1]);
                //        break;
                //    case XInit.isomap:
                //XEst = Embed.Isomap(curY, aX.Size[1]);
                //        break;
                //}

                return XEst;
            }
        }

        public override double LogLikelihood()
        {
            return 0;
        }

        public override void ComputeAllGradients()
        {
            using (ILScope.Enter())
            {
                dcX.ConnectedDataNode.PullGradientsFormFactorNodes();
                ILArray<double> gA = ILMath.multiply(aK, dcX.ConnectedDataNode.GetValuesGradient());
                dcA.SetGradient(gA);

                //dcX.ConnectedDataNode.SetOptimizingMaskNone();
            }
        }

        public override void PullDataFromDataNodes()
        {
            base.PullDataFromDataNodes();
            UpdateParameter();
        }

        public override void BeforeOnDataNodesChanged()
        {
            using (ILScope.Enter())
            {
                PullDataFromDataNodes();
                // As X data node is managed internally, calculate and push X data before other nodes use it
                X = GetFunctionalMap();
            }
        }

        public override void OnDataNodesChanged()
        {
        }

        public override void AfterOnDataNodesChanged()
        {
            ComputeAllGradients();
        }

        public override void OnDeserializedMethod()
        {
            using (ILScope.Enter())
            {
                base.OnDeserializedMethod();
                aX = ILMath.localMember<double>();
                aYNorm = ILMath.localMember<double>();
                aK = ILMath.localMember<double>();
                aKInv = ILMath.localMember<double>();
            }
        }

    }
}
