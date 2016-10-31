using ILNumerics;

//% rosenbrock.m This function returns the function value, partial derivatives
//% and Hessian of the (general dimension) rosenbrock function, given by:
//%
//%       f(x) = sum_{i=1:D-1} 100*(x(i+1) - x(i)^2)^2 + (1-x(i))^2 
//%
//% where D is the dimension of x. The true minimum is 0 at x = (1 1 ... 1).
//%
//% Carl Edward Rasmussen, 2001-07-21.
//D = length(x);
//f = sum(100*(x(2:D)-x(1:D-1).^2).^2 + (1-x(1:D-1)).^2);

//if nargout > 1
//  df = zeros(D, 1);
//  df(1:D-1) = - 400*x(1:D-1).*(x(2:D)-x(1:D-1).^2) - 2*(1-x(1:D-1));
//  df(2:D) = df(2:D) + 200*(x(2:D)-x(1:D-1).^2);
//end

//if nargout > 2
//  ddf = zeros(D,D);
//  ddf(1:D-1,1:D-1) = diag(-400*x(2:D) + 1200*x(1:D-1).^2 + 2);
//  ddf(2:D,2:D) = ddf(2:D,2:D) + 200*eye(D-1);
//  ddf = ddf - diag(400*x(1:D-1),1) - diag(400*x(1:D-1),-1);
//end

namespace GPLVM.Numerical
{
    public class Rosenbrock : IFunctionWithGradient
    {
        public ILArray<double> X = ILMath.localMember<double>();

        public Rosenbrock()
        {
        }

        public int NumParameters
        {
            get { return X.Length; }
        }

        public double Value()
        {
            int D = X.Length;
            double f = (double)ILMath.sum(100 * ILMath.pow(X[ILMath.r(1, D - 1)] - ILMath.pow(X[ILMath.r(0, D - 2)], 2), 2) + ILMath.pow(1 - X[ILMath.r(0, D - 2)], 2));
            return f;
        }

        public ILArray<double> Gradient()
        {
            int D = X.Length;
            ILArray<double> df = ILMath.zeros(D, 1);
            df[ILMath.r(0, D-2)] = - 400 * X[ILMath.r(0, D-2)] * (X[ILMath.r(1, D - 1)] - ILMath.pow(X[ILMath.r(0, D - 2)], 2)) - 2 * (1 - X[ILMath.r(0, D-2)]);
            df[ILMath.r(1, D-1)] = df[ILMath.r(1, D-1)] + 200 * (X[ILMath.r(1, D - 1)] - ILMath.pow(X[ILMath.r(0, D - 2)], 2));
            return df.T;
        }

        public ILArray<double> Parameters
        {
            get { return X.C; }
            set { X.a = value; }
        }
    }
}
