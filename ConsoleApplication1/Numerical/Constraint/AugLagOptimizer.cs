using System;
using ILNumerics;

namespace GPLVM.Numerical
{
    public class AugLagOptimizer : IFunctionWithGradient, IFunctionWithGradientConstraintOptimizer
    {
        /// <summary>
        ///   Augmented Lagrangiam method for constrained non-linear optimization.
        /// </summary>
        /// <remarks>
        /// Augmented Lagrangian methods are a certain class of algorithms for solving 
        /// constrained optimization problems. They have similarities to penalty methods 
        /// in that they replace a constrained optimization problem by a series of 
        /// unconstrained problems; the difference is that the augmented Lagrangian method 
        /// adds an additional term to the unconstrained objective. This additional term 
        /// is designed to mimic a Lagrange multiplier. The augmented Lagrangian is not 
        /// the same as the method of Lagrange multipliers.
        /// 
        /// Viewed differently, the unconstrained objective is the Lagrangian of the 
        /// constrained problem, with an additional penalty term (the augmentation).
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description><a href="http://ab-initio.mit.edu/nlopt">
        ///       Steven G. Johnson, The NLopt nonlinear-optimization package, http://ab-initio.mit.edu/nlopt </a></description></item>
        ///     <item><description><a href="http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.72.6121">
        ///       E. G. Birgin and J. M. Martinez, "Improving ultimate convergence of an augmented Lagrangian
        ///       method," Optimization Methods and Software vol. 23, no. 2, p. 177-195 (2008). </a></description></item>
        ///   </list>
        /// </para>   
        /// </remarks>
        /// 

        private IFunctionWithGradientOptimizer dualSolver;
        private IFunctionWithGradientConstraint objective;

        //INonlinearConstraint[] lesserThanConstraints;
        //INonlinearConstraint[] greaterThanConstraints;
        //INonlinearConstraint[] equalityConstraints;

        private double rho;
        private double[] lambda; // equality multipliers
        private double[] mu;     // "lesser than"  inequality multipliers
        private double[] nu;     // "greater than" inequality multipliers


        // Stopping criteria
        private double ftol_abs = 0;
        private double ftol_rel = 1e-10;
        private double xtol_rel = 1e-10;

        private int functionEvaluations = 0;
        private int maxEvaluations;
        private int iterations;

        public int NumParameters
        {
            get { return objective.NumParameters; }
        }

        // Augmented Lagrangian objective
        public double Value()
        {
            // Compute
            //
            //   Phi(x) = f(x) + rho/2 sum(c_i(x)²) - sum(lambda_i * c_i(x))
            //

            double sumOfSquares = 0;
            double weightedSum = 0;
            double rho2 = rho / 2;

            // For each equality constraint
            for (int i = 0; i < objective.equalityConstraints.Length; i++)
            {
                double c = objective.equalityConstraints[i].Function() - objective.equalityConstraints[i].Value;

                sumOfSquares += rho2 * c * c;
                weightedSum += lambda[i] * c;
            }

            // For each "lesser than" inequality constraint
            for (int i = 0; i < objective.lesserThanConstraints.Length; i++)
            {
                double c = objective.lesserThanConstraints[i].Function() - objective.lesserThanConstraints[i].Value;

                if (c > 0)
                {
                    sumOfSquares += rho2 * c * c;
                    weightedSum += mu[i] * c;
                }
            }

            // For each "greater than" inequality constraint
            for (int i = 0; i < objective.greaterThanConstraints.Length; i++)
            {
                double c = -objective.greaterThanConstraints[i].Function() + objective.greaterThanConstraints[i].Value;

                if (c > 0)
                {
                    sumOfSquares += rho2 * c * c;
                    weightedSum += nu[i] * c;
                }
            }

            double phi = objective.Value() + sumOfSquares - weightedSum;

            return phi;
        }

