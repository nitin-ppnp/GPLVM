using System;
using ILNumerics;

namespace GPLVM.Numerical
{
    public class BFGSOptimizer : IFunctionWithGradientOptimizer
    {
        private int n;
        private int corrections = 5; // number of corrections; should be a value between 3 and 7; 5 is default

        private double tolerance = 1e-10;

        private const double stpmin = 1e-20;
        private const double stpmax = 1e20;
        private const double ftol = 0.0001;
        private const double xtol = 1e-16; // machine precision

        private double gtol = 0.9;
        private int maxfev = 50;

        private double[] upperBound;
        private double[] lowerBound;

        /// <summary>
        ///   Limited-memory Broyden–Fletcher–Goldfarb–Shanno (L-BFGS) optimization method.
        /// </summary>
        /// <remarks>
        /// <para>
        ///   The L-BFGS algorithm is a member of the broad family of quasi-Newton optimization
        ///   methods. L-BFGS stands for 'Limited memory BFGS'. Indeed, L-BFGS uses a limited
        ///   memory variation of the Broyden–Fletcher–Goldfarb–Shanno (BFGS) update to approximate
        ///   the inverse Hessian matrix (denoted by Hk). Unlike the original BFGS method which
        ///   stores a dense  approximation, L-BFGS stores only a few vectors that represent the
        ///   approximation implicitly. Due to its moderate memory requirement, L-BFGS method is
        ///   particularly well suited for optimization problems with a large number of variables.</para>
        /// <para>
        ///   L-BFGS never explicitly forms or stores Hk. Instead, it maintains a history of the past
        ///   <c>m</c> updates of the position <c>x</c> and gradient <c>g</c>, where generally the history
        ///   <c>m</c>can be short, often less than 10. These updates are used to implicitly do operations
        ///   requiring the Hk-vector product.</para>
        ///   
        /// <para>
        ///   The framework implementation of this method is based on the original FORTRAN source code
        ///   by Jorge Nocedal (see references below). The original FORTRAN source code of LBFGS (for
        ///   unconstrained problems) is available at http://www.netlib.org/opt/lbfgs_um.shar and had
        ///   been made available under the public domain. </para>
        /// 
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description><a href="http://www.netlib.org/opt/lbfgs_um.shar">
        ///        Jorge Nocedal. Limited memory BFGS method for large scale optimization (Fortran source code). 1990.
        ///        Available in http://www.netlib.org/opt/lbfgs_um.shar </a></description></item>
        ///     <item><description>
        ///        Jorge Nocedal. Updating Quasi-Newton Matrices with Limited Storage. <i>Mathematics of Computation</i>,
        ///        Vol. 35, No. 151, pp. 773--782, 1980.</description></item>
        ///     <item><description>
        ///        Dong C. Liu, Jorge Nocedal. On the limited memory BFGS method for large scale optimization.</description></item>
        ///    </list></para>
        /// </remarks>
        /// 
        /// <summary>
        ///   Creates a new instance of the L-BFGS optimization algorithm.
        /// </summary>
        /// 
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        ///
        public void Optimize(IFunctionWithGradient function, int maxIterations, bool display)
        {
            n = function.NumParameters;

            upperBound = new double[n];
            lowerBound = new double[n];

            for (int i = 0; i < upperBound.Length; i++)
                lowerBound[i] = Double.NegativeInfinity;

            for (int i = 0; i < upperBound.Length; i++)
                upperBound[i] = Double.PositiveInfinity;

            optimize(function, maxIterations, display);
        }

