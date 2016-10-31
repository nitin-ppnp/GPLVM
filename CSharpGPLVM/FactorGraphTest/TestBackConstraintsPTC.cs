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
using GPLVM.Dynamics;
using GPLVM.Backconstraint;
using FactorGraph.Core;
using FactorGraph.FactorNodes;

namespace FactorGraphTest
{
    partial class Program
    {
        static ILRetArray<double> TestOriginalBackConstraintPTC(ILInArray<double> inData, ApproximationType aType)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check<double>(inData);

                GP_LVM gplvm = new GP_LVM(nLatents, aType, BackConstType.ptc);
                gplvm.AddData(data);
                gplvm.NumInducing = data.S[0] / 2;
                GPAcceleration acceleration = new GPAcceleration();
                gplvm.Prior = acceleration;
                gplvm.Initialize();

                SCGOptimizer optimizer = new SCGOptimizer();
                optimizer.sLogFileName = "CSharpBack_Orig.log";
                optimizer.bLogEnabled = bLogEnabled;
                optimizer.Optimize(new GPLVMToFunctionWithGradientAdapter(gplvm), nIterations, true);

                ILArray<double> XStart = ILMath.empty();
                XStart.a = gplvm.X[ILMath.r(0, 1), ILMath.full];
                ILArray<double> YPredicted = ILMath.empty();
                YPredicted.a = gplvm.PredictData(acceleration.SimulateDynamics(XStart, 10));
                Console.WriteLine("Predicted data: " + YPredicted.ToString());
                return YPredicted;
            }
        }

        static ILRetArray<double> TestFactorGraphBackConstraintPTC(ILInArray<double> inData, ApproximationType aType)
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

                // Construct BackConstraint kernel
                var kBack = new RBFKernBack();
                kBack.Parameter = 2;

                // Data nodes
                var dnX = new MatrixDataNode("X");
                var dnY = new MatrixDataNode("Y");
                var dnLogScale = new MatrixDataNode("Log Scale");
                var dnKGPLVM = new MatrixDataNode("GPLVM kernel parameters");
                var dnKDynamics = new MatrixDataNode("Dynamics kernel parameters");
                var dnInducing = new MatrixDataNode("Inducing variables");
                var dnBeta = new MatrixDataNode("Beta");
                var dnKBack = new MatrixDataNode("Back constraints kernel parameters");
                var dnBackMap = new MatrixDataNode("Back constraints mapping parameters");

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
                var fnBack = new PTCBackConstraintNode("Back constraints", dnX, dnY);

                // Construct factor graph
                var graph = new Graph("Simle GPDM");
                //graph.DataNodes.Add(dnX); // dnX is already in BackConstraint node
                graph.DataNodes.Add(dnBackMap);
                graph.DataNodes.Add(dnY);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnInducing);
                graph.DataNodes.Add(dnLogScale);
                graph.DataNodes.Add(dnKGPLVM);
                if (fnGPLVM.UseInducing())
                    graph.DataNodes.Add(dnBeta);
                graph.DataNodes.Add(dnKDynamics);
                graph.DataNodes.Add(dnKBack);
                graph.FactorNodes.Add(fnGPLVM);
                graph.FactorNodes.Add(fnDynamics);
                graph.FactorNodes.Add(fnBack);

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
                fnBack.DataConnectorA.ConnectDataNode(dnBackMap);
                
                ILArray<double> YPredicted = ILMath.empty();
                try
                {
                    // Initialize the nodes. Order is important
                    fnGPLVM.Initialize();
                    fnBack.Initialize();
                    fnDynamics.Initialize();

                    // Force recalculation of gradients
                    graph.ComputeGradients();

                    var optimizer = new SCGOptimizer();
                    optimizer.sLogFileName = "CSharpBack_New.log";
                    optimizer.bLogEnabled = bLogEnabled;
                    optimizer.Optimize(graph, nIterations, true);

                    ILArray<double> XStart = ILMath.empty();
                    XStart.a = dnX.GetValues()[ILMath.r(0, 1), ILMath.full];
                    YPredicted.a = fnGPLVM.PredictData(fnDynamics.SimulateDynamics(XStart, 10));
                    Console.WriteLine("Predicted data: " + YPredicted.ToString());
                }
                catch (GraphException e)
                {
                    Console.WriteLine(e.ToString());
                }
                return YPredicted;
            }
        }

        static void TestBackConstraintPTC(ILInArray<double> inData, ApproximationType aType = ApproximationType.ftc, double fErrorTolarance = 1e-6)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check<double>(inData);

                Console.WriteLine("\r\n###############################################################################");
                Console.WriteLine("GPDM with BackConstraints. Approximation type: " + aType.ToString());

                Console.WriteLine("\r\nFactorGraph GPDM with BackConstraints:");
                ILArray<double> predictedFactorGraph = ILMath.empty();
                predictedFactorGraph.a = TestFactorGraphBackConstraintPTC(data, aType);
                
                Console.WriteLine("\r\nOriginal GPDM with PTC BackConstraints:");
                ILArray<double> predictedOriginal = ILMath.empty();
                predictedOriginal.a = TestOriginalBackConstraintPTC(data, aType);
                
                if (ILMath.allall(ILMath.abs(predictedOriginal - predictedFactorGraph) <= fErrorTolarance))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("(Original GPDM == FactorGraph GPDM) test PASSED");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("(Original GPDM == FactorGraph GPDM) test FAILED");
                }
                Console.ResetColor();
            }
        }
    }
}
