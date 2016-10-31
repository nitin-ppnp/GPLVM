namespace GPLVM.Numerical
{
    public interface IFunctionWithGradientOptimizer
    {
        void Optimize(IFunctionWithGradient model, int maxIterations, bool display);
    }
}
