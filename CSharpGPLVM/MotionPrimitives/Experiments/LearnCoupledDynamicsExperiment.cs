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
using MotionPrimitives.SkeletonMap;

namespace MotionPrimitives.Experiments
{
    public class LearnCoupledDynamicsExperiment : Experiment
    {
        protected DataNodeWithSegments dnXUpper;
        protected DataNodeWithSegments dnYUpper;
        protected DataNodeWithSegments dnXLower;
        protected DataNodeWithSegments dnYLower;
        //protected MatrixDataNode dnBackMap;
        protected MatrixDataNode dnBackMapUpper;
        protected MatrixDataNode dnBackMapLower;
        protected CompoundMatrixDataNode dnXCompound;
        protected PTCBackConstraintNode fnBackUpper;
        protected PTCBackConstraintNode fnBackLower;
        protected GPLVMNode fnGPLVMUpper;
        protected GPLVMNode fnGPLVMLower;
        protected AccelerationDynamicsNode fnDynamics;
        protected SkeletonMap.SkeletonMap FlubberMap;
        protected List<JointsGroupData> FlubberGroups;
        protected JointsGroupData GroupLower;
        protected JointsGroupData GroupUpper;
        protected ModulationKern kModulationUpper;
        protected ModulationKern kModulationLower;
        public ModulationKern kModulationUpperToUpper;
        public ModulationKern kModulationLowerToUpper;
        public ModulationKern kModulationUpperToLower;
        public ModulationKern kModulationLowerToLower;
        public double StartPhaseUpper = 0;
        public double StartPhaseLower = 0;
        //protected JointsGroupData GroupPosition;

        protected ILArray<double> prevX = ILMath.localMember<double>();

        public LearnCoupledDynamicsExperiment(string sName, int latents)
            : base(sName, latents)
        {
        }

        public ILRetArray<double> XValuesUpper
        {
            get { return dnXUpper.GetValues(); }
        }

        public ILRetArray<double> YValuesUpper
        {
            get { return dnYUpper.GetValues(); }
        }

        public ILRetArray<double> XValuesLower
        {
            get { return dnXLower.GetValues(); }
        }

        public ILRetArray<double> YValuesLower
        {
            get { return dnYLower.GetValues(); }
        }

        public override ILRetArray<double> GetXValues()
        {
            return dnXCompound.GetValues();
        }

        public ILRetArray<int> Segments
        {
            get { return dnYLower.Segments; }
        }

        public override FactorNode GetDynamicsNode()
        { 
            return fnDynamics;
        }

