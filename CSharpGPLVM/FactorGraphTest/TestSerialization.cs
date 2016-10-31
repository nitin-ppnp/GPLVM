using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Kernel;
using GPLVM.Numerical;
using GPLVM.Optimisation;
using FactorGraph.Core;
using FactorGraph.FactorNodes;
using FactorGraph.Utils;

namespace FactorGraphTest
{
    public partial class Program
    {
        static void TestFactorGraphGPLVMSerialization(ILInArray<double> inY, ILInArray<double> inTestInput, ApproximationType aType = ApproximationType.ftc, double fErrorTolarance = 1e-6)
        {
            using (ILScope.Enter(inY, inTestInput))
            {
                ILArray<double> Y = ILMath.check<double>(inY);
                ILArray<double> testInput = ILMath.check<double>(inTestInput);

                // Construct a kernel
                var kCompound = new CompoundKern();
                var kRBF = new RBFKern();
                var kLinear = new LinearKern();
                var kWhite = new WhiteKern();
                kCompound.AddKern(kRBF);
                kCompound.AddKern(kLinear);
                kCompound.AddKern(kWhite);

                // Data nodes
                var dnX = new MatrixDataNode("X");
                var dnY = new MatrixDataNode("Y");
                var dnK = new MatrixDataNode("Kernel parameters");
                var dnLogScale = new MatrixDataNode("Log Scale");
                var dnInducing = new MatrixDataNode("Inducing variables");
                var dnBeta = new MatrixDataNode("Beta");

                int N = Y.S[0];
                int D = Y.S[1];
                int Q = nLatents;
                dnX.SetValuesSize(new ILSize(new int[] { N, Q }));
                dnY.SetValuesSize(new ILSize(new int[] { N, D }));
                dnX.SetOptimizingMaskAll();
                dnY.SetOptimizingMaskNone();

                dnY.SetValues(Y);

                // Factor nodes
                var fnGPLVM = new GPLVMNode("GPLVM", kCompound, aType);
                fnGPLVM.NumInducingMax = Y.S[0] / 2;

                // Construct factor graph
                var graph = new Graph("Simle GPLVM");
                graph.DataNodes.Add(dnX);
                graph.DataNodes.Add(dnY);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnInducing);
                graph.DataNodes.Add(dnLogScale);
                graph.DataNodes.Add(dnK);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnBeta);
                graph.FactorNodes.Add(fnGPLVM);

                fnGPLVM.DataConnectorX.ConnectDataNode(dnX);
                fnGPLVM.DataConnectorY.ConnectDataNode(dnY);
                fnGPLVM.DataConnectorLogScale.ConnectDataNode(dnLogScale);
                fnGPLVM.DataConnectorKernel.ConnectDataNode(dnK);
                if (fnGPLVM.UseInducing())
                {
                    fnGPLVM.DataConnectorInducing.ConnectDataNode(dnInducing);
                    fnGPLVM.DataConnectorBeta.ConnectDataNode(dnBeta);
                }

