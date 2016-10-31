namespace GPLVM.Numerical
{
    public interface IFunctionWithGradientConstraintOptimizer
    {
        void Optimize(IFunctionWithGradientConstraint model, int maxIterations, bool display);
    }
}
