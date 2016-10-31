using GPLVM.GPLVM;
using GPLVM.Numerical;
using GPLVM.Backconstraint;
using GPLVM.Dynamics;

namespace GPLVM.Optimisation
{
    public static class CERCG
    {
        #region Public Functions
        public static void Optimize(ref GP_LVM model, int maxIterations = 100, bool display = true)
        {
            CERCGMinimize optimizer = new CERCGMinimize();
            optimizer.Optimize(new GPLVMToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref StyleGPLVM2 model, int maxIterations = 100, bool display = true)
        {
            CERCGMinimize optimizer = new CERCGMinimize();
            optimizer.Optimize(new StyleGPLVMToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref BackProjection model, int maxIterations = 100, bool display = true)
        {
            CERCGMinimize optimizer = new CERCGMinimize();
            optimizer.Optimize(new BackProjectionToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref BackConstraintMLP model, int maxIterations = 100, bool display = true)
        {
            CERCGMinimize optimizer = new CERCGMinimize();
            optimizer.Optimize(new MLPToFunctionWithGradientAdapter(model), maxIterations, display);
        }

        public static void Optimize(ref GPAccelerationNode model, int maxIterations = 100, bool display = true)
        {
            CERCGMinimize optimizer = new CERCGMinimize();
            optimizer.Optimize(new GPAccelerationToFunctionWithGradientAdapter(model), maxIterations, display);
        }
        #endregion

        #region Private Functions

        #endregion
    }
}