        private unsafe void optimize(IFunctionWithGradient function, int maxIterations, bool display)
        {
            // Initialization
            int iterations;
            int evaluations;

            int m = corrections;

            // Obtain initial Hessian
            ILArray<double> diagonal = ILMath.ones(n);
            ILArray<double> g = ILMath.empty();
            ILArray<double> x = function.Parameters;

            // work vector
            double[] work = createWorkVector(n, m);

            // Make initial evaluation
            double f = function.Value();
            g = function.Gradient();

            iterations = 0;
            evaluations = 1;

            fixed (double* w = work)
            {
                // The first N locations of the work vector are used to
                //  store the gradient and other temporary information.

                double* rho = &w[n];                   // Stores the scalars rho.
                double* alpha = &w[n + m];             // Stores the alphas in computation of H*g.
                double* steps = &w[n + 2 * m];         // Stores the last M search steps.
                double* delta = &w[n + 2 * m + n * m]; // Stores the last M gradient diferences.


                // Initialize work vector
                for (int i = 0; i < g.Length; i++)
                    steps[i] = (double)(-g[i] * diagonal[i]);

                // Initialize statistics
                double gnorm = Util.Euclidean(g);
                double xnorm = Util.Euclidean(x);
                double stp = 1.0 / gnorm;
                double stp1 = stp;

                // Initialize loop
                int nfev, point = 0;
                int npt = 0, cp = 0;
                bool finish = false;

                // Main optimization loop.
                if (display)
                    System.Console.WriteLine("\nNumerical optimization via BFGS\n===============================\n");


                // Start main
                while (!finish && iterations <= maxIterations)
                {
                    iterations++;
                    double bound = iterations - 1;

                    if (iterations != 1)
                    {
                        if (iterations > m)
                            bound = m;

                        double ys = 0;
                        for (int i = 0; i < n; i++)
                            ys += delta[npt + i] * steps[npt + i];

                        // Compute the diagonal of the Hessian
                        // or use an approximation by the user.

                        double yy = 0;
                        for (int i = 0; i < n; i++)
                            yy += delta[npt + i] * delta[npt + i];
                        double d = ys / yy;

                        for (int i = 0; i < n; i++)
                            diagonal[i] = d;


                        // Compute -H*g using the formula given in:
                        //   Nocedal, J. 1980, "Updating quasi-Newton matrices with limited storage",
                        //   Mathematics of Computation, Vol.24, No.151, pp. 773-782.

                        cp = (point == 0) ? m : point;
                        rho[cp - 1] = 1.0 / ys;
                        for (int i = 0; i < n; i++)
                            w[i] = (double)-g[i];

                        cp = point;
                        for (int i = 1; i <= bound; i += 1)
                        {
                            if (--cp == -1) cp = m - 1;

                            double sq = 0;
                            for (int j = 0; j < n; j++)
                                sq += steps[cp * n + j] * w[j];

                            double beta = alpha[cp] = rho[cp] * sq;
                            for (int j = 0; j < n; j++)
                                w[j] -= beta * delta[cp * n + j];
                        }

                        for (int i = 0; i < diagonal.Length; i++)
                            w[i] *= (double)diagonal[i];

                        for (int i = 1; i <= bound; i += 1)
                        {
                            double yr = 0;
                            for (int j = 0; j < n; j++)
                                yr += delta[cp * n + j] * w[j];

                            double beta = alpha[cp] - rho[cp] * yr;
                            for (int j = 0; j < n; j++)
                                w[j] += beta * steps[cp * n + j];

                            if (++cp == m) cp = 0;
                        }

                        npt = point * n;

                        // Store the search direction
                        for (int i = 0; i < n; i++)
                            steps[npt + i] = w[i];

                        stp = 1;
                    }

                    // Save original gradient
                    for (int i = 0; i < g.Length; i++)
                        w[i] = (double)g[i];


                    // Obtain the one-dimensional minimizer of f by computing a line search
                    mcsrch(function, x, ref f, ref g, &steps[point * n], ref stp, out nfev, diagonal);

                    // Register evaluations
                    evaluations += nfev;

                    // Compute the new step and
                    // new gradient differences
                    for (int i = 0; i < g.Length; i++)
                    {
                        steps[npt + i] *= stp;
                        delta[npt + i] = (double)(g[i] - w[i]);
                    }

                    if (++point == m) point = 0;


                    // Check for termination
                    gnorm = Util.Euclidean(g);
                    xnorm = Util.Euclidean(x);
                    xnorm = Math.Max(1.0, xnorm);

                    if (gnorm / xnorm <= tolerance)
                        finish = true;

                    if (display)
                        System.Console.WriteLine("Cycle: {0}\t  Error: {1}\t  Step Size: {2}", iterations, f, stp);
                }
            }
        }

