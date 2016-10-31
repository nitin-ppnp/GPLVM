using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ILNumerics;
using DataFormats;
using GPLVM;
using GPLVM.Dynamics.Topology;
using MotionPrimitives;

namespace MotionPrimitivesApplication
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.Run(new FormMain());
        }
    }
}
