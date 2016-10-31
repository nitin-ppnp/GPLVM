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
using FactorGraph.Utils;
using FactorGraph.Core;
using FactorGraph.DataNodes;
using FactorGraph.FactorNodes;
using GPLVM.Utils.Character;
using MotionPrimitives.SkeletonMap;
using MotionPrimitives.DataSets;

namespace MotionPrimitives.Experiments
{
    public abstract class Experiment
    {
        public ApproximationType GPLVMApproximation = ApproximationType.ftc;
        public ApproximationType DynamicsApproximation = ApproximationType.ftc;
        public int NumberOfInducingPoints = 100;
        public int LatentDimensions = 3;
        public Representation representation = Representation.exponential;
        
        protected double fFrameTime = 1 / 30f;
        public int nIterations = 40;
        protected bool bLogEnabled = true;
        protected bool bWriteLog = false;
        
        protected string experimentName;
        protected Graph graph;

        public Skeleton skeleton;

        public Experiment(string sName, int latents)
        {
            experimentName = sName;
            LatentDimensions = latents;
            //CreateGraph();
        }

        public Graph GraphModel
        {
            get { return graph; }
        }

        public double FrameTime
        {
            get { return fFrameTime; }
        }

        // Create new graph with specific structure
        public virtual void CreateGraph()
        {
            graph = new Graph(experimentName);
        }

        // Fill the graph with data
        public abstract void LoadBVHData(string sFileName);

        public virtual void LoadDataSet(DataSet dataSet)
        {
            throw new NotImplementedException();
        }

        public abstract FactorNode GetDynamicsNode();

        public abstract ILRetArray<double> GetXValues();
        
        public virtual ILRetArray<int> GetSegments()
        {
            return new int[] { 0 };
        }

        // Factor nodes require initialization in specific order
        public abstract void InitFactorNodes();

        public virtual void RunOptimization()
        {
            InitFactorNodes();
            switch (2)
            {
                case 1:
                    {
                        var optimizer = new SCGOptimizer();
                        optimizer.sLogFileName = experimentName + ".log";
                        optimizer.bLogEnabled = (bLogEnabled && bWriteLog);
                        optimizer.Optimize(graph, nIterations, bLogEnabled);
                    }
                    break;
                case 2:
                    {
                        var optimizer = new CERCGMinimize();
                        optimizer.Optimize(graph, nIterations, bLogEnabled);
                    }
                    break;
                case 3:
                    {
                        var optimizer2 = new CERCGMinimize();
                        optimizer2.Optimize(graph, nIterations, bLogEnabled);

                        var optimizer = new SCGOptimizer();
                        optimizer.sLogFileName = experimentName + ".log";
                        optimizer.bLogEnabled = (bLogEnabled && bWriteLog);
                        optimizer.Optimize(graph, nIterations, bLogEnabled);
                    }
                    break;
            }

            PrintReport();
        }

        public virtual void PrintReport()
        {
        }

        // Setup generation-specific parameters
        public abstract void InitGeneration();

        // Run data generation of the graph
        public abstract bool GenerateFrame(ILOutArray<double> frameData, out Representation representationType);

        public virtual void SaveToFile(string prefix, string sFileName)
        {
            Serializer.Serialize(graph, prefix + "Graph " + sFileName);
            Serializer.Serialize(skeleton, prefix + "Skeleton " + sFileName);
        }

        public virtual void LoadFromFile(string prefix, string sFileName)
        {
            graph = (Graph)Serializer.Deserialize(typeof(Graph), prefix + "Graph " + sFileName);
            skeleton = (Skeleton)Serializer.Deserialize(typeof(Skeleton), prefix + "Skeleton " + sFileName);
        }

        public abstract ILRetArray<double> PredictFullX(int kSizeFactor = 1);
        
    }
}
