using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using DataFormats;
using GPLVM;
using GPLVM.Kernel;
using GPLVM.Numerical;
using GPLVM.GPLVM;
using FactorGraph.Core;
using FactorGraph.DataNodes;
using FactorGraph.FactorNodes;
using GPLVM.Utils.Character;

namespace MotionPrimitives.Experiments
{
    public class LearnCircularDynamicsExperiment : Experiment
    {
        protected DataNodeWithSegments dnX;
        protected DataNodeWithSegments dnY;
        protected GPLVMNode fnGPLVM;
        protected AccelerationDynamicsNode fnDynamics;
        protected PTCBackConstraintNode fnBack;

        protected ILArray<double> prevX = ILMath.empty();

        public LearnCircularDynamicsExperiment(string sName, int latents)
            : base(sName, latents)
        {
        }

        public ILArray<double> XValues
        {
            get { return dnX.GetValues(); }
        }

        public ILArray<double> YValues
        {
            get { return dnY.GetValues(); }
        }

        public ILArray<int> Segments
        {
            get { return dnY.Segments; }
        }

        public override ILRetArray<double> GetXValues()
        {
            return dnX.GetValues();
        }

        public override FactorNode GetDynamicsNode()
        {
            return fnDynamics;
        }

        public override void CreateGraph()
        {
            base.CreateGraph();

            // Construct the GPLVM kernel
            var kCompoundGPLVM = new CompoundKern();
            kCompoundGPLVM.AddKern(new RBFKern());
            kCompoundGPLVM.AddKern(new LinearKern());
            kCompoundGPLVM.AddKern(new WhiteKern());

            // Construct the dynamics kernel
            var kCompoundDynamics = new CompoundKern();

            //var kTensor = new TensorKern();
            //var kDirection = new DirectionKern();
            //kDirection.VarianceScaleParameterEnabled = false;
            //// TODO: make a custom pluggable initializer
            //List<ILArray<double>> aKernelIndexes = new List<ILArray<double>>();
            //var kRBF = new RBFKern();
            //kTensor.AddKern(kRBF);
            //aKernelIndexes.Add(ILMath.counter<double>(LatentDimensions, 1, LatentDimensions)); // RBF part
            //kTensor.AddKern(kDirection);
            //aKernelIndexes.Add(ILMath.counter<double>(0, 1, 2 * LatentDimensions)); // Direction part
            //kTensor.Indexes = aKernelIndexes;
            //kCompoundDynamics.AddKern(kTensor);

            kCompoundDynamics.AddKern(new RBFAccelerationKern());
            kCompoundDynamics.AddKern(new LinearAccelerationKern());

            kCompoundDynamics.AddKern(new WhiteKern());

            // Data nodes
            dnX = new DataNodeWithSegments("X");
            dnY = new DataNodeWithSegments("Y");
            var dnLogScale = new MatrixDataNode("Log Scale");
            var dnKGPLVM = new MatrixDataNode("GPLVM kernel parameters");
            var dnKDynamics = new MatrixDataNode("Dynamics kernel parameters");
            var dnInducingGPLVM = new MatrixDataNode("Inducing variables GPLVM");
            var dnInducingGPDM = new MatrixDataNode("Inducing variables GPDM");
            var dnBetaGPLVM = new MatrixDataNode("Beta GPLVM");
            var dnBetaGPDM = new MatrixDataNode("Beta GPDM");
            var dnBackMap = new MatrixDataNode("Back constraints mapping parameters");

            // X and Y spaces share the segments data
            dnX.Segments = dnY.Segments;

            // Factor nodes
            fnGPLVM = new GPLVMNode("GPLVM", kCompoundGPLVM, GPLVMApproximation);
            fnDynamics = new AccelerationDynamicsNode("Dynamics", kCompoundDynamics, DynamicsApproximation);

            fnGPLVM.NumInducingMax = NumberOfInducingPoints;
            fnDynamics.NumInducingMax = NumberOfInducingPoints;
            fnBack = new PTCBackConstraintNode("Back constraints", dnX, dnY);

            // Construct factor graph
            //graph.DataNodes.Add(dnX);
            graph.DataNodes.Add(dnY);
            if (fnGPLVM.UseInducing())
                graph.DataNodes.Add(dnInducingGPLVM);
            graph.DataNodes.Add(dnLogScale);
            graph.DataNodes.Add(dnKGPLVM);
            if (fnGPLVM.UseInducing())
                graph.DataNodes.Add(dnBetaGPLVM);
            if (fnDynamics.UseInducing())
                graph.DataNodes.Add(dnBetaGPDM);
            graph.DataNodes.Add(dnKDynamics);
            graph.FactorNodes.Add(fnGPLVM);
            graph.FactorNodes.Add(fnDynamics);
            graph.FactorNodes.Add(fnBack);

            fnGPLVM.DataConnectorX.ConnectDataNode(dnX);
            fnGPLVM.DataConnectorY.ConnectDataNode(dnY);
            fnGPLVM.DataConnectorKernel.ConnectDataNode(dnKGPLVM);
            fnGPLVM.DataConnectorLogScale.ConnectDataNode(dnLogScale);
            if (fnGPLVM.UseInducing())
            {
                fnGPLVM.DataConnectorInducing.ConnectDataNode(dnInducingGPLVM);
                fnGPLVM.DataConnectorBeta.ConnectDataNode(dnBetaGPLVM);
            }
            if (fnDynamics.UseInducing())
                fnDynamics.DataConnectorBeta.ConnectDataNode(dnBetaGPDM);
            fnDynamics.DataConnectorKernel.ConnectDataNode(dnKDynamics);
            fnDynamics.DataConnectorX.ConnectDataNode(dnX);
            fnBack.DataConnectorA.ConnectDataNode(dnBackMap);
        }

