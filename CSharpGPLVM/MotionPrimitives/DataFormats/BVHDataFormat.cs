using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ILNumerics;

namespace MotionPrimitives.DataFormats
{
    public class BVHNode
    {
        public string Name;
        public int ID;
        public int ParentID;
        public ILArray<double> Offset = new double[3] { 0, 0, 0 };
        public string[] Channels = new string[0];
        public ILArray<int> RotInd = ILMath.empty<int>();
        public ILArray<int> PosInd = ILMath.empty<int>();
        public string RotOrder = null;
        public ILArray<int> RotOrderInt = ILMath.empty<int>();
        public List<BVHNode> Children = new List<BVHNode>();
    }

    public class BVHDataFormat
    {
        private BVHNode root = null;
        private CharacterSkeleton skeleton;
        private double frameTime;
        public ILArray<double> Motion = ILMath.empty();

        public BVHDataFormat(string sFileName)
        {
            frameTime = 0;
            LoadFile(sFileName);
        }

        public CharacterSkeleton Skeleton
        {
            get { return skeleton; }
        }

        public double FrameTime
        {
            get { return frameTime; }
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
                            root = ReadJoint(fileStream, ref i);
                            skeleton = new CharacterSkeleton();
                            i = 0;
                            CreateIDandTree(root, ref i);
                            break;
                        case "MOTION":
                            ReadMotion(fileStream);
                            break;
                    }
                }
                tokens = ReadLineTokens(fileStream);
            }
            fileStream.Close();
            return Motion;
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
                frameTime = Double.Parse(tokens[2], CultureInfo.InvariantCulture);
            Motion = ILMath.zeros(new int[] { nFrames, GetNumKinematicParameters(root) });
            for (int k1 = 0; k1 < nFrames; k1++)
            {
                tokens = ReadLineTokens(stream);
                for (int k2 = 0; k2 < tokens.Length; k2++)
                    Motion[k1, k2] = Double.Parse(tokens[k2], CultureInfo.InvariantCulture);
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
                        char[] rotOrder = new char[3];
                        int[] rotOrderInt = new int[3];
                        for (int k = 0; k < nChannels; k++)
                        {
                            res.Channels[k] = tokens[k + 2];
                            switch (tokens[k + 2])
                            {
                                case "Xrotation":
                                    res.RotInd[0] = channelNo + k;
                                    rotOrder[orderNo] = 'x';
                                    rotOrderInt[orderNo++] = 0;
                                    break;
                                case "Yrotation":
                                    res.RotInd[1] = channelNo + k;
                                    rotOrder[orderNo] = 'y';
                                    rotOrderInt[orderNo++] = 1;
                                    break;
                                case "Zrotation":
                                    res.RotInd[2] = channelNo + k;
                                    rotOrder[orderNo] = 'z';
                                    rotOrderInt[orderNo++] = 2;
                                    break;
                                case "Xposition":
                                    res.PosInd[0] = channelNo + k;
                                    break;
                                case "Yposition":
                                    res.PosInd[1] = channelNo + k;
                                    break;
                                case "Zposition":
                                    res.PosInd[2] = channelNo + k;
                                    break;
                            }
                        }
                        res.RotOrder = new String(rotOrder);
                        res.RotOrderInt = new int[] { rotOrderInt[0], rotOrderInt[1], rotOrderInt[2] };
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
