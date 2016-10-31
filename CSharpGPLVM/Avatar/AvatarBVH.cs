using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Styles;
using GPLVM.Dynamics;
using GPLVM.Utils.Character;
using DataFormats;
using ILNumerics;
using Avatar.Synthesis;

namespace Avatar
{
    public class AvatarBVH
    {
        private Skeleton _skeleton;
        private Representation _repType;

        private ILArray<double> styleVariable;

        private ILArray<double> oldPose;
        private ILArray<double> newPose;
        private ILArray<double> absPose;
        private ILArray<double> trainingData;

        private ILArray<double> initY = new double[3] { -27.65, 93.45, 33.84 };

        private double frameTime;
        private int rowIndex;
        private int currentSequence;

        private bool starting_frame = true;

        private StyleGPLVM2 subAngle;

        private Synthesizer _synthesizer;

        public Skeleton skeleton
        {
            get { return _skeleton; }
        }

        public double FrameTime
        {
            get { return frameTime; }
        }

        private ILArray<double> InitTranslation
        {
            get { return initY; }
            set { initY.a = value; }
        }

        public AvatarBVH(Representation repType = Representation.exponential)
        {
            _repType = repType;
            rowIndex = 0;
            currentSequence = 0;
        }

        public bool init(StyleGPLVM2 gplvm, BVHData loadedBVH)
        {
            subAngle = gplvm;
            //styleVariable = subAngle.Styles["Emotions"].StyleVariable;

            _skeleton = subAngle.SkeletonTree;

            ILArray<double> tmp = ILMath.empty();

            trainingData = subAngle.Y;
            newPose = trainingData[0, ILMath.full];

            absPose = newPose.C;
            absPose[0, 0] = initY[0];
            absPose[0, 2] = initY[2];

            //postProcess = new DataPostProcess(_skeleton, loadedBVH.FrameTime);

            //_synthesizer = new Synthesizer(ref subAngle);

            return true;
        }

        public Frame PlayTrainingData()
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

                if (!starting_frame)
                {
                    absPose = AbsoluteRoot(oldPose, newPose);
                }

                if (rowIndex < trainingData.S[0] - 1)
                    rowIndex++;
                else
                {
                    rowIndex = 0;
                    currentSequence = 0;
                }
            }

            finalPose = getFullPose(absPose);

            Frame frame = new Frame();
            frame.CreateStreamFrame(finalPose, skeleton, _repType);
            frame.PID = 3;

            return frame;
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
