using System;
using GPLVM.GPLVM;
using DataFormats;
using ILNumerics;
using GPLVM.Synthesis;

namespace GPLVM.Utils.Character
{
    public class AvatarBVH
    {
        private Skeleton _skeleton;
        private Representation _repType;

        private ILArray<double> styleVariable;

        private ILArray<double> oldPose = ILMath.localMember<double>();
        private ILArray<double> newPose = ILMath.localMember<double>();
        private ILArray<double> absPose = ILMath.localMember<double>();
        private ILArray<double> trainingData = ILMath.localMember<double>();

        private ILArray<double> _X = ILMath.localMember<double>();
        private ILArray<double> _Xnew = ILMath.localMember<double>();
        //private ILArray<double> _Yvar = ILMath.localMember<double>();

        private ILArray<double> _Y = ILMath.localMember<double>();
        private ILArray<double> _oldY = ILMath.localMember<double>();

        private ILArray<double> initY = new double[3] { -27.65, 93.45, 33.84 };

        private double frameTime = 0.04;
        private int rowIndex;
        private int currentSequence;

        private bool starting_frame = true;

        private GP_LVM subAngle;

        private SynthesisOptimizer _optimizer;

        public bool IsConstraint {set; get;}

        private DataPostProcess _postPrc;

        private ILArray<double> _targetLocation = ILMath.localMember<double>();
        private ILArray<double> Ynz = ILMath.localMember<double>();

        private bool _init = false;

        public AvatarBVH(Representation repType = Representation.exponential)
        {
            _repType = repType;
            rowIndex = 0;
            currentSequence = 0;

            IsConstraint = false;
        }

        public Skeleton skeleton
        {
            get { return _skeleton; }
        }

        public double FrameTime
        {
            get { return frameTime; }
            set
            {
                frameTime = value;
            }
        }

        public IGPLVM Model
        {
            get { return subAngle; }
        }

        public ILArray<double> PostMean
        {
            get { return newPose; }   
        }

        public DataPostProcess PostProcess
        {
            get { return _postPrc; }
        }

        //public ILArray<double> TestInput
        //{
        //    get { return _XStar; }
        //    set { _XStar.a = value; }
        //}

        public Representation RepresentationType
        {
            get { return _repType; }
        }

        private ILArray<double> InitTranslation
        {
            get { return initY; }
            set { initY.a = value; }
        }

        public bool init(StyleGPLVM2 gplvm, DataPostProcess pp)
        {
            subAngle = gplvm;
            //styleVariable = subAngle.Styles["Emotions"].StyleVariable;

            ILArray<double> tmp = ILMath.empty();

            trainingData = subAngle.Y;
            newPose = trainingData[0, ILMath.full].C;

            absPose.a = newPose.C;
            //absPose[0, 0] = initY[0];
            //absPose[0, 2] = initY[2];

            _X.a = subAngle.X[1, ILMath.full];

            _skeleton = ((StyleGPLVM2)subAngle).SkeletonTree;
            _postPrc = pp;

            _optimizer = new SynthesisOptimizer(subAngle, _postPrc, true, _repType);

            return true;
        }

        public bool init(GP_LVM gplvm, BVHData loadedBVH)
        {
            subAngle = gplvm;
            //styleVariable = subAngle.Styles["Emotions"].StyleVariable;

            ILArray<double> tmp = ILMath.empty();

            trainingData = subAngle.Y;
            newPose = trainingData[0, ILMath.full].C;

            absPose.a = newPose.C;
            //absPose[0, 0] = initY[0];
            //absPose[0, 2] = initY[2];

            _X.a = subAngle.X[1, ILMath.full];
            Ynz.a = ILMath.zeros(2, subAngle.Y.S[1]);

            Ynz[0, ILMath.full] = subAngle.PredictData(_X);

            _skeleton = loadedBVH.skeleton;
            frameTime = loadedBVH.FrameTime;
            _postPrc = new DataPostProcess(_skeleton, frameTime);

            _optimizer = new SynthesisOptimizer(subAngle, _postPrc, true, _repType);

            return true;
        }

        public Frame PlayTrainingData(bool bUseRelativeRoot)
        {
            ILArray<double> seg = subAngle.Segments;
            ILArray<double> finalPose = ILMath.empty();

            if (rowIndex < trainingData.S[0])
            {
                bool starting_frame = false;
                if (rowIndex == 0) starting_frame = true;
                if (currentSequence < subAngle.Segments.Length - 1)
                {
                    if (rowIndex == subAngle.Segments[currentSequence + 1])
                    {
                        currentSequence++;
                        starting_frame = true;
                    }
                }

                oldPose = absPose.C;
                newPose = trainingData[rowIndex, ILMath.full].C;

                if (!starting_frame && bUseRelativeRoot)
                {
                    absPose = _postPrc.AbsoluteRoot(oldPose, newPose, _repType);
                }

                if (rowIndex < trainingData.S[0] - 1)
                    rowIndex++;
                else
                {
                    rowIndex = 0;
                    currentSequence = 0;
                }
            }

            if (bUseRelativeRoot)
                finalPose = _postPrc.getFullPose(absPose, _repType);
            else
                finalPose = newPose.C;
            

            Frame frame = new Frame();
            frame.CreateStreamFrame(finalPose, skeleton, _repType);
            frame.PID = 3;

            return frame;
        }

