using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        ///   Concatenates two arrays along the 1-st dinension.
        ///   ILNumerics can't combine an emty array with another one.
        ///   inX can be empty
        /// </summary>
        /// 
        public static ILRetArray<T> Concatenate<T>(ILInArray<T> inX, ILInArray<T> inY, int iDimension = 0)
        {
            using (ILScope.Enter(inX, inY))
            {
                ILArray<T> x = ILMath.check(inX);
                ILArray<T> y = ILMath.check(inY);
                ILArray<T> joint = ILMath.empty<T>();

                if (x.IsEmpty)
                {
                    joint = y.C;
                }
                else if (y.IsEmpty)
                {
                    joint = x.C;
                }
                else
                {
                    joint = x.C;
                    switch (iDimension)
                    {
                        case 0:
                            joint[ILMath.r(x.S[0], x.S[0] + y.S[0] - 1), ILMath.full] = y;
                            break;
                        case 1:
                            joint[ILMath.full, ILMath.r(x.S[1], x.S[1] + y.S[1] - 1)] = y;
                            break;
                    }
                }                

                return joint;
            }
        }
    }
}
