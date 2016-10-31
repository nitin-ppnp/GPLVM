using GraphicalModel.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using ILNumerics;
using System.Text;
using GraphicalModel.Factors.Likelihoods;

namespace GraphicalModel.Factors
{
    public class CategoricalFactor : Factor
    {
        public static string CP_V = "ClusterPriors";
        public static string CP_M = "ModelClusters";
        protected DataExpander deV;
        protected DataExpander deM;

        private int selectedPoint;
        private int totalPoints;

        public CategoricalFactor(FactorDesc desc)
            : base(desc)
        {

        }

        /// <summary>
        /// Returns the data points belonging to the cluster
        /// </summary>
        /// <param name="clusterNumber"># of the cluster</param>
        /// <returns>Data points belonging to the cluster</returns>
        public ILRetArray<int> GetClusterData(int clusterNumber)
        {
            deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
            deM.Expand();
            return ILMath.find(deM.ExpandedData == clusterNumber);

        }


        public ILRetArray<int> GetClusterPointCount()
        {
            deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
            deM.Expand();
            ILArray<int> pointCount = ILMath.zeros<int>(Convert.ToInt32(deM.ExpandedData.ToArray().Distinct().ToArray().Length));
            for (int i = 0; i < deM.ExpandedData.Length; i++)
            {
                pointCount[Convert.ToInt32(deM.ExpandedData[i])] = pointCount[Convert.ToInt32(deM.ExpandedData[i])] + 1;
            }

            return pointCount;
        }

        public void SelectPoint(int index)
        {
            selectedPoint = index;
        }

        private int SelectedPoint
        {
            get
            {
                if(selectedPoint == TimePoints)
                    throw new Exception("No Point Selected");
                return selectedPoint;
            }
        }

        public void DeSelectPoint(int index)
        {
            selectedPoint = totalPoints;
        }

        int TimePoints
        {
            get
            {
                deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
                deM.Expand();
                totalPoints = deM.ExpandedData.Length;
                return totalPoints;
            }
        }

        ILRetArray<int> TotalClusters {
            get
            {
                deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
                deM.Expand();
                return Convert.ToInt32(deM.ExpandedData.ToArray().Distinct().ToArray().Length);
            }

        }

        public ILRetArray<int> GetClusters()
        {
            deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
            deM.Expand();
            return Convert.ToInt32(deM.ExpandedData.ToArray().Distinct().ToArray());
        }

        public void UpdateSelectedPoint(int clusterNumber)
        {
            deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
            deM.Expand();
            deM.ExpandedData[SelectedPoint] = clusterNumber;
        }

        /// <summary>
        /// Finds the posteriors of the parameters of every cluster after one full iteration of MCMC.
        /// In this case it finds the probabilities of each teansitions(z's) / each sensory (s's) events belonging to 
        /// every cluster by looking at the observations of each cluster and respective variables(z's or s's) i.e.
        /// it get's the cluster number from the cluster parameter variable connected to it and using this it finds all the 
        /// observations associated with this cluster. Then it finds the p-vector of this cluster and updates the parameter
        /// variable. This p-vector is the parameter of the categorical Distri.
        /// </summary>
        public void ComputeParametersOfClusters()
        {
            // Loop through the clusters

            // Get the points connected to the cluster number by looking at the index of M's==clusterNumber

            // Compute the distribution of the sensory inputs. i.e. count the number of different 
            // sensory i/p labels and normalize.

            // Get the parameter node of the cluster and store the probability vector in that.

        }

        //Multinomial Likelihood
        private double LogLikelihood()
        {
            return 0;
        }

        /// <summary>
        /// Uses the probabilities prior from PolyaUrn output and Samples a cluster number from Categorical Distribution
        ///  by feeding these probabilities as parameters.
        /// Also updates the cluster number ie M(t) ie of the selected point;
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        public ILRetArray<int> SampleClusterNumber(int currentPoint)
        {
            // Get the probability prior from V
            deV = (DataExpander)(lConnectionPoints.FindByName(CP_V).ConnectedDataObject);
            deV.Expand();

            // Store the sampled cluster number in M(currentPoint)
            // If seperate variables for z,s,x then need to update those too
            deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
            deM.Expand();
            // Sample a cluster number from categorical distribution using this probability vector as a parameter
            deM.ExpandedData[currentPoint] = Convert.ToDouble(Multinomial.Sample(Enumerable.Range(0, 
                                                deV.ExpandedData.Length - 1).ToArray<int>(), deV.ExpandedData, 1));
            deM.Unexpand();
            return Convert.ToInt32(deM.ExpandedData[currentPoint]);
        }

        public override void Initialize()
        {

            // Z is like an observable initialized using the movement labels. It is an integer.
        }


        public override double FunctionValue()
        {
            return LogLikelihood();
        }
    }

    public class CategoricalFactorBuilder : FactorBuilder
    {
        public CategoricalFactorBuilder()
        {
            Type = "Categorical";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new CategoricalFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(CategoricalFactor.CP_M, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(CategoricalFactor.CP_V, desc));
            return desc;
        }
    }
}
