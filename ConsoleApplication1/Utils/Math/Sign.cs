using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// For each element of X, SIGN(X) returns 1 if the element is 
        /// greater than zero, 0 if it equals zero and -1 if it is less 
        /// than zero.
        /// </summary>
        public static ILRetArray<double> Sign(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);

                for (int i = 0; i < x.Size[0]; i++)
                    for (int j = 0; j < x.Size[1]; j++)
                    {
                        if (x[i, j] < 0) x[i, j] = -1;
                        else if (x[i, j] > 0) x[i, j] = 1;
                        else if (ILMath.isnan(x[i, j])) x[i, j] = 1;

                    }

                return x;
            }
        }
    }
}
