using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        ///   Indexed accumulator for vectors. Data samples are rows.
        /// </summary>
        /// 
        public static ILRetArray<double> IndexAccum(ILInArray<double> inX, ILInArray<int> inI)
        {
            using (ILScope.Enter(inX, inI))
            {
                ILArray<double> x = ILMath.check(inX);
                ILArray<int> i = ILMath.check<int>(inI);
                int m = (int)ILMath.max(i);
                ILArray<double> accum = ILMath.zeros(m+1, x.S[1]);

                int j = 0;
                foreach (int k in i)
                {
                    accum[k, ILMath.full] += x[j, ILMath.full];
                    j++;
                }

                return accum;
            }
        }
    }
}
