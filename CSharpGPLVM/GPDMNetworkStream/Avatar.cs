using System;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Utils.Character;
using DataFormats;
using ILNumerics;

namespace GPDMNetworkStream
{
    public class Avatar
    {
        private XMLReadWrite reader;
        private BVHData bvh;

        private StyleGPLVM2 subAngle;
        //private GP_LVM subAngle;
        private GP_LVM interaction;
        private BackProjection backSub1;
        private BackProjection backInter;

        private Skeleton _skeleton;
        private Representation _repType;

        private ILArray<double> styleVariable;
        private ILArray<double> distance;

        private ILArray<double> oldPose = ILMath.localMember<double>();
        private ILArray<double> newPose = ILMath.localMember<double>();
        private ILArray<double> absPose = ILMath.localMember<double>(); 
        private ILArray<double> trainingData;

        private double frameTime;
        private int rowIndex;
        private int currentSequence;

        private bool starting_frame = true;

        DataPostProcess postProcess;

        private ILArray<double> initY = new double[3] { -27.65, 93.45, 33.84 };

        public Skeleton skeleton
        {
            get { return _skeleton; }
        }

        public double FrameTime
        {
            get { return frameTime; }
        }

        public bool StartFrame
        {
            get { return starting_frame; }
            set { starting_frame = value; }
        }

        public Avatar(Representation repType = Representation.radian)
        {
            _repType = repType;
            rowIndex = 0;
            currentSequence = 0;
            initY = initY.T;
        }

        public bool init()
        {
            reader = new XMLReadWrite();

            reader.read(@"..\..\..\..\Data\XML\StyleIK\01_100iter_deg2rad_Style2_interactionFITC_SCG.xml", ref interaction);
            reader.read(@"..\..\..\..\Data\XML\StyleIK\01_100iter_deg2rad_Style2_backSub1FITC_SCG.xml", ref backSub1);
            reader.read(@"..\..\..\..\Data\XML\StyleIK\01_100iter_deg2rad_Style2_backInterFITC_SCG.xml", ref backInter);


            subAngle = (StyleGPLVM2)interaction.Nodes[1];
            styleVariable = subAngle.Styles["Emotions"].StyleVariable;

            //reader.read(@"..\..\..\..\Data\XML\TestWalk.xml", ref subAngle);

            //subAngle = new StyleGPLVM2();
            bvh = new BVHData(_repType);
            bvh.LoadFile(@"..\..\..\..\Data\HighFive\BVH\angry\Ia\nick_angry_Ia_2.bvh");
            //subAngle.AddData(bvh.LoadFile(@"..\..\..\..\Data\HighFive\BVH\angry\Ia\nick_angry_Ia_2.bvh"));

            _skeleton = bvh.skeleton;
            frameTime = bvh.FrameTime;

            ILArray<double> tmp = ILMath.empty();
            using (ILMatFile matRead = new ILMatFile(@"..\..\..\..\Data\HighFive\TobyTPosec3d.mat"))
            {
                tmp.a = matRead.GetArray<double>(0);
            }

            tmp = tmp[0, ILMath.full];
            ILArray<double> root = (tmp[0, ILMath.r(0, 2)] + tmp[0, ILMath.r(3, 5)]) / 2;
            distance = ILMath.zeros(1, 2);
            distance[0] = ILMath.sqrt(ILMath.pow(tmp[6] - root[0], 2) + ILMath.pow(tmp[7] - root[1], 2) + ILMath.pow(tmp[8] - root[2], 2));
            distance[1] = ILMath.sqrt(ILMath.pow(tmp[9] - root[0], 2) + ILMath.pow(tmp[10] - root[1], 2) + ILMath.pow(tmp[11] - root[2], 2));
            
            //trainingData = ComputeEulerDeg(subAngle.Y);
            trainingData = subAngle.Y;
            newPose = trainingData[0, ILMath.full];
            
            absPose.a = newPose.C;
            absPose[0, 0] = initY[0];
            absPose[0, 2] = initY[2];

            postProcess = new DataPostProcess(_skeleton, bvh.FrameTime);

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
                    absPose = postProcess.AbsoluteRoot(oldPose, newPose, _repType);
                }

                if (rowIndex < trainingData.S[0] - 1)
                    rowIndex++;
                else
                {
                    rowIndex = 0;
                    currentSequence = 0;
                }
            }

            finalPose = postProcess.getFullPose(absPose, _repType);

            Frame frame = new Frame();
            frame.CreateStreamFrame(finalPose, skeleton, _repType);//Representation.radian);
            frame.PID = 3;

