using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Kernel;
using GPLVM.Numerical;
using GPLVM.Optimisation;
using GPLVM.Styles;
using FactorGraph.Core;
using FactorGraph.FactorNodes;
using FactorGraph.DataNodes;

namespace FactorGraphTest
{
    partial class Program
    {
        static void TestDirectionKern(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);
                var kDirection = new DirectionKern();

                kDirection.VarianceScaleParameterEnabled = false;
                ILArray<double> K = kDirection.ComputeKernelMatrix(X, X);
                Console.WriteLine("Direction kernel matrix: " + K.ToString());

                ILArray<double> dL_dK = ILMath.ones(X.S[0], X.S[0]);
                ILArray<double> dL_dX = kDirection.LogLikGradientX(X, dL_dK);
                Console.WriteLine("dL_dX: " + dL_dX.ToString());

                var kLinear = new LinearKern();
                int q = X.S[1] / 2;
                ILArray<double> vX = X[ILMath.full, ILMath.r(q, 2 * q - 1)] - X[ILMath.full, ILMath.r(0, q - 1)];
                K = kLinear.ComputeKernelMatrix(vX, vX);
                Console.WriteLine("Linear kernel matrix: " + K.ToString());
                dL_dX = kLinear.LogLikGradientX(vX, dL_dK);
                Console.WriteLine("dL_dX: " + dL_dX.ToString());
            }
        }
    }
}