        public override void CreateGraph()
        {
            base.CreateGraph();

            // Construct the GPLVM kernel upper
            var kCompoundGPLVMUpper = new CompoundKern();
            kCompoundGPLVMUpper.AddKern(new RBFKern());
            kCompoundGPLVMUpper.AddKern(new LinearKern());
            kCompoundGPLVMUpper.AddKern(new WhiteKern());

            // Construct the GPLVM kernel lower
            var kCompoundGPLVMLower = new CompoundKern();
            kCompoundGPLVMLower.AddKern(new RBFKern());
            kCompoundGPLVMLower.AddKern(new LinearKern());
            kCompoundGPLVMLower.AddKern(new WhiteKern());

            // Construct the dynamics kernel
            var kCompoundDynamics = new CompoundKern();
            kModulationUpper = new ModulationKern();
            kModulationLower = new ModulationKern();
            var kCompoundUpper = new CompoundKern();
            var kCompoundLower = new CompoundKern();
            kCompoundUpper.AddKern(new RBFAccelerationKern());
            kCompoundUpper.AddKern(new LinearAccelerationKern());
            kCompoundLower.AddKern(new RBFAccelerationKern());
            kCompoundLower.AddKern(new LinearAccelerationKern());
            kModulationUpper.SetInnerKern(kCompoundUpper);
            kModulationLower.SetInnerKern(kCompoundLower);
            var kModulationProduct = new TensorKern();
            kModulationProduct.Indexes = new List<ILArray<double>>();
            ILArray<double> ind0 = ILMath.localMember<double>();
            ind0.a = ILMath.counter(LatentDimensions) - 1;
            ind0.a = Util.Concatenate<double>(ind0, ILMath.counter(LatentDimensions) - 1 + 2 * LatentDimensions);
            kModulationProduct.Indexes.Add(ind0);
            ILArray<double> ind1 = ILMath.localMember<double>();
            ind1.a = ILMath.counter(LatentDimensions) - 1 + LatentDimensions;
            ind1.a = Util.Concatenate<double>(ind1, ILMath.counter(LatentDimensions) - 1 + 3 * LatentDimensions);
            kModulationProduct.Indexes.Add(ind1);
            kModulationProduct.AddKern(kModulationUpper);
            kModulationProduct.AddKern(kModulationLower);
            kCompoundDynamics.AddKern(kModulationProduct);
            kCompoundDynamics.AddKern(new WhiteKern());

            // Data nodes
            dnXUpper = new DataNodeWithSegments("X upper");
            dnYUpper = new DataNodeWithSegments("Y upper");
            dnXLower = new DataNodeWithSegments("X lower");
            dnYLower = new DataNodeWithSegments("Y lower");
            dnXCompound = new CompoundMatrixDataNode("X");
            var dnLogScaleUpper = new MatrixDataNode("Log Scale upper");
            var dnLogScaleLower = new MatrixDataNode("Log Scale lower");
            var dnKGPLVMUpper = new MatrixDataNode("GPLVM kernel parameters upper");
            var dnKGPLVMLower = new MatrixDataNode("GPLVM kernel parameters lower");
            var dnKDynamics = new MatrixDataNode("Dynamics kernel parameters");
            var dnInducingUpper = new MatrixDataNode("Inducing variables upper");
            var dnInducingLower = new MatrixDataNode("Inducing variables lower");
            var dnBetaUpper = new MatrixDataNode("Beta upper");
            var dnBetaLower = new MatrixDataNode("Beta lower");
            //dnBackMap = new MatrixDataNode("Back constraints mapping parameters");
            dnBackMapUpper = new MatrixDataNode("Back constraints mapping parameters upper");
            dnBackMapLower = new MatrixDataNode("Back constraints mapping parameters lower");

            // X and Y spaces share the segments data
            dnXUpper.Segments = dnYUpper.Segments;
            dnXLower.Segments = dnYLower.Segments;

            // Factor nodes
            fnGPLVMUpper = new GPLVMNode("GPLVM upper", kCompoundGPLVMUpper, GPLVMApproximation);
            fnGPLVMLower = new GPLVMNode("GPLVM lower", kCompoundGPLVMLower, GPLVMApproximation);
            fnBackLower = new PTCBackConstraintNode("Back lower", dnXLower, dnYLower);
            fnBackUpper = new PTCBackConstraintNode("Back upper", dnXUpper, dnYUpper);
            
            fnDynamics = new AccelerationDynamicsNode("Dynamics", kCompoundDynamics, DynamicsApproximation);

            // Construct factor graph
            dnXCompound.AddInnerDataNode(dnXUpper);
            dnXCompound.AddInnerDataNode(dnXLower);
            graph.DataNodes.Add(dnYUpper);
            graph.DataNodes.Add(dnYLower);
            if (fnGPLVMLower.UseInducing())
                graph.DataNodes.Add(dnInducingLower);
            if (fnGPLVMUpper.UseInducing())
                graph.DataNodes.Add(dnInducingUpper);
            graph.DataNodes.Add(dnLogScaleLower);
            graph.DataNodes.Add(dnLogScaleUpper);
            graph.DataNodes.Add(dnKGPLVMLower);
            graph.DataNodes.Add(dnKGPLVMUpper);
            if (fnGPLVMLower.UseInducing())
                graph.DataNodes.Add(dnBetaLower);
            if (fnGPLVMUpper.UseInducing())
                graph.DataNodes.Add(dnBetaUpper);
            graph.DataNodes.Add(dnKDynamics);
            //graph.DataNodes.Add(dnBackMap);
            graph.DataNodes.Add(dnBackMapUpper);
            graph.DataNodes.Add(dnBackMapLower);
            graph.FactorNodes.Add(fnGPLVMLower);
            graph.FactorNodes.Add(fnGPLVMUpper);
            graph.FactorNodes.Add(fnBackUpper);
            graph.FactorNodes.Add(fnBackLower);
            graph.FactorNodes.Add(fnDynamics);

            fnGPLVMUpper.DataConnectorX.ConnectDataNode(dnXUpper);
            fnGPLVMUpper.DataConnectorY.ConnectDataNode(dnYUpper);
            fnGPLVMUpper.DataConnectorKernel.ConnectDataNode(dnKGPLVMUpper);
            fnGPLVMUpper.DataConnectorLogScale.ConnectDataNode(dnLogScaleUpper);
            if (fnGPLVMUpper.UseInducing())
            {
                fnGPLVMUpper.DataConnectorInducing.ConnectDataNode(dnInducingUpper);
                fnGPLVMUpper.DataConnectorBeta.ConnectDataNode(dnBetaUpper);
            }
            fnGPLVMLower.DataConnectorX.ConnectDataNode(dnXLower);
            fnGPLVMLower.DataConnectorY.ConnectDataNode(dnYLower);
            fnGPLVMLower.DataConnectorKernel.ConnectDataNode(dnKGPLVMLower);
            fnGPLVMLower.DataConnectorLogScale.ConnectDataNode(dnLogScaleLower);
            if (fnGPLVMLower.UseInducing())
            {
                fnGPLVMLower.DataConnectorInducing.ConnectDataNode(dnInducingLower);
                fnGPLVMLower.DataConnectorBeta.ConnectDataNode(dnBetaLower);
            }
            //fnBackUpper.DataConnectorA.ConnectDataNode(dnBackMap);
            //fnBackLower.DataConnectorA.ConnectDataNode(dnBackMap);
            fnBackUpper.DataConnectorA.ConnectDataNode(dnBackMapUpper);
            fnBackLower.DataConnectorA.ConnectDataNode(dnBackMapLower);

            fnDynamics.DataConnectorKernel.ConnectDataNode(dnKDynamics);
            fnDynamics.DataConnectorX.ConnectDataNode(dnXCompound);
            fnDynamics.DataNodes.Add(dnXCompound);
            fnDynamics.CreatePredictors(2); // two dynamics predictors for upper and lower bodies

            UpdateReferences();
        }

