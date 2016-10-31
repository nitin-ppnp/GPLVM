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

namespace FactorGraphTest
{
    public class PartPlate
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

        public PartPlate(string partName, Graph graph, int partsCount, int partNumber,
            CompoundMatrixDataNodeWithSegments dnXCompound, int latentDimensions,
            bool useBackConstraint,
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
        }

        protected void InitializeDynamicsKernelIndexes()
        {
            // Nothing to initialize here
        }
    }
}
