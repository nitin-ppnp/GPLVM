using System.IO;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Dynamics;
using GPLVM.Backconstraint;
using GPLVM.Styles;
using GPLVM.Optimisation;
using ILNumerics;
using DataFormats;
using System.Windows.Forms;
using GPLVM.Utils.Character;

namespace Models
{
    public class TippingTurning
    {
        private int numIter = 80;
        private StyleGPLVM2 gpAvatarIdle;
        private StyleGPLVM2 gpAvatarTurn;

        private ApproximationType approxType = ApproximationType.ftc;
        private OptimizerType optType = OptimizerType.SCGOptimizer;
        private Representation _repType = Representation.exponential;

        private DataPostProcess pp;
        private Skeleton _skeleton;
        private double frameTime;

        private ILArray<double> styleVariableIdleE = ILMath.localMember<double>();
        private ILArray<double> styleVariableTurnE = ILMath.localMember<double>();
        private ILArray<double> styleVariableIdleC = ILMath.localMember<double>();
        private ILArray<double> styleVariableTurnC = ILMath.localMember<double>();

        private AvatarBVH avatarIdle;
        private AvatarBVH avatarTurn;

        private int jointID = 22; // Right Hand ID

        //private string prefix = "00_PTC";
        private string prefix = "01";

        private bool bUseRelativeRoot = true;

        protected ILArray<double> _XIdle = ILMath.localMember<double>();
        protected ILArray<double> _XTurn = ILMath.localMember<double>();

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

        public ILArray<double> XIdle
        {
            get { return gpAvatarIdle.X[ILMath.r(gpAvatarIdle.Segments[0], gpAvatarIdle.Segments[3]), ILMath.full]; }
        }

        public ILArray<double> XTurn
        {
            get { return gpAvatarTurn.X[ILMath.r(gpAvatarTurn.Segments[0], gpAvatarTurn.Segments[4]), ILMath.full]; }
        }

        public ILArray<double> XIdleDynamics
        {
            get 
            {
                ILArray<double> x = _XIdle[1, ILMath.full];
                x[ILMath.full, 2] = 0;
                return x; 
            }
        }

        public ILArray<double> XTurnDynamics
        {
            get
            {
                ILArray<double> x = _XTurn[1, ILMath.full];
                x[ILMath.full, 2] = 0;
                return x;
            }
        }

        public Frame PredictDataIdle(ILInArray<double> inStyleValueE, ILInArray<double> inStyleValueC)
        {
            using (ILScope.Enter(inStyleValueE, inStyleValueC))
            {
                ILArray<double> styleValueE = ILMath.check(inStyleValueE);
                ILArray<double> styleValueC = ILMath.check(inStyleValueC);
                ILArray<double> XpredTmp = ILMath.empty();
                ILArray<double> tmpStyleE = ILMath.zeros(styleVariableIdleE.S);
                ILArray<double> tmpStyleC = ILMath.zeros(styleVariableIdleC.S);

                if (_XIdle.IsEmpty) _XIdle.a = gpAvatarIdle.X[ILMath.r(0, 1), ILMath.r(0, gpAvatarIdle.LatentDimension - 1)].C;

                XpredTmp = _XIdle[1, ILMath.full];
                XpredTmp[0, ILMath.r(ILMath.end + 1, ILMath.end + gpAvatarIdle.LatentDimension)] = _XIdle[0, ILMath.full];
                XpredTmp = ((GPAcceleration)gpAvatarIdle.Prior).PredictData(XpredTmp);

                _XIdle[0, ILMath.full] = _XIdle[1, ILMath.full].C;
                _XIdle[1, ILMath.full] = XpredTmp.C;

                tmpStyleE[0, ILMath.full] = styleVariableIdleE[0, ILMath.full] * styleValueE[0];
                tmpStyleE[1, ILMath.full] = styleVariableIdleE[1, ILMath.full] * styleValueE[1];
                tmpStyleE[2, ILMath.full] = styleVariableIdleE[2, ILMath.full] * styleValueE[2];

                tmpStyleC[0, ILMath.full] = styleVariableIdleC[0, ILMath.full] * styleValueC[0];
                tmpStyleC[1, ILMath.full] = styleVariableIdleC[1, ILMath.full] * styleValueC[1];
                tmpStyleC[2, ILMath.full] = styleVariableIdleC[2, ILMath.full] * styleValueC[2];
                tmpStyleC[3, ILMath.full] = styleVariableIdleC[3, ILMath.full] * styleValueC[3];

                Frame frame = new Frame();
                frame.CreateStreamFrame(avatarIdle.SynthesisTest(Util.Concatenate<double>(_XIdle[1, ILMath.full], Util.Concatenate<double>(ILMath.sum(tmpStyleE, 0), 
                    ILMath.sum(tmpStyleC, 0), 1), 1), bUseRelativeRoot), skeleton, _repType);
                frame.PID = 3;

                return frame;
            }
        }