        protected List<IKernel> FindAllKernels(IKernel kernel, Type type)
        {
            var res = new List<IKernel>();
            if (kernel.GetType() == type)
                res.Add(kernel);
            if ((kernel is CompoundKern) || (kernel is TensorKern))
            {
                List<IKernel> lKernels = kernel is CompoundKern ? ((CompoundKern)kernel).Kernels : ((TensorKern)kernel).Kernels;
                foreach (IKernel k in lKernels)
                {
                    res.AddRange(FindAllKernels(k, type));
                }
            }
            if (kernel is ModulationKern)
                res.AddRange(FindAllKernels(((ModulationKern)kernel).GetInnerKern(), type));
            return res;
        }

        public override void LoadBVHData(string sFileName)
        {
            CreateGraph();
            BVHData bvh = new BVHData(representation);
            ILArray<double> data = bvh.LoadFile(sFileName);
            fFrameTime = bvh.FrameTime;
            skeleton = bvh.skeleton;

            FlubberMap = PredefinedMaps.CreateFlubberUpperLowerMap(skeleton, JointsGroup.DataChannelsMode.RootRotation4Channels);
            FlubberGroups = FlubberMap.SplitDataIntoGroups(data);
            GroupLower = FlubberGroups.First(x => x.Group.Name == "Lower body part");
            GroupUpper = FlubberGroups.First(x => x.Group.Name == "Upper body part");
            //GroupPosition = FlubberGroups.First(x => x.Group.Name == "Pelvis position");
            dnYUpper.AddData(GroupUpper.Data);
            dnYLower.AddData(GroupLower.Data);

            {
                int N = GroupUpper.Data.S[0];
                int D = GroupUpper.Data.S[1];
                int Q = LatentDimensions;
                dnXUpper.SetValuesSize(new ILSize(new int[] { N, Q }));
                dnXUpper.SetOptimizingMaskAll();
                dnYUpper.SetOptimizingMaskNone();
            }
            {
                int N = GroupLower.Data.S[0];
                int D = GroupLower.Data.S[1];
                int Q = LatentDimensions;
                dnXLower.SetValuesSize(new ILSize(new int[] { N, Q }));
                dnXLower.SetOptimizingMaskAll();
                dnYLower.SetOptimizingMaskNone();
            }
        }

