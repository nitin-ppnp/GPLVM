using ILNumerics;
using System.Linq;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Union of two arrays A and B. 
        /// </summary>
        /// <param name="inA">Array A.</param>
        /// <param name="inB">Array B.</param> 
        /// <returns>Union of A and B.</returns>
        public static ILRetArray<int> union(ILInArray<int> inA, ILInArray<int> inB)
        {
            using (ILScope.Enter(inA, inB))
            {
                ILArray<int> A = ILMath.check(inA);
                ILArray<int> B = ILMath.check(inB);


                var result = A.Union(B);

                ILArray<int> C = ILMath.zeros<int>(1, result.ToArray().Count());

                int cnt = 0;
                foreach (int value in result)
                    C[cnt++] = value;

                return ILMath.sort(C);
            }
        }
    }
}
