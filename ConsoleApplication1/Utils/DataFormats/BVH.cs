using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;

namespace DataFormats
{
    public struct BVHChannelDescription
    {
        public string Name;
        public int ID;
    }

    public class BVHNode
    {
        public string Name;
        public int ID;
        public int ParentID;
        public ILArray<double> Offset = ILMath.localMember<double>();
        public string[] Channels = new string[0];
        public ILArray<int> rotInd = ILMath.localMember<int>();
        public ILArray<int> posInd = ILMath.localMember<int>();
        public string order = null;
        public ILArray<int> orderInt = ILMath.localMember<int>();
        public List<BVHNode> Children = new List<BVHNode>();

        public BVHNode()
        {
            Offset.a = new double[3] { 0, 0, 0 };
        }
    }

    public class BVHData
    {
        // Desired root rotation order.
        // Currently set to YZX.
        private ILArray<int> desiredRootOrder = new int[3] { 1, 2, 0 };
        private string desiredRootOrderStr = "yzx";

        private BVHNode Root = null;
        private Skeleton _skeleton;
        private double _FrameTime;
        public ILArray<double> Motion = ILMath.empty();
        private Representation _repType;
        public bool _relativeRoot;

        public BVHData(Representation repType = Representation.radian, bool relativeRoot = true)
        {
            _FrameTime = 0;
            _repType = repType;
            _relativeRoot = relativeRoot;
        }

        public Skeleton skeleton
        {
            get { return _skeleton; }
        }

        public double FrameTime
        {
            get { return _FrameTime; }
        }

        public ILArray<double> LoadFile(string sFileName)
        {
            Console.WriteLine("Loading " + sFileName + "...");
            var fileStream = new StreamReader(sFileName);
            string[] tokens = ReadLineTokens(fileStream);
            while (tokens != null)
            {
                if (tokens.Length > 0)
                {
                    switch (tokens[0])
                    {
                        case "HIERARCHY":
                            int i = 0;
                            Root = ReadJoint(fileStream, ref i);
                            _skeleton = new Skeleton();
                            i = 0;
                            CreateIDandTree(Root, ref i);
                            break;
                        case "MOTION":
                            ReadMotion(fileStream);
                            break;
                    }
                }
                tokens = ReadLineTokens(fileStream);
            }
            fileStream.Close();

            RearrangeMotion();

            if (_relativeRoot)
            {
                TransformRoot();
                RelativeRoot();
            }
            
            if (_repType == Representation.exponential)
                ToExponentialMap();
            if (_repType == Representation.quaternion)
                ToQuaternion();

            return Motion;
        }

        protected void RelativeRoot()
        {
            ILArray<double> oldMotion = Motion.C;
            ILArray<double> delta = ILMath.empty();
            //yzx
            for (int t = 0; t < Motion.S[0]; t++)
            {
                // Get indices of current and previous frame.
                int curridx = t;
                int previdx = t - 1;
                if (previdx < 0)
                { // Don't have a previous frame, so use the next one.
                    curridx = t + 1;
                    previdx = t;
                }

                // Obtain translation and rotation from current and previous frames.
                double prevRotation = (double)Util.rad2deg(oldMotion[previdx, 4]);
                double currRotation = (double)Util.rad2deg(oldMotion[curridx, 4]);
                double rotation = (double)oldMotion[t, 4];

                ILArray<double> prevTranslation = ILMath.zeros(1,2);
                ILArray<double> currTranslation = ILMath.zeros(1,2);

                prevTranslation[0] = oldMotion[previdx, 0].C;
                prevTranslation[1] = oldMotion[previdx, 2].C;

                currTranslation[0] = oldMotion[curridx, 0].C;
                currTranslation[1] = oldMotion[curridx, 2].C;

                delta = (currTranslation - prevTranslation) / _FrameTime;

                // Remove root rotation from horizontal translation.
                delta = ILMath.multiply(Util.Rotation2DCounterClock(rotation), delta.T);

                Motion[t, 0] = delta[0].C;
                Motion[t, 2] = delta[1].C;
                Motion[t, 4] = Util.modDeg(currRotation - prevRotation) / _FrameTime;
            }
        }

        protected void TransformRoot()
        {
            // Convert Euler angles for each frame.
            for (int t = 0; t < Motion.S[0]; t++)
            {
                Motion[t, skeleton.Joints[0].rotInd] = Util.convertEuler(Motion[t, skeleton.Joints[0].rotInd], skeleton.Joints[0].order, desiredRootOrderStr);
            }

            // Store new root order.
            skeleton.Joints[0].SwitchOrder(desiredRootOrder);
        }