                ILArray<double> YPredicted = ILMath.empty();
                ILArray<double> YPredictedLoaded = ILMath.empty();
                try
                {
                    // Initialize the nodes
                    fnGPLVM.Initialize();

                    var optimizer = new SCGOptimizer();
                    optimizer.sLogFileName = "CSharpGPLVM_New.log";
                    optimizer.bLogEnabled = bLogEnabled;
                    optimizer.Optimize(graph, 1, true);

                    Serializer.Serialize(graph, "Graph.xml");
                    Graph graphLoaded = (Graph)Serializer.Deserialize(typeof(Graph), "Graph.xml");

                    YPredicted.a = fnGPLVM.PredictData(testInput);
                    YPredictedLoaded.a = ((GPLVMNode)graphLoaded.FactorNodes.FindByName("GPLVM")).PredictData(testInput);
                    Console.WriteLine("Predicted data: " + YPredicted.ToString());
                    Console.WriteLine("Predicted data loaded: " + YPredictedLoaded.ToString());

                    if (ILMath.allall(ILMath.abs(YPredicted - YPredictedLoaded) <= fErrorTolarance))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("(FactorGraph GPLVM == Loaded FactorGraph GPLVM) test PASSED");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("(FactorGraph GPLVM == Loaded FactorGraph GPLVM) test FAILED");
                    }
                    Console.ResetColor();
                }
                catch (GraphException e)
                {
                    Console.WriteLine(e.ToString());
                    throw e;
                }                
            }
        }

        static ILRetArray<double> TestFactorGraphGPDMSerialization(ILInArray<double> inData, ApproximationType aType, double fErrorTolarance = 1e-6)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check<double>(inData);

                // Construct the GPLVM kernel
                var kCompoundGPLVM = new CompoundKern();
                kCompoundGPLVM.AddKern(new RBFKern());
                kCompoundGPLVM.AddKern(new LinearKern());
                kCompoundGPLVM.AddKern(new WhiteKern());

                // Construct the dynamics kernel
                var kCompoundDynamics = new CompoundKern();
                kCompoundDynamics.AddKern(new RBFAccelerationKern());
                kCompoundDynamics.AddKern(new LinearAccelerationKern());
                kCompoundDynamics.AddKern(new WhiteKern());

                // Data nodes
                var dnX = new MatrixDataNode("X");
                var dnY = new MatrixDataNode("Y");
                var dnLogScale = new MatrixDataNode("Log Scale");
                var dnKGPLVM = new MatrixDataNode("GPLVM kernel parameters");
                var dnKDynamics = new MatrixDataNode("Dynamics kernel parameters");
                var dnInducing = new MatrixDataNode("Inducing variables");
                var dnBeta = new MatrixDataNode("Beta");

                int N = data.S[0];
                int D = data.S[1];
                int Q = nLatents;
                dnX.SetValuesSize(new ILSize(new int[] { N, Q }));
                dnY.SetValuesSize(new ILSize(new int[] { N, D }));
                dnX.SetOptimizingMaskAll();
                dnY.SetOptimizingMaskNone();

                dnY.SetValues(data);

                // Factor nodes
                var fnGPLVM = new GPLVMNode("GPLVM", kCompoundGPLVM, aType);
                fnGPLVM.NumInducingMax = data.S[0] / 2;
                var fnDynamics = new AccelerationDynamicsNode("Dynamics", kCompoundDynamics);

                // Construct factor graph
                var graph = new Graph("Simle GPDM");
                graph.DataNodes.Add(dnX);
                graph.DataNodes.Add(dnY);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnInducing);
                graph.DataNodes.Add(dnLogScale);
                graph.DataNodes.Add(dnKGPLVM);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnBeta);
                graph.DataNodes.Add(dnKDynamics);
                graph.FactorNodes.Add(fnGPLVM);
                graph.FactorNodes.Add(fnDynamics);

                fnGPLVM.DataConnectorX.ConnectDataNode(dnX);
                fnGPLVM.DataConnectorY.ConnectDataNode(dnY);
                fnGPLVM.DataConnectorKernel.ConnectDataNode(dnKGPLVM);
                fnGPLVM.DataConnectorLogScale.ConnectDataNode(dnLogScale);
                if (fnGPLVM.UseInducing())
                {
                    fnGPLVM.DataConnectorInducing.ConnectDataNode(dnInducing);
                    fnGPLVM.DataConnectorBeta.ConnectDataNode(dnBeta);
                }
                fnDynamics.DataConnectorKernel.ConnectDataNode(dnKDynamics);
                fnDynamics.DataConnectorX.ConnectDataNode(dnX);

                ILArray<double> YPredicted = ILMath.empty();
                ILArray<double> YPredictedLoaded = ILMath.empty();
                try
                {
                    // Initialize the nodes
                    fnGPLVM.Initialize();
                    fnDynamics.Initialize();

                    var optimizer = new SCGOptimizer();
                    optimizer.sLogFileName = "CSharpGPDM_New.log";
                    optimizer.bLogEnabled = bLogEnabled;
                    optimizer.Optimize(graph, nIterations, true);

                    Serializer.Serialize(graph, "Graph.xml");
                    Graph graphLoaded = (Graph)Serializer.Deserialize(typeof(Graph), "Graph.xml");


                    ILArray<double> XStart = ILMath.empty();
                    XStart.a = dnX.GetValues()[ILMath.r(0, 1), ILMath.full];
                    YPredicted.a = fnGPLVM.PredictData(fnDynamics.SimulateDynamics(XStart, 10));
                    YPredictedLoaded.a = ((GPLVMNode)graphLoaded.FactorNodes.FindByName("GPLVM")).PredictData(
                        ((AccelerationDynamicsNode)graphLoaded.FactorNodes.FindByName("Dynamics")).SimulateDynamics(XStart, 10));
                    Console.WriteLine("Predicted data: " + YPredicted.ToString());
                    Console.WriteLine("Predicted data loaded: " + YPredictedLoaded.ToString());

                    if (ILMath.allall(ILMath.abs(YPredicted - YPredictedLoaded) <= fErrorTolarance))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("(FactorGraph GPDM == Loaded FactorGraph GPDM) test PASSED");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("(FactorGraph GPDM == Loaded FactorGraph GPDM) test FAILED");
                    }
                    Console.ResetColor();
                }
                catch (GraphException e)
                {
                    Console.WriteLine(e.ToString());
                }
                return YPredicted;
            }
        }
    }    
}
