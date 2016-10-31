using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        ///   Gets the Euclidean norm for a vector.
        /// </summary>
        /// 
        public static double Euclidean(ILInArray<double> a)
        {
            return (double)ILMath.sqrt(SquareEuclidean(a));
        }

        public static ILArray<double> EuclideanArray(ILInArray<double> a)
        {
            ILArray<double> b = ILMath.zeros(a.S[0], 1);
            for (int i = 0; i < a.S[0]; i++)
                b[i] = ILMath.sqrt(SquareEuclidean(a[i, ILMath.full]));
            return b;
        }
    }
}
