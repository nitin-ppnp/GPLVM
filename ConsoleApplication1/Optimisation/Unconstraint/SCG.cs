using GPLVM.GPLVM;
using GPLVM.Numerical;
using GPLVM.Backconstraint;
using GPLVM.Synthesis;
using GPLVM.Dynamics;

namespace GPLVM.Optimisation
{
    public static class SCG
    {
        #region Public Functions
        public static void Optimize(ref GP_LVM model, int maxIterations = 100, bool display = true)
        {
            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.Optimize(new GPLVMToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref StyleGPLVM2 model, int maxIterations = 100, bool display = true)
        {
            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.Optimize(new StyleGPLVMToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref BackProjection model, int maxIterations = 100, bool display = true)
        {
            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.Optimize(new BackProjectionToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref BackConstraintMLP model, int maxIterations = 100, bool display = true)
        {
            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.Optimize(new MLPToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref SynthesisOptimizerTestIK model, int maxIterations = 100, bool display = true)
        {
            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.Optimize(new SynthesisTestIKToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref GPAccelerationNode model, int maxIterations = 100, bool display = true)
        {
            SCGOptimizer optimizer = new SCGOptimizer();
            optimizer.Optimize(new GPAccelerationToFunctionWithGradientAdapter(model), maxIterations, display);
        }
        #endregion

        #region Private Functions

        #endregion
    }
}