        public Frame Synthesis(ILInArray<double> inTestInputs, bool bUseRelativeRoot)
        {
            using (ILScope.Enter(inTestInputs))
            {
                ILArray<double> xTmp = ILMath.empty();
                ILArray<double> finalPose = ILMath.empty();

                //if (inTestInputs != null)// && subAngle.Prior.Type != Prior.PriorType.PriorTypeDynamics)
                    _Xnew = ILMath.check(inTestInputs);
                //else if (inTestInputs != null && subAngle.Prior.Type == Prior.PriorType.PriorTypeDynamics)
                //    _XStar.a = ILMath.check(inTestInputs);

                //if (subAngle.Prior.Type == Prior.PriorType.PriorTypeDynamics)
                //{
                //    switch (((IDynamics)subAngle.Prior).DynamicsType)
                //    {
                //        case DynamicType.DynamicTypeVelocity:
                //            xTmp = ((IDynamics)subAngle.Prior).PredictData(_XStar);
                //            _XStar.a = xTmp.C;
                //            break;
                //        case DynamicType.DynamicTypeAcceleration:
                //            //XpredTmp = Xpred[n - 1, ILMath.full];
                //            //XpredTmp[0, ILMath.r(ILMath.end + 1, ILMath.end + _q)] = Xpred[n - 2, ILMath.full];
                //            //Xpred[n, ILMath.full] = PredictData(XpredTmp);

                //            xTmp = ((IDynamics)subAngle.Prior).PredictData(_XStar);
                //            _XStar[0, ILMath.r(subAngle.LatentDimension, ILMath.end)] = _XStar[0, ILMath.r(0, subAngle.LatentDimension - 1)].C;
                //            _XStar[0, ILMath.r(0, subAngle.LatentDimension - 1)] = xTmp.C;
                //            break;
                //    }
                //}

                oldPose.a = absPose.C;
                ILArray<double> Yvar = ILMath.empty();

                if (IsConstraint)
                {
                    subAngle.PredictData(xTmp, Yvar);
                    //newPose.a = _optimizer.ConstraintPose();

                    //subAngle.TestInput = xTmp;
                    newPose.a = subAngle.Ynew.C;
                    //subAngle.PostVar = _Yvar;
                }
                else
                {
                    ILArray<double> X2 = Util.Concatenate<double>(_X, _Xnew);

                    Ynz = subAngle.PredictData(X2, Yvar);

                    if (!_init)
                        _oldY.a = Ynz[0, ILMath.full].C;

                    Yvar -= ILMath.eye(2, 2) * subAngle.Noise;

                    //double Ysigma = (double)(Yvar[1, 1] - Yvar[1, 0] * Yvar[0, 1] / Yvar[0, 0]);
                    double Ysigma = (double)(Yvar[0, 0] - Yvar[1, 0] * Yvar[0, 1] / Yvar[1, 1]);

                    newPose.a = Ynz[1, ILMath.full] + ILMath.min(1.0, (Yvar[1, 0] / Yvar[0, 0])) * (_oldY - Ynz[0, ILMath.full]);
                    //ILArray<double> _Ymean = Ynz[1, ILMath.full] + ILMath.min(1.0, (Yvar[1, 0] / Yvar[0, 0])) * (_oldY - Ynz[0, ILMath.full]);

                    //newPose.a = subAngle.PredictData(_X);
                }

                if (bUseRelativeRoot)
                {
                    absPose.a = _postPrc.AbsoluteRoot(oldPose, newPose, _repType);
                    finalPose = _postPrc.getFullPose(absPose, _repType);
                }
                else
                    finalPose = newPose.C;

                _oldY.a = Ynz[1, ILMath.full].C;
                _X.a = _Xnew.C;

                _init = true;

                Frame frame = new Frame();
                frame.CreateStreamFrame(finalPose, skeleton, _repType);
                frame.PID = 3;

                return frame;
            }
        }

        public void Reset()
        {
            absPose.a = trainingData[0, ILMath.full].C;
            _X.a = subAngle.X[1, ILMath.full];

            rowIndex = 0;
            currentSequence = 0;
        }