        public override void InitFactorNodes()
        {
            // Initialize the nodes
            fnGPLVMUpper.NumInducingMax = XValuesUpper.S[0]/3;
            fnGPLVMUpper.Initialize();
            fnBackUpper.Initialize();
            fnGPLVMLower.NumInducingMax = XValuesLower.S[0] / 3;
            fnGPLVMLower.Initialize();
            fnBackLower.Initialize();
            fnDynamics.Initialize();
            // Force recalculation of gradients
            // This is required for correct initialiaztion of the BackConstraint gradients!!!
            graph.ComputeGradients();
            //this.SaveToFile(DateTime.Now.ToString().Replace('/', '-').Replace(':', '-') + "cyclic.xml");

            PrintReport();
        }

        public override void RunOptimization()
        {
            base.RunOptimization();
            fnDynamics.UpdatePredictors();
        }

        public override void PrintReport()
        {
            //Console.WriteLine("Back mapping: " + dnBackMap.GetValues().ToString());
            Console.WriteLine("Upper mapping: " + dnBackMapUpper.GetValues().ToString());
            Console.WriteLine("Lower mapping: " + dnBackMapLower.GetValues().ToString());
        }

        protected int iFrame = 0;
        public override void InitGeneration()
        {
            // Take first two points in X space
            prevX.a = dnXCompound.GetValues()[ILMath.r(0, 1), ILMath.full];
            //double speedFactor = 1.0;
            //prevX[1, ILMath.full] = (1 - speedFactor) * prevX[0, ILMath.full] + speedFactor * prevX[1, ILMath.full];

            // Adjust the start phase
            double frameStep = (double)dnBackMapUpper.GetValues()[1, 0];
            int skipUpper = (int)(2 * Math.PI * StartPhaseUpper / frameStep);
            int skipLower = (int)(2 * Math.PI * StartPhaseLower / frameStep);

            ILArray<double> XUpper = fnDynamics.SimulateDynamics(prevX, skipUpper + 2);
            XUpper = XUpper[ILMath.r(ILMath.end - 1, ILMath.end), ILMath.r(0, LatentDimensions - 1)];

            ILArray<double> XLower = fnDynamics.SimulateDynamics(prevX, skipLower + 2);
            XLower = XLower[ILMath.r(ILMath.end - 1, ILMath.end), ILMath.r(LatentDimensions, 2 * LatentDimensions - 1)];

            prevX.a = Util.Concatenate<double>(XUpper, XLower, 1);
            iFrame = 0;
        }

        public override bool GenerateFrame(ILOutArray<double> frameData, out Representation representationType)
        {
            representationType = representation;
            ILArray<double> nextUpper = fnDynamics.PredictorsSimulateDynamics(0, prevX, 3)[ILMath.end, ILMath.r(0, LatentDimensions - 1)].T;
            ILArray<double> nextLower = fnDynamics.PredictorsSimulateDynamics(1, prevX, 3)[ILMath.end, ILMath.r(LatentDimensions, 2 * LatentDimensions - 1)].T;
            ILArray<double> nextX = Util.Concatenate<double>(nextUpper, nextLower);
            ILArray<double> upper = fnGPLVMUpper.PredictData(nextX[ILMath.r(0, LatentDimensions - 1)].T);
            ILArray<double> lower = fnGPLVMLower.PredictData(nextX[ILMath.r(LatentDimensions, 2 * LatentDimensions - 1)].T);
            var groups = new List<JointsGroupData>();
            groups.Add(GroupLower.CreateCopyWithNewData(lower));
            groups.Add(GroupUpper.CreateCopyWithNewData(upper));
            frameData.a = FlubberMap.MergeDataFromGroups(groups);
            
            // Update 2-nd order dynamics history
            prevX[0, ILMath.full] = prevX[1, ILMath.full];
            prevX[1, ILMath.full] = nextX;
            return true;
        }

