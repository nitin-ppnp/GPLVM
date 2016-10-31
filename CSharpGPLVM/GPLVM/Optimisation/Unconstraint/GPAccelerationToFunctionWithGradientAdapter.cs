using GPLVM.Numerical;
using ILNumerics;
using GPLVM.Dynamics;

namespace GPLVM.Optimisation
{
    public class GPAccelerationToFunctionWithGradientAdapter: IFunctionWithGradient
    {
        protected GPAccelerationNode _model;

        public GPAccelerationToFunctionWithGradientAdapter(GPAccelerationNode model)
        {
            _model = model;
        }

        public int NumParameters
        {
            get { return _model.NumParameterInHierarchy; }
        }

        public double Value()
        {
            // As the SCG minimizes the function, negative log-likelihood is used
            return -_model.LogLikelihood();
        }

        public ILArray<double> Gradient()
        {
            // As the function value is negated, gradient should be negated too
            return -_model.LogLikGradient();
        }

        public ILArray<double> Parameters
        {
            get { return _model.LogParameter; }
            set { _model.LogParameter = value; }
        }

    }
}
