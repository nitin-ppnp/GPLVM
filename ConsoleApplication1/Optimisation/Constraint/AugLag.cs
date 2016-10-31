using System.Collections.Generic;
using GPLVM.GPLVM;
using GPLVM.Numerical;
using GPLVM.Synthesis;

namespace GPLVM.Optimisation
{
    public static class AugLag
    {
        #region Public Functions
        public static void Optimize(ref GP_LVM model, IEnumerable<INonlinearConstraint> constraints, int maxIterations = 100, bool display = true)
        {
            AugLagOptimizer optimizer = new AugLagOptimizer();
            optimizer.Optimize(new SynthesisToFunctionWithGradientAdapter(model, constraints), maxIterations, display);
        }

        public static void Optimize(SynthesisOptimizer model, int maxIterations = 100, bool display = true)
        {
            AugLagOptimizer optimizer = new AugLagOptimizer();
            optimizer.Optimize(model, maxIterations, display);
        }
        #endregion
    }
}
