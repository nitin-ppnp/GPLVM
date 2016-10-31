using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Factors.Likelihoods;

namespace GraphicalModel.Factors
{
    public class BetaFactor : Factor
    {
         protected DataExpander deAlpha;
        protected DataExpander deV;

        private int selectedPoint;

        public BetaFactor(FactorDesc desc)
            : base(desc)
        {
        }

        public override void Initialize()
        {
            
        }

        private ILRetArray<double> Sample(ILInArray<double> inAlpha, ILInArray<double> inBeta)
        {
            using (ILScope.Enter())
            {
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> beta = ILMath.check(inBeta);
                return Beta.Sample(inAlpha, inBeta);
            }
        }

        public override void ComputeAllGradients()
        {

        }

        /// <summary>
        /// Computes the Likelihood of each cluster. 
        /// Assumes that the selected point belongs to a new cluster.
        /// V should be a variable length array. It holds the mixing proportions. 
        /// Length(V) == #Clusters and Values(V) == Normalized Beta Probabilities
        /// </summary>
        /// <returns></returns>
        private ILRetArray<double> ComputeMixingProportions()
        {
            //Count the number of distinct M's

            //Count the number of elements belonging to each cluster

            //Get the alpha value

            //Get the reference to all the factors that connects M with it's descendents

            //Compute the probability of all the existing clusters

            //Compute the probability of the selected point belonging to a new cluster

            //Store these probabilities in V

            return 0;
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
}

