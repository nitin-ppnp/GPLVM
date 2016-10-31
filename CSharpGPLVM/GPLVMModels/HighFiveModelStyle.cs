using System.IO;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Prior;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using GPLVM.Styles;
using GPLVM.Optimisation;
using ILNumerics;
using DataFormats;

namespace Models
{
    public class HighFiveModelStyle
    {
        private int numIter = 20;
        private GP_LVM subPoint;
        private StyleGPLVM2 subAngle;
        //private GP_LVM subAngle;
        private GP_LVM interaction;
        private BackProjection backSub1;
        private BackProjection backInter;
        private ApproximationType approxType;
        private OptimizerType optType;

        private Representation _repType;

        public void init(ApproximationType _approxType, OptimizerType _optType, Representation repType)
        {
            approxType = _approxType;
            optType = _optType;
            _repType = repType;

            // Subject1 Markerset
            subPoint = new GP_LVM(2, approxType, BackConstType.none, XInit.pca);
            subPoint.NumInducing = 200;

            loadC3D(@"..\..\..\..\Data\HighFiveTest\c3d_all.mat");

            // Subject2 jointAngles
            subAngle = new StyleGPLVM2(3, _approxType, _repType);
            subAngle.NumInducing = 400;

            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\angry"), "Emotions", "angry");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\happy"), "Emotions", "happy");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\neutral"), "Emotions", "neutral");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\sad"), "Emotions", "sad");

            // interaction layer
            interaction = new GP_LVM(3, approxType, BackConstType.none, XInit.pca);
            interaction.NumInducing = 200;

            interaction.AddNode(subPoint);
            interaction.AddNode(subAngle);
            interaction.Prior = new Gaussian();

            interaction.Initialize();
        }

        public void learnModel()
        {
            switch (optType)
            {
                case OptimizerType.SCGOptimizer:
                    SCG.Optimize(ref interaction, numIter);

                    backSub1 = new BackProjection(subPoint.M, subPoint.X, subPoint.Bias, subPoint.Scale, approxType);
                    backSub1.NumInducing = 600;
                    backSub1.Initialize();
                    SCG.Optimize(ref backSub1, numIter);

                    backInter = new BackProjection(subPoint.X, interaction.X, interaction.Bias[ILMath.r(0, subPoint.X.S[1] - 1)], interaction.Scale[ILMath.r(0, subPoint.X.S[1] - 1)], approxType);
                    backInter.NumInducing = 600;
                    backInter.Initialize();
                    SCG.Optimize(ref backInter, numIter);
                    break;
                case OptimizerType.BFGSOptimizer:
                    BFGS.Optimize(ref interaction, numIter);

                    backSub1 = new BackProjection(subPoint.M, subPoint.X, subPoint.Bias, subPoint.Scale, approxType);
                    backSub1.NumInducing = 200;
                    backSub1.Initialize();
                    BFGS.Optimize(ref backSub1, numIter);

                    backInter = new BackProjection(subPoint.X, interaction.X, interaction.Bias[ILMath.r(0, subPoint.X.S[1] - 1)], interaction.Scale[ILMath.r(0, subPoint.X.S[1] - 1)], approxType);
                    backInter.NumInducing = 200;
                    backInter.Initialize();
                    BFGS.Optimize(ref backInter, numIter);
                    break;
                case OptimizerType.CERCGOptimizer:
                    for (int i = 0; i < 3; i++)
                    {
                        interaction.Masking = Mask.latents;
                        CERCG.Optimize(ref interaction, numIter / 3);

                        interaction.Masking = Mask.kernel;
                        CERCG.Optimize(ref interaction, numIter / 3);
                    }

                    interaction.Masking = Mask.full;
                    CERCG.Optimize(ref interaction, numIter / 3);

                    backSub1 = new BackProjection(subPoint.M, subPoint.X, subPoint.Bias, subPoint.Scale, approxType);
                    backSub1.NumInducing = 200;
                    backSub1.Initialize();
                    CERCG.Optimize(ref backSub1, numIter);

                    backInter = new BackProjection(subPoint.X, interaction.X, interaction.Bias[ILMath.r(0, subPoint.X.S[1] - 1)], interaction.Scale[ILMath.r(0, subPoint.X.S[1] - 1)], approxType);
                    backInter.NumInducing = 200;
                    backInter.Initialize();
                    CERCG.Optimize(ref backInter, numIter);
                    break;
            }
            
        }

        public void showPlots()
        {
            //Form3 form_Pos1 = new Form3(subPoint.Y[ILMath.full, ILMath.r(0, 2)]);
            //form_Pos1.ShowDialog();
            //Form3 form_Pos2 = new Form3(subPoint.Y[ILMath.full, ILMath.r(3, 5)]);
            //form_Pos2.ShowDialog();

            Form1 form1 = new Form1(subAngle.X, subAngle.Segments);
            form1.ShowDialog();

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

        public void SaveModel(string prefix)
        {
            XMLReadWrite writer = new XMLReadWrite();

            if (optType == OptimizerType.SCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_Style2_interactionFTC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backSub1FTC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backInterFTC_SCG.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_Style2_interactionDTC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backSub1DTC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backInterDTC_SCG.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_Style2_interactionFITC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backSub1FITC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backInterFITC_SCG.xml");
                        break;
                }
            if (optType == OptimizerType.BFGSOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\interactionFTC_BFGS.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\backSub1FTC_BFGS.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\backInterFTC_BFGS.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\interactionDTC_BFGS.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\backSub1DTC_BFGS.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\backInterDTC_BFGS.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\interactionFITC_BFGS.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\backSub1FITC_BFGS.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\backInterFITC_BFGS.xml");
                        break;
                }
            if (optType == OptimizerType.CERCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_Style2_interactionFTC_CERCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backSub1FTC_CERCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backInterFTC_CERCG.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_Style2_interactionDTC_CERCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backSub1DTC_CERCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backInterDTC_CERCG.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_Style2_interactionFITC_CERCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backSub1FITC_CERCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_Style2_backInterFITC_CERCG.xml");
                        break;
                }
        }

        private void loadBVH(string[] folder, string style, string substyle)
        {
            ILArray<double> data = ILMath.empty();
            ILArray<double> vel = ILMath.empty();
            string[] files;

            if (subAngle.Styles == null)
                subAngle.AddStyle(style, new Style(style));

            for (int i = 0; i < folder.Length; i++)
            {
                //if (i == 5)
                //{
                    //files = Directory.GetFiles(folder[i]);
                    //for (int j = 0; j < files.Length; j++)
                    //{
                        BVHData bvh = new BVHData(_repType);

                        data = bvh.LoadFile(folder[i]);

                        subAngle.AddData(data);
                        vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(6, ILMath.end)]);
                        data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                        subAngle.Styles[style].AddSubStyle(substyle, data.Size[0]);
                    //}
                //}
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
                subPoint.AddData(data);
            }

            data = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.r(6, 11)] * 0.1;

            /*tmp = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.full] * 0.1;
            root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
            root[ILMath.full, 2] = 0;
            data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
            data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

            //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
            subPoint.AddData(data);
        }
    }
}
