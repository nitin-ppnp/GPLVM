using System.Linq;
using ILNumerics;

namespace GPLVM.Numerical
{
    public class LinearSystem : IFunctionWithGradient
    {
        // The system is AX + B = 0;
        public ILArray<double> A;
        public ILArray<double> B;
        public ILArray<double> X;

        public int NumParameters
        {
            get { return B.Length; }
        }
        
        public double Value()
        {
            ILArray<double> residuals = ILMath.multiply(A, X) + B;
            return ILMath.multiply(residuals.T, residuals).ElementAt(0);
        }

        public ILArray<double> Gradient()
        {
            ILRetArray<double> res = 2 * ILMath.multiply(A.T, (ILMath.multiply(A, X) + B)).T;
            return res;
        }

        public ILArray<double> Parameters
        {
            get { return X.C; }
            set { X.a = value; }
        }                
    }
}
