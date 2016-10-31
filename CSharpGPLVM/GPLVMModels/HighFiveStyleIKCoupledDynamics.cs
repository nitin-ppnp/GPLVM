using System.IO;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Prior;
using GPLVM.Dynamics;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using GPLVM.Styles;
using GPLVM.Optimisation;
using ILNumerics;
using DataFormats;
using GPLVM.Utils.Character;

namespace Models
{
    public class HighFiveStyleIKCoupledDynamics : IModel
    {
        private int numIter = 30;
        private GP_LVM subPoint;
        private StyleGPLVM2 subAngle;
        private GP_LVM kinematic;

        private GPAccelerationNode interaction;
        private BackProjection backSub1;
        private BackProjection backInter;

        private ApproximationType approxType = ApproximationType.ftc;
        private OptimizerType optType = OptimizerType.SCGOptimizer;
        private Representation _repType = Representation.exponential;

        private DataPostProcess pp;
        private Skeleton _skeleton;
        private double frameTime;

        private ILArray<double> styleVariable = ILMath.localMember<double>();

        private AvatarBVH avatar;

        private int jointID = 22; // Right Hand ID

        private string prefix = "01";

        private bool bUseRelativeRoot = false;

        public DataPostProcess PostProcess
        {
            get { return pp; }
        }

        public Skeleton skeleton
        {
            get { return _skeleton; }
        }

        public double FrameTime
        {
            get { return frameTime; }
        }

        public Frame PredictData(ILInArray<double> inTestInputs, ILInArray<double> inStyleValue)
        {
            using (ILScope.Enter(inTestInputs, inStyleValue))
            {
                ILArray<double> XStar = ILMath.check(inTestInputs);
                ILArray<double> styleValue = ILMath.check(inStyleValue);

                ILArray<double> Ytmp = ILMath.empty();

                ILArray<double> backSub = ILMath.empty();
                ILArray<double> backinter = ILMath.empty();

                ILArray<double> tmpStyle = ILMath.zeros(styleVariable.S);

                backSub = backSub1.PredictData(XStar);
                backinter = backInter.PredictData(backSub);
                Ytmp = interaction.PredictData(backinter);

                Ytmp = Ytmp[ILMath.full, ILMath.r(((IGPLVM)interaction.Nodes[0]).LatentDimension, ILMath.end)];

                tmpStyle[0, ILMath.full] = styleVariable[0, ILMath.full] * styleValue[0];
                tmpStyle[1, ILMath.full] = styleVariable[1, ILMath.full] * styleValue[1];
                tmpStyle[2, ILMath.full] = styleVariable[2, ILMath.full] * styleValue[2];
                tmpStyle[3, ILMath.full] = styleVariable[3, ILMath.full] * styleValue[3];

                tmpStyle = ILMath.sum(tmpStyle, 0);

                Ytmp[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + tmpStyle.S[1])] = tmpStyle;

                return avatar.Synthesis(Ytmp, bUseRelativeRoot);
            }
        }

        public Frame PlayTrainingData()
        {
            return avatar.PlayTrainingData(true);
        }

        public void Reset()
        {
            avatar.Reset();
        }

        public bool init(bool isLoad)
        {
            if (isLoad)
            {
                LoadModel();

                subPoint = (GP_LVM)interaction.Nodes[0];

                subAngle = (StyleGPLVM2)interaction.Nodes[1];
                styleVariable.a = subAngle.Styles["Emotions"].StyleVariable;

                subAngle.SkeletonTree = _skeleton;

                avatar = new AvatarBVH(_repType);
                avatar.init(subAngle, pp);
            }
            else
            {
                // Subject1 Markerset
                subPoint = new GP_LVM(2, approxType, BackConstType.none, XInit.pca);
                subPoint.NumInducing = 200;

                loadC3D(@"..\..\..\..\Data\NewHighFive\c3d_all.mat");

                // Subject2 jointAngles
                subAngle = new StyleGPLVM2(3, approxType, _repType, BackConstType.ptc);
                subAngle.NumInducing = 400;

                // Targets for the hand
                kinematic = new GP_LVM(subPoint.Y.S[1], approxType);

                loadBVH(Directory.GetFiles(@"..\..\..\..\Data\NewHighFive\BVH\angry"), "Emotions", "angry");
                loadBVH(Directory.GetFiles(@"..\..\..\..\Data\NewHighFive\BVH\happy"), "Emotions", "happy");
                loadBVH(Directory.GetFiles(@"..\..\..\..\Data\NewHighFive\BVH\neutral"), "Emotions", "neutral");
                loadBVH(Directory.GetFiles(@"..\..\..\..\Data\NewHighFive\BVH\sad"), "Emotions", "sad");

                kinematic.X = subPoint.Y.C;
                kinematic.Prior = new Gaussian();
                kinematic.NumInducing = 200;
                kinematic.Masking = Mask.kernel;
                kinematic.Initialize();

                // interaction layer
                interaction = new GPAccelerationNode(approxType);
                interaction.NumInducing = 200;

                interaction.AddNode(subPoint);
                interaction.AddNode(subAngle);
                //interaction.Prior = new GPAcceleration();

                interaction.Initialize();
            }

            return true;
        }

