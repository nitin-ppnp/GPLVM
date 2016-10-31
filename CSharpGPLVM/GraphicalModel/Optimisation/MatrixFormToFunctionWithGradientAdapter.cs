using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM.GPLVM;
using GPLVM.Numerical;
using GraphicalModel;

namespace GraphicalModel.Optimisation
{
    public class MatrixFormToFunctionWithGradientAdapter : IFunctionWithGradient
    {
        protected MatrixForm pMatrixForm;

        public MatrixFormToFunctionWithGradientAdapter(MatrixForm matrixForm)
        {
            pMatrixForm = matrixForm;
        }

        public int NumParameters
        {
            get { return pMatrixForm.NumParameters(); }
        }

        public double Value()
        {
            // As the SCG minimizes the function, negative log-likelihood is used
            return -pMatrixForm.FunctionValue();
        }

        public ILArray<double> Gradient()
        {
            // As the function value is negated, gradient should be negated too
            return -pMatrixForm.FunctionGradient().T;
        }

        public ILArray<double> Parameters
        {
            get { return pMatrixForm.Parameters; }
            set { pMatrixForm.Parameters = value; }
        }

    }
}
