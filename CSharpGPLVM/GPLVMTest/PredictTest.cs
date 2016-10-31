using System;

using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Dynamics;
using ILNumerics;
using DataFormats;
using GPLVM.Utils.Character;

namespace GPLVMTest
{
    public class PredictTest
    {
        private int jointID = 22; // right hand
        private GP_LVM gplvm;
        private int frame = 20;
        private Representation repType = Representation.exponential;
        private AvatarBVH avatar;

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

            OgreClient ogreClient = new OgreClient(bvh.skeleton, repType);
            //ogreClient.go(avatar.Synthesis(gplvm.X[frame, ILMath.full]));

            //avatar.Synthesis(gplvm.X[frame, ILMath.full]);

            DataPostProcess pp = new DataPostProcess(bvh.skeleton, bvh.FrameTime);
            //pp.JointGlobalPosition(jointID, avatar.PostMean, position, true, true, repType);

            //goal = position.C;
            //goal[0] = 0;

            //ConstraintIK constraints = new ConstraintIK(pp, jointID, repType, null);
            //constraints.GoalPosition = goal;
            //avatar.Constraints.Add(constraints);

            //avatar.Synthesis(gplvm.X[frame, ILMath.full]);
            //pp.JointGlobalPosition(jointID, avatar.Model.Ynew, position, true, true, repType);

            ILArray<double> xStar = gplvm.X[ILMath.r(0,1), ILMath.full].C;
            //xStar[0, ILMath.r(ILMath.end + 1, ILMath.end + gplvm.LatentDimension)] = gplvm.X[0, ILMath.full];
            ILArray<double> xTmp = ((GPAcceleration)gplvm.Prior).SimulateDynamics(xStar, 100);

            //Form1 form1 = new Form1(gplvm.X, xTmp, gplvm.Segments);
            //form1.ShowDialog();

            ogreClient.Connect();
            for (int i = 0; i < 100; i++)
            {
                var start = DateTime.Now;
                ogreClient.Stream(avatar.Synthesis(xTmp[i, ILMath.full], true));
                //ogreClient.Stream(avatar.PlayTrainingData());
                //System.Threading.Thread.Sleep(TimeSpan.FromSeconds(avatar.FrameTime - DateTime.Now.Subtract(start).Seconds));
            }

            //ogreClient.go(avatar.Synthesis(gplvm.X[frame, ILMath.full]));
        }
    }
}