        /// <summary>
        ///   Finds a step which satisfies a sufficient decrease and curvature condition.
        /// </summary>
        /// 
        private unsafe void mcsrch(IFunctionWithGradient function, ILArray<double> x, ref double f, ref ILArray<double> g, double* s,
            ref double stp, out int nfev, ILArray<double> wa)
        {
            double ftest1 = 0;
            int infoc = 1;

            nfev = 0;

            // Compute the initial gradient in the search direction
            // and check that s is a descent direction.
            double dginit = 0;

            for (int j = 0; j < g.Length; j++)
                dginit = dginit + (double)(g[j] * s[j]);

            if (dginit >= 0)
                throw new LineSearchFailedException(0, "The search direction is not a descent direction.");

            bool brackt = false;
            bool stage1 = true;

            double finit = f;
            double dgtest = ftol * dginit;
            double width = stpmax - stpmin;
            double width1 = width / 0.5;

            for (int j = 0; j < x.Length; j++)
                wa[j] = x[j];

            // The variables stx, fx, dgx contain the values of the
            // step, function, and directional derivative at the best
            // step.

            double stx = 0;
            double fx = finit;
            double dgx = dginit;

            // The variables sty, fy, dgy contain the value of the
            // step, function, and derivative at the other endpoint
            // of the interval of uncertainty.

            double sty = 0;
            double fy = finit;
            double dgy = dginit;

            // The variables stp, f, dg contain the values of the step,
            // function, and derivative at the current step.

            double dg = 0;


            while (true)
            {
                // Set the minimum and maximum steps to correspond
                // to the present interval of uncertainty.

                double stmin, stmax;

                if (brackt)
                {
                    stmin = Math.Min(stx, sty);
                    stmax = Math.Max(stx, sty);
                }
                else
                {
                    stmin = stx;
                    stmax = stp + 4.0 * (stp - stx);
                }

                // Force the step to be within the bounds stpmax and stpmin.

                stp = Math.Max(stp, stpmin);
                stp = Math.Min(stp, stpmax);

                // If an unusual termination is to occur then let
                // stp be the lowest point obtained so far.

                if ((brackt && (stp <= stmin || stp >= stmax)) ||
                    (brackt && stmax - stmin <= xtol * stmax) ||
                    (nfev >= maxfev - 1) || (infoc == 0))
                    stp = stx;

                // Evaluate the function and gradient at stp
                // and compute the directional derivative.
                // We return to main program to obtain F and G.

                for (int j = 0; j < x.Length; j++)
                {
                    x[j] = wa[j] + stp * s[j];

                    if (x[j] > upperBound[j])
                        x[j] = upperBound[j];
                    else if (x[j] < lowerBound[j])
                        x[j] = lowerBound[j];
                }

                function.Parameters = x.C;
                // Reevaluate function and gradient
                f = function.Value();
                g = function.Gradient();

                nfev++;
                dg = 0;

                for (int j = 0; j < g.Length; j++)
                    dg = dg + (double)(g[j] * s[j]);

                ftest1 = finit + stp * dgtest;

                // Test for convergence.

                if (nfev >= maxfev)
                    return;
                    //throw new LineSearchFailedException(3, "Maximum number of function evaluations has been reached.");

                if ((brackt && (stp <= stmin || stp >= stmax)) || infoc == 0)
                    return;
                    //throw new LineSearchFailedException(6, "Rounding errors prevent further progress." +
                    //    "There may not be a step which satisfies the sufficient decrease and curvature conditions. Tolerances may be too small.");

                if (stp == stpmax && f <= ftest1 && dg <= dgtest)
                    return;
                    //throw new LineSearchFailedException(5, "The step size has reached the upper bound.");

                if (stp == stpmin && (f > ftest1 || dg >= dgtest))
                    return;
                    //throw new LineSearchFailedException(4, "The step size has reached the lower bound.");

                if (brackt && stmax - stmin <= xtol * stmax)
                    return;
                    //throw new LineSearchFailedException(2, "Relative width of the interval of uncertainty is at machine precision.");

                if (f <= ftest1 && Math.Abs(dg) <= gtol * (-dginit))
                    return;

                // Not converged yet. Continuing with the search.

                // In the first stage we seek a step for which the modified
                // function has a nonpositive value and nonnegative derivative.

                if (stage1 && f <= ftest1 && dg >= Math.Min(ftol, gtol) * dginit)
                    stage1 = false;

                // A modified function is used to predict the step only if we
                // have not obtained a step for which the modified function has
                // a nonpositive function value and nonnegative derivative, and
                // if a lower function value has been obtained but the decrease
                // is not sufficient.

                if (stage1 && f <= fx && f > ftest1)
                {
                    // Define the modified function and derivative values.

                    double fm = f - stp * dgtest;
                    double fxm = fx - stx * dgtest;
                    double fym = fy - sty * dgtest;

                    double dgm = dg - dgtest;
                    double dgxm = dgx - dgtest;
                    double dgym = dgy - dgtest;

                    // Call cstep to update the interval of uncertainty
                    // and to compute the new step.

                    SearchStep(ref stx, ref fxm, ref dgxm,
                        ref sty, ref fym, ref dgym, ref stp,
                        fm, dgm, ref brackt, out infoc);

                    // Reset the function and gradient values for f.
                    fx = fxm + stx * dgtest;
                    fy = fym + sty * dgtest;
                    dgx = dgxm + dgtest;
                    dgy = dgym + dgtest;
                }
                else
                {
                    // Call mcstep to update the interval of uncertainty
                    // and to compute the new step.

                    SearchStep(ref stx, ref fx, ref dgx,
                        ref sty, ref fy, ref dgy, ref stp,
                        f, dg, ref brackt, out infoc);
                }

                // Force a sufficient decrease in the size of the
                // interval of uncertainty.

                if (brackt)
                {
                    if (Math.Abs(sty - stx) >= 0.66 * width1)
                        stp = stx + 0.5 * (sty - stx);

                    width1 = width;
                    width = Math.Abs(sty - stx);
                }

            }
        }

