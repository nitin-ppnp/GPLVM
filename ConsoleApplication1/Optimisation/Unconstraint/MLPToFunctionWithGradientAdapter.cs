using GPLVM.Numerical;
using ILNumerics;
using GPLVM.Backconstraint;

namespace GPLVM.Optimisation
{
    public class MLPToFunctionWithGradientAdapter : IFunctionWithGradient
    {
        protected BackConstraintMLP _model;

        public MLPToFunctionWithGradientAdapter(BackConstraintMLP model)
        {
            _model = model;
        }

        public int NumParameters
        {
            get { return _model.NumParameter; }
        }

        public double Value()
        {
            return _model.MLPError();
        }

        public ILArray<double> Gradient()
        {
            return _model.BackPropagateGradient();
        }

        public ILArray<double> Parameters
        {
            get { return _model.LogParameter; }
            set { _model.LogParameter = value; }
        }
    }
}
