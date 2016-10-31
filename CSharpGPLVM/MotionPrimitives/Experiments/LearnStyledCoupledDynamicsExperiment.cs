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
using FactorGraph.Utils;
using FactorGraph.DataNodes;
using FactorGraph.FactorNodes;
using GPLVM.Utils.Character;
using MotionPrimitives.SkeletonMap;
using MotionPrimitives.DataSets;
using MotionPrimitives.Numerical;

namespace MotionPrimitives.Experiments
{
    public class LearnStyledCoupledDynamicsExperiment : Experiment
    {
        public class BodyPartPlate
        {
            public string Name;
            public DataNodeWithSegments dnY; // observed data of the part
            public DataNodeWithSegments dnX; // latent points 
            public MatrixDataNode dnGPLVMLogScale; // scaling of the GPLVM
            public MatrixDataNode dnGPLVMKern; // GPLVM kernel parameters
            public MatrixDataNode dnGPLVMInducing; // GPLVM inducing points 
            public MatrixDataNode dnGPLVMBeta; // GPLVM beta parameter

            public MatrixDataNode dnBackMap; // back-constraint mapping
            public MatrixDataNode dnBackKern; // back-constraints kernel parameters

            public MatrixDataNode dnDynamicsKern; // dynamics kernel parameters
            public MatrixDataNode dnDynamicsBeta; // beta for the dynamics

            public GPLVMNode fnGPLVM;
            public KBRBackConstraintNode fnBack;
            public StyleAccelerationDynamicsNode fnDynamics;