        internal static void SearchStep(ref double stx, ref double fx, ref double dx,
                                   ref double sty, ref double fy, ref double dy,
                                   ref double stp, double fp, double dp,
                                   ref bool brackt, out int info)
        {
            bool bound;
            double stpc, stpf, stpq;

            info = 0;

            if ((brackt && (stp <= Math.Min(stx, sty) || stp >= Math.Max(stx, sty))) ||
                (dx * (stp - stx) >= 0.0) || (stpmax < stpmin)) return;

            // Determine if the derivatives have opposite sign.
            double sgnd = dp * (dx / Math.Abs(dx));

            if (fp > fx)
            {
                // First case. A higher function value.
                // The minimum is bracketed. If the cubic step is closer
                // to stx than the quadratic step, the cubic step is taken,
                // else the average of the cubic and quadratic steps is taken.

                info = 1;
                bound = true;
                double theta = 3.0 * (fx - fp) / (stp - stx) + dx + dp;
                double s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dx), Math.Abs(dp)));
                double gamma = s * Math.Sqrt((theta / s) * (theta / s) - (dx / s) * (dp / s));

                if (stp < stx) gamma = -gamma;

                double p = gamma - dx + theta;
                double q = gamma - dx + gamma + dp;
                double r = p / q;
                stpc = stx + r * (stp - stx);
                stpq = stx + ((dx / ((fx - fp) / (stp - stx) + dx)) / 2) * (stp - stx);

                if (Math.Abs(stpc - stx) < Math.Abs(stpq - stx))
                    stpf = stpc;
                else
                    stpf = stpc + (stpq - stpc) / 2.0;

