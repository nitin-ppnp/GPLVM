using GraphicalModel.Adapters;
using GraphicalModel.Factors.Likelihoods;
using ILNumerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphicalModel.Factors
{
    public class GaussianWishartFactor : Factor, IFunctionsToBeImplementedInParameterPriors
    {
        protected DataExpander deLambda;
        protected DataExpander deW;
        protected DataExpander deNu;
        protected DataExpander deMu0;
        protected DataExpander deMu;
        protected DataExpander deSigma;
        public static string CP_LAMBDA = "Lambda";
        public static string CP_W = "W";
        public static string CP_NU = "nu";
        public static string CP_MU0 = "mu0";
        public static string CP_MU = "LatentClusterMean";
        public static string CP_SIGMA = "LatentClusterCovariance";

        public GaussianWishartFactor(FactorDesc desc)
            : base(desc)
        {
        }

        /// <summary>
        /// This Method is called whenever a new cluster is created. 
        /// It generates a sample for the cluster parameters (i.e. mean and cov for Gaussian Distri).
        /// It also creates a new Variable (for Cluster parameter) dynamically and stores  the cluster number and
        ///  it's associated parameters. Also creates appropriate connection from the Variable to factors(one to this factor 
        ///  and other to Gaussian Factor).
        /// </summary> 
        /// <param name="clusterNumber"></param>
        /// <returns></returns>
        public void SampleClusterParameters(int clusterNumber)
        {
            deMu = (DataExpander)(lConnectionPoints.FindByName(CP_MU).ConnectedDataObject);
            deSigma = (DataExpander)(lConnectionPoints.FindByName(CP_SIGMA).ConnectedDataObject);

            // Get the hyperparameter node connected to the factor. 
            deLambda = (DataExpander)(lConnectionPoints.FindByName(CP_LAMBDA).ConnectedDataObject);
            deW = (DataExpander)(lConnectionPoints.FindByName(CP_W).ConnectedDataObject);
            deNu = (DataExpander)(lConnectionPoints.FindByName(CP_NU).ConnectedDataObject);
            deMu0 = (DataExpander)(lConnectionPoints.FindByName(CP_MU0).ConnectedDataObject);

            // Generate a sample for the covariance and store it in the parameter node.
            deSigma.ExpandedData.a = deSigma.ExpandedData.Concat(Wishart.Sample(
                                        Convert.ToInt32(deNu.ExpandedData),deW.ExpandedData)).ToArray<double>();
            
            // Generate a sample for mean and store it in the parameter node.
            deMu.ExpandedData.a = deMu.ExpandedData.Concat(Gaussian.Sample(deMu0.ExpandedData, 
                                        deSigma.ExpandedData[ILMath.end, ILMath.full, ILMath.full], 1)).ToArray<double>();

        }

        /// <summary>
        /// This method is called when after every iteration of the MCMC algorithm to compute the posterior
        /// of the hyperparameters.
        /// </summary>
        /// <returns></returns>
        public void ComputeClusterParameterPosterior(ILInArray<double> inX, int clusterNumber)
        {
            using (ILScope.Enter(inX))
            {
                //Cluster Data
                ILArray<double> X = ILMath.check(inX);

                //Get the Parameters 
                deMu = (DataExpander)(lConnectionPoints.FindByName(CP_MU).ConnectedDataObject);
                deSigma = (DataExpander)(lConnectionPoints.FindByName(CP_SIGMA).ConnectedDataObject);

                // Get the hyperparameters connected to the factor. 
                deLambda = (DataExpander)(lConnectionPoints.FindByName(CP_LAMBDA).ConnectedDataObject);
                deW = (DataExpander)(lConnectionPoints.FindByName(CP_W).ConnectedDataObject);
                deNu = (DataExpander)(lConnectionPoints.FindByName(CP_NU).ConnectedDataObject);
                deMu0 = (DataExpander)(lConnectionPoints.FindByName(CP_MU0).ConnectedDataObject);

                deMu.Expand();
                deSigma.Expand();
                // Iterate over every cluster

                // Compute the posterior for hyperparameters for every cluster.
                ILArray<double> postMu0 = 0;
                ILArray<int> postNu = 0;
                ILArray<double> postLambda = 0;
                ILArray<double> postW = 0;

                ////
                ////  TODO Calculations of posterior hyperparams
                //// See the formulas and update the variable names for mu, sample mean, mu0;
                ////

                //Find the number of time points
                int numberOfTimePoints = X.Size[0];
                //First find the cluster mean
                ILArray<double> clusterMean = ILMath.mean(X);
                //Subtract the prior value of mean from X for subsequent use
                X = X - ILMath.repmat(deMu.ExpandedData[clusterNumber]);
                ILArray<double> mu0_clusterMean_difference = deMu0.ExpandedData - clusterMean;

                postLambda = ILMath.pinv(ILMath.pinv(deLambda.ExpandedData) + numberOfTimePoints);
                postNu = Convert.ToInt32(deNu.ExpandedData) + numberOfTimePoints;
                postMu0 = postLambda * (ILMath.pinv(deLambda.ExpandedData) * deMu0.ExpandedData
                            + numberOfTimePoints * clusterMean);
                postW = deW.ExpandedData + ILMath.multiply(X.T, X)
                            + ((ILMath.pinv(deLambda.ExpandedData) * numberOfTimePoints) / (ILMath.pinv(postLambda)))
                            * (ILMath.multiply(mu0_clusterMean_difference.T, mu0_clusterMean_difference));


                // Sample a value for mean and covariance ie the posterior parameters 
                // and store it in the respective cluster parameter node
                deSigma.ExpandedData[clusterNumber, ILMath.full, ILMath.full] = Wishart.Sample(postNu, postW);
                deMu.ExpandedData[clusterNumber, ILMath.full] = Gaussian.Sample(postMu0,
                            deSigma.ExpandedData[clusterNumber, ILMath.full, ILMath.full] / postLambda, 1);


                deMu.Unexpand();
                deSigma.Unexpand();
            }
            throw new NotImplementedException();            
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

    public class GaussianWishartFactorBuilder : FactorBuilder
    {
        public GaussianWishartFactorBuilder()
        {
            Type = "GaussianWishart";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new GaussianWishartFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianWishartFactor.CP_LAMBDA, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianWishartFactor.CP_MU, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianWishartFactor.CP_MU0, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianWishartFactor.CP_NU, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianWishartFactor.CP_SIGMA, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(GaussianWishartFactor.CP_W, desc));
            return desc;
        }
    }

}
