using System.IO;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Dynamics;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using GPLVM.Optimisation;
using ILNumerics;
using DataFormats;

namespace Models
{
    public class TestGPLVM
    {
        private int numIter = 100;
        private GP_LVM gplvm;
        private StyleGPLVM2 sgplvm;
        private StyleGPLVM loadGPLVM;

        private Representation _repType = Representation.exponential;
        private ApproximationType approxType = ApproximationType.ftc;

        public void init()
        {
            gplvm = new GP_LVM(2, approxType, BackConstType.ptc, XInit.pca);
            gplvm.Prior = new GPAcceleration();
            gplvm.NumInducing = 50;

            //sgplvm = new StyleGPLVM2(3, approxType, _repType);
            //sgplvm.Prior = new Gaussian();
            //sgplvm.NumInducing = 50;

            //loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\PlotTest"), "Emotions", "angry");
            /*loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\happy"), "Emotions", "happy");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\neutral"), "Emotions", "neutral");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\sad"), "Emotions", "sad");*/

            //gplvm.Initialize();
            loadC3D(Directory.GetFiles(@"..\..\..\..\Data\NewHighFive\C3D\"));
            //loadC3D(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\C3D\"));
            //loadC3D(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\C3D\"));
            //loadC3D(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\C3D\"));

            using (ILMatFile matW = new ILMatFile())
            {
                matW.AddArray(gplvm.Y);
                matW.AddArray(gplvm.Segments);
                matW.Write(@"..\..\..\..\Data\NewHighFive\c3d_all2.mat");
            }

            //gplvm.Prior = new Gaussian();

            //gplvm.Initialize();

            /*sgplvm = new StyleGPLVM(3, ApproximationType.dtc);
            sgplvm.NumInducing = 200;

            loadBVH(Directory.GetDirectories(@"..\..\..\..\Data\HighFiveTest\C3D\"), "Emotions", "angry");
            loadBVH(Directory.GetDirectories(@"..\..\..\..\Data\HighFive\BVH\happy"), "Emotions", "happy");
            /*loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\neutral"), "Emotions", "neutral");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\sad"), "Emotions", "sad");*/

            /*sgplvm.Prior = new Gaussian();

            sgplvm.Initialize();*/

            /*using (ILMatFile matW = new ILMatFile())
            {
                matW.AddArray(sgplvm.X);
                matW.AddArray(sgplvm.Segments);
                matW.AddArray(sgplvm.Styles["Emotions"].FactorIndex);
                matW.Write(@"..\..\..\..\Data\subAngleBefore.mat");
            }*/
            //showPlots();
        }

        public void learnModel()
        {
            SCG.Optimize(ref gplvm, numIter);
        }

        public void showPlots()
        {
            Form1 form1 = new Form1(gplvm.X, gplvm.Segments);
            form1.ShowDialog();
        }

        public void SaveModel()
        {
            XMLReadWrite writer = new XMLReadWrite();

            writer.write(ref gplvm, @"..\..\..\..\Data\XML\TestHighFive.xml");
        }

        public void LoadModel()
        {
            XMLReadWrite reader = new XMLReadWrite();

            reader.read(@"C:\Users\HmetalT\Desktop\test.xml", ref loadGPLVM);
        }

        private void loadBVH(string[] folder, string style, string substyle)
        {
            ILArray<double> data = ILMath.empty();
            ILArray<double> vel = ILMath.empty();
            string[] files;

            //if (sgplvm.Styles == null)
            //    sgplvm.AddStyle(style, new Style(style));

            //for (int i = 5; i < 8; i++)//folder.Length; i++)
            for (int i = 0; i < folder.Length; i++)
            {
                //if (i == 5)
                //{
                //files = Directory.GetFiles(folder[i]);
                //for (int j = 0; j < files.Length; j++)
                //{
                BVHData bvh = new BVHData(_repType);
                //bvh.LoadFile(folder[i]);

                data = bvh.LoadFile(folder[i]); //DataProcess.ProcessBVH(ref bvh, _repType);
                vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(6, ILMath.end)]);
                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                gplvm.AddData(data);

                //sgplvm.Styles[style].AddSubStyle(substyle, data.Size[0]);
                //}
                //}
            }
        }

        private void loadC3D(string[] files)
        {
            ILArray<double> tmp;
            ILArray<double> data;
            ILArray<double> root;
            for (int i = 0; i < files.Length; i++)
            {
                /*tmp = DataProcess.ProcessC3D(files[i]);
                root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
                data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;
                gplvm.AddData(data);*/

                /*tmp = DataProcess.ProcessC3D(files[i]);
                data = tmp[ILMath.full, ILMath.r(6, 11)];
                gplvm.AddData(data);*/

                /*tmp = DataProcess.ProcessC3D(files[i]);
                root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
                data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;
                gplvm.AddData(data);*/

                gplvm.AddData(DataProcess.ProcessC3D(files[i]));
            }
        }
    }
}