            public BodyPartPlate(string partName, Graph graph, int partsCount, int partNumber,
                CompoundMatrixDataNodeWithSegments dnXCompound, int latentDimensions,
                bool useBackConstraint,
                bool usePGEKern,
                ApproximationType GPLVMApproximation = ApproximationType.ftc,
                ApproximationType dynamicsApproximation = ApproximationType.ftc)
            {
                Name = partName;
                
                // Data nodes
                dnY = new DataNodeWithSegments("Y " + Name);
                dnX = new DataNodeWithSegments("X " + Name);
                dnXCompound.AddInnerDataNode(dnX);
                dnGPLVMLogScale = new MatrixDataNode("Log scale " + Name);
                dnGPLVMKern = new MatrixDataNode("GPLVM kernel parameters " + Name);
                dnDynamicsKern = new MatrixDataNode("Dynamics kernel parameters " + Name);
                dnGPLVMInducing = new MatrixDataNode("Inducing variables " + Name);
                dnGPLVMBeta = new MatrixDataNode("Beta GPLVM" + Name);
                dnDynamicsBeta = new MatrixDataNode("Beta dynamics " + Name);
                if (useBackConstraint)
                {
                    dnBackMap = new MatrixDataNode("Back constraints mapping parameters " + Name);
                    dnBackKern = new MatrixDataNode("Back constraints mapping kernel parameters " + Name);
                }

                // Construct the GPLVM kernel
                var kernCompoundGPLVM = new CompoundKern();
                kernCompoundGPLVM.AddKern(new RBFKern());
                kernCompoundGPLVM.AddKern(new LinearKern());
                kernCompoundGPLVM.AddKern(new WhiteKern());

                // Construct BackConstraint kernel
                var kernBack = new RBFKernBack();
                kernBack.Parameter = 2;

                // Construct the dynamics kernel
                //latentDimensions
                var indexes = new List<ILArray<int>>();
                IKernel kernDynamics;
                if (usePGEKern)
                {
                    var pgeKern = new PGEKern();
                    var kernDynamicsParts = new List<IKernel>();
                    for (int i = 0; i < partsCount; i++)
                    {
                        var kernProductStyleRBF = new TensorKern();
                        //var kernStyle = new StyleAccelerationKern();
                        var kernCompound = new CompoundKern();
                        kernCompound.AddKern(new RBFAccelerationKern());
                        kernCompound.AddKern(new LinearAccelerationKern());
                        kernProductStyleRBF.AddKern(kernCompound);
                        kernDynamicsParts.Add(kernProductStyleRBF);
                        // Create indexes w.r.t. style variables
                        //kernProductStyleRBF.Indexes = ...

                        // Create indexes for current partial kernel
                        ILArray<int> partIndexes = ILMath.localMember<int>();
                        partIndexes.a = ILMath.counter<int>(0, 1, latentDimensions) + i * latentDimensions;
                        partIndexes.a = Util.Concatenate<int>(partIndexes, ILMath.counter<int>(0, 1, latentDimensions) + i * latentDimensions + partsCount * latentDimensions);
                        indexes.Add(partIndexes);
                    }
                    pgeKern.Kernels = kernDynamicsParts;
                    pgeKern.Indexes = indexes;
                    kernDynamics = pgeKern;

                    //ILArray<double> p = pgeKern.Parameter;
                    //p[partNumber] = 1;
                    //pgeKern.Parameter = p;
                }
                else
                {
                    var kernCompound = new CompoundKern();
                    kernCompound.AddKern(new RBFAccelerationKern());
                    kernCompound.AddKern(new LinearAccelerationKern());
                    kernCompound.AddKern(new WhiteKern());
                    kernDynamics = kernCompound;
                }

                // Factor nodes
                fnGPLVM = new GPLVMNode("GPLVM " + Name, kernCompoundGPLVM, GPLVMApproximation);
                if (useBackConstraint)
                {
                    fnBack = new KBRBackConstraintNode("Back " + Name, kernBack, dnX, dnY);
                    fnBack.DataConnectorKernel.ConnectDataNode(dnBackKern);
                }
                fnDynamics = new StyleAccelerationDynamicsNode("Dynamics " + Name, kernDynamics, dynamicsApproximation);
                fnDynamics.DynamicsIndexes = ILMath.counter<int>(0, 1, latentDimensions) + partNumber * latentDimensions;
                
                // Styled dynamics factor node needs special treatment
                fnDynamics.OnInitializeKernelIndexes += InitializeDynamicsKernelIndexes;
                if (useBackConstraint)
                    fnDynamics.DataNodes.Add(dnXCompound); // make dnXCompound searchable

                // Create emotion data node connector and connect it
                //var emotionDCDesc = fnDynamics.CreateFactorDataConnector("Emotion factor");
                //emotionDCDesc.ConnectionPoint.ConnectDataNode(dnEmotionStyle);

                // Add nodes to the factor graph
                graph.DataNodes.Add(dnY);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnGPLVMInducing);
                graph.DataNodes.Add(dnGPLVMLogScale);
                graph.DataNodes.Add(dnGPLVMKern);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnGPLVMBeta);
                if (fnDynamics.UseInducing())
                    graph.DataNodes.Add(dnDynamicsBeta);
                graph.DataNodes.Add(dnDynamicsKern);
                if (useBackConstraint)
                {
                    graph.DataNodes.Add(dnBackMap);
                    graph.DataNodes.Add(dnBackKern);
                    graph.FactorNodes.Add(fnBack);
                }
                //graph.DataNodes.Add(dnEmotionStyle);
                graph.FactorNodes.Add(fnGPLVM);
                graph.FactorNodes.Add(fnDynamics);

                fnGPLVM.DataConnectorX.ConnectDataNode(dnX);
                fnGPLVM.DataConnectorY.ConnectDataNode(dnY);
                fnGPLVM.DataConnectorKernel.ConnectDataNode(dnGPLVMKern);
                fnGPLVM.DataConnectorLogScale.ConnectDataNode(dnGPLVMLogScale);
                if (fnGPLVM.UseInducing())
                {
                    fnGPLVM.DataConnectorInducing.ConnectDataNode(dnGPLVMInducing);
                    fnGPLVM.DataConnectorBeta.ConnectDataNode(dnGPLVMBeta);
                }
                if (useBackConstraint)
                {
                    fnBack.DataConnectorA.ConnectDataNode(dnBackMap);
                }
                fnDynamics.DataConnectorKernel.ConnectDataNode(dnDynamicsKern);
                fnDynamics.DataConnectorX.ConnectDataNode(dnXCompound);
                //fnDynamics.DataNodes.Add(dnXCompound);
                if (fnDynamics.UseInducing())
                {
                    fnDynamics.DataConnectorBeta.ConnectDataNode(dnDynamicsBeta);
                }
                fnDynamics.CreatePredictors(1); // create one predictor
            }
            
