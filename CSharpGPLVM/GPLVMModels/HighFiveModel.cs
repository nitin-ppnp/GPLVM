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
    public enum OptimizerType
    {
        SCGOptimizer,
        BFGSOptimizer,
        CERCGOptimizer
    }

    public class HighFiveModel
    {
        private int numIter = 50;
        private GP_LVM subPoint;
        private GP_LVM subAngle;
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
            subAngle = new GP_LVM(3, approxType, BackConstType.none, XInit.pca);
            subAngle.NumInducing = 200;

            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\angry"), "Emotions", "angry");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\happy"), "Emotions", "happy");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\neutral"), "Emotions", "neutral");
            loadBVH(Directory.GetFiles(@"..\..\..\..\Data\HighFiveTest\BVH\sad"), "Emotions", "sad");

            // interaction layer
            interaction = new GP_LVM(3, approxType, BackConstType.none, XInit.pca);
            interaction.NumInducing = 200;

            interaction.AddNode(subPoint);
            interaction.AddNode(subAngle);
            interaction.Prior = new GPAcceleration(_approxType);

            interaction.Initialize();
        }

        public void learnModel()
        {
            switch (optType)
            {
                case OptimizerType.SCGOptimizer:
                    SCG.Optimize(ref interaction, numIter);

                    backSub1 = new BackProjection(subPoint.M, subPoint.X, subPoint.Bias, subPoint.Scale, approxType);
                    backSub1.NumInducing = 200;
                    backSub1.Initialize();
                    SCG.Optimize(ref backSub1, numIter);

                    backInter = new BackProjection(subPoint.X, interaction.X, interaction.Bias[ILMath.r(0, subPoint.X.S[1] - 1)], interaction.Scale[ILMath.r(0, subPoint.X.S[1] - 1)], approxType);
                    backInter.NumInducing = 200;
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
                        interaction.Masking = Mask.kernel;
                        CERCG.Optimize(ref interaction, numIter / 3);
                        

                        interaction.Masking = Mask.latents;
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

            /*Form3 form2 = new Form3(subAngle.X);
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
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_interactionFTC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_backSub1FTC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_backInterFTC_SCG.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_interactionDTC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_backSub1DTC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_backInterDTC_SCG.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\" + prefix + "_interactionFITC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\" + prefix + "_backSub1FITC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\" + prefix + "_backInterFITC_SCG.xml");
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
        }

        private void loadBVH(string[] files, string style, string substyle)
        {
            //ILArray<double> data = ILMath.empty();

            //if (subAngle.Styles == null)
            //    subAngle.AddStyle(style, new Style(style));

            for (int i = 0; i < files.Length; i++)
            {
                BVHData bvh = new BVHData(_repType);
                //bvh.LoadFile(files[i]);

                //DataProcess.ProcessBVH(ref bvh, _repType);

                subAngle.AddData(bvh.LoadFile(files[i]));

                //subAngle.Styles[style].AddSubStyle(substyle, data.Size[0]);

                //if (subAngle.SkeletonTree == null)
                //    subAngle.SkeletonTree = DataProcess.CreateTree(ref bvh);

                bvh = null;
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
