using GPLVM.GPLVM;
using GPLVM.Numerical;
using ILNumerics;

namespace GPLVM.Optimisation
{
    public class BackProjectionToFunctionWithGradientAdapter : IFunctionWithGradient
    {
        protected BackProjection _model;

        public BackProjectionToFunctionWithGradientAdapter(BackProjection model)
        {
            _model = model;
        }

        public int NumParameters
        {
            get { return _model.NumParameter; }
        }

        public double Value()
        {
            return -_model.LogLikelihood();
        }

        public ILArray<double> Gradient()
        {
            return -_model.LogLikGradient();
        }

        public ILArray<double> Parameters
        {
            get { return _model.LogParameter; }
            set { _model.LogParameter = value; }
        }
    }
}
