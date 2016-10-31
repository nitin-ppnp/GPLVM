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
using FactorGraph.Utils;
using FactorGraph.Core;
using FactorGraph.FactorNodes;
using FactorGraph.DataNodes;

namespace FactorGraphTest
{
    partial class Program
    {
        static ILRetArray<double> TestFactorGraphPGEKern(ILInArray<double> inData, ILInArray<int> inPartsIndexes, int nLatents)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check<double>(inData);
                ILArray<int> partsIndexes = ILMath.check<int>(inPartsIndexes);

                int nParts = inPartsIndexes.S[0];
                int nDataSamples = data.S[0];

                // Create the graph
                var graph = new Graph("Test PGEKern");
                var dnXCompound = new CompoundMatrixDataNodeWithSegments("X");

                List<PartPlate> partPlates = new List<PartPlate>();
                for (int i = 0; i<nParts; i++)
                {
                    var newPartPlate = new PartPlate("Part " + i.ToString(), graph,
                        nParts, i, dnXCompound, nLatents, false);
                    partPlates.Add(newPartPlate);
                }
                
                // Set the data
                for (int i = 0; i < nParts; i++)
                {
                    partPlates[i].dnY.AddData(data[ILMath.full, partsIndexes[i, ILMath.full]]);
                }

                // Initialize the graph
                for (int i = 0; i < nParts; i++)
                {
                    var part = partPlates[i];
                    part.dnX.SetValuesSize(new ILSize(new int[] { nDataSamples, nLatents }));
                    part.dnX.SetOptimizingMaskAll();
                    part.dnY.SetOptimizingMaskNone();
                    // X and Y spaces share the segments data
                    part.dnX.Segments = part.dnY.Segments;
                }

                for (int i = 0; i < nParts; i++)
                {
                    var part = partPlates[i];
                    part.fnGPLVM.Initialize();
                }

                for (int i = 0; i < nParts; i++)
                {
                    var part = partPlates[i];
                    part.fnDynamics.Initialize();
                }

                graph.ComputeGradients();


