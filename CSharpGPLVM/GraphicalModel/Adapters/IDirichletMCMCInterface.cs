using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Inference;

namespace GraphicalModel.Adapters
{
    public interface IFunctionsToBeImplementedInParameterPriors
    {
             
        
        /// <summary>
        /// Implement a function which computes the posterior for the cluster parameters 
        /// In Model adapter loop through all the observations and get the data values of the
        /// observations and then pass those data values to ComputeClusterParameterPosterior
        /// function in the factor connecting the parameters to their respective hyperparameters 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="clusterNumber"></param>
        void ComputeClusterParameterPosterior(ILInArray<double> inX, int clusterNumber);


        /// <summary>
        /// Implement a function which draws the cluster parameters whenever a new cluster is created.
        /// Also adds a new node and links it to the corresponding factor. This node must have it's asscoiated cluster number
        /// and must store the parameters associated with that cluster. 
        /// </summary>
        /// <param name="clusterNumber"></param>
        void SampleClusterParameters(int clusterNumber);
        
    }

    public interface IFunctionsToBeImplementedInParameterFactors
    {
        
        /// <summary>
        /// Implement a function which computes the parameters of each clusters after every iteration of MCMC 
        /// </summary>
        void ComputeParametersOfClusters();

        /// <summary>
        /// Computes the probability of the current point belonging to each cluster w.r.t all the 
        /// factors
        /// </summary>
        /// <param name="selectedPoint">Time point index</param>
        /// <returns>
        /// A matrix containing probabilities of the point belonging to each cluster. Each row 
        /// represents the probability w.r.t the particular factor. Each column is probability
        /// w.r.t each cluster. Hence M(i,j) is the probability of the point belonging to cluster j
        /// w.r.t factor i (say Gaussian Factor, etc).
        /// </returns>
        ILRetArray<double> ComputeProbability(int selectedPoint);

    }

    public interface IFunctionsToBeImplementedInClusterFactor
    {
        
        /// <summary>
        /// Implement a function that draws a sample from categorical distribution given the mixing proportions
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        ILRetArray<int> SampleClusterNumber(int point);

        
        /// <summary>
        /// Update the cluster number of the selected point
        /// </summary>
        void UpdateSelectedPoint(int clusterNumber);

        
        /// <summary>
        /// Number of time points
        /// </summary>
        int TimePoints { get; }

        
        /// <summary>
        /// Get all cluster numbers
        /// </summary>
        /// <returns></returns>
        ILRetArray<int> GetClusters();

        
        /// <summary>
        /// Sets a flag on the selected time point(for Polya Urn Scheme)
        /// </summary>
        /// <param name="index"></param>
        void SelectPoint(int index);

        
        /// <summary>
        /// Sets a flag on the selected time point(for Polya Urn Scheme)
        /// </summary>
        /// <param name="index"></param>
        void DeSelectPoint(int index);

        
        /// <summary>
        /// Get the total number of clusters
        /// </summary>
        ILRetArray<int> TotalClusters { get; }

        /// <summary>
        /// Returns the Indices of the time points belonging to a particular cluster
        /// </summary>
        /// <param name="clusterNumber"></param>
        /// <returns></returns>
        ILRetArray<double> GetClusterData(int clusterNumber);

        /// <summary>
        /// Returns the total number of points belonging to each cluster
        /// </summary>
        /// <returns></returns>
        ILRetArray<int> GetClusterPointCount();
    }
    
    public interface IFunctionsToBeImplementedInClusterPrior
    {
        
        /// <summary>
        /// Implement the function that computes the mixing proportions 
        /// </summary>
        void ComputeMixingProportions(ILInArray<int> inNoOfPointsInClusters, ILArray<double> inProbOfPtEClusterGivenFactor);

    }


    public interface IDirichletMCMCInterface : IFunctionsToBeImplementedInParameterPriors,
                                                IFunctionsToBeImplementedInParameterFactors,
                                                IFunctionsToBeImplementedInClusterFactor,
                                                IFunctionsToBeImplementedInClusterPrior
    {
        
        
    }
}
