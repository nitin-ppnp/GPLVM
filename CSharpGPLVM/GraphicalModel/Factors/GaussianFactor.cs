using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Factors.Likelihoods;
using GraphicalModel.Adapters;

namespace GraphicalModel.Factors
{
    public class GaussianFactor : Factor, IFunctionsToBeImplementedInParameterFactors
    {
        public static string CP_M = "ModelClusters";
        public static string CP_X = "Latent";
        public static string CP_MU = "LatentClusterMean";
        public static string CP_SIGMA = "LatentClusterCovariance";
        protected DataExpander deX;
        protected DataExpander deMu;
        protected DataExpander deSigma;
        protected DataExpander deM;


        public GaussianFactor(FactorDesc desc)
            : base(desc)
        {
        }

        public ILRetArray<double> ComputeProbability(int pointIndex)
        {
            deX = (DataExpander)(lConnectionPoints.FindByName(CP_X).ConnectedDataObject);
            deMu = (DataExpander)(lConnectionPoints.FindByName(CP_MU).ConnectedDataObject);
            deSigma = (DataExpander)(lConnectionPoints.FindByName(CP_SIGMA).ConnectedDataObject);
            deX.Expand();
            deMu.Expand();
            deSigma.Expand();
            int totalNumberOfClusters = deMu.ExpandedData.Size[0];
            ILArray<double> probability = new double[totalNumberOfClusters];
            for (int i = 0; i < totalNumberOfClusters; i++)
            {
                probability[i] = ILMath.exp(Gaussian.LogLikelihood(deMu.ExpandedData[i,ILMath.full],
                                                                deSigma.ExpandedData[i, ILMath.full, ILMath.full], 
                                                                deX.ExpandedData[i, ILMath.full]));
            }

            return probability;
        }

        /// <summary>
        /// Updates the parameters of every cluster after one full iteration of MCMC.
        /// In this case it finds the mean and the cov of each clusters by looking at the data points of each cluster i.e.
        /// it get's the cluster number from the cluster parameter variable connected to it and using this it finds all the 
        /// observations associated with this cluster. Then it finds the mean and cov of this cluster and updates the parameter
        /// variable. 
        /// </summary>
        public void ComputeParametersOfClusters()
        {
            deX = (DataExpander)(lConnectionPoints.FindByName(CP_X).ConnectedDataObject);
            deMu = (DataExpander)(lConnectionPoints.FindByName(CP_MU).ConnectedDataObject);
            deSigma = (DataExpander)(lConnectionPoints.FindByName(CP_SIGMA).ConnectedDataObject);
            deX.Expand();
            deMu.Expand();
            deSigma.Expand();
            // Loop through the clusters
            // Get the points connected to the cluster number by looking at the index of M's==clusterNumber
            foreach (int k in deM.ExpandedData.ToArray().Distinct().ToArray())
            {
                // Compute the mean and Covariance of each cluster
                deMu.ExpandedData[k, ILMath.full] =
                    Gaussian.ComputeMean(deX.ExpandedData[deM.ExpandedData == k, ILMath.full]);
                deSigma.ExpandedData[k, ILMath.full, ILMath.full] =
                    Gaussian.ComputeCovariance(deX.ExpandedData[deM.ExpandedData == k, ILMath.full]);
                
            }

        }

        public override void Initialize()
        {
            deX = (DataExpander)(lConnectionPoints.FindByName(CP_X).ConnectedDataObject);
            deMu = (DataExpander)(lConnectionPoints.FindByName(CP_MU).ConnectedDataObject);
            deSigma = (DataExpander)(lConnectionPoints.FindByName(CP_SIGMA).ConnectedDataObject);
            deM = (DataExpander)(lConnectionPoints.FindByName(CP_M).ConnectedDataObject);
            deM.Expand();
            deX.Expand();
            deM.ExpandedData.a = ILMath.zeros(deX.ExpandedData.Size[0]);
            deM.Unexpand();

            int nD = deX.ExpandedData.Size[1];
            //Change this dynamically to set it to the truncation level ie max number of clusters
            int truncationLevel = 30;

            deMu.Expand();
            deSigma.Expand();
            deMu.ExpandedData = new double [truncationLevel, nD];
            deMu.ExpandedData[0, ILMath.full] = Gaussian.ComputeMean(deX.ExpandedData);
            deSigma.ExpandedData = new double [truncationLevel, nD, nD];
            deSigma.ExpandedData[0, ILMath.full, ILMath.full] = Gaussian.ComputeCovariance(deX.ExpandedData);
            deMu.Unexpand();
            deSigma.Unexpand();

        }

        public double LogLikelihood()
        {
            double L = 0;
            return L;
        }



        public override void ComputeAllGradients()
        {

        }

        public override double FunctionValue()
        {
            return LogLikelihood();
        }
    }

    public class GaussianFactorBuilder : FactorBuilder
    {
        public GaussianFactorBuilder()
        {
            Type = "Gaussian";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new GaussianFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianFactor.CP_M, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianFactor.CP_X, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianFactor.CP_MU, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianFactor.CP_SIGMA, desc));
            return desc;
        }
    }
}