        protected void ReadMotion(StreamReader stream)
        {
            string[] tokens;
            int nFrames = 0;
            tokens = ReadLineTokens(stream);
            if (tokens[0] == "Frames:")
                nFrames = Convert.ToInt32(tokens[1]);
            tokens = ReadLineTokens(stream);
            if (tokens[0] == "Frame" && tokens[1] == "Time:")
                _FrameTime = Double.Parse(tokens[2], CultureInfo.InvariantCulture);
            Motion = ILMath.zeros( new int[] {nFrames, GetNumKinematicParameters(Root)});
            for (int k1 = 0; k1 < nFrames; k1++)
            {
                tokens = ReadLineTokens(stream);
                for (int k2 = 0; k2 < tokens.Length; k2++)
                {
                    Motion[k1, k2] = double.Parse(tokens[k2], NumberStyles.AllowExponent | NumberStyles.Number, CultureInfo.InvariantCulture);
                }
            }
        }

        protected int GetNumKinematicParameters(BVHNode node)
        {
            int res = node.Channels.Length;
            foreach (var child in node.Children)
                res += GetNumKinematicParameters(child);
            return res;
        }

        protected string[] ReadLineTokens(StreamReader stream)
        {
            string[] tokens = null;
            string line = stream.ReadLine();
            if (line != null)
            {
                char[] delimiters = new char[] { ' ', '\t' };
                tokens = line.Trim().Split(delimiters);
            }
            return tokens;
        }

        protected BVHNode ReadJoint(StreamReader stream, ref int channelNo)
        {
            BVHNode res = new BVHNode();
            int orderNo;
            string[] tokens = ReadLineTokens(stream);
            while (tokens[0] != "}")
            {
                switch (tokens[0])
                {
                    case "OFFSET":
                        res.Offset[0] = Convert.ToDouble(tokens[1], CultureInfo.InvariantCulture);
                        res.Offset[1] = Convert.ToDouble(tokens[2], CultureInfo.InvariantCulture);
                        res.Offset[2] = Convert.ToDouble(tokens[3], CultureInfo.InvariantCulture);
                        break;
                    case "CHANNELS":
                        int nChannels = Convert.ToInt32(tokens[1]);
                        res.Channels = new string[nChannels];
                        orderNo = 0;
                        char[] ord = new char[3];
                        int[] ordInt = new int[3];
                        for (int k = 0; k < nChannels; k++)
                        {
                            res.Channels[k] = tokens[k + 2];
                            switch (tokens[k + 2])
                            {
                                case "Xrotation":
                                    res.rotInd[0] = channelNo + k;
                                    ord[orderNo] = 'x';
                                    ordInt[orderNo++] = 0;
                                    break;
                                case "Yrotation":
                                    res.rotInd[1] = channelNo + k;
                                    ord[orderNo] = 'y';
                                    ordInt[orderNo++] = 1;
                                    break;
                                case "Zrotation":
                                    res.rotInd[2] = channelNo + k;
                                    ord[orderNo] = 'z';
                                    ordInt[orderNo++] = 2;
                                    break;
                                case "Xposition":
                                    res.posInd[0] = channelNo + k;
                                    break;
                                case "Yposition":
                                    res.posInd[1] = channelNo + k;
                                    break;
                                case "Zposition":
                                    res.posInd[2] = channelNo + k;
                                    break;
                            }
                        }
                        res.order = new String(ord);
                        res.orderInt = new int[] { ordInt[0], ordInt[1], ordInt[2] };
                        channelNo += nChannels;
                        break;
                    case "ROOT":
                        res = ReadJoint(stream, ref channelNo);
                        res.Name = tokens[1];
                        return res;
                    case "JOINT":
                    case "End":
                        BVHNode node = ReadJoint(stream, ref channelNo);
                        node.Name = tokens[1];
                        res.Children.Add(node);
                        break;
                }
                tokens = ReadLineTokens(stream);
            }
            return res;
        }

