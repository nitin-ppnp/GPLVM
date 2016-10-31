using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPLVM.Graph;
using GPLVM.Kernel;
using GPLVM.Dynamics;
using GPLVM.Optimisation;
using GPLVM.Prior;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using GPLVM.GPLVM;
using ILNumerics;
using System.Xml;
using DataFormats;
using GPLVM.Utils.Character;

namespace ConsoleApplication1
{
    class Program
    {


        static void Main(string[] args)
        {
            test test1 = new test();
            test1.init();

            //test1.showPlots(0);
            test1.showPlots(1);
            test1.showPlots(2);

            test1.learnmodel();

            //test1.showPlots(0);
            test1.showPlots(1);
            test1.showPlots(2);

            Console.ReadLine();
        }

    }
}
