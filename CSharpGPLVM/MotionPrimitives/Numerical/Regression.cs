using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;

namespace MotionPrimitives.Numerical
{
    public class Regression
    {
        /// <summary>
        /// Linear ordinary least squares regression
        /// </summary>
        /// <param name="inX"></param>
        /// <param name="inY"></param>
        /// <returns></returns>
        public static ILRetArray<double> RemoveLinear(ILInArray<double> inY)
        {
            using (ILScope.Enter(inY))
            {
                ILArray<double> Y = ILMath.check(inY);
                ILArray<double> YCentered = Y - ILMath.mean(Y, 0);
                ILArray<double> X = ILMath.counter(Y.S[0]);
                X = X - ILMath.mean(X, 0);

                ILArray<double> beta = LinearLeastSquares(X, YCentered);
                ILArray<double> yCorrected = Y - ILMath.multiply(X, beta);

                return yCorrected;
            }
        }

        /// <summary>
        /// Linear ordinary least squares regression
        /// </summary>
        /// <param name="inX"></param>
        /// <param name="inY"></param>
        /// <returns>Estimated beta</returns>
        public static ILRetArray<double> LinearLeastSquares(ILInArray<double> inX, ILInArray<double> inY)
        {
            using (ILScope.Enter(inX, inY))
            {
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> Y = ILMath.check(inY);
                ILArray<double> beta = ILMath.empty();

                beta = ILMath.multiply(Util.pdinverse(ILMath.multiply(X.T ,X)), ILMath.multiply(X.T ,Y));

                return beta;
            }
        }

    }
}
