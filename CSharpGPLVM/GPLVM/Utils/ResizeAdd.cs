using System;
using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        ///   Resize input arrays to max size and add.
        ///   ILNumerics throws an exception when input arrays have different size.
        /// </summary>
        /// 
        public static ILRetArray<double> ResizeAdd(ILInArray<double> inX, ILInArray<double> inY)
        {
            using (ILScope.Enter(inX, inY))
            {
                ILArray<double> x = ILMath.check(inX);
                ILArray<double> y = ILMath.check(inY);                
                int s0 = Math.Max(x.S[0], y.S[0]);
                int s1 = Math.Max(x.S[1], y.S[1]);
                ILArray<double> sum = ILMath.zeros(s0, s1);
                sum[ILMath.r(0, x.S[0]-1), ILMath.r(0, x.S[1]-1)] = x;
                sum[ILMath.r(0, y.S[0]-1), ILMath.r(0, y.S[1]-1)] += y;
                return sum;
            }
        }
    }
}