            return frame;
        }

        public Frame Simulate(Frame c3d, ILArray<double> styleValue)
        {
            using (ILScope.Enter())
            {
                var start = DateTime.Now;
                ILArray<double> finalPose = ILMath.empty();

                ILArray<double> inX = ILMath.empty();
                ILArray<double> tmpX = ILMath.zeros(1, c3d.NUM_ENTRIES * 3);
                ILArray<double> Ytmp = ILMath.empty();
                ILArray<double> velocity = ILMath.empty();
                ILArray<double> globalPos = ILMath.empty();
                ILArray<double> root = ILMath.empty();

                ILArray<double> backSub = ILMath.empty();
                ILArray<double> backinter = ILMath.empty();

                ILArray<double> tmpStyle = ILMath.zeros(styleVariable.S);

                int cnt = 0;

                for (int i = 0; i < c3d.NUM_ENTRIES; i++)
                {
                    tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[0];
                    tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[1];
                    tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[2];
                }

                /*using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\..\..\..\Data\ViconStream.txt", true))
                {
                    file.WriteLine(tmpX.ToString());
                }

                if (lastPosition.IsEmpty)
                    velocity = ILMath.zeros(1, 6);
                else
                    velocity = tmpX[ILMath.full, ILMath.r(0, 5)] - lastPosition;

                lastPosition = tmpX[ILMath.full, ILMath.r(0, 5)];

                // ballance of RSHF and RSHB setting as root
                root = (tmpX[ILMath.full, ILMath.r(6, 8)] + tmpX[ILMath.full, ILMath.r(9, 11)]) / 2;
                root[ILMath.full, 2] = 0;

                // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
                inX = tmpX[ILMath.full, ILMath.r(0, 2)] - root; 
                inX[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmpX[ILMath.full, ILMath.r(3, 5)] - root;

                // normalize possition w.r.t the training data
                inX[ILMath.full, ILMath.r(0, 2)] *= ratio[0];
                inX[ILMath.full, ILMath.r(3, 5)] *= ratio[1];*/

                inX = tmpX.C;//[ILMath.full, ILMath.r(0, 5)];

                backSub = backSub1.PredictData(inX);
                backinter = backInter.PredictData(backSub);
                Ytmp = interaction.PredictData(backinter);

                Ytmp = Ytmp[ILMath.full, ILMath.r(((IGPLVM)interaction.Nodes[0]).LatentDimension, ILMath.end)];

                tmpStyle[0, ILMath.full] = styleVariable[0, ILMath.full] * styleValue[0];
                tmpStyle[1, ILMath.full] = styleVariable[1, ILMath.full] * styleValue[1];
                tmpStyle[2, ILMath.full] = styleVariable[2, ILMath.full] * styleValue[2];
                tmpStyle[3, ILMath.full] = styleVariable[3, ILMath.full] * styleValue[3];

                tmpStyle = ILMath.sum(tmpStyle, 0);

                Ytmp[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + tmpStyle.S[1])] = tmpStyle;

                if (StartFrame == true)
                    absPose.a = trainingData[0, ILMath.full].C;

                oldPose.a = absPose.C;
                newPose.a = subAngle.PredictData(Ytmp).C;

                if (!starting_frame)
                {
                    absPose.a = postProcess.AbsoluteRoot(oldPose, newPose, _repType);
                }

                finalPose = postProcess.getFullPose(absPose, _repType);

                Frame frame = new Frame();
                frame.CreateStreamFrame(finalPose, skeleton, _repType);//Representation.radian);
                frame.PID = 3;

                starting_frame = false;

                //frameTime = Convert.ToDouble(TimeSpan.FromSeconds(DateTime.Now.Subtract(start).Seconds));

                return frame;
            }
        }

        private ILRetArray<double> ComputeEulerDeg(ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);
                ILArray<double> euler = data.C;

                if (_repType == Representation.exponential)
                    for (int i = 0; i < data.S[0]; i++)
                        for (int j = 0; j < _skeleton.Joints.Count; j++)
                            euler[i, _skeleton.Joints[j].rotInd] =
                                Util.deg2rad(Util.FromRotationMatrixToEuler(Util.expToQuat(data[i, _skeleton.Joints[j].rotInd]).ToRotationMatrix(), _skeleton.Joints[j].order));
                else
                    for (int j = 0; j < _skeleton.Joints.Count; j++)
                        euler[ILMath.full, _skeleton.Joints[j].rotInd] = Util.rad2deg(data[ILMath.full, _skeleton.Joints[j].rotInd]);

                ILRetArray<double> ret = euler;
                return ret;
            }
        }

        private ILRetArray<double> ComputeRadian(ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);
                ILArray<double> radian = data.C;

                for (int j = 0; j < _skeleton.Joints.Count; j++)
                    radian[ILMath.full, _skeleton.Joints[j].rotInd] = Util.deg2rad(data[ILMath.full, _skeleton.Joints[j].rotInd]);

                ILRetArray<double> ret = radian;

                return ret;
            }
        }

    }
}
