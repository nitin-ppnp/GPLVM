using System;

using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Dynamics;
using ILNumerics;
using DataFormats;
using GPLVM.Utils.Character;
using GPLVM.DebugTools;

namespace GPLVMTest
{
    public class FootSkateTest
    {
        private int jointID = 22; // right hand
        private GP_LVM gplvm;
        private int frame = 20;
        private Representation repType = Representation.exponential;
        private AvatarBVH avatar;
        private int numSteps = 30;

        public void go()
        {
            XMLReadWrite reader = new XMLReadWrite();
            ILArray<double> position = ILMath.empty();
            ILArray<double> goal = ILMath.empty();

            reader.read(@"..\..\..\..\Data\XML\TestHighFive.xml", ref gplvm);
            BVHData bvh = new BVHData(repType);
            bvh.LoadFile(@"..\..\..\..\Data\HighFiveTest\BVH\angry\nick_angry_Ia_1.bvh");

            avatar = new AvatarBVH(repType);
            avatar.init(gplvm, bvh);

            DataPostProcess pp = new DataPostProcess(bvh.skeleton, bvh.FrameTime);
            //pp.JointGlobalPosition(jointID, avatar.Model.Ynew, position, true, true, repType);

            ILArray<double> xStar = gplvm.X[ILMath.r(0,1), ILMath.full].C;
            ILArray<double> xTmp = ((GPAcceleration)gplvm.Prior).SimulateDynamics(xStar, numSteps);
            ILArray<double> finalPose = ILMath.empty();

            //Form1 form1 = new Form1(gplvm.X, xTmp, gplvm.Segments);
            //form1.ShowDialog();

            avatar.IsConstraint = true;
            finalPose = avatar.SynthesisTest(xTmp[0, ILMath.full], false);
            FileLogger fl = new FileLogger(@"..\..\..\..\Data\log.log");
            for (int i = 1; i < numSteps; i++)
            {
                System.Console.WriteLine("\nIteration {0}: ", i);
                finalPose[ILMath.end + 1, ILMath.full] = avatar.SynthesisTest(xTmp[i, ILMath.full], false);
                fl.WriteDelimiter('#');
                pp.JointGlobalPosition(4, avatar.Model.Ynew, position, true, true, repType);
                fl.Write("LeftFoot", position);
                pp.JointGlobalPosition(8, avatar.Model.Ynew, position, true, true, repType);
                fl.Write("RightFoot", position);
                fl.WriteDelimiter('\n');
            }
            fl.Close();

            //using (ILMatFile matW = new ILMatFile())
            //{
            //    matW.AddArray(finalPose);
            //    matW.Write(@"..\..\..\..\Data\HighFiveTest\finalPose.mat");
            //}

            Frame frame = new Frame();

            OgreClient ogreClient = new OgreClient(bvh.skeleton, repType);
            ogreClient.Connect();
            for (int i = 0; i < numSteps; i++)
            {
                var start = DateTime.Now;
                frame.CreateStreamFrame(finalPose[i, ILMath.full], bvh.skeleton, repType);
                frame.PID = 3;
                ogreClient.Stream(frame);
                //ogreClient.Stream(avatar.PlayTrainingData());
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(avatar.FrameTime - DateTime.Now.Subtract(start).Seconds));
            }
        }
    }
}