        public ILArray<double> Gradient()
        {
            // Compute
            //
            //   Phi'(x) = f'(x) + rho sum(c_i(x)*c_i'(x)) - sum(lambda_i * c_i'(x))
            //

            ILArray<double> g = objective.Gradient();

            double[] sum = new double[Parameters.Length];
            double[] weightedSum = new double[Parameters.Length];

            // For each equality constraint
            for (int i = 0; i < objective.equalityConstraints.Length; i++)
            {
                double c = objective.equalityConstraints[i].Function() - objective.equalityConstraints[i].Value;
                ILArray<double> cg = objective.equalityConstraints[i].Gradient();

                for (int j = 0; j < cg.Length; j++)
                {
                    sum[j] += rho * c * (double)cg[j];
                    weightedSum[j] += lambda[i] * (double)cg[j];
                }
            }

            // For each "lesser than" inequality constraint
            for (int i = 0; i < objective.lesserThanConstraints.Length; i++)
            {
                double c = objective.lesserThanConstraints[i].Function() - objective.lesserThanConstraints[i].Value;
                ILArray<double> cg = objective.lesserThanConstraints[i].Gradient();

                if (c > 0)
                {
                    // Constraint is being violated
                    for (int j = 0; j < cg.Length; j++)
                    {
                        sum[j] += rho * c * (double)cg[j];
                        weightedSum[j] += mu[i] * (double)cg[j];
                    }
                }
            }

            // For each "greater-than" inequality constraint
            for (int i = 0; i < objective.greaterThanConstraints.Length; i++)
            {
                double c = -objective.greaterThanConstraints[i].Function() + objective.greaterThanConstraints[i].Value;
                ILArray<double> cg = objective.greaterThanConstraints[i].Gradient();

                if (c > 0)
                {
                    // Constraint is being violated
                    for (int j = 0; j < cg.Length; j++)
                    {
                        sum[j] += rho * c * -(double)cg[j];
                        weightedSum[j] += nu[i] * -(double)cg[j];
                    }
                }
            }

            for (int i = 0; i < g.Length; i++)
                g[i] += sum[i] - weightedSum[i];

            return g;
        }

        public ILArray<double> Parameters
        {
            get { return objective.Parameters; }
            set { objective.Parameters = value; }
        }

        /// <summary>
        ///   Minimizes the given function. 
        /// </summary>
        /// 
        /// <param name="function">The function to be minimized.</param>
        /// 
        /// <returns>The minimum value found at the <see cref="Solution"/>.</returns>
        /// 
        public void Optimize(IFunctionWithGradientConstraint function, int maxIterations, bool display)
        {
            //if (function.NumParameters != numberOfVariables)
            //    throw new ArgumentOutOfRangeException("function",
            //        "Incorrect number of variables in the objective function. " +
            //        "The number of variables must match the number of variables set in the solver.");

            dualSolver = new SCGOptimizer();
            objective = function;

            minimize(maxIterations, display);
        }

