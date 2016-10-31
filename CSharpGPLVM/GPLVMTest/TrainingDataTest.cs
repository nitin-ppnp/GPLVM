using System;
using System.IO;

using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Dynamics;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using ILNumerics;
using DataFormats;
using GPLVM.Utils.Character;

namespace GPLVMTest
{
    public class TrainingDataTest
    {
        private int jointID = 22; // right hand
        private GP_LVM gplvm;
        private int frame = 20;
        private Representation repType = Representation.exponential;
        private AvatarBVH avatar;
        private int numSteps = 500;

        private ApproximationType approxType = ApproximationType.ftc;

        public void go()
        {
            gplvm = new GP_LVM(3, approxType, BackConstType.none, XInit.pca);
            gplvm.Prior = new GPAcceleration();
            gplvm.NumInducing = 50;

            BVHData bvh = new BVHData(repType);
            bvh.LoadFile(@"..\..\..\..\Data\TippingTurning\Idle\AnCh\Anger\AnChi_ANGER_Idle.bvh");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\AnCh\Anger"), "Emotions", "angry");

            gplvm.Initialize();

            avatar = new AvatarBVH(repType);
            avatar.init(gplvm, bvh);

            OgreClient ogreClient = new OgreClient(avatar.skeleton, repType);
            ogreClient.Connect();
            for (; ; )//for (int i = 0; i < gplvm.Y.S[0]; i++)
            {
                var start = DateTime.Now;
                ogreClient.Stream(avatar.PlayTrainingData(true));
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(avatar.FrameTime - DateTime.Now.Subtract(start).Seconds));
            }
        }

        private void loadBVH(string[] folder, string style, string substyle)
        {
            ILArray<double> data = ILMath.empty();
            ILArray<double> vel = ILMath.empty();
            string[] files;

            //if (sgplvm.Styles == null)
            //    sgplvm.AddStyle(style, new Style(style));

            for (int i = 0; i < folder.Length; i++)
            {
                BVHData bvh = new BVHData(repType);

                data = bvh.LoadFile(folder[i]); //DataProcess.ProcessBVH(ref bvh, _repType);
                vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(6, ILMath.end)]);
                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                gplvm.AddData(data);
            }
        }
    }
}
