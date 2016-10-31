using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Computes Moor-Penrose pseudoinverse. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="inA"></param>
        public static ILRetArray<double> pinverse(ILInArray<double> inA)
        {
            using (ILScope.Enter(inA))
            {
                ILArray<double> A = ILMath.check(inA);

                return ILMath.pinv(A);
            }
        }
    }
}
