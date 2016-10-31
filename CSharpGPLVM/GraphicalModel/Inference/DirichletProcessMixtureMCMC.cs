using GraphicalModel.Adapters;
using ILNumerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphicalModel.Inference
{
    public class DirichletProcessMixtureMCMC
    {
        public void Infer(IDirichletMCMCInterface model, int numberOfIterations)
        {
            int iter = 0;
            while (iter < numberOfIterations)
            {
                int currentPoint = 0;
                while (currentPoint < model.TimePoints)
                {
                    model.SelectPoint(currentPoint);
                    model.ComputeMixingProportions(model.GetClusterPointCount(), model.ComputeProbability(currentPoint));
                    int clusterNumber = (int) model.SampleClusterNumber(currentPoint);
                    if (!ILMath.find(model.GetClusters() == clusterNumber).IsEmpty)
                    {
                        model.SampleClusterParameters(clusterNumber);
                    }

                    // Update the selected point
                    model.UpdateSelectedPoint(clusterNumber); 
                    currentPoint++;
                }
                //model.ComputeParametersOfClusters();
                //model.ComputeClusterParameterPosterior();
                for (int i = 0; i < model.TotalClusters; i++)
                {
                    model.ComputeClusterParameterPosterior(model.GetClusterData(i), i);
                    //Note:
                    //In Model adapter loop through all the observations and get the data values of the
                    //observations and then pass those data values to ComputeClusterParameterPosterior
                    //function in the factor connecting the parameters to their respective hyperparameters


                }
                iter++;
            }
        }

    }
}
