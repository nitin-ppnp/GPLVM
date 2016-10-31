using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Kernel;
using GPLVM.Numerical;
using GPLVM.Optimisation;
using GPLVM.Dynamics;
using FactorGraph.Core;
using FactorGraph.FactorNodes;
using FactorGraph.DataNodes;

namespace FactorGraphTest
{
    partial class Program
    {
        static void TestCERCGMinimize()
        {
            using (ILScope.Enter())
            {
                var rosenbock = new Rosenbrock();
                rosenbock.Parameters = ILMath.zeros(1, 2);

                var optimizer = new CERCGMinimize();
                optimizer.sLogFileName = "Rosenbock_Orig.log";
                optimizer.bLogEnabled = bLogEnabled;
                optimizer.Optimize(rosenbock, nIterations, true);

                Console.WriteLine("Min argument: " + rosenbock.Parameters.ToString());

            }
        }
    }
}
