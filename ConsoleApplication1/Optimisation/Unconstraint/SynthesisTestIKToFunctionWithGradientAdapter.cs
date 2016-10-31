using GPLVM.Numerical;
using ILNumerics;
using GPLVM.Synthesis;

namespace GPLVM.Optimisation
{
    public class SynthesisTestIKToFunctionWithGradientAdapter : IFunctionWithGradient
    {
        protected SynthesisOptimizerTestIK _optimizer;

        public SynthesisTestIKToFunctionWithGradientAdapter(SynthesisOptimizerTestIK model)
        {
            _optimizer = model;
        }

        public int NumParameters
        {
            get { return _optimizer.NumParameter; }
        }

        public double Value()
        {
            // As the SCG minimizes the function, negative log-likelihood is used
            return -_optimizer.LogLikelihood();
        }

        public ILArray<double> Gradient()
        {
            // As the function value is negated, gradient should be negated too
            return -_optimizer.LogLikGradient();
        }

        public ILArray<double> Parameters
        {
            get { return _optimizer.LogPostParameter; }
            set { _optimizer.LogPostParameter = value; }
        }
    }
}
