using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace GraphicalModel.Adapters
{
    public class DirichletMCMCAdapter : IDirichletMCMCInterface
    {

        public void ComputeClusterParameterPosterior(ILInArray<double> X, int clusterNumber)
        {
            throw new NotImplementedException();
        }

        public void SampleClusterParameters(int clusterNumber)
        {
            throw new NotImplementedException();
        }

        public void ComputeParametersOfClusters()
        {
            throw new NotImplementedException();
        }

        public ILRetArray<double> ComputeProbability(int selectedPoint)
        {
            throw new NotImplementedException();
        }

        public ILRetArray<int> SampleClusterNumber(int point)
        {
            throw new NotImplementedException();
        }

        public void UpdateSelectedPoint(int clusterNumber)
        {
            throw new NotImplementedException();
        }

        public int TimePoints
        {
            get { throw new NotImplementedException(); }
        }

        public ILRetArray<int> GetClusters()
        {
            throw new NotImplementedException();
        }

        public void SelectPoint(int index)
        {
            throw new NotImplementedException();
        }

        public void DeSelectPoint(int index)
        {
            throw new NotImplementedException();
        }

        public ILRetArray<int> TotalClusters
        {
            get { throw new NotImplementedException(); }
        }

        public ILRetArray<double> GetClusterData(int clusterNumber)
        {
            throw new NotImplementedException();
        }

        public ILRetArray<int> GetClusterPointCount()
        {
            throw new NotImplementedException();
        }

        public void ComputeMixingProportions(ILInArray<int> inNoOfPointsInClusters, ILArray<double> inProbOfPtEClusterGivenFactor)
        {
            throw new NotImplementedException();
        }
    }
}