        public override void LoadFromFile(string prefix, string sFileName)
        {
            base.LoadFromFile(prefix, sFileName);
            UpdateReferences();
        }
        
        public void UpdateReferences()
        {
            // Update references to nodes
            dnXCompound = graph.FindDataNodeByName("X") as CompoundMatrixDataNode;
            dnXUpper = graph.FindDataNodeByName("X upper") as DataNodeWithSegments;
            dnYUpper = graph.FindDataNodeByName("Y upper") as DataNodeWithSegments;
            fnGPLVMUpper = graph.FactorNodes.FindByName("GPLVM upper") as GPLVMNode;
            dnXLower = graph.FindDataNodeByName("X lower") as DataNodeWithSegments;
            dnYLower = graph.FindDataNodeByName("Y lower") as DataNodeWithSegments;
            //dnBackMap = graph.FindDataNodeByName("Back constraints mapping parameters") as MatrixDataNode;
            dnBackMapUpper = graph.FindDataNodeByName("Back constraints mapping parameters upper") as MatrixDataNode;
            dnBackMapLower = graph.FindDataNodeByName("Back constraints mapping parameters lower") as MatrixDataNode;
            fnGPLVMLower = graph.FactorNodes.FindByName("GPLVM lower") as GPLVMNode;
            fnDynamics = graph.FactorNodes.FindByName("Dynamics") as AccelerationDynamicsNode;

            var lKernels = FindAllKernels(fnDynamics.KernelObject, typeof(ModulationKern));
            kModulationUpper = (ModulationKern)lKernels[0];
            kModulationLower = (ModulationKern)lKernels[1];
            lKernels = FindAllKernels(fnDynamics.PredictionStructs[0].Kernel, typeof(ModulationKern));
            kModulationUpperToUpper = (ModulationKern)lKernels[0];
            kModulationLowerToUpper = (ModulationKern)lKernels[1];
            lKernels = FindAllKernels(fnDynamics.PredictionStructs[1].Kernel, typeof(ModulationKern));
            kModulationUpperToLower = (ModulationKern)lKernels[0];
            kModulationLowerToLower = (ModulationKern)lKernels[1];
        }

        public override ILRetArray<double> PredictFullX(int kSizeFactor = 1)
        {
            double speedFactor = 1.0;
            ILArray<double> XPredicted = ILMath.empty();
            ILArray<double> XStart = ILMath.empty();
            XStart.a = dnXCompound.GetValues()[ILMath.r(0, 1), ILMath.full];
            XStart[1, ILMath.full] = (1 - speedFactor) * XStart[0, ILMath.full] + speedFactor * XStart[1, ILMath.full];
            XPredicted.a = fnDynamics.SimulateDynamics(XStart, kSizeFactor * (dnXCompound.GetValues().S[0] - 2));
            return Util.Concatenate<double>(XStart, XPredicted);
        }

        public ILRetArray<double> PredictFullY()
        {
            return ILMath.empty();
            //ILArray<double> YPredicted = ILMath.empty();
            //ILArray<double> XStart = ILMath.empty();
            //XStart.a = dnX.GetValues()[ILMath.r(0, 1), ILMath.full];
            //YPredicted.a = fnGPLVM.PredictData(fnDynamics.SimulateDynamics(XStart, dnX.GetValues().S[0] - 2));
            //return Util.Concatenate<double>(fnGPLVM.PredictData(XStart), YPredicted);        
        }
    }
}