            protected void InitializeDynamicsKernelIndexes()
            { 
                // Nothing to initialize here
            }

        }

        protected List<BodyPartPlate> bodyPartPlates = new List<BodyPartPlate>();
        protected CompoundMatrixDataNodeWithSegments dnXCompound;
        protected SkeletonMap.SkeletonMap FlubberMap;
        protected List<JointsGroupData> FlubberGroups;
        protected JointsGroupData GroupLower;
        protected JointsGroupData GroupUpper;
        protected bool useBackConstraint;
        protected bool usePGEKern;
        
        protected ILArray<double> prevX = ILMath.localMember<double>();

        public LearnStyledCoupledDynamicsExperiment(string sName, int latents)
            : base(sName, latents)
        {
            string sBVHFileName = @"..\..\..\..\..\..\Data\Emotional Walks\Niko\BVH\NikoMapped_NeutralWalk01.bvh";
            BVHData bvh = new BVHData(representation);
            ILArray<double> data = bvh.LoadFile(sBVHFileName);
            fFrameTime = bvh.FrameTime;
            skeleton = bvh.skeleton;
            //FlubberMap = PredefinedMaps.CreateFlubberFullMap(skeleton, JointsGroup.DataChannelsMode.RootRotation4Channels);
            FlubberMap = PredefinedMaps.CreateFlubberUpperLowerMap(skeleton, JointsGroup.DataChannelsMode.RootRotation4Channels);
            //FlubberMap = PredefinedMaps.CreateFlubberUpperLowerPelvisMap(skeleton, JointsGroup.DataChannelsMode.RootRotation4Channels);
            //FlubberMap = PredefinedMaps.CreateFlubberHeadUpperLowerPelvisMap(skeleton, JointsGroup.DataChannelsMode.RootRotation4Channels);
            
            useBackConstraint = false;
            usePGEKern = true;
            //CreateGraph();
        }

        public List<BodyPartPlate> BodyPartsPlates
        {
            get { return bodyPartPlates;  }
        }

        public override ILRetArray<double> GetXValues()
        {
            return dnXCompound.GetValues();
        }

        public override ILRetArray<int> GetSegments()
        {
            return bodyPartPlates[1].dnY.Segments;
        }

        //public ILRetArray<int> Segments
        //{
        //    get { return dnYLower.Segments; }
        //}

        public override FactorNode GetDynamicsNode()
        {
            throw new NotImplementedException();
        }

        public override void CreateGraph()
        {
            base.CreateGraph();

            dnXCompound = new CompoundMatrixDataNodeWithSegments("X");
            if (!useBackConstraint)
                graph.DataNodes.Add(dnXCompound);

            bodyPartPlates.Clear();
            int i = 0;
            foreach (var group in FlubberMap.ChannelsGroups)
            {
                var newBodyPartPlate = new BodyPartPlate(group.Name, graph,
                    FlubberMap.ChannelsGroups.Count, i, dnXCompound, 
                    LatentDimensions, useBackConstraint, usePGEKern);
                bodyPartPlates.Add(newBodyPartPlate);
                i++;
            }
            
            //UpdateReferences();
        }

        protected void InitializeDynamicsKernelIndexes()
        {
            //int k = 0;
            //foreach (FactorDesc desc in fnDynamics.FactorDescs)
            //{
            //    int nDimensions = desc.ConnectionPoint.ConnectedDataNode.GetValuesSize()[1];
            //    desc.IndexesInFullData.a = k + ILMath.counter<double>(0, 1, nDimensions);
            //    k = k + nDimensions;
            //}
            //// Stucture of X: {XUpperVector, XLowerVector, StyleVector}
            //// Setup factor indexes in full data set
            //kModulationProduct.Indexes = new List<ILArray<double>>();
            ////kProductStyleRBFUpper.Indexes = new List<ILArray<double>>();
            ////kProductStyleRBFLower.Indexes = new List<ILArray<double>>();
            ////ILArray<int> aDimensions = ILMath.zeros<int>(fnDynamics.FactorDescs.Count);
            ////for (int i = 0; i < fnDynamics.FactorDescs.Count; i++) // for all styles
            ////{
            ////    aDimensions[i] = fnDynamics.FactorDescs[i].ConnectionPoint.ConnectedDataNode.GetValuesSize()[1];
            ////}
            ////int nDimMotion = (int)aDimensions[0] / 2;
            ////int nDimStyles = 0; //(int)aDimensions[1];
            ////int nDimAll = 2 * nDimMotion + nDimStyles;

            //kModulationProduct.Indexes = new List<ILArray<double>>();
            //ILArray<double> ind0 = ILMath.localMember<double>();
            //ind0.a = ILMath.counter(LatentDimensions) - 1;
            //ind0.a = Util.Concatenate(ind0, ILMath.counter(LatentDimensions) - 1 + 2 * LatentDimensions);
            //kModulationProduct.Indexes.Add(ind0);
            //ILArray<double> ind1 = ILMath.localMember<double>();
            //ind1.a = ILMath.counter(LatentDimensions) - 1 + LatentDimensions;
            //ind1.a = Util.Concatenate(ind1, ILMath.counter(LatentDimensions) - 1 + 3 * LatentDimensions);
            //kModulationProduct.Indexes.Add(ind1);

            ////ILArray<double> aModIndUpper = ILMath.empty();
            ////aModIndUpper.a = ILMath.counter(nDimMotion) - 1;
            ////aModIndUpper.a = Util.Concatenate<double>(aModIndUpper, ILMath.counter(nDimStyles) - 1 + 2 * nDimMotion);
            ////aModIndUpper.a = Util.Concatenate<double>(aModIndUpper, ILMath.counter(nDimMotion) - 1 + nDimAll);
            ////aModIndUpper.a = Util.Concatenate<double>(aModIndUpper, ILMath.counter(nDimStyles) - 1 + nDimAll + 2 * nDimMotion);
            ////kModulationProduct.Indexes.Add(aModIndUpper);

            ////ILArray<double> aModIndLower = ILMath.empty();
            ////aModIndLower.a = ILMath.counter(nDimMotion) - 1 + nDimMotion;
            ////aModIndLower.a = Util.Concatenate<double>(aModIndLower, ILMath.counter(nDimStyles) - 1 + 2 * nDimMotion);
            ////aModIndLower.a = Util.Concatenate<double>(aModIndLower, ILMath.counter(nDimMotion) - 1 + nDimAll + nDimMotion);
            ////aModIndLower.a = Util.Concatenate<double>(aModIndLower, ILMath.counter(nDimStyles) - 1 + nDimAll + 2 * nDimMotion);
            ////kModulationProduct.Indexes.Add(aModIndLower);

            ////ILArray<double> aRBFInd = ILMath.localMember<double>();
            ////aRBFInd.a = ILMath.counter(nDimMotion) - 1;
            ////aRBFInd.a = Util.Concatenate(aRBFInd, ILMath.counter(nDimMotion) - 1 + nDimMotion + nDimStyles);
            ////ILArray<double> aStyleInd = ILMath.localMember<double>();
            ////aStyleInd.a = ILMath.counter(nDimStyles) - 1 + nDimMotion;
            ////aStyleInd.a = Util.Concatenate(aStyleInd, ILMath.counter(nDimStyles) - 1 + 2 * nDimMotion + nDimStyles);
            ////kProductStyleRBFUpper.Indexes.Add(aStyleInd.C);
            ////kProductStyleRBFUpper.Indexes.Add(aRBFInd.C);
            ////kProductStyleRBFLower.Indexes.Add(aStyleInd.C);
            ////kProductStyleRBFLower.Indexes.Add(aRBFInd.C);

            //fnDynamics.CreatePredictors(2); // two dynamics predictors for upper and lower bodies
        }

        //protected List<IKernel> FindAllKernels(IKernel kernel, Type type)
        //{
        //    var res = new List<IKernel>();
        //    if (kernel.GetType() == type)
        //        res.Add(kernel);
        //    if ((kernel is CompoundKern) || (kernel is TensorKern))
        //    {
        //        List<IKernel> lKernels = kernel is CompoundKern ? ((CompoundKern)kernel).Kernels : ((TensorKern)kernel).Kernels;
        //        foreach (IKernel k in lKernels)
        //        {
        //            res.AddRange(FindAllKernels(k, type));
        //        }
        //    }
        //    if (kernel is ModulationKern)
        //        res.AddRange(FindAllKernels(((ModulationKern)kernel).GetInnerKern(), type));
        //    return res;
        //}

        public override void LoadDataSet(DataSet dataSet)
        {
            // So far we can handle only ine style
            using (ILScope.Enter())
            {
                CreateGraph();

                var substyles = dataSet.Styles.First().Substyles;
                int dataSetSamples = 0;
                foreach (FileDesc fileDesc in dataSet.Files)
                {
                    var substyle = fileDesc.Substyles.First().Substyle;
                    var fileName = fileDesc.FileName;

                    BVHData bvh = new BVHData(representation);
                    ILArray<double> data = bvh.LoadFile(fileName);
                    fFrameTime = bvh.FrameTime;
                    skeleton = bvh.skeleton;

                    //data = Regression.RemoveLinear(data);

                    FlubberGroups = FlubberMap.SplitDataIntoGroups(data);
                    int i = 0;
                    foreach (var group in FlubberGroups)
                    {
                        bodyPartPlates[i].dnY.AddData(group.Data);
                        i++;
                    }
                    //dnEmotionStyle.AddData(substyle, data.S[0]);
                    dataSetSamples += data.S[0];
                }

                foreach (var part in bodyPartPlates)
                {
                    part.dnX.SetValuesSize(new ILSize(new int[] { dataSetSamples, LatentDimensions }));
                    part.dnX.SetOptimizingMaskAll();
                    part.dnY.SetOptimizingMaskNone();
                    // X and Y spaces share the segments data
                    part.dnX.Segments = part.dnY.Segments;
                }
                foreach (var part in bodyPartPlates)
                {
                    int k = 0;
                    foreach (FactorDesc desc in part.fnDynamics.FactorDescs)
                    {
                        int nDimensions = desc.ConnectionPoint.ConnectedDataNode.GetValuesSize()[1];
                        desc.IndexesInFullData.a = k + ILMath.counter<double>(0, 1, nDimensions);
                        k = k + nDimensions;
                    }
                }
            }
            factorNodesInitialized = false;
            //UpdateReferences();
        }

        public override void LoadBVHData(string sFileName)
        {
            throw new NotImplementedException();
        }

        protected bool factorNodesInitialized = false;
        public override void InitFactorNodes()
        {
            if (!factorNodesInitialized)
            {
                foreach (var part in bodyPartPlates)
                {
                    // Initialize the nodes
                    part.fnGPLVM.NumInducingMax = NumberOfInducingPoints;
                    part.fnGPLVM.Initialize();
                }
                // Init upper from lower
                if (bodyPartPlates.Count == 2)
                    bodyPartPlates[0].dnX.SetValues(bodyPartPlates[1].dnX.GetValues());
                if (useBackConstraint)
                {
                    foreach (var part in bodyPartPlates)
                        part.fnBack.Initialize();
                }
                foreach (var part in bodyPartPlates)
                {
                    // Dynamics has to be initilaized after back constraints
                    part.fnDynamics.NumInducingMax = NumberOfInducingPoints;
                    part.fnDynamics.Initialize();
                }
                // Force recalculation of gradients
                // This is required for correct initialiaztion of the BackConstraint gradients!!!
                graph.ComputeGradients();
                //graph.Gradient();
                //UpdateReferences();
                factorNodesInitialized = true;
                return;
            }
            else
                graph.ComputeGradients();
        }

        public override void RunOptimization()
        {
            base.RunOptimization();
            foreach (var part in bodyPartPlates)
            {
                part.fnDynamics.CreatePredictors(1); // re-create one predictor
                part.fnDynamics.UpdatePredictors();
            }
        }

        public override void PrintReport()
        {
            ILArray<double> kParams = ILMath.empty();
            foreach (var part in bodyPartPlates)
            {
                kParams.a = Util.Concatenate<double>(kParams, part.fnDynamics.KernelObject.Parameter);
            }
            Console.WriteLine("Optimized dynamics kernel parameters: " + kParams.T.ToString());

            kParams = ILMath.empty();
            foreach (var part in bodyPartPlates)
            {
                kParams.a = Util.Concatenate<double>(kParams, part.fnGPLVM.KernelObject.Parameter);
            }
            Console.WriteLine("Optimized GPLVM kernel parameters: " + kParams.T.ToString());

            //Console.WriteLine("Back mapping: " + dnBackMap.GetValues().ToString());
            //Console.WriteLine("Upper mapping: " + dnBackMapUpper.GetValues().ToString());
            //Console.WriteLine("Lower mapping: " + dnBackMapLower.GetValues().ToString());
            //Console.WriteLine("Kernel parameters:"); 
            //foreach (var part in bodyPartPlates)
            //{
            //    Console.WriteLine(part.fnDynamics.KernelObject.Parameter[ILMath.full].ToString());
            //}
        }

        protected int iFrame = 0;
        public override void InitGeneration()
        {
            // Take first two points in X space
            prevX.a = dnXCompound.GetValues()[ILMath.r(0, 1), ILMath.full];
            //double speedFactor = 1.0;
            //prevX[1, ILMath.full] = (1 - speedFactor) * prevX[0, ILMath.full] + speedFactor * prevX[1, ILMath.full];

            // Adjust the start phase
            //double frameStep = (double)dnBackMapUpper.GetValues()[1, 0];
            //int skipUpper = (int)(2 * Math.PI * StartPhaseUpper / frameStep);
            //int skipLower = (int)(2 * Math.PI * StartPhaseLower / frameStep);

            iFrame = 0;
        }

        public override bool GenerateFrame(ILOutArray<double> frameData, out Representation representationType)
        {
            representationType = representation;

            ILArray<double> nextX = ILMath.zeros(new ILSize(new int[] {1, prevX.S[1]}));
            var groups = new List<JointsGroupData>();
            int i = 0;
            foreach (var part in bodyPartPlates)
            {
                ILArray<double> partXNew = part.fnDynamics.PredictorsSimulateDynamics(0, prevX, 3)[ILMath.end, ILMath.full];
                ILArray<double> partYNew = part.fnGPLVM.PredictData(partXNew);
                groups.Add(FlubberGroups[i].CreateCopyWithNewData(partYNew));
                nextX[part.fnDynamics.DynamicsIndexes] = partXNew;
                i++;
            }
            frameData.a = FlubberMap.MergeDataFromGroups(groups);

            // Update 2-nd order dynamics history
            prevX[0, ILMath.full] = prevX[1, ILMath.full];
            prevX[1, ILMath.full] = nextX;
            return true;
        }

        public override void SaveToFile(string prefix, string sFileName)
        {
            base.SaveToFile(prefix, sFileName);
            Serializer.Serialize(FlubberMap, prefix + "Flubber map " + sFileName);
        }

        public override void LoadFromFile(string prefix, string sFileName)
        {
            base.LoadFromFile(prefix, sFileName);
            FlubberMap = (SkeletonMap.SkeletonMap)Serializer.Deserialize(typeof(SkeletonMap.SkeletonMap), prefix + "Flubber map " + sFileName);
            FlubberGroups = FlubberMap.SplitDataIntoGroups(ILMath.empty());
            GroupLower = FlubberGroups.First(x => x.Group.Name == "Lower body part");
            GroupUpper = FlubberGroups.First(x => x.Group.Name == "Upper body part");
            factorNodesInitialized = false;
            UpdateReferences();
        }

        public void UpdateReferences()
        {
            // Update references to nodes
            //dnXCompound = graph.FindDataNodeByName("X") as CompoundMatrixDataNodeWithSegments;
            //dnXUpper = graph.FindDataNodeByName("X upper") as DataNodeWithSegments;
            //dnYUpper = graph.FindDataNodeByName("Y upper") as DataNodeWithSegments;
            //fnGPLVMUpper = graph.FactorNodes.FindByName("GPLVM upper") as GPLVMNode;
            //dnXLower = graph.FindDataNodeByName("X lower") as DataNodeWithSegments;
            //dnYLower = graph.FindDataNodeByName("Y lower") as DataNodeWithSegments;
            //dnBackMapUpper = graph.FindDataNodeByName("Back constraints mapping parameters upper") as MatrixDataNode;
            //dnBackMapLower = graph.FindDataNodeByName("Back constraints mapping parameters lower") as MatrixDataNode;
            //fnGPLVMLower = graph.FactorNodes.FindByName("GPLVM lower") as GPLVMNode;
            ////fnDynamics = graph.FactorNodes.FindByName("Dynamics") as AccelerationDynamicsNode;
            //fnDynamics = graph.FactorNodes.FindByName("Dynamics") as StyleAccelerationDynamicsNode;

            //fnBackUpper = graph.FactorNodes.FindByName("Back upper") as KBRBackConstraintNode;
            //fnBackLower = graph.FactorNodes.FindByName("Back lower") as KBRBackConstraintNode;
            
            //var lKernels = FindAllKernels(fnDynamics.KernelObject, typeof(ModulationKern));
            //kModulationUpper = (ModulationKern)lKernels[0];
            //kModulationLower = (ModulationKern)lKernels[1];
            //if (fnDynamics.PredictionStructs.Count == 2)
            //{
            //    lKernels = FindAllKernels(fnDynamics.PredictionStructs[0].Kernel, typeof(ModulationKern));
            //    kModulationUpperToUpper = (ModulationKern)lKernels[0];
            //    kModulationLowerToUpper = (ModulationKern)lKernels[1];
            //    lKernels = FindAllKernels(fnDynamics.PredictionStructs[1].Kernel, typeof(ModulationKern));
            //    kModulationUpperToLower = (ModulationKern)lKernels[0];
            //    kModulationLowerToLower = (ModulationKern)lKernels[1];
            //}
        }

        public override ILRetArray<double> PredictFullX(int kSizeFactor = 1)
        {
            ILArray<double> XStart = ILMath.empty();
            XStart.a = dnXCompound.GetValues()[ILMath.r(0, 1), ILMath.full];
            int nSteps = kSizeFactor * (dnXCompound.GetValues().S[0]) - 2;

            ILArray<double> XPredicted = XStart.C;
            ILArray<double> nextX = ILMath.zeros(new ILSize(new int[] {1, XStart.S[1]}));
            for (int i = 0; i < nSteps; i++)
            {
                XStart = XPredicted[ILMath.r(ILMath.end - 1, ILMath.end), ILMath.full];
                foreach (var part in bodyPartPlates)
                {
                    ILArray<double> partXNew = part.fnDynamics.PredictorsSimulateDynamics(0, XStart, 3)[ILMath.end, ILMath.full];
                    nextX[part.fnDynamics.DynamicsIndexes] = partXNew;
                }
                XPredicted.a = Util.Concatenate<double>(XPredicted, nextX);
            }

            return XPredicted;
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