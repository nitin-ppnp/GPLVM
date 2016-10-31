using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        ///   Gets the Euclidean norm for a vector.
        /// </summary>
        /// 
        public static double SquareEuclidean(ILInArray<double> a)
        {
            /*double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
                sum += (double)(a[i] * a[i]);
            return sum;*/

            return (double)ILMath.sum(ILMath.multiplyElem(a,a));
        }
    }
}
