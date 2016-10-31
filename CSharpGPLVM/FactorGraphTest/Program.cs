using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILNumerics;
using DataFormats;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Kernel;
using GPLVM.Numerical;
using GPLVM.Optimisation;
using GPLVM.Dynamics;
using FactorGraph.Core;
using FactorGraph.FactorNodes;

namespace FactorGraphTest
{
    partial class Program
    {
        static int nLatents = 2;
        static int nIterations = 20;
        static bool bLogEnabled = false;

        static bool bRunTestGPLVM = false;
        static bool bRunTestGPDM = true;
        static bool bRunTestSerialization = false;
        static bool bRunTestStyleGPLVM = false;
        static bool bRunTestBackConstraintsKBR = false;
        static bool bRunTestBackConstraintsPTC = false;
        static bool bRunTestDirectionKern = false;
        static bool bRunTestCERCEMinimize = false;
        static bool bRunTestPGEKern = false;

        static void Main(string[] args)
        {
            ILArray<double> data;
            ILArray<double> testInput;
            BVHData bvh;

            switch (2)
            {
                case 0:
                    bvh = new BVHData(Representation.exponential);
                    data = bvh.LoadFile(@"..\..\..\..\..\..\Data\Emotional Walks\Niko\BVH\NikoMapped_NeutralWalk02.bvh");
                    break;
                case 1:
                    bvh = new BVHData(Representation.radian);
                    data = bvh.LoadFile(@"..\..\..\..\Data\HandWave\HandWaveSegmented\Handwave_Angry.bvh");
                    break;
                case 2:
                    data = new double[,] { { 0, 0, 0 }, { 1, 0, 0 }, { 2, 0, 0 }, { 3, 3, 0 }, { 4, 4, 0 }, 
                                           { 5, 5, 1 }, { 6, 6, 1 }, { 7, 7, 1 }, { 8, 9, 1 }, { 9, 9, 1 },
                                           { 9, 9, 1 }, { 8, 9, 2 }, { 7, 7, 2 }, { 6, 6, 2 }, { 5, 5, 5 },
                                           { 4, 4, 6 }, { 3, 3, 7 }, { 2, 0, 8 }, { 1, 0, 9 }, { 0, 0, 10 },};
                    data = data.T;
                    break;
            }

            if (bRunTestSerialization)
            {
                nLatents = 1;
                testInput = new double[,] { { -1 }, { 0 }, { 1 } };
                testInput = testInput.T;
                TestFactorGraphGPLVMSerialization(data, testInput, ApproximationType.fitc);
                TestFactorGraphGPDMSerialization(data, ApproximationType.fitc);
            }

            if (bRunTestGPLVM)
            {
                testInput = new double[,] { { -1 }, { 0 }, { 1 } };
                testInput = testInput.T;
                nLatents = testInput.S[1];
                TestGPLVM(data, testInput, ApproximationType.ftc);
                TestGPLVM(data, testInput, ApproximationType.dtc, 5e-1);
                TestGPLVM(data, testInput, ApproximationType.fitc, 5e-1);
            }
            if (bRunTestGPDM)
            {
                nLatents = 3;
                TestGPDM(data, ApproximationType.ftc, 1e-3);
                TestGPDM(data, ApproximationType.dtc, 5e-1);
                TestGPDM(data, ApproximationType.fitc, 5e-1);
            }
            if (bRunTestBackConstraintsKBR)
            {
                nLatents = 1;
                TestBackConstraintKBR(data, ApproximationType.ftc);
                //TestGPDM(data, ApproximationType.dtc, 5e-1);
                //TestGPDM(data, ApproximationType.fitc, 5e-1);
            }
            if (bRunTestBackConstraintsPTC)
            {
                nLatents = 2;
                TestBackConstraintPTC(data, ApproximationType.ftc);
            }
            if (bRunTestStyleGPLVM)
            {
                var lTrials = new TrialDataWithStyleList();
                
                bvh = new BVHData(Representation.radian);
                data = bvh.LoadFile(@"..\..\..\..\Data\HighFiveDemoFTC\BVH\angry\nick_angry_IIa_3.bvh");
                lTrials.Add(new TrialDataWithStyle(data, "angry"));
                
                bvh = new BVHData(Representation.radian);
                data = bvh.LoadFile(@"..\..\..\..\Data\HighFiveDemoFTC\BVH\happy\nick_happy_IIa_2.bvh");
                lTrials.Add(new TrialDataWithStyle(data, "happy"));
                
                TestStyleGPLVM(lTrials, ApproximationType.ftc);
                TestStyleGPLVM(lTrials, ApproximationType.dtc);
                TestStyleGPLVM(lTrials, ApproximationType.fitc);
            }
            if (bRunTestDirectionKern)
            {
                data = new double[,] { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 }, 
                                           { 0, 2 }, { 1, 3 }, { 2, 4 }, { 3, 5 }, { 4, 6 } };
                data = data.T;
                TestDirectionKern(data);
            }
            if (bRunTestCERCEMinimize)
            {
                TestCERCGMinimize();
            }
            if (bRunTestPGEKern)
            {
                TestPGEKern();
            }

            Console.ReadKey();
        }
    }

    
}
