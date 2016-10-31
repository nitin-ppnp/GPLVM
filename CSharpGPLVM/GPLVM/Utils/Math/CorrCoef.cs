using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        
        /// <summary>
        /// Correlation coefficients.
        /// </summary>
        /// <remarks>
        /// calculates a matrix R of correlation coefficients for an array X, 
        /// in which each row is an observation and each column is a variable.
        /// </remarks>
        public static ILRetArray<double> CorrCoef(ILInArray<double> inX, ILInArray<double> inY = null)
        {
            using (ILScope.Enter(inX, inY))
            {
                ILArray<double> x = ILMath.check(inX);
                ILArray<double> y = ILMath.empty();

                ILArray<double> r = ILMath.empty();

                if (inY != null) y = ILMath.check(inY);

                if (!y.IsEmpty)
                {
                    x = x[ILMath.full];
                    y = y[ILMath.full];

                    if (x.Length != y.Length)
                    {
                        System.Console.WriteLine("MATLAB:corrcoef:XYmismatch. The lengths of X and Y must match.");
                        return 0;
                    }
                    x[ILMath.full,ILMath.end + 1] = y;
                }

                int n = x.Size[0];
                int m = x.Size[1];

                r = ILMath.cov(x.T).T;
                ILArray<double> d = ILMath.sqrt(ILMath.diag(r)); // sqrt first to avoid under/overflow
                d = ILMath.multiply(d, d.T);
                r = ILMath.divide(r, d);

                // Fix up possible round-off problems, while preserving NaN: put exact 1 on the
                // diagonal, and limit off-diag to [-1,1].
                ILArray<int> t = ILMath.find(ILMath.abs(r) > 1); 
                r[t] = ILMath.divide(r[t], ILMath.abs(r[t]));
                r[ILMath.r(1,m,ILMath.end)] = Sign(ILMath.diag(r));

                return r;
            }
        }
    }
}
