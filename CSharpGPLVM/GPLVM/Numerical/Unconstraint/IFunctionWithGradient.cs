using ILNumerics;

namespace GPLVM.Numerical
{
    public interface IFunctionWithGradient
    {
        int NumParameters
        {
            get;
        }
        double Value();
        ILArray<double> Gradient();
        ILArray<double> Parameters
        {
            get;
            set;
        }
    }
}
