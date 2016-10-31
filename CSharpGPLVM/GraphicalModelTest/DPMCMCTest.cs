using GraphicalModel;
using GraphicalModel.Factors;
using GraphicalModel.Factors.Likelihoods;
using ILNumerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphicalModelTest
{
    public class DPMCMCTest
    {
        public static void Test(String[] args)
        {
            double[] mu = new double[] { 10, 10 };
            double[,] sigma = new double[,] { { 1, 0 }, { 0, 1 } };

            ILArray<double> x = new double[2, 200];
            x[ILMath.r(0, 99), ILMath.full] = (Gaussian.Sample(mu, sigma, 100)).T;

            mu = new double[] { 30, 60 };
            sigma = new double[,] { { 0.5, 1 }, { 0, 0.3 } };
            x[ILMath.r(100, 129), ILMath.full] = (Gaussian.Sample(mu, sigma, 30)).T;

            mu = new double[] { 80, 80 };
            sigma = new double[,] { { 2, 0.3 }, { 0.3, 2 } };
            x[ILMath.r(130, 199), ILMath.full] = (Gaussian.Sample(mu, sigma, 70)).T;

            var factory = new GraphicModelFactory();
            factory.RegisterFactor(new CategoricalFactorBuilder());
            factory.RegisterFactor(new PolyaUrnFactorBuilder());
            factory.RegisterFactor(new GaussianFactorBuilder());
            factory.RegisterFactor(new GaussianWishartFactorBuilder());

            var rootDesc = new PlateDesc("root", null);
            var polyaFactorDesc = factory.BuildFactorDesc("PolyaUrn");
            var categoricalDesc = factory.BuildFactorDesc("Categorical");
            var gaussianDesc = factory.BuildFactorDesc("Gaussian");
            var gaussianWishartDesc = factory.BuildFactorDesc("GaussianWishart");

            var plateXYDesc = new PlateDesc("X-Y plate", rootDesc);
            var varX = new VariableDesc("X", 2, EVariableMode.LatentUnique);
            var varM = new VariableDesc("M", 1, EVariableMode.LatentExpandable);
            var varV = new VariableDesc("V", 1, EVariableMode.LatentExpandable);
            var varAlpha = new VariableDesc("Alpha", 1, EVariableMode.LatentExpandable);

            var varMu = new VariableDesc("Mu", 2, EVariableMode.LatentExpandable);
            var varSigma = new VariableDesc("Sigma", 1, EVariableMode.LatentExpandable);
            var varMu0 = new VariableDesc("Mu0", 1, EVariableMode.LatentExpandable);
            var varLambda = new VariableDesc("Lambda", 1, EVariableMode.LatentExpandable);
            var varNu = new VariableDesc("Nu", 1, EVariableMode.LatentExpandable);
            var varW = new VariableDesc("W", 1, EVariableMode.LatentExpandable);

        }
    }
}
