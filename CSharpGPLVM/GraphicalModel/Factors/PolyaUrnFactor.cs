using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace GraphicalModel.Factors
{
    public class PolyaUrnFactor : Factor
    {
        protected DataExpander deAlpha;
        protected DataExpander deV;
        public static string CP_V = "V";
        public static string CP_ALPHA = "Alpha";
        private int selectedPoint;

        public PolyaUrnFactor(FactorDesc desc)
            : base(desc)
        {
        }

        public override void Initialize()
        {
            
        }

        
        public override void ComputeAllGradients()
        {

        }

        /// <summary>
        /// Computes the Likelihood of each cluster using Polya Urn Scheme. 
        /// Assumes that the selected point belongs to a new cluster.
        /// V should be a variable length array. It holds the mixing proportions. 
        /// Length(V) == #Clusters and Values(V) == PolyaUrnProbabilities
        /// </summary>
        /// <returns></returns>
        private void ComputeMixingProportions(ILInArray<int> inNoOfPointsInClusters, ILInArray<double> inProbabilities)
        {  
            using(ILScope.Enter(inNoOfPointsInClusters, inProbabilities))
            {

                //Count the number of elements belonging to each cluster
                ILArray<int> noOfPointsInClusters = ILMath.check(inNoOfPointsInClusters);
                
                //Probability of the point belonging to every cluster w.r.t factor
                ILArray<double> probabilities = ILMath.check(inProbabilities);

                //Count the number of distinct M's
                int totalNumberOfClusters = noOfPointsInClusters.Length;

                int numberOfPoints = (int) ILMath.sumall(noOfPointsInClusters);

                deV = (DataExpander)(lConnectionPoints.FindByName(CP_V).ConnectedDataObject);
                deV.Expand();

                //Get the alpha value
                deAlpha = (DataExpander)(lConnectionPoints.FindByName(CP_ALPHA).ConnectedDataObject);
                deAlpha.Expand();
                
                //Compute the probability of the point belonging to every cluster (Polya Urn Scheme)
                ILArray<double> probabilityECluster = new double[totalNumberOfClusters + 1];

                for (int i = 0; i < totalNumberOfClusters; i++)
                {
                    probabilityECluster[i] = ((int)noOfPointsInClusters[i] / (numberOfPoints - 1 + (double)deAlpha.ExpandedData))
                                                * ILMath.prod(probabilities[ILMath.full, i]);
                }

                //Compute the probability of the selected point belonging to a new cluster
                probabilityECluster[ILMath.end] = ((double)deAlpha.ExpandedData / (numberOfPoints - 1 + (double)deAlpha.ExpandedData))
                                                    * ILMath.prod(ILMath.sum(probabilities, 1));



                //Store these probabilities in V
                deV.ExpandedData.a = probabilityECluster;
            }
            
        }
        
        public override double FunctionValue()
        {
            return 0;
        }

        public int SelectPoint
        {
            get
            {
                return this.selectedPoint;
            }
            set
            {
                this.selectedPoint = value;
            }
        }
        
    }

    public class PolyaUrnFactorBuilder : FactorBuilder
    {
        public PolyaUrnFactorBuilder()
        {
            Type = "PolyaUrn";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new PolyaUrnFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(PolyaUrnFactor.CP_ALPHA, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(PolyaUrnFactor.CP_V, desc));
            return desc;
        }
    }

}
