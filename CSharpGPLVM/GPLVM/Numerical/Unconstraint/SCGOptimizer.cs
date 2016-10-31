using System;
using GPLVM.DebugTools;
using ILNumerics;

namespace GPLVM.Numerical
{
    public class SCGOptimizer : IFunctionWithGradientOptimizer
    {
        public string sLogFileName = "log.log";
        public bool bLogEnabled = false;
        /// <summary>
        /// Scaled conjugate gradient optimization.
        /// Minimizes the function value
        /// </summary>
        /// <param name="function"></param>
        /// <param name="maxIterations"></param>
        /// <param name="display"></param>
        public void Optimize(IFunctionWithGradient function, int maxIterations, bool display)
        {
            int nparams = function.NumParameters;


            ILArray<double> gradold = ILMath.empty();
            ILArray<double> x = function.Parameters;
            ILArray<double> d = ILMath.empty();
            ILArray<double> xnew = ILMath.empty();
            ILArray<double> xplus = ILMath.empty();
            ILArray<double> gplus = ILMath.empty();
            ILArray<double> gradnew = ILMath.empty(); 

            double sigma0 = 1.0e-4;
            
            double fold = function.Value();   // Initial function value.
            Console.WriteLine("Starting value: " + fold.ToString());
            double fnow = fold;
            gradnew.a = function.Gradient();	// Initial gradient.

            gradold.a = gradnew.C;
            /*double[] gradoldTmp = null;
            gradnew.ExportValues(ref gradoldTmp);
            gradold = gradoldTmp;*/

            d.a = -gradnew.C;                             // Initial search direction.
            bool success = true;                        // Force calculation of directional derivs.
            int nsuccess = 0;                           // nsuccess counts number of successes.
            double beta = 1.0f;                         // Initial scale parameter.
            double betamin = 1.0e-15;                   // Lower bound on scale.
            double betamax = 1.0e100;                   // Upper bound on scale.
            int j = 1;                                  // j counts number of iterations.

            

            double sigma, delta, kappa = 0, theta = 0, mu = 0, alpha = 0, Delta = 0, gamma = 0;

            double fnew;
            FileLogger fl = new FileLogger(sLogFileName);
            try
            {
                fl.Enabled = bLogEnabled;

                // Main optimization loop.
                if (display)
                    System.Console.WriteLine("Numerical optimization via SCG\n==============================");

                while (j <= maxIterations)
                {
                    fl.WriteDelimiter('#');
                    fl.Write("Parameters", function.Parameters);
                    fl.Write("Function value", function.Value());
                    fl.Write("Function gradient", function.Gradient());
                    // Calculate first and second directional derivatives.
                    if (success)
                    {
                        mu = (double)ILMath.multiply(d, gradnew.T);
                        if (mu >= 0)
                        {
                            d.a = -gradnew.C;
                            mu = (double)ILMath.multiply(d, gradnew.T);
                        }
                        kappa = (double)ILMath.multiply(d, d.T);
                        if (kappa < ILMath.eps)
                        {
                            return;
                        }

                        sigma = sigma0 / (double)ILMath.sqrt(kappa);
                        xplus.a = x + sigma * d;
                        fl.Write("Setting new parameters", xplus);
                        function.Parameters = xplus.C;
                        fl.Write("Reading new parameters", function.Parameters);
                        fl.Write("Function value", function.Value());
                        fl.Write("Function gradient", function.Gradient());
                        gplus.a = function.Gradient();
                        theta = (double)(ILMath.multiply(d, (gplus.T - gradnew.T))) / sigma;
                    }

                    // Increase effective curvature and evaluate step size alpha.
                    delta = theta + beta * kappa;
                    if (delta <= 0)
                    {
                        delta = beta * kappa;
                        beta = beta - theta / kappa;
                        //return; // mu is not updated, return to prevent result corruption
                    }
                    alpha = -mu / delta;

                    // Calculate the comparison ratio.
                    xnew.a = x + alpha * d;
                    fl.Write("Setting new parameters", xnew);
                    function.Parameters = xnew.C;
                    fl.Write("Reading new parameters", function.Parameters);
                    fl.Write("Function value", function.Value());
                    fl.Write("Function gradient", function.Gradient());
                    fnew = function.Value();
                    Delta = 2 * (fnew - fold) / (alpha * mu);
                    if (Delta >= 0)
                    {
                        success = true;
                        nsuccess = nsuccess + 1;
                        x.a = xnew.C;
                        fnow = fnew;
                    }
                    else
                    {
                        success = false;
                        fnow = fold;
                    }

                    if (display)
                        System.Console.WriteLine("Cycle: {0}\t  Error: {1}\t  Scale: {2}", j, fnow, beta);

                    if (success)
                    {
                        // Test for termination
                        if (ILMath.max(ILMath.abs(ILMath.multiply(alpha, d))) < 1.0e-03 && ILMath.max(ILMath.abs(fnew - fold)) < 1.0e-03)
                            return;

                        else
                        {
                            // Update variables for new position
                            fold = fnew;
                            gradold.a = gradnew.C;
                            fl.Write("Setting new parameters", x);
                            function.Parameters = x.C;
                            fl.Write("Reading new parameters", function.Parameters);
                            fl.Write("Function value", function.Value());
                            fl.Write("Function gradient", function.Gradient());
                            gradnew.a = function.Gradient();
                            // If the gradient is zero then we are done.
                            if (ILMath.multiply(gradnew, gradnew.T) == 0)
                                return;
                        }
                    }

                    // Adjust beta according to comparison ratio.
                    if (Delta < 0.25)
                        beta = Math.Min(4.0 * beta, betamax);

                    if (Delta > 0.75)
                        beta = Math.Max(0.5 * beta, betamin);


                    // Update search direction using Polak-Ribiere formula, or re-start 
                    // in direction of negative gradient after nparams steps.
                    if (nsuccess == nparams)
                    {
                        d.a = -gradnew.C;
                        nsuccess = 0;
                    }
                    else
                    {
                        if (success)
                        {
                            gamma = (double)ILMath.multiply(gradold - gradnew, gradnew.T) / (mu);
                            d.a = gamma * d - gradnew;
                        }
                    }
                    j++;
                }
            }
            finally
            {
                fl.Close();
            }
        }

    }
}