                ILArray<double> YPredicted = ILMath.empty();
                try
                {
                    switch (2)
                    {
                        case 1:
                            {
                                var optimizer = new SCGOptimizer();
                                optimizer.sLogFileName = "CSharpGPDM_New.log";
                                optimizer.bLogEnabled = (bLogEnabled);
                                optimizer.Optimize(graph, nIterations, true);
                            }
                            break;
                        case 2:
                            {
                                var optimizer = new CERCGMinimize();
                                optimizer.Optimize(graph, nIterations, true);
                            }
                            break;
                        case 3:
                            {
                                var optimizer = new SCGOptimizer();
                                optimizer.sLogFileName = "CSharpGPDM_New.log";
                                optimizer.bLogEnabled = (bLogEnabled);
                                optimizer.Optimize(graph, nIterations, true);

                                var optimizer2 = new CERCGMinimize();
                                optimizer2.Optimize(graph, nIterations, true);
                            }
                            break;
                        case 4:
                            {
                                var optimizer = new BFGSOptimizer();
                                optimizer.Optimize(graph, 10, true);
                            }
                            break;
                    }
                    Console.WriteLine();

                    //var optimizer = new SCGOptimizer();
                    //optimizer.sLogFileName = "CSharpGPDM_New.log";
                    //optimizer.bLogEnabled = bLogEnabled;
                    //optimizer.Optimize(graph, nIterations, true);

                    ILArray<double> kParams = ILMath.empty();
                    foreach (var part in partPlates)
                    {
                        kParams.a = Util.Concatenate<double>(kParams, part.fnDynamics.KernelObject.Parameter);
                    }
                    Console.WriteLine("Optimized dynamics kernel parameters: " + kParams.T.ToString());

                    kParams = ILMath.empty();
                    foreach (var part in partPlates)
                    {
                        kParams.a = Util.Concatenate<double>(kParams, part.fnGPLVM.KernelObject.Parameter);
                    }
                    Console.WriteLine("Optimized GPLVM kernel parameters: " + kParams.T.ToString());

                    ILArray<double> xSpace = ILMath.empty();
                    foreach (var part in partPlates)
                    {
                        xSpace.a = Util.Concatenate<double>(xSpace, part.dnX.GetValues(), 1);
                    }
                    Console.WriteLine("X space: " + xSpace.ToString());

                    ILArray<double> XStart = ILMath.empty();
                    XStart.a = dnXCompound.GetValues()[ILMath.r(0, 1), ILMath.full];
                    int nSteps = (dnXCompound.GetValues().S[0]) - 2;

                    ILArray<double> XPredicted = XStart.C;
                    ILArray<double> nextX = ILMath.zeros(new ILSize(new int[] { 1, XStart.S[1] }));
                    for (int i = 0; i < nSteps; i++)
                    {
                        XStart = XPredicted[ILMath.r(ILMath.end - 1, ILMath.end), ILMath.full];
                        foreach (var part in partPlates)
                        {
                            ILArray<double> partXNew = part.fnDynamics.SimulateDynamics(XStart, 3)[ILMath.end, ILMath.full];
                            nextX[part.fnDynamics.DynamicsIndexes] = partXNew;
                        }
                        XPredicted.a = Util.Concatenate<double>(XPredicted, nextX);
                    }

                    Console.WriteLine("Generated X space: " + XPredicted.ToString());

                    //ILArray<double> yScale = ILMath.empty();
                    //foreach (var part in partPlates)
                    //{
                    //    yScale.a = Util.Concatenate(yScale, part.fnGPLVM.Scale, 1);
                    //}
                    //Console.WriteLine("Y scale: " + yScale.ToString());

                    foreach (var part in partPlates)
                    {
                        YPredicted.a = Util.Concatenate<double>(YPredicted, part.fnGPLVM.PredictData(XPredicted[ILMath.full, part.fnDynamics.DynamicsIndexes]), 1);
                    }

                }
                catch (GraphException e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("Input data: " + data.ToString());
                Console.WriteLine("Predicted data: " + YPredicted.ToString());
                Console.WriteLine("Predicted data - input data: " + (YPredicted - data).ToString());

                //ILArray<double> aAlpha = ILMath.empty();
                //foreach (var part in partPlates)
                //    aAlpha.a = Util.Concatenate(aAlpha, part.fnGPLVM.aAlpha, 1);
                //Console.WriteLine("aAlpha: " + aAlpha.ToString());

                return YPredicted;
            }
        }

        static void TestPGEKern()
        {
            using (ILScope.Enter())
            {
                
                Console.WriteLine("\r\n###############################################################################");
                //Console.WriteLine("PGEKern test. Approximation type: " + aType.ToString());


                int nSamples = 20;
                
                ILArray<double> f1 = ILMath.sin(0.51 * ILMath.counter(nSamples));
                ILArray<double> w1 = new double[] { 1, -5 };
                ILArray<double> d1 = ILMath.multiply(f1, w1.T);// +0.1 * ILMath.randn(nSamples, 2);

                ILArray<double> f2 = ILMath.sin(0.50 * ILMath.counter(nSamples));
                ILArray<double> w2 = new double[] { 1, -5 };
                ILArray<double> d2 = ILMath.multiply(f2, w2.T);// +0.1 * ILMath.randn(nSamples, 2);

                ILArray<double> f3 = ILMath.sin(0.90 * ILMath.counter(nSamples));
                ILArray<double> w3 = new double[] { 1, -5 };
                ILArray<double> d3 = ILMath.multiply(f3, w3.T);// +0.1 * ILMath.randn(nSamples, 2);
                
                ILArray<double> data = Util.Concatenate<double>(Util.Concatenate<double>(d1, d2, 1), d3, 1);

                //data = d3;
                //ILArray<int> partsIndexes = new int[,] { { 0, 1 } };

                ILArray<int> partsIndexes = new int[,] { { 0, 1 }, { 2, 3 }, {4, 5} };
                //ILArray<int> partsIndexes = new int[,] { { 0 }, { 1 }, { 2 }, { 3 }, { 4 }, { 5 } };

                ILArray<double> predictedData = TestFactorGraphPGEKern(data, partsIndexes.T, 1);
                
                
                Console.ResetColor();
            }
        }
    }
}