        public override void LoadBVHData(string sFileName)
        {
            CreateGraph();
            BVHData bvh = new BVHData(representation);
            ILArray<double> data = bvh.LoadFile(sFileName);
            fFrameTime = bvh.FrameTime;
            skeleton = bvh.skeleton;
            dnY.AddData(data);

            int N = data.S[0];
            int D = data.S[1];
            int Q = LatentDimensions;
            dnX.SetValuesSize(new ILSize(new int[] { N, Q }));
            dnX.SetOptimizingMaskAll();
            dnY.SetOptimizingMaskNone();
        }

        public override void InitFactorNodes()
        {
            // Initialize the nodes
            fnGPLVM.NumInducingMax = XValues.S[0] / 3;
            fnDynamics.NumInducingMax = XValues.S[0] / 3;
            fnGPLVM.Initialize();
            fnBack.Initialize();
            fnDynamics.Initialize();
            // Force recalculation of gradients
            // This is required for correct initialiaztion of the BackConstraint gradients!!!
            graph.ComputeGradients();
        }

        protected int iFrame = 0;
        public override void InitGeneration()
        {
            // Take first two points in X space
            prevX.a = dnX.GetValues()[ILMath.r(0, 1), ILMath.full];
            double speedFactor = 1.0;
            //double speedFactor = 0.3;
            prevX[1, ILMath.full] = (1 - speedFactor) * prevX[0, ILMath.full] + speedFactor * prevX[1, ILMath.full];
            iFrame = 0;
        }

        public override bool GenerateFrame(ILOutArray<double> frameData, out Representation representationType)
        {
            representationType = representation;
            //frameData.a = dnY.Values[iFrame, ILMath.full];
            //iFrame++;
            //return true;

            // Generate next frame
            ILArray<double> nextX = fnDynamics.SimulateDynamics(prevX, 3);
            nextX = nextX[ILMath.end, ILMath.full];
            frameData.a = fnGPLVM.PredictData(nextX);

            // Update 2-nd order dynamics history
            prevX[0, ILMath.full] = prevX[1, ILMath.full];
            prevX[1, ILMath.full] = nextX;
            return true;
        }

        public override void LoadFromFile(string prefix, string sFileName)
        {
            base.LoadFromFile(prefix, sFileName);
            // Update references to nodes
            dnX = graph.DataNodes.FindByName("X") as DataNodeWithSegments;
            dnY = graph.DataNodes.FindByName("Y") as DataNodeWithSegments;
            fnDynamics = graph.FactorNodes.FindByName("Dynamics") as AccelerationDynamicsNode;
            fnGPLVM = graph.FactorNodes.FindByName("GPLVM") as GPLVMNode;
        }

        public override ILRetArray<double> PredictFullX(int kSizeFactor = 1)
        {
            double speedFactor = 1.0;
            ILArray<double> XPredicted = ILMath.empty();
            ILArray<double> XStart = ILMath.empty();
            XStart.a = dnX.GetValues()[ILMath.r(0, 1), ILMath.full];
            XStart[1, ILMath.full] = (1 - speedFactor) * XStart[0, ILMath.full] + speedFactor * XStart[1, ILMath.full];
            XPredicted.a = fnDynamics.SimulateDynamics(XStart, kSizeFactor * (dnX.GetValues().S[0] - 2));
            return Util.Concatenate<double>(XStart, XPredicted);
        }

        public ILRetArray<double> PredictFullY()
        {
            ILArray<double> YPredicted = ILMath.empty();
            ILArray<double> XStart = ILMath.empty();
            XStart.a = dnX.GetValues()[ILMath.r(0, 1), ILMath.full];
            YPredicted.a = fnGPLVM.PredictData(fnDynamics.SimulateDynamics(XStart, dnX.GetValues().S[0] - 2));
            return Util.Concatenate<double>(fnGPLVM.PredictData(XStart), YPredicted);
        }
    }
}