                brackt = true;
            }
            else if (sgnd < 0.0)
            {
                // Second case. A lower function value and derivatives of
                // opposite sign. The minimum is bracketed. If the cubic
                // step is closer to stx than the quadratic (secant) step,
                // the cubic step is taken, else the quadratic step is taken.

                info = 2;
                bound = false;
                double theta = 3 * (fx - fp) / (stp - stx) + dx + dp;
                double s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dx), Math.Abs(dp)));
                double gamma = s * Math.Sqrt((theta / s) * (theta / s) - (dx / s) * (dp / s));

                if (stp > stx) gamma = -gamma;

                double p = (gamma - dp) + theta;
                double q = ((gamma - dp) + gamma) + dx;
                double r = p / q;
                stpc = stp + r * (stx - stp);
                stpq = stp + (dp / (dp - dx)) * (stx - stp);

                if (Math.Abs(stpc - stp) > Math.Abs(stpq - stp))
                    stpf = stpc;
                else stpf = stpq;

                brackt = true;
            }
            else if (Math.Abs(dp) < Math.Abs(dx))
            {
                // Third case. A lower function value, derivatives of the
                // same sign, and the magnitude of the derivative decreases.
                // The cubic step is only used if the cubic tends to infinity
                // in the direction of the step or if the minimum of the cubic
                // is beyond stp. Otherwise the cubic step is defined to be
                // either stpmin or stpmax. The quadratic (secant) step is also
                // computed and if the minimum is bracketed then the the step
                // closest to stx is taken, else the step farthest away is taken.

                info = 3;
                bound = true;
                double theta = 3 * (fx - fp) / (stp - stx) + dx + dp;
                double s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dx), Math.Abs(dp)));
                double gamma = s * Math.Sqrt(Math.Max(0, (theta / s) * (theta / s) - (dx / s) * (dp / s)));

                if (stp > stx) gamma = -gamma;

                double p = (gamma - dp) + theta;
                double q = (gamma + (dx - dp)) + gamma;
                double r = p / q;

                if (r < 0.0 && gamma != 0.0)
                    stpc = stp + r * (stx - stp);
                else if (stp > stx)
                    stpc = stpmax;
                else stpc = stpmin;

                stpq = stp + (dp / (dp - dx)) * (stx - stp);

                if (brackt)
                {
                    if (Math.Abs(stp - stpc) < Math.Abs(stp - stpq))
                        stpf = stpc;
                    else stpf = stpq;
                }
                else
                {
                    if (Math.Abs(stp - stpc) > Math.Abs(stp - stpq))
                        stpf = stpc;
                    else stpf = stpq;
                }
            }
            else
            {
                // Fourth case. A lower function value, derivatives of the
                // same sign, and the magnitude of the derivative does
                // not decrease. If the minimum is not bracketed, the step
                // is either stpmin or stpmax, else the cubic step is taken.

                info = 4;
                bound = false;

                if (brackt)
                {
                    double theta = 3 * (fp - fy) / (sty - stp) + dy + dp;
                    double s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dy), Math.Abs(dp)));
                    double gamma = s * Math.Sqrt((theta / s) * (theta / s) - (dy / s) * (dp / s));

                    if (stp > sty) gamma = -gamma;

                    double p = (gamma - dp) + theta;
                    double q = ((gamma - dp) + gamma) + dy;
                    double r = p / q;
                    stpc = stp + r * (sty - stp);
                    stpf = stpc;
                }
                else if (stp > stx)
                    stpf = stpmax;
                else stpf = stpmin;
            }

            // Update the interval of uncertainty. This update does not
            // depend on the new step or the case analysis above.

            if (fp > fx)
            {
                sty = stp;
                fy = fp;
                dy = dp;
            }
            else
            {
                if (sgnd < 0.0)
                {
                    sty = stx;
                    fy = fx;
                    dy = dx;
                }
                stx = stp;
                fx = fp;
                dx = dp;
            }

            // Compute the new step and safeguard it.
            stpf = Math.Min(stpmax, stpf);
            stpf = Math.Max(stpmin, stpf);
            stp = stpf;

            if (brackt && bound)
            {
                if (sty > stx)
                    stp = Math.Min(stx + 0.66 * (sty - stx), stp);
                else
                    stp = Math.Max(stx + 0.66 * (sty - stx), stp);
            }

            return;
        }

        private double[] createWorkVector(int numberOfVariables, int corrections)
        {
            return new double[numberOfVariables * (2 * corrections + 1) + 2 * corrections];
        }
    }
}