        public void learnModel()
        {
            switch (optType)
            {
                case OptimizerType.SCGOptimizer:
                    System.Console.WriteLine("\nOptimizing Interaction Model");
                    SCG.Optimize(ref interaction, numIter);

                    System.Console.WriteLine("\nOptimizing BackProjection 1");
                    backSub1 = new BackProjection(subPoint.M, subPoint.X, subPoint.Bias, subPoint.Scale, approxType);
                    backSub1.NumInducing = 200;
                    backSub1.Initialize();
                    SCG.Optimize(ref backSub1, numIter);

                    System.Console.WriteLine("\nOptimizing BackProjection 2");
                    backInter = new BackProjection(subPoint.X, interaction.X, interaction.Bias[ILMath.r(0, subPoint.X.S[1] - 1)], interaction.Scale[ILMath.r(0, subPoint.X.S[1] - 1)], approxType);
                    backInter.NumInducing = 200;
                    backInter.Initialize();
                    SCG.Optimize(ref backInter, numIter);

                    System.Console.WriteLine("\nOptimizing Goal Predictions");
                    SCG.Optimize(ref kinematic, numIter);
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

                    BFGS.Optimize(ref kinematic, numIter);
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

                    CERCG.Optimize(ref kinematic, numIter);
                    break;
            }

        }