        private double minimize(int maxIterations, bool display)
        {
            double ICM = Double.PositiveInfinity;

            double minPenalty = Double.PositiveInfinity;
            double minValue = Double.PositiveInfinity;

            double penalty;
            double currentValue;

            bool minFeasible = false;
            int noProgressCounter = 0;
            int maxCount = 20;
            iterations = 0;

            // magic parameters from Birgin & Martinez
            const double tau = 0.5, gam = 10;
            const double lam_min = -1e20;
            const double lam_max = 1e20;
            const double mu_max = 1e20;
            const double nu_max = 1e20;

            ILArray<double> Solution = objective.Parameters;
            //Parameters = Solution;

            lambda = new double[objective.equalityConstraints.Length];
            mu = new double[objective.lesserThanConstraints.Length];
            nu = new double[objective.greaterThanConstraints.Length];
            rho = 1;

            maxEvaluations = maxIterations;


            // Starting rho suggested by B & M 
            if (lambda.Length > 0 || mu.Length > 0 || nu.Length > 0)
            {
                double con2 = 0;
                penalty = 0;

                // Evaluate function
                functionEvaluations++;
                currentValue = objective.Value();

                bool feasible = true;

                // For each equality constraint
                for (int i = 0; i < objective.equalityConstraints.Length; i++)
                {
                    double c = objective.equalityConstraints[i].Function() - objective.equalityConstraints[i].Value;

                    penalty += Math.Abs(c);
                    feasible = feasible && Math.Abs(c) <= objective.equalityConstraints[i].Tolerance;
                    con2 += c * c;
                }

                // For each "lesser than" inequality constraint
                for (int i = 0; i < objective.lesserThanConstraints.Length; ++i)
                {
                    double c = objective.lesserThanConstraints[i].Function() - objective.lesserThanConstraints[i].Value;

                    penalty += c > 0 ? c : 0;
                    feasible = feasible && c <= objective.lesserThanConstraints[i].Tolerance;
                    if (c > 0) con2 += c * c;
                }

                // For each "greater than" inequality constraint
                for (int i = 0; i < objective.greaterThanConstraints.Length; ++i)
                {
                    double c = -objective.greaterThanConstraints[i].Function() + objective.greaterThanConstraints[i].Value;

                    penalty += c > 0 ? c : 0;
                    feasible = feasible && c <= objective.greaterThanConstraints[i].Tolerance;
                    if (c > 0) con2 += c * c;
                }

                minValue = currentValue;
                minPenalty = penalty;
                minFeasible = feasible;
                rho = Math.Max(1e-6, Math.Min(10, 2 * Math.Abs(minValue) / con2));
            }


            while (true)
            {
                double prevICM = ICM;

                try
                {
                    // Minimize the dual problem using current solution
                    //Parameters = currentSolution;
                    dualSolver.Optimize(this, maxIterations, display);
                }
                catch (LineSearchFailedException)
                {
                }
                catch (NotFiniteNumberException)
                {
                }

                // Evaluate function
                functionEvaluations++;
                currentValue = objective.Value();

                ICM = 0;
                penalty = 0;
                bool feasible = true;

                // Update lambdas
                for (int i = 0; i < objective.equalityConstraints.Length; i++)
                {
                    double c = objective.equalityConstraints[i].Function() - objective.equalityConstraints[i].Value;

                    double newLambda = lambda[i] + rho * c;
                    penalty += Math.Abs(c);
                    feasible = feasible && Math.Abs(c) <= objective.equalityConstraints[i].Tolerance;
                    ICM = Math.Max(ICM, Math.Abs(c));
                    lambda[i] = Math.Min(Math.Max(lam_min, newLambda), lam_max);
                }

                // Update mus
                for (int i = 0; i < objective.lesserThanConstraints.Length; i++)
                {
                    double c = objective.lesserThanConstraints[i].Function() - objective.lesserThanConstraints[i].Value;

                    double newMu = mu[i] + rho * c;
                    penalty += c > 0 ? c : 0;
                    feasible = feasible && c <= objective.lesserThanConstraints[i].Tolerance;
                    ICM = Math.Max(ICM, Math.Abs(Math.Max(c, -mu[i] / rho)));
                    mu[i] = Math.Min(Math.Max(0.0, newMu), mu_max);
                }

                // Update nus
                for (int i = 0; i < objective.greaterThanConstraints.Length; i++)
                {
                    double c = -objective.greaterThanConstraints[i].Function() + objective.greaterThanConstraints[i].Value;

                    double newNu = nu[i] + rho * c;
                    penalty += c > 0 ? c : 0;
                    feasible = feasible && c <= objective.greaterThanConstraints[i].Tolerance;
                    ICM = Math.Max(ICM, Math.Abs(Math.Max(c, -nu[i] / rho)));
                    nu[i] = Math.Min(Math.Max(0.0, newNu), nu_max);
                }

                // Update rho
                if (ICM > tau * prevICM)
                {
                    rho *= gam;
                }


                // Check if we should stop
                if (
                      (feasible &&
                         (!minFeasible || penalty < minPenalty || currentValue < minValue)
                      ) || (!minFeasible && penalty < minPenalty)
                    )
                {
                    if (feasible)
                    {
                        if (relstop(minValue, currentValue, ftol_rel, ftol_abs))
                            return minValue;

                        bool xtolreach = true;
                        for (int i = 0; i < objective.Parameters.Length; i++)
                            if (!relstop((double)Solution[i], (double)objective.Parameters[i], xtol_rel, 0))
                                xtolreach = false;
                        if (xtolreach)
                            return minValue;
                    }

                    minValue = currentValue;
                    minPenalty = penalty;
                    minFeasible = feasible;

                    Solution = objective.Parameters;

                    noProgressCounter = 0;
                }
                else
                {
                    if (ICM == 0)
                        return minValue;

                    noProgressCounter++;

                    if (noProgressCounter > maxCount)
                        return minValue;
                }


                // Go to next iteration
                iterations++;

                if (maxEvaluations > 0 && functionEvaluations >= maxEvaluations)
                    return minValue;

            }

        }

        static bool relstop(double vold, double vnew, double reltol, double abstol)
        {
            if (Double.IsInfinity(vold))
                return false;

            return (Math.Abs(vnew - vold) < abstol
               || Math.Abs(vnew - vold) < reltol * (Math.Abs(vnew) + Math.Abs(vold)) * 0.5
               || (reltol > 0 && vnew == vold)); // catch vnew == vold == 0 
        }
    }
}
