using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILNumerics;
using GraphicalModel;
using GraphicalModel.Factors;
using GraphicalModel.Kernels;

namespace GraphicalModelTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                GPLVMTest.Test(args);
                //GPDMTest.Test(args);
            }
            catch (GraphicalModelException e)
            {
                System.Console.WriteLine("Exception caught: " + e.ToString());
            }

            System.Console.WriteLine("Finished!");
            System.Console.ReadKey();
        }
    }
}