        public Frame PredictDataTurn(ILInArray<double> inStyleValueE, ILInArray<double> inStyleValueC)
        {
            using (ILScope.Enter(inStyleValueE, inStyleValueC))
            {
                ILArray<double> styleValueE = ILMath.check(inStyleValueE);
                ILArray<double> styleValueC = ILMath.check(inStyleValueC);
                ILArray<double> XpredTmp = ILMath.empty();
                ILArray<double> tmpStyleE = ILMath.zeros(styleVariableTurnE.S);
                ILArray<double> tmpStyleC = ILMath.zeros(styleVariableTurnC.S);

                if (_XTurn.IsEmpty) _XTurn.a = gpAvatarTurn.X[ILMath.r(0, 1), ILMath.r(0, gpAvatarTurn.LatentDimension - 1)].C;

                XpredTmp = _XTurn[1, ILMath.full];
                XpredTmp[0, ILMath.r(ILMath.end + 1, ILMath.end + gpAvatarTurn.LatentDimension)] = _XTurn[0, ILMath.full];
                XpredTmp = ((GPAcceleration)gpAvatarTurn.Prior).PredictData(XpredTmp);

                _XTurn[0, ILMath.full] = _XTurn[1, ILMath.full].C;
                _XTurn[1, ILMath.full] = XpredTmp.C;

                tmpStyleE[0, ILMath.full] = styleVariableTurnE[0, ILMath.full] * styleValueE[0];
                tmpStyleE[1, ILMath.full] = styleVariableTurnE[1, ILMath.full] * styleValueE[1];
                tmpStyleE[2, ILMath.full] = styleVariableTurnE[2, ILMath.full] * styleValueE[2];

                tmpStyleC[0, ILMath.full] = styleVariableTurnC[0, ILMath.full] * styleValueC[0];
                tmpStyleC[1, ILMath.full] = styleVariableTurnC[1, ILMath.full] * styleValueC[1];
                tmpStyleC[2, ILMath.full] = styleVariableTurnC[2, ILMath.full] * styleValueC[2];
                tmpStyleC[3, ILMath.full] = styleVariableTurnC[3, ILMath.full] * styleValueC[3];

                Frame frame = new Frame();
                frame.CreateStreamFrame(avatarTurn.SynthesisTest(Util.Concatenate<double>(_XTurn[1, ILMath.full], Util.Concatenate<double>(ILMath.sum(tmpStyleE, 0), 
                    ILMath.sum(tmpStyleC, 0), 1), 1), bUseRelativeRoot), skeleton, _repType);
                frame.PID = 3;

                return frame;
            }
        }

        public Frame PlayTrainingDataIdle()
        {
            return avatarIdle.PlayTrainingData(bUseRelativeRoot);
        }

        public Frame PlayTrainingDataTurn()
        {
            return avatarTurn.PlayTrainingData(bUseRelativeRoot);
        }

