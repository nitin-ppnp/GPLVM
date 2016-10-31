using System;
using System.Collections.Generic;
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


namespace FactorGraphTest
{
    partial class Program
    {
        static ILRetArray<double> TestOriginalGPLVM(ILInArray<double> inY, ILInArray<double> inTestInput, ApproximationType aType)
        {
            using (ILScope.Enter(inY, inTestInput))
            {
                ILArray<double> Y = ILMath.check<double>(inY);
                ILArray<double> testInput = ILMath.check<double>(inTestInput);

                GP_LVM gplvm = new GP_LVM(nLatents, aType);
                gplvm.AddData(Y);
                gplvm.NumInducing = Y.S[0] / 2;

                gplvm.Initialize();
                SCGOptimizer optimizer = new SCGOptimizer();
                optimizer.sLogFileName = "CSharpGPLVM_Orig.log";
                optimizer.bLogEnabled = bLogEnabled;
                optimizer.Optimize(new GPLVMToFunctionWithGradientAdapter(gplvm), nIterations, true);
                ILArray<double> YPredicted = gplvm.PredictData(testInput);
                Console.WriteLine("Predicted data: " + YPredicted.ToString());
                return YPredicted;
            }
        }

        static ILRetArray<double> TestFactorGraphGPLVM(ILInArray<double> inY, ILInArray<double> inTestInput, ApproximationType aType)
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
                try
                {
                    // Initialize the nodes
                    fnGPLVM.Initialize();

                    var optimizer = new SCGOptimizer();
                    optimizer.sLogFileName = "CSharpGPLVM_New.log";
                    optimizer.bLogEnabled = bLogEnabled;
                    optimizer.Optimize(graph, nIterations, true);
                    YPredicted.a = fnGPLVM.PredictData(testInput);
                    Console.WriteLine("Predicted data: " + YPredicted.ToString());
                }
                catch (GraphException e)
                {
                    Console.WriteLine(e.ToString());
                    throw e;
                }
                return YPredicted;
            }
        }

        static void TestGPLVM(ILInArray<double> inData, ILInArray<double> inTestInput, ApproximationType aType = ApproximationType.ftc, double fErrorTolarance = 1e-6)
        {
            using (ILScope.Enter(inData, inTestInput))
            {
                ILArray<double> data = ILMath.check<double>(inData);
                ILArray<double> testInput = ILMath.check<double>(inTestInput);

                Console.WriteLine("\r\n###############################################################################");
                Console.WriteLine("GPLVM. Approximation type: " + aType.ToString());
                Console.WriteLine("\r\nOriginal GPLVM:");
                ILArray<double> predictedOriginal = ILMath.empty();
                predictedOriginal.a = TestOriginalGPLVM(data, testInput, aType);

                Console.WriteLine("\r\nFactorGraph GPLVM:");
                ILArray<double> predictedFactorGraph = ILMath.empty();
                predictedFactorGraph.a = TestFactorGraphGPLVM(data, testInput, aType);

                if (ILMath.allall(ILMath.abs(predictedOriginal - predictedFactorGraph) <= fErrorTolarance))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("(Original GPLVM == FactorGraph GPLVM) test PASSED");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("(Original GPLVM == FactorGraph GPLVM) test FAILED");
                }
                Console.ResetColor();
            }
        }
    }
}
