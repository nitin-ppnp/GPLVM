
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Embeddings;
using ILNumerics;
using DataFormats;

namespace GPLVMTest
{
    public class TrajectoryPlots
    {
        
        private GP_LVM gplvm1;
        private GP_LVM gplvm2;
        private GP_LVM interaction;
        private Representation repType = Representation.radian;
        private int numSteps = 500;

        private ApproximationType approxType = ApproximationType.ftc;
        private XInit initType = XInit.lle;

        public void go()
        {

            BVHData bvh = new BVHData(repType);

            ILArray<double> data = bvh.LoadFile(@"..\..\..\..\Data\TippingTurning\Turning\AnCh\Anger\AnChi_ANGER_Turn.bvh")[ILMath.full, ILMath.r(3, ILMath.end)];

            Form2 form1 = new Form2(data);
            form1.ShowDialog();
        }
    }
}
