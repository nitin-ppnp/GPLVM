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
        public static int NumElements<T>(ILInArray<T> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<T> x = ILMath.check(inX);
                if (x.IsEmpty)
                {
                    return 0;
                }

                int nElements = 1;
                ILSize s = x.S;
                for (int i = 0; i < s.NumberOfElements; i++)
                {
                    nElements = nElements * s[i];
                }
                return nElements;
            }
        }
    }
}