        public ILRetArray<double> SynthesisTest(ILInArray<double> inTestInputs, bool bUseRelativeRoot)
        {
            if (subAngle.Mode != LerningMode.selfposterior)
                    subAngle.Mode = LerningMode.selfposterior;

            using (ILScope.Enter(inTestInputs))
            {
                oldPose.a = absPose.C;

                _Xnew.a = ILMath.check(inTestInputs);

                ILArray<double> finalPose = ILMath.empty();

                if (IsConstraint)
                {
                    ILArray<double> rootPos = ILMath.zeros(1, 4);
                    ILArray<double> Yvar = ILMath.empty();

                    ILArray<double> X2 = Util.Concatenate<double>(_X, _Xnew);

                    rootPos[0, 0] = oldPose[0, 0];
                    rootPos[0, 1] = oldPose[0, 1];
                    rootPos[0, 2] = oldPose[0, 2];
                    rootPos[0, 3] = oldPose[0, 3];

                    //Ynz[1, ILMath.full] = subAngle.PredictData(_Xnew, Yvar);
                    Ynz = subAngle.PredictData(X2, Yvar);
                
                    // Constraint Synthesis
                    //if (synthesis_activated_)
                    //{
                        // This is a little hack to smooth things out.
                        // Basically, we are assuming the synthesis is "noiseless", and removing the noise from the velocity and pose
                        // covariance. This has the effect of smoothing out the synthesis.
                        //Yvar -= ILMath.eye(2, 2) * subAngle.Noise;

                        // Run optimizer to get constrained pose.
                        newPose.a = _optimizer.ConstraintPose(Yvar, Ynz, rootPos, _targetLocation, subAngle.Scale, Ynz);
                    //}
                    //else
                    //    newPose.a = Ynz2[1, ILMath.full];
                }
                
                    //subAngle.PredictData(xTmp, _Yvar);
                    //newPose.a = _optimizer.ConstraintPose();

                    ////subAngle.TestInput = xTmp;
                    //newPose.a = subAngle.Ynew.C;
                    //subAngle.PostVar = _Yvar;
                else
                {
                    newPose.a = subAngle.PredictData(_Xnew);
                }

                subAngle.Ynew = newPose.C;

                if (bUseRelativeRoot)
                {
                    absPose.a = _postPrc.AbsoluteRoot(oldPose, newPose, _repType);
                    finalPose = _postPrc.getFullPose(absPose, _repType);
                }
                else
                    finalPose = newPose.C;

                _X.a = _Xnew.C;

                return finalPose;
            }
        }

        public Frame Simulate(Frame c3d, ILArray<double> styleMultiplier)
        {
            var start = DateTime.Now;

            ILArray<double> inX = ILMath.empty();
            ILArray<double> tmpX = ILMath.zeros(1, c3d.NUM_ENTRIES * 3);
            ILArray<double> Ytmp = ILMath.empty();
            ILArray<double> velocity = ILMath.empty();
            ILArray<double> globalPos = ILMath.empty();
            ILArray<double> root = ILMath.empty();

            ILArray<double> tmpStyle = ILMath.zeros(styleVariable.S);

            int cnt = 0;

            for (int i = 0; i < c3d.NUM_ENTRIES; i++)
            {
                tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[0];
                tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[1];
                tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[2];
            }

            /*inX = tmpX[ILMath.full, ILMath.r(0, 5)];
            Ytmp = interaction.PredictData(backInter.PredictData(backSub1.PredictData(inX)));

            Ytmp = Ytmp[ILMath.full, ILMath.r(((IGPLVM)interaction.Nodes[0]).LatentDimension, ILMath.end)];

            tmpStyle[0, ILMath.full] = styleVariable[0, ILMath.full] * styleValue[0];
            tmpStyle[1, ILMath.full] = styleVariable[1, ILMath.full] * styleValue[1];
            tmpStyle[2, ILMath.full] = styleVariable[2, ILMath.full] * styleValue[2];
            tmpStyle[3, ILMath.full] = styleVariable[3, ILMath.full] * styleValue[3];

            tmpStyle = ILMath.sum(tmpStyle, 0);

            Ytmp[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + tmpStyle.S[1])] = tmpStyle;

            Ytmp = subAngle.PredictData(Ytmp).C;

            Ytmp = ComputeEulerDeg(Ytmp).C;

            oldPose = newPose.C;
            newPose = Ytmp.C;

            if (!starting_frame)
            {
                newPose = postProcess.AbsoluteRoot(oldPose, Ytmp).C;
            }*/

            Frame frame = new Frame();
            //frame.CreateStreamFrame(ComputeRadian(newPose), skeleton, _repType);
            frame.PID = 3;

            starting_frame = false;

            //frameTime = Convert.ToDouble(TimeSpan.FromSeconds(DateTime.Now.Subtract(start).Seconds));

            return frame;
        }
    }
}
