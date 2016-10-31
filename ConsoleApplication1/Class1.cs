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
using Models;

namespace ConsoleApplication1
{
    class test
    {
        private GP_LVM gp1;
        private GP_LVM gp2;
        private GP_LVM gp0;
        private GP_LVM gp3;
        private GP_LVM gp4;

        public void init()
        {
            gp1 = new GP_LVM(3, ApproximationType.ftc, BackConstType.none, XInit.pca);
            gp2 = new GP_LVM(3, ApproximationType.ftc, BackConstType.none, XInit.pca);
            gp0 = new GP_LVM(3, ApproximationType.ftc, BackConstType.none, XInit.pca);
            gp3 = new GP_LVM(3, ApproximationType.ftc, BackConstType.none, XInit.pca);
            gp4 = new GP_LVM(3, ApproximationType.ftc, BackConstType.none, XInit.pca);

            loadC3D_2(@"..\..\..\Data\NewHighFive\c3d_all.mat");
            loadC3D_4(@"..\..\..\Data\NewHighFive\c3d_all.mat");
            gp1.AddNode(gp2);
            gp3.AddNode(gp4);
            gp0.AddNode(gp1);
            gp0.AddNode(gp3);
            gp0.Prior = new GPAcceleration();
            
            gp0.Initialize();
     
        }


        public void learnmodel()
        {
            Console.WriteLine("optimizing...");
            SCG.Optimize(ref gp0, 30);
        }


        private void loadC3D_2(string files)
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
                data = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.r(0, 5)];

                /*tmp = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.full] * 0.1;
                root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
                root[ILMath.full, 2] = 0;
                data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
                gp2.AddData(data);
            }

            data = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.r(0, 5)];

            /*tmp = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.full] * 0.1;
            root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
            root[ILMath.full, 2] = 0;
            data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
            data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

            //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
            gp2.AddData(data);
        }

        private void loadC3D_4(string files)
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
                data = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.r(0, 5)];

                /*tmp = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.full] * 0.1;
                root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
                root[ILMath.full, 2] = 0;
                data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
                gp4.AddData(data);
            }

            data = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.r(0, 5)];

            /*tmp = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.full] * 0.1;
            root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
            root[ILMath.full, 2] = 0;
            data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
            data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

            //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
            gp4.AddData(data);
        }

        public void showPlots(int i)
        {
            //Form3 form_Pos1 = new Form3(subPoint.Y[ILMath.full, ILMath.r(0, 2)]);
            //form_Pos1.ShowDialog();
            //Form3 form_Pos2 = new Form3(subPoint.Y[ILMath.full, ILMath.r(3, 5)]);
            //form_Pos2.ShowDialog();
            Form1 form1;
            switch (i)
            {
                case 0:
                    form1 = new Form1(gp0.X[ILMath.full, ILMath.r(0, 2)], gp0.Segments);
                    form1.ShowDialog();
                    break;
                case 1:
                    form1 = new Form1(gp1.X[ILMath.full, ILMath.r(0, 2)], gp1.Segments);
                    form1.ShowDialog();
                    break;
                case 2:
                    form1 = new Form1(gp2.X[ILMath.full, ILMath.r(0, 2)], gp2.Segments);
                    form1.ShowDialog();
                    break;
                case 3:
                    form1 = new Form1(gp1.X[ILMath.full, ILMath.r(0, 2)], gp3.Segments);
                    form1.ShowDialog();
                    break;
                case 4:
                    form1 = new Form1(gp2.X[ILMath.full, ILMath.r(0, 2)], gp4.Segments);
                    form1.ShowDialog();
                    break;
            }
                



            

            /*FormTmp form1 = new FormTmp(subPoint.X, subPoint.Segments);
            form1.ShowDialog();

            //ILArray<double> _X = subAngle.X[ILMath.full, ILMath.r(0, 1)];
            //_X[ILMath.full, 2] = 0;
            ILArray<double> _X = subAngle.X;
            Form3 form2 = new Form3(_X);
            form2.ShowDialog();

            Form3 form3 = new Form3(interaction.X);
            form3.ShowDialog();*/
        }
    }
}
