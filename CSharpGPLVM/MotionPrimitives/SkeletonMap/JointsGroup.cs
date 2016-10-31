using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;

namespace MotionPrimitives.SkeletonMap
{
    [DataContract()]
    public class JointChannels
    {
        [Flags]
        public enum Channels
        {
            None = 0,
            Position = 1,
            Rotation = 2,
            All = 3,
        }
        [DataMember()]
        private string jointName;
        [DataMember()]
        private bool usePosition;
        [DataMember()]
        private bool useRotation;

        public JointChannels(string name, Channels channelsFlag = Channels.All)
        {
            jointName = name;
            usePosition = (channelsFlag & Channels.Position) == Channels.Position;
            useRotation = (channelsFlag & Channels.Rotation) == Channels.Rotation;
        }
        public string JointName
        {
            get { return jointName; }
        }
        public bool UseRotation
        {
            get { return useRotation; }
        }
        public bool UsePosition
        {
            get { return usePosition; }
        }
    }

    [DataContract()]
    public class JointsGroup
    {
        public enum DataChannelsMode
        {
            RootRotation3Channels,
            RootRotation4Channels,
        }
        [DataMember()]
        private string name;
        [DataMember()]
        private ILArray<int> channels = ILMath.localMember<int>();
        [DataMember()]
        private List<JointChannels> jointChannels;

        public string Name
        {
            get { return name; }
        }
        public ILRetArray<int> Channels
        {
            get { return channels.C; }
        }
        public List<JointChannels> JointChannels
        {
            get { return jointChannels; }
        }

        public JointsGroup(string name, Skeleton skeleton, DataChannelsMode dataChannelsMode, List<JointChannels> jointChannels)
        {
            this.name = name;
            this.jointChannels = jointChannels;
            foreach (JointChannels jointChannel in jointChannels)
            {
                Joint joint = skeleton.Joints.Where(x => x.Name == jointChannel.JointName).First();
                switch (dataChannelsMode)
                {
                    case DataChannelsMode.RootRotation3Channels:
                        if (jointChannel.UsePosition)
                            channels.a = Util.Concatenate<int>(channels, joint.posInd);
                        if (jointChannel.UseRotation)
                            channels.a = Util.Concatenate<int>(channels, joint.rotInd);
                        break;
                    case DataChannelsMode.RootRotation4Channels:
                        if (joint.ID == 0) // root joint
                        {
                            if (jointChannel.UsePosition)
                                channels.a = Util.Concatenate<int>(channels, joint.posInd);
                            if (jointChannel.UseRotation)
                            {
                                channels.a = Util.Concatenate<int>(channels, joint.rotInd);
                                channels.a = Util.Concatenate<int>(channels, ILMath.max(joint.rotInd)[0] + 1); // Additional chanel for vertical axis rotation
                            }
                        }
                        else
                        {
                            if (jointChannel.UsePosition)
                                channels.a = Util.Concatenate<int>(channels, joint.posInd + 1);
                            if (jointChannel.UseRotation)
                                channels.a = Util.Concatenate<int>(channels, joint.rotInd + 1);
                        }
                        break;
                }
            }
        }
    }

    [DataContract()]
    public class JointsGroupData
    {
        [DataMember()]
        private JointsGroup jointsGroup;
        public ILArray<double> Data = ILMath.localMember<double>();
       
        public JointsGroupData(JointsGroup jointsGroup, ILInArray<double> inFullData)
        {
            using (ILScope.Enter(inFullData))
            {
                ILArray<double> fullData = ILMath.check(inFullData);
                this.jointsGroup = jointsGroup;
                if (!fullData.IsEmpty)
                    Data.a = fullData[ILMath.full, jointsGroup.Channels];
            }
        }

        public JointsGroupData CreateEmptyCopy()
        {
            return new JointsGroupData(jointsGroup);
        }

        public JointsGroupData CreateCopyWithNewData(ILInArray<double> inNewData)
        {
            using (ILScope.Enter(inNewData))
            {
                ILArray<double> newData = ILMath.check(inNewData);
                var res = CreateEmptyCopy();
                res.Data.a = newData;
                return res;
            }
        }

        private JointsGroupData(JointsGroup jointsGroup)
        {
            this.jointsGroup = jointsGroup;
        }

        public JointsGroup Group
        {
            get { return jointsGroup; }
        }

        [OnDeserialized()]
        public void OnDeserializedMethod(StreamingContext context)
        {
            Data = ILMath.localMember<double>();
        }
    }
}