        protected void RearrangeMotion()
        {
            ILArray<double> data = ILMath.empty();
            ILArray<double> globPos = ILMath.empty();

            int cnt = 0;
            for (int i = 0; i < skeleton.Joints.Count; i++)
            {
                if (i == 0)
                {
                    globPos = Motion[ILMath.full, skeleton.Joints[i].posInd];
                    skeleton.Joints[i].posInd[0] = cnt++;
                    skeleton.Joints[i].posInd[1] = cnt++;
                    skeleton.Joints[i].posInd[2] = cnt++;
                }
                if (skeleton.Joints[i].Name != "Site")
                    if (!skeleton.Joints[i].rotInd.IsEmpty)
                        if (i == 0)
                        {
                            data = Util.deg2rad(Motion[ILMath.full, skeleton.Joints[i].rotInd]);

                            skeleton.Joints[i].rotInd[0] = cnt++;
                            skeleton.Joints[i].rotInd[1] = cnt++;
                            skeleton.Joints[i].rotInd[2] = cnt++;
                        }
                        else
                        {
                            data[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + skeleton.Joints[i].rotInd.Length)] =
                                    Util.deg2rad(Motion[ILMath.full, skeleton.Joints[i].rotInd]);

                            skeleton.Joints[i].rotInd[0] = cnt++;
                            skeleton.Joints[i].rotInd[1] = cnt++;
                            skeleton.Joints[i].rotInd[2] = cnt++;
                        }
            }

            Motion = globPos;
            Motion[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + data.Size[1])] = data;
        }

        private void ToExponentialMap()
        {
            ILArray<double> newChannel = ILMath.zeros(Motion.S[0], Motion.S[1] + 1);
            newChannel[ILMath.full, ILMath.r(0, 2)] = Motion[ILMath.full, ILMath.r(0, 2)].C;

            for (int i = 0; i < skeleton.Joints.Count; i++)
                for (int j = 0; j < Motion.S[0]; j++)
                    if (i == 0)
                    {
                        if(_relativeRoot)
                        {
                            newChannel[j, 3] = Motion[j, skeleton.Joints[0].rotInd[1]].C;

                            ILArray<double> tmpRot = Motion[j, skeleton.Joints[i].rotInd];
                            tmpRot[1] = 0;

                            newChannel[j, skeleton.Joints[i].rotInd + 1] = Util.eulerToExp(tmpRot, skeleton.Joints[i].order);
                        }
                        else
                            newChannel[j, skeleton.Joints[i].rotInd] = Util.eulerToExp(Motion[j, skeleton.Joints[i].rotInd], skeleton.Joints[i].order);
                    }
                    else
                    {
                        if (_relativeRoot)
                            newChannel[j, skeleton.Joints[i].rotInd + 1] = Util.eulerToExp(Motion[j, skeleton.Joints[i].rotInd], skeleton.Joints[i].order);
                        else
                            newChannel[j, skeleton.Joints[i].rotInd] = Util.eulerToExp(Motion[j, skeleton.Joints[i].rotInd], skeleton.Joints[i].order);
                    }

            Motion = newChannel.C;
        }

        private void ToQuaternion()
        {
            ILArray<double> newChannel = ILMath.zeros(Motion.S[0], Motion.S[1] + skeleton.Joints.Count + 1);
            newChannel[ILMath.full, ILMath.r(0, 2)] = Motion[ILMath.full, ILMath.r(0, 2)].C;

            Quaternion quad;

            for (int i = 0; i < skeleton.Joints.Count; i++)
            {
                int jntIdx = (int)((skeleton.Joints[i].rotInd[0] - 3) / 3) * 4 + 4;

                int xidx = jntIdx + 0;
                int yidx = jntIdx + 1;
                int zidx = jntIdx + 2;
                int widx = jntIdx + 3;

                for (int j = 0; j < Motion.S[0]; j++)
                    if (i == 0)
                    {
                        newChannel[j, 3] = Motion[j, skeleton.Joints[0].rotInd[1]].C;

                        ILArray<double> tmpRot = Motion[j, skeleton.Joints[i].rotInd];
                        tmpRot[1] = 0;

                        quad = Util.eulerToQuaternion(tmpRot, skeleton.Joints[i].order);
                        newChannel[j, xidx] = quad.x;
                        newChannel[j, yidx] = quad.y;
                        newChannel[j, zidx] = quad.z;
                        newChannel[j, widx] = quad.w;
                    }
                    else
                    {
                        quad = Util.eulerToQuaternion(Motion[j, skeleton.Joints[i].rotInd], skeleton.Joints[i].order);
                        newChannel[j, xidx] = quad.x;
                        newChannel[j, yidx] = quad.y;
                        newChannel[j, zidx] = quad.z;
                        newChannel[j, widx] = quad.w;
                    }
            }
            Motion = newChannel.C;
        }

        protected void CreateIDandTree(BVHNode node, ref int IDCounter)
        {
            if (IDCounter == 0) node.ParentID = -1;
            if (node.Name != "Site")
            {
                node.ID = IDCounter++;
                skeleton.AddJoint(node);
                foreach (BVHNode child in node.Children)
                {
                    child.ParentID = node.ID;
                    CreateIDandTree(child, ref IDCounter);
                }
            }
        }
    }
}
