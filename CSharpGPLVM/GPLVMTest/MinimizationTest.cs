
using GPLVM.Numerical;

namespace GPLVMTest
{
    public class MinimizationTest
    {
        public void go()
        {
            //using (ILScope.Enter())
            //{
                //var rosenbock = new Rosenbrock();
                //rosenbock.Parameters = ILMath.zeros(1, 2);

                //var optimizer = new SCGOptimizer();
                //optimizer.Optimize(rosenbock, 100, true);

                //Console.WriteLine("Min argument: " + rosenbock.Parameters.ToString());

                //rosenbock.Parameters = ILMath.zeros(1, 2);
                //var optimizer2 = new BFGSOptimizer();
                //optimizer2.Optimize(rosenbock, 100, true);

                //Console.WriteLine("Min argument: " + rosenbock.Parameters.ToString());

                var test = new NonlinearConstraintFunctionWithGradientTest();
                var optimizer = new AugLagOptimizer();

                optimizer.Optimize(test, 100, true);
            //}
        }
    }
}
