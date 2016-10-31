using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Set difference of two sets of positive integers A and B. 
        /// C containing the values in A that are not in B.
        /// </summary>
        /// <param name="inA">Array A.</param>
        /// <param name="inB">Array B.</param> 
        /// <returns>Values in A that are not in B.</returns>
        public static ILRetArray<int> setdiff(ILInArray<int> inA, ILInArray<int> inB)
        {
            using (ILScope.Enter(inA, inB))
            {
                ILArray<int> A = ILMath.check(inA);
                ILArray<int> B = ILMath.check(inB);

                ILArray<int> C;

                if (A.IsEmpty)
                    return C = ILMath.empty<int>();

                else if (B.IsEmpty)
                    return A;

                else // both non-empty
                {
                    ILArray<int> bits = ILMath.zeros<int>(1, (int)ILMath.max(ILMath.max(A), ILMath.max(B)));
                    bits[A] = 1;
                    bits[B] = 0;
                    C = A[ILMath.tological(bits[A])];
                }

                return C;
            }
        }
    }
}
