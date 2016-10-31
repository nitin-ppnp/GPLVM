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
    /// Periodic type back-constraint.
    /// Provides functional mapping from Observed (Y) to Latent (X) space.
    /// </summary>
    /// 
    [DataContract(IsReference = true)]
    public class PTCBackConstraintNode : FactorNode
    {
        public static string CP_ARGUMENT = "Argument";
        public static string CP_FUNCTION = "Function";
        public static string CP_MAP = "Map";

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
        private ILArray<double> aY = ILMath.localMember<double>();

        public PTCBackConstraintNode(string sName, IMatrixDataNode dnX, IMatrixDataNode dnY)
            : base(sName)
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
            get { return dcX.Values; }
            set { dcX.Values = value; }
        }

        // Function values
        protected ILArray<double> Y
        {
            get { return dcY.Values; }
            set { dcY.Values = value; }
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
            dcA.ConnectedDataNode.SetValuesSize(new ILSize(Segments.Length * 2));
            dcA.ConnectedDataNode.SetOptimizingMaskAll();
            dcA.SetGradient(ILMath.zeros(dcA.Values.S));
            dcX.SetGradient(ILMath.zeros(dcX.Values.S));

            aX.a = X;
            aY.a = Y;

            CreatePhase();
            
            nD = Y.Size[1];
            nN = A.Size[0];
            nQ = A.Size[1];

            UpdateParameter();
            //ComputeAllGradients();
        }

        public ILRetArray<double> GetFunctionalMap()
        {
            using (ILScope.Enter())
            {
                return aX.C;
            }
        }

        private void UpdateParameter()
        {
            using (ILScope.Enter())
            {
                aX.a = X;
                double theta_0, delta;
                ILArray<int> segmentsSizes = ILMath.empty<int>();
                segmentsSizes.a = GetSegmentsSizes();
                int numSeg = segmentsSizes.S[0];
                for (int i = 0; i < numSeg; i++)
                {
                    theta_0 = (double)A[i];
                    delta = (double)A[numSeg + i];
                    for (int j = (int)segmentsSizes[i, 0]; j < segmentsSizes[i, 1]; j++)
                    {
                        int j_1 = j - (int)segmentsSizes[i, 0];
                        aX[j, 0] = ILMath.cos(theta_0 + (j_1) * delta);
                        aX[j, 1] = ILMath.sin(theta_0 + (j_1) * delta); // extracting the phase and setting to x_j
                    }
                }
            }
        }

        private void CreatePhase()
        {
            using (ILScope.Enter())
            {
                ILArray<double> curY = ILMath.empty();
                ILArray<complex> tmp = ILMath.empty<complex>();
                ILArray<double> curX = ILMath.empty();
                ILArray<double> curX_norm = ILMath.empty();
                ILArray<double> angles = ILMath.empty();
                ILArray<double> offset = ILMath.zeros(Segments.Length, 1); // offset and step size
                ILArray<double> step = ILMath.zeros(Segments.Length, 1); // offset and step size

                ILArray<int> sementsSizes = ILMath.empty<int>();
                sementsSizes.a = GetSegmentsSizes();
                int numSeg = sementsSizes.S[0];

                for (int i = 0; i < numSeg; i++)
                {
                    curY.a = aY[ILMath.r(sementsSizes[i, 0], sementsSizes[i, 1] - 1), ILMath.full].C;
                    curY.a = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

                    curX = EstimateX(curY);

                    curX_norm = ILMath.sqrt(ILMath.multiplyElem(curX[":", 0], curX[":", 0]) + ILMath.multiplyElem(curX[":", 1], curX[":", 1])); // getting the circle
                    curX[":", 0] = ILMath.divide(curX[":", 0], curX_norm);
                    curX[":", 1] = ILMath.divide(curX[":", 1], curX_norm); // normalizing the circle

                    angles = ILMath.atan(ILMath.divide(curX[":", 1], curX[":", 0]));

                    step[i] = ILMath.median(ILMath.abs(angles[ILMath.r(1, ILMath.end)] - angles[ILMath.r(0, ILMath.end - 1)])); // computing step size
                    aY[ILMath.r(sementsSizes[i, 0], sementsSizes[i, 1] - 1), ILMath.full] = curY;
                }

                if (numSeg > 1)
                {
                    int l = (int)(sementsSizes[0, 1] - sementsSizes[0, 0]);
                    for (int i = 1; i < numSeg; i++)
                    {
                        ILArray<int> idx = ILMath.empty<int>();
                        ILMath.min(ILMath.sum(ILMath.pow(ILMath.repmat(aY[Segments[i], ":"], l, 1) - aY[ILMath.r(sementsSizes[0, 0], sementsSizes[0, 1] - 1), ":"], 2), 1), idx);

                        offset[i] = ((double)(idx - 1)) * step[0]; // applying the angles
                    }
                }

                // !!!!
                //step[0] = 0.25;
                A = Util.Concatenate<double>(offset, step, 0);
            }
        }

        public ILRetArray<double> BackConstraintsGradient(ILInArray<double> ingX)
        {
            using (ILScope.Enter(ingX))
            {
                ILArray<double> gX = ILMath.check(ingX);
                ILArray<double> dX_dA = ILMath.zeros(gX.S[0], gX.S[1], Segments.Length * 2);
                ILRetArray<double> gA = ILMath.empty();
                ILArray<double> dL_dA = ILMath.zeros(Segments.Length * 2, 1);

                double theta_0, delta;
                int numSeg = Segments.Length;
                for (int n = 0; n < numSeg - 1; n++)
                {
                    theta_0 = (double)A[n];
                    delta = (double)A[numSeg + n];
                    for (int j = (int)Segments[n]; j < Segments[n + 1]; j++)
                    {
                        int j_1 = j - (int)Segments[n];

                        dX_dA[j, 0, n] = -ILMath.sin(theta_0 + (j_1) * delta);
                        dX_dA[j, 1, n] = ILMath.cos(theta_0 + (j_1) * delta);

                        dX_dA[j, 0, numSeg + n] = -ILMath.sin(theta_0 + (j_1) * delta);
                        dX_dA[j, 1, numSeg + n] = ILMath.cos(theta_0 + (j_1) * delta);
                    }
                }

                theta_0 = (double)A[numSeg - 1];
                delta = (double)A[numSeg + numSeg - 1];

                for (int j = (int)Segments[ILMath.end]; j < aY.Size[0]; j++)
                {
                    int j_1 = j - (int)Segments[ILMath.end];

                    dX_dA[j, 0, numSeg - 1] = -ILMath.sin(theta_0 + (j_1) * delta);
                    dX_dA[j, 1, numSeg - 1] = ILMath.cos(theta_0 + (j_1) * delta);

                    dX_dA[j, 0, numSeg + numSeg - 1] = -ILMath.sin(theta_0 + (j_1) * delta);
                    dX_dA[j, 1, numSeg + numSeg - 1] = ILMath.cos(theta_0 + (j_1) * delta);
                }


                for (int n = 0; n < numSeg; n++)
                {
                    dL_dA[n] = ILMath.trace(ILMath.multiply(gX.T, dX_dA[ILMath.full, ILMath.full, n]));
                    dL_dA[numSeg + n] = ILMath.trace(ILMath.multiply(gX.T, dX_dA[ILMath.full, ILMath.full, numSeg + n]));
                }

                gA = dL_dA;

                return gA;
            }
        }

        private ILRetArray<double> EstimateX(ILInArray<double> inCurY)
        {
            using (ILScope.Enter(inCurY))
            {
                ILArray<double> curY = ILMath.check(inCurY);
                ILArray<double> XEst = ILMath.empty();

                //switch (_initX)
                //{
                //    case XInit.pca:
                        XEst.a = Embed.PCA(curY, aX.Size[1]);
                //        break;
                //    case XInit.kernelPCA:
                //        X = Embed.KernelPCA(curY, _X.Size[1]);
                //        break;
                //    case XInit.lle:
                //XEst.a = Embed.LLE(curY, 15, aX.Size[1]);
                //        break;
                //    case XInit.smallRand:
                //        X = Embed.SmallRand(curY, _X.Size[1]);
                //        break;
                //    case XInit.isomap:
                //        X = Embed.Isomap(curY, _X.Size[1]);
                //        break;
                //}

                return XEst;
            }
        }

        public override double FunctionValue()
        {
            return 0;
        }

        public override void ComputeAllGradients()
        {
            using (ILScope.Enter())
            {
                dcX.ConnectedDataNode.PullGradientsFormFactorNodes();
                ILArray<double> gX = dcX.ConnectedDataNode.GetValuesGradient();
                ILArray<double> gA = BackConstraintsGradient(gX);
                dcA.SetGradient(gA);
                
                // Remove first 2 dimensions of X from optimization set
                ILArray<int> mask = ILMath.counter<int>(0, 1, gX.S);
                mask[ILMath.full, ILMath.r(0,1)] = ILMath.empty<int>();
                dcX.ConnectedDataNode.SetOptimizingMask(mask[ILMath.full]);
            }
        }

        public override void PullDataFromDataNodes()
        {
            base.PullDataFromDataNodes();
            UpdateParameter();
        }

        public override void BeforeOnDataNodesChanged()
        {
            PullDataFromDataNodes();
            // As X data node is managed internally, calculate and push X data before other nodes use it
            X = GetFunctionalMap();
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
                aY = ILMath.localMember<double>();
            }
        }
    }
}