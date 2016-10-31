using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace GraphicalModel.Factors
{
    public class DirichletFactor: Factor
    {

        public DirichletFactor(FactorDesc desc)
            : base(desc)
        {
        }

        /// <summary>
        /// This Method is called whenever a new cluster is created. 
        /// It generates a sample for the cluster parameters (i.e. probability vector for categorical distri).
        /// It also creates a new Variable (for Cluster parameter) dynamically and stores  the cluster number and
        ///  it's associated parameters. Also creates appropriate connection from the Variable to factors(one to this factor 
        ///  and other to Categorical Factor).
        /// </summary>
        /// <param name="clusterNumber"></param>
        /// <returns></returns>
        public ILRetArray<double> SampleClusterParameters( int clusterNumber)
        {

            // Get the hyperparameter node connected to the factor. 

            // Create a new parameter node for mean and covariance for cluster == clusterNumber
            // Or create a new index (if using only a single node to represent the parameters)

            // Generate a sample from the Dirichlet distribution to serve as a prior to the multinomial distribution.
            // Store it in the parameter node.

            // (If we decide to create separate nodes for the parameters) Update the connection points of the parameter node.
            // Also create/do necessary updates to the data expanders and matrix data nodes
 
            
            return 0;
        
        }


        public override void Initialize()
        {
            
        }


        public override void ComputeAllGradients()
        {
        }


        public override double FunctionValue()
        {
            return 0;
        }
    }
}
