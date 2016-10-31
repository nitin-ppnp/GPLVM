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
using GPLVM.Styles;
using FactorGraph.Core;
using FactorGraph.FactorNodes;
using FactorGraph.DataNodes;

namespace FactorGraphTest
{
    partial class Program
    {
        class TrialDataWithStyle
        {
            public ILArray<double> Data;
            public string Substyle;

            public TrialDataWithStyle(ILArray<double> inData, string sSubstyle)
            {
                Data = inData.C;
                Substyle = sSubstyle;
            }
        }

        class TrialDataWithStyleList : List<TrialDataWithStyle>
        {
        }

        static ILRetArray<double> TestOriginalStyleGPLVM(TrialDataWithStyleList lTrials, ApproximationType aType)
        {
            StyleGPLVM2 sgplvm = new StyleGPLVM2(nLatents, aType);
            Style style = new Style("Emotion");
            sgplvm.AddStyle("Emotion", style);
            foreach (TrialDataWithStyle trial in lTrials)
            {
                style.AddSubStyle(trial.Substyle, trial.Data.S[0]);
                sgplvm.AddData(trial.Data);
            }

            sgplvm.NumInducing = lTrials.First().Data.S[0] / 2;
            sgplvm.Initialize();

            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.sLogFileName = "CSharpStyleGPLVM_Orig.log";
            optimizer.bLogEnabled = bLogEnabled;
            optimizer.Optimize(new GPLVMToFunctionWithGradientAdapter(sgplvm), nIterations, true);

            ILArray<double> testInput = sgplvm.X[0, ILMath.r(0, nLatents-1)].C;
            testInput.a = Util.Concatenate<double>(
                testInput, 
                ILMath.repmat(0.5 + ILMath.zeros<double>(1, style.SubStyles.Count), testInput.S[0], 1), 
                1);

            ILArray<double> YPredicted = sgplvm.PredictData(testInput);
            Console.WriteLine("Latent input: " + testInput.ToString());
            Console.WriteLine("Predicted data: " + YPredicted.ToString());
            return YPredicted;
        }

        static ILRetArray<double> TestFactorGraphStyleGPLVM(TrialDataWithStyleList lTrials, ApproximationType aType)
        {
            // Construct a kernel
            var kRBFCompound = new CompoundKern();
            var kRBF = new RBFKern();
            var kLinear = new LinearKern();
            kRBFCompound.AddKern(kRBF);
            kRBFCompound.AddKern(kLinear);
            var kStyle = new StyleKern();
            var kTensor = new TensorKern();
            kTensor.AddKern(kRBFCompound);
            kTensor.AddKern(kStyle);
            var kFullCompound = new CompoundKern();
            var kWhite = new WhiteKern();
            kFullCompound.AddKern(kTensor);
            kFullCompound.AddKern(kWhite);
            
            
            // Data nodes
            var dnX = new MatrixDataNode("X");
            var dnY = new DataNodeWithSegments("Y");
            var dnK = new MatrixDataNode("Kernel parameters");
            var dnLogScale = new MatrixDataNode("Log Scale");
            var dnInducing = new MatrixDataNode("Inducing variables");
            var dnBeta = new MatrixDataNode("Beta");
            var dnStyle = new StyleDataNode("Emotion style");
            var dnStyleInducing = new StyleDataNode("Emotion style inducing");

            int N = 0;
            foreach (TrialDataWithStyle trial in lTrials)
            {
                dnY.AddData(trial.Data);
                dnStyle.AddData(trial.Substyle, trial.Data.S[0]);
                N += trial.Data.S[0];
            }
            
            int Q = nLatents;
            dnX.SetValuesSize(new ILSize(new int[] { N, Q }));
            dnX.SetOptimizingMaskAll();
            dnY.SetOptimizingMaskNone();

            // Factor nodes
            var fnGPLVM = new StyleGPLVMNode("Style GPLVM", kFullCompound, aType);
            fnGPLVM.NumInducingMax = lTrials.First().Data.S[0] / 2;

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
            graph.DataNodes.Add(dnStyle);
            if (fnGPLVM.UseInducing())
                graph.DataNodes.Add(dnStyleInducing);

            fnGPLVM.DataConnectorX.ConnectDataNode(dnX);
            fnGPLVM.DataConnectorY.ConnectDataNode(dnY);
            fnGPLVM.DataConnectorLogScale.ConnectDataNode(dnLogScale);
            fnGPLVM.DataConnectorKernel.ConnectDataNode(dnK);
            var styleDCDesc = fnGPLVM.CreateFactorDataConnector("Emotional style factor");
            styleDCDesc.ConnectionPoint.ConnectDataNode(dnStyle);

            if (fnGPLVM.UseInducing())
            {
                fnGPLVM.DataConnectorInducing.ConnectDataNode(dnInducing);
                fnGPLVM.DataConnectorBeta.ConnectDataNode(dnBeta);
                styleDCDesc.ConnectionPointInducing.ConnectDataNode(dnStyleInducing);
            }

            ILArray<double> YPredicted = ILMath.empty();
            try
            {
                // Initialize the nodes
                fnGPLVM.Initialize();

                var optimizer = new SCGOptimizer();
                optimizer.sLogFileName = "CSharpStyleGPLVM_New.log";
                optimizer.bLogEnabled = bLogEnabled;
                optimizer.Optimize(graph, nIterations, true);

                ILArray<double> testInput = dnX.GetValues()[0, ILMath.full].C;
                testInput.a = Util.Concatenate<double>(
                    testInput,
                    ILMath.repmat(0.5 + ILMath.zeros<double>(1, dnStyle.Substyles.Count), testInput.S[0], 1),
                    1);

                YPredicted.a = fnGPLVM.PredictData(testInput);
                Console.WriteLine("Latent input: " + testInput.ToString());
                Console.WriteLine("Predicted data: " + YPredicted.ToString());
            }
            catch (GraphException e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
            return YPredicted;

        }

        static void TestStyleGPLVM(TrialDataWithStyleList lTrials, ApproximationType aType = ApproximationType.ftc, double fErrorTolarance = 1e-6)
        {
            Console.WriteLine("\r\n###############################################################################");
            Console.WriteLine("Style GPLVM. Approximation type: " + aType.ToString());
            Console.WriteLine("\r\nOriginal StyleGPLVM:");
            ILArray<double> predictedOriginal = ILMath.empty();
            predictedOriginal.a = TestOriginalStyleGPLVM(lTrials, aType);

            Console.WriteLine("\r\nFactorGraph StyleGPLVM:");
            ILArray<double> predictedFactorGraph = ILMath.empty();
            predictedFactorGraph.a = TestFactorGraphStyleGPLVM(lTrials, aType);

            if (ILMath.allall(ILMath.abs(predictedOriginal - predictedFactorGraph) <= fErrorTolarance))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("(Original StyleGPLVM == FactorGraph StyleGPLVM) test PASSED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("(Original StyleGPLVM == FactorGraph StyleGPLVM) test FAILED");
            }
            Console.ResetColor();
        }
    }
}
