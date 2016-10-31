using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Alias to atox
        /// </summary>
        /// <param name="inX"></param>
        /// <returns></returns>
        public static ILRetArray<double> exp(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);
                return atox(X);
            }
        }
   
        /// <summary>
        /// Log to real computing. 
        /// </summary>
        /// <param name="inX">The log matrix.</param>
        public static ILRetArray<double> atox(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);

                double limVal = 36;
                ILArray<double> y = ILMath.zeros(x.Size);
                ILArray<int> index = ILMath.empty<int>();
                index.a = ILMath.find(x < -limVal);
                y[index] = ILMath.eps;
                x[index] = double.NaN;
                index.a = ILMath.find(x < limVal);
                y[index] = ILMath.exp(x[index]);
                x[index] = double.NaN;
                index.a = ILMath.find(!ILMath.isnan(x));
                if (!ILMath.isempty(index))
                    y[index] = ILMath.exp(limVal);
                return y;
            }
        }
    }
}