        public void Reset()
        {
            avatarIdle.Reset();
            avatarTurn.Reset();

            _XTurn.a = gpAvatarTurn.X[ILMath.r(0, 1), ILMath.r(0, gpAvatarTurn.LatentDimension - 1)].C;
            _XIdle.a = gpAvatarIdle.X[ILMath.r(0, 1), ILMath.r(0, gpAvatarIdle.LatentDimension - 1)].C;
        }

        public bool init(bool isLoad)
        {
            if (isLoad)
            {
                LoadModel();

                styleVariableIdleE.a = gpAvatarIdle.Styles["Emotions"].StyleVariable;
                styleVariableTurnE.a = gpAvatarTurn.Styles["Emotions"].StyleVariable;
                styleVariableIdleC.a = gpAvatarIdle.Styles["Character"].StyleVariable;
                styleVariableTurnC.a = gpAvatarTurn.Styles["Character"].StyleVariable;

                gpAvatarIdle.SkeletonTree = _skeleton;
                gpAvatarTurn.SkeletonTree = _skeleton;

                avatarIdle = new AvatarBVH(_repType);
                avatarIdle.init(gpAvatarIdle, pp);

                avatarTurn = new AvatarBVH(_repType);
                avatarTurn.init(gpAvatarTurn, pp);

                _XIdle.a = gpAvatarIdle.X[ILMath.r(0, 1), ILMath.r(0, gpAvatarIdle.LatentDimension - 1)].C;
                _XTurn.a = gpAvatarTurn.X[ILMath.r(0, 1), ILMath.r(0, gpAvatarTurn.LatentDimension - 1)].C;
            }
            else
            {
                gpAvatarIdle = new StyleGPLVM2(2, approxType, _repType, BackConstType.ptcLinear);
                gpAvatarIdle.NumInducing = 80;
                gpAvatarIdle.Prior = new GPAcceleration();

                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\AnCh\Neutral"), "Emotions", "neutral", "Character", "AnCh");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\AnCh\Anger"), "Emotions", "angry", "Character", "AnCh");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\AnCh\Fear"), "Emotions", "fear", "Character", "AnCh");

                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\MaSt\Neutral"), "Emotions", "neutral", "Character", "MaSt");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\MaSt\Anger"), "Emotions", "angry", "Character", "MaSt");
                //loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\MaSt\Fear"), "Emotions", "fear", "Character", "MaSt");

                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\NiTa\Neutral"), "Emotions", "neutral", "Character", "NiTa");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\NiTa\Anger"), "Emotions", "angry", "Character", "NiTa");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\NiTa\Fear"), "Emotions", "fear", "Character", "NiTa");

                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\OlGa\Neutral"), "Emotions", "neutral", "Character", "OlGa");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\OlGa\Anger"), "Emotions", "angry", "Character", "OlGa");
                loadBVHIdle(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Idle\OlGa\Fear"), "Emotions", "fear", "Character", "OlGa");

                gpAvatarIdle.Initialize();

                gpAvatarTurn = new StyleGPLVM2(2, approxType, _repType, BackConstType.ptcLinear);
                gpAvatarTurn.NumInducing = 100;
                gpAvatarTurn.Prior = new GPAcceleration();

                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\AnCh\Neutral"), "Emotions", "neutral", "Character", "AnCh");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\AnCh\Anger"), "Emotions", "angry", "Character", "AnCh");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\AnCh\Fear"), "Emotions", "fear", "Character", "AnCh");

                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\MaSt\Neutral"), "Emotions", "neutral", "Character", "MaSt");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\MaSt\Anger"), "Emotions", "angry", "Character", "MaSt");
                //loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\MaSt\Fear"), "Emotions", "fear", "Character", "MaSt");

                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\NiTa\Neutral"), "Emotions", "neutral", "Character", "NiTa");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\NiTa\Anger"), "Emotions", "angry", "Character", "NiTa");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\NiTa\Fear"), "Emotions", "fear", "Character", "NiTa");

                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\OlGa\Neutral"), "Emotions", "neutral", "Character", "OlGa");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\OlGa\Anger"), "Emotions", "angry", "Character", "OlGa");
                loadBVHTurn(Directory.GetFiles(@"..\..\..\..\Data\TippingTurning\Turning\OlGa\Fear"), "Emotions", "fear", "Character", "OlGa");

                gpAvatarTurn.Initialize();

                //using (ILMatFile matW = new ILMatFile())
                //{
                //    matW.AddArray(gpAvatarTurn.X);
                //    matW.Write(@"..\..\..\..\Data\TippingTurning\XTest.mat");
                //}
            }

            return true;
        }

        public void learnModel()
        {
            switch (optType)
            {
                case OptimizerType.SCGOptimizer:
                    System.Console.WriteLine("\nOptimizing Interaction Model");
                    SCG.Optimize(ref gpAvatarIdle, numIter);
                    SCG.Optimize(ref gpAvatarTurn, numIter);

                    break;
                case OptimizerType.BFGSOptimizer:
                    BFGS.Optimize(ref gpAvatarTurn, numIter);
                    BFGS.Optimize(ref gpAvatarTurn, numIter);

                    break;
                case OptimizerType.CERCGOptimizer:
                    for (int i = 0; i < 3; i++)
                    {
                        gpAvatarIdle.Masking = Mask.latents;
                        CERCG.Optimize(ref gpAvatarIdle, numIter / 3);

                        gpAvatarIdle.Masking = Mask.kernel;
                        CERCG.Optimize(ref gpAvatarIdle, numIter / 3);

                        gpAvatarTurn.Masking = Mask.latents;
                        CERCG.Optimize(ref gpAvatarTurn, numIter / 3);

                        gpAvatarTurn.Masking = Mask.kernel;
                        CERCG.Optimize(ref gpAvatarTurn, numIter / 3);
                    }

                    gpAvatarIdle.Masking = Mask.full;
                    CERCG.Optimize(ref gpAvatarIdle, numIter / 3);

                    gpAvatarTurn.Masking = Mask.full;
                    CERCG.Optimize(ref gpAvatarTurn, numIter / 3);

                    break;
            }

        }

        public void showPlots()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            

            //ILArray<double> X = gpAvatarIdle.X[ILMath.full, ILMath.r(0, 2)].C;
            //X[ILMath.full, 2] = 0;
            //Form1 form1 = new Form1(X, gpAvatarIdle.Segments);
            //form1.ShowDialog();

            ILArray<double> X = gpAvatarTurn.X[ILMath.full, ILMath.r(0, 2)].C;
            //X[ILMath.full, 2] = 0;
            Application.Run(new Form1(X, gpAvatarTurn.Segments));
            //form2.ShowDialog();
        }

        public void SaveModel()
        {
            XMLReadWrite writer = new XMLReadWrite();

            if (optType == OptimizerType.SCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFTC_SCG.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFTC_SCG.xml");

                        break;

                    case ApproximationType.dtc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleDTC_SCG.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnDTC_SCG.xml");

                        break;

                    case ApproximationType.fitc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFITC_SCG.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFITC_SCG.xml");

                        break;
                }
            if (optType == OptimizerType.BFGSOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFTC_BFGS.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFTC_BFGS.xml");

                        break;

                    case ApproximationType.dtc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleDTC_BFGS.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnDTC_BFGS.xml");

                        break;

                    case ApproximationType.fitc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFITC_BFGS.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFITC_BFGS.xml");

                        break;
                }
            if (optType == OptimizerType.CERCGOptimizer)
                switch (approxType)
                {
                    case ApproximationType.ftc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFTC_CERCG.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFTC_CERCG.xml");

                        break;

                    case ApproximationType.dtc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleDTC_CERCG.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnDTC_CERCG.xml");
                        break;

                    case ApproximationType.fitc:
                        writer.write(ref gpAvatarIdle, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFITC_CERCG.xml");
                        writer.write(ref gpAvatarTurn, @"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFITC_CERCG.xml");
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
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFTC_SCG.xml", ref gpAvatarIdle);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFTC_SCG.xml", ref gpAvatarTurn);
                        break;

                    case ApproximationType.dtc:
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleDTC_SCG.xml", ref gpAvatarIdle);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnDTC_SCG.xml", ref gpAvatarTurn);
                        break;

                    case ApproximationType.fitc:
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningIdleFITC_SCG.xml", ref gpAvatarIdle);
                        reader.read(@"..\..\..\..\Data\XML\StyleIK\" + prefix + "_TippingTurningTurnFITC_SCG.xml", ref gpAvatarTurn);
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
            bvh.LoadFile(@"..\..\..\..\Data\TippingTurning\Idle\AnCh\Anger\AnChi_ANGER_Idle.bvh");

            _skeleton = bvh.skeleton;
            frameTime = bvh.FrameTime;
            pp = new DataPostProcess(_skeleton, frameTime);
        }

        private void loadBVHIdle(string[] folder, string style, string substyle, string character, string subCharacter)
        {
            using (ILScope.Enter())
            {
                ILArray<double> data = ILMath.empty();
                ILArray<double> vel = ILMath.empty();
                ILArray<double> position = ILMath.empty();
                ILCell positions = ILMath.cell();

                BVHData bvh;

                if (gpAvatarIdle.Styles == null)
                {
                    gpAvatarIdle.AddStyle(style, new Style(style));
                    gpAvatarIdle.AddStyle(character, new Style(character));
                }

                for (int i = 0; i < folder.Length; i++)
                {
                    bvh = new BVHData(_repType, bUseRelativeRoot);

                    data = bvh.LoadFile(folder[i]);
                    vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(6, ILMath.end)]);
                    data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                    gpAvatarIdle.AddData(data);
                    
                    gpAvatarIdle.Styles[style].AddSubStyle(substyle, data.Size[0]);
                    gpAvatarIdle.Styles[character].AddSubStyle(subCharacter, data.Size[0]);

                    if (pp == null)
                    {
                        pp = new DataPostProcess(bvh.skeleton, bvh.FrameTime);
                        _skeleton = bvh.skeleton;
                        frameTime = bvh.FrameTime;
                    }
                }
            }
        }

        private void loadBVHTurn(string[] folder, string style, string substyle, string character, string subCharacter)
        {
            using (ILScope.Enter())
            {
                ILArray<double> data = ILMath.empty();
                ILArray<double> vel = ILMath.empty();
                ILArray<double> position = ILMath.empty();
                ILCell positions = ILMath.cell();

                BVHData bvh;

                if (gpAvatarTurn.Styles == null)
                {
                    gpAvatarTurn.AddStyle(style, new Style(style));
                    gpAvatarTurn.AddStyle(character, new Style(character));
                }

                for (int i = 0; i < folder.Length; i++)
                {
                    bvh = new BVHData(_repType, bUseRelativeRoot);

                    data = bvh.LoadFile(folder[i]);
                    //vel = Util.Velocity<double>(data[ILMath.full, ILMath.r(6, ILMath.end)]);
                    //data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + vel.S[1])] = vel.C;
                    gpAvatarTurn.AddData(data);
                    
                    gpAvatarTurn.Styles[style].AddSubStyle(substyle, data.Size[0]);
                    gpAvatarTurn.Styles[character].AddSubStyle(subCharacter, data.Size[0]);

                    if (pp == null)
                    {
                        pp = new DataPostProcess(bvh.skeleton, bvh.FrameTime);
                        _skeleton = bvh.skeleton;
                        frameTime = bvh.FrameTime;
                    }
                }
            }
        }
    }
}
