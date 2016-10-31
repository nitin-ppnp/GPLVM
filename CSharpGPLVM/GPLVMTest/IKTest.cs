using GPLVM;
using DataFormats;
using ILNumerics;
using GPLVM.Synthesis.Constraints;
using GPLVM.Synthesis;
using GPLVM.Optimisation;

namespace GPLVMTest
{
    public class IKTest
    {
        public void go()
        {
            int jointID = 22; // right hand
            ILArray<double> data = ILMath.empty();
            ILArray<double> position = ILMath.empty();
            ILArray<double> goal = ILMath.zeros(1, 3);
            Representation repType = Representation.exponential;
            ILCell positions = ILMath.cell();
            ILArray<double> position2 = ILMath.empty();

            goal[0] = -40;
            goal[1] = 130;
            goal[2] = 30;

            BVHData bvh = new BVHData(repType);
            data = bvh.LoadFile(@"..\..\..\..\Data\TPose.bvh");

            DataPostProcess pp = new DataPostProcess(bvh.skeleton, bvh.FrameTime);
            pp.JointGlobalPosition(jointID, data[0, ILMath.full], position, true, true, repType);
            pp.ForwardKinematics(data[0, ILMath.full], positions, true, true, repType);
            position2 = positions.GetArray<double>(jointID)[ILMath.r(0, 2), 3].T;

            ConstraintIKTest ik = new ConstraintIKTest(pp, jointID);
            ik.GoalPosition = goal.C;
            ik.Parameter = data[0, ILMath.full].C;

            SynthesisOptimizerTestIK opt = new SynthesisOptimizerTestIK(ik);

            SCG.Optimize(ref opt, 50);

            //OgreClient ogreClient = new OgreClient(bvh.skeleton, repType);
            //ogreClient.go(pp.getFullPose(ik.Parameter, repType));

            pp.JointGlobalPosition(jointID, data[0, ILMath.full], position, true, true, repType);
        }
    }
}