        public void showPlots()
        {
            //Form3 form_Pos1 = new Form3(subPoint.Y[ILMath.full, ILMath.r(0, 2)]);
            //form_Pos1.ShowDialog();
            //Form3 form_Pos2 = new Form3(subPoint.Y[ILMath.full, ILMath.r(3, 5)]);
            //form_Pos2.ShowDialog();

            Form1 form1 = new Form1(subAngle.X[ILMath.full, ILMath.r(0, 2)], subAngle.Segments);
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

        public void SaveModel()
        {
            XMLReadWrite writer = new XMLReadWrite();

            if (optType == OptimizerType.SCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFTC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FTC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFTC_SCG.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFTC_SCG.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionDTC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1DTC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterDTC_SCG.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicDTC_SCG.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFITC_SCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FITC_SCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFITC_SCG.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFITC_SCG.xml");
                        break;
                }
            if (optType == OptimizerType.BFGSOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_interactionFTC_BFGS.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backSub1FTC_BFGS.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backInterFTC_BFGS.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFTC_BFGS.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_interactionDTC_BFGS.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backSub1DTC_BFGS.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backInterDTC_BFGS.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicDTC_BFGS.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_interactionFITC_BFGS.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backSub1FITC_BFGS.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backInterFITC_BFGS.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFITC_BFGS.xml");
                        break;
                }
            if (optType == OptimizerType.CERCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFTC_CERCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FTC_CERCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFTC_CERCG.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFTC_CERCG.xml");
                        break;

                    case ApproximationType.dtc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionDTC_CERCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1DTC_CERCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterDTC_CERCG.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicDTC_CERCG.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFITC_CERCG.xml");
                        writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FITC_CERCG.xml");
                        writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFITC_CERCG.xml");
                        writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFITC_CERCG.xml");
                        break;
                }
        }

        private void LoadModel()
        {
            XMLReadWrite reader = new XMLReadWrite();

            if (optType == OptimizerType.SCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFTC_SCG.xml", ref interaction);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FTC_SCG.xml" , ref backSub1);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFTC_SCG.xml", ref backInter);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFTC_SCG.xml", ref kinematic);
                        break;

                    case ApproximationType.dtc:
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionDTC_SCG.xml", ref interaction);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1DTC_SCG.xml"   , ref backSub1);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterDTC_SCG.xml"  , ref backInter);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicDTC_SCG.xml"  , ref kinematic);
                        break;

                    case ApproximationType.fitc:
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFITC_SCG.xml", ref interaction);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FITC_SCG.xml"   , ref backSub1);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFITC_SCG.xml"  , ref backInter);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFITC_SCG.xml"  , ref kinematic);
                        break;
                }
            //if (optType == OptimizerType.BFGSOptimizer)
            //    switch (approxType)
            //    {
            //        case ApproximationType.ftc:
            //            writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_interactionFTC_BFGS.xml");
            //            writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backSub1FTC_BFGS.xml");
            //            writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backInterFTC_BFGS.xml");
            //            writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFTC_BFGS.xml");
            //            break;

            //        case ApproximationType.dtc:
            //            writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_interactionDTC_BFGS.xml");
            //            writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backSub1DTC_BFGS.xml");
            //            writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backInterDTC_BFGS.xml");
            //            writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicDTC_BFGS.xml");
            //            break;

            //        case ApproximationType.fitc:
            //            writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_interactionFITC_BFGS.xml");
            //            writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backSub1FITC_BFGS.xml");
            //            writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_backInterFITC_BFGS.xml");
            //            writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFITC_BFGS.xml");
            //            break;
            //    }
            //if (optType == OptimizerType.CERCGOptimizer)
            //    switch (approxType)
            //    {
            //        case ApproximationType.ftc:
            //            writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFTC_CERCG.xml");
            //            writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FTC_CERCG.xml");
            //            writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFTC_CERCG.xml");
            //            writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFTC_CERCG.xml");
            //            break;

            //        case ApproximationType.dtc:
            //            writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionDTC_CERCG.xml");
            //            writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1DTC_CERCG.xml");
            //            writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterDTC_CERCG.xml");
            //            writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicDTC_CERCG.xml");
            //            break;

            //        case ApproximationType.fitc:
            //            writer.write(ref interaction, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_interactionFITC_CERCG.xml");
            //            writer.write(ref backSub1, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backSub1FITC_CERCG.xml");
            //            writer.write(ref backInter, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_backInterFITC_CERCG.xml");
            //            writer.write(ref kinematic, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_Style2_kinematicFITC_CERCG.xml");
            //            break;
            //    }

            BVHData bvh = new BVHData(_repType, bUseRelativeRoot);
            bvh.LoadFile(@"..\..\..\..\Data\NewHighFive\BVH\SubAngle_TPoseNick1.bvh");

            _skeleton = bvh.skeleton;
            frameTime = bvh.FrameTime;
            pp = new DataPostProcess(_skeleton, frameTime);
        }

        private void loadBVH(string[] folder, string style, string substyle)
        {
            using (ILScope.Enter())
            {
                ILArray<double> data = ILMath.empty();
                ILArray<double> vel = ILMath.empty();
                ILArray<double> position = ILMath.empty();
                ILCell positions = ILMath.cell();

                BVHData bvh;

                if (subAngle.Styles == null)
                    subAngle.AddStyle(style, new Style(style));

                for (int i = 0; i < folder.Length; i++)
                {
                    bvh = new BVHData(_repType, bUseRelativeRoot);

                    data = bvh.LoadFile(folder[i]);

                    subAngle.AddData(data);
                    //vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(6, ILMath.end)]);
                    //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                    subAngle.Styles[style].AddSubStyle(substyle, data.Size[0]);

                    if (pp == null)
                    {
                        pp = new DataPostProcess(bvh.skeleton, bvh.FrameTime);
                        _skeleton = bvh.skeleton;
                        frameTime = bvh.FrameTime;
                    }

                    position = ILMath.zeros(data.S[0], 3);
                    for (int j = 0; j < data.S[0]; j++)
                    {
                        pp.ForwardKinematics(data[j, ILMath.full], positions, true, true, _repType);
                        position[j, ILMath.full] = positions.GetArray<double>(jointID)[ILMath.r(0, 2), 3].T;
                    }
                    kinematic.AddData(position);
                }
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
                data = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.r(0, 5)];

                /*tmp = Y[ILMath.r(seg[i], seg[i + 1] - 1), ILMath.full] * 0.1;
                root = (tmp[ILMath.full, ILMath.r(0, 2)] + tmp[ILMath.full, ILMath.r(3, 5)]) / 2; // ballance of RSHF and RSHB setting as root
                root[ILMath.full, 2] = 0;
                data = tmp[ILMath.full, ILMath.r(6, 8)] - root; // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmp[ILMath.full, ILMath.r(9, 11)] - root;*/

                //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.S[1])] = DataProcess.Velocity(data, 0);
                subPoint.AddData(data);
            }

            data = Y[ILMath.r(seg[ILMath.end], Y.S[0] - 1), ILMath.r(0, 5)];

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
