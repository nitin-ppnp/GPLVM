using System.IO;

using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using ILNumerics;
using DataFormats;

namespace GPLVMTest
{
    public class EmbeddingPlots
    {
        
        private GP_LVM gplvm1;
        private GP_LVM gplvm2;
        private GP_LVM interaction;
        private Representation repType = Representation.exponential;
        private int numSteps = 500;

        private ApproximationType approxType = ApproximationType.ftc;
        private XInit initType = XInit.lle;

        public void go()
        {
            //gplvm = new GP_LVM(3, approxType, BackConstType.none, XInit.pca);
            //gplvm.Prior = new GPAcceleration();
            //gplvm.NumInducing = 50;

            gplvm1 = new GP_LVM(3, approxType, BackConstType.none, initType);
            gplvm2 = new GP_LVM(2, approxType, BackConstType.none, initType);
            interaction = new GP_LVM(3, approxType, BackConstType.none, initType);

            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\PlotTest"), "Emotions", "angry");
            //loadBVH(Directory.GetFiles(@"..\..\..\..\Data\Drawer\"), "Emotions", "angry");
            //loadC3D(@"..\..\..\..\Data\HighFiveTest\c3d_angry.mat");

            //interaction.AddNode(gplvm1);
            //interaction.AddNode(gplvm2);
            //interaction.Prior = new Gaussian();
            //interaction.Initialize();
            gplvm1.Initialize();
            //SCG.Optimize(ref interaction, 20);

            Form1 form1 = new Form1(gplvm1.X, gplvm1.Segments);
            form1.ShowDialog();
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

                data = bvh.LoadFile(folder[i])[ILMath.full, ILMath.r(3, ILMath.end)]; //DataProcess.ProcessBVH(ref bvh, _repType);
                vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(3, ILMath.end)]);
                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                gplvm1.AddData(data);
            }
        }

        private void loadC3D(string files)
        {
            ILArray<double> Y = ILMath.empty();
            ILArray<double> seg = ILMath.empty();
            using (ILMatFile matRead = new ILMatFile(files))
            {
                Y.a = matRead.GetArray<double>(0);
                seg.a = matRead.GetArray<double>(1);
            }

            ILArray<double> data = ILMath.empty();
            ILArray<double> tmp = ILMath.empty();
            ILArray<double> root = ILMath.empty();

            for (int i = 0; i < seg.Length - 1; i++)
            {
                data = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.r(6, 11)] * 0.1;

                /*tmp = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.full] * 0.1;
                root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
                root[ILMath.full, 2] = 0;
                data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
                gplvm2.AddData(data);
            }

            data = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.r(6, 11)] * 0.1;

            /*tmp = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.full] * 0.1;
            root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
            root[ILMath.full, 2] = 0;
            data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
            data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

            //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
            gplvm2.AddData(data);
        }
    }
}
