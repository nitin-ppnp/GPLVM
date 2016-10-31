using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM.Utils.Character;

namespace MotionPrimitives.SkeletonMap
{
    // Flubber joints connectivity:
    //ROOT Bip001_Pelvis
    //JOINT Bip001_Spine
    //  JOINT Bip001_L_Thigh
    //    JOINT Bip001_L_Calf
    //      JOINT Bip001_L_Foot
    //        JOINT Bip001_L_Toe0
    //  JOINT Bip001_R_Thigh
    //    JOINT Bip001_R_Calf
    //      JOINT Bip001_R_Foot
    //        JOINT Bip001_R_Toe0
    //  JOINT Bip001_Spine1
    //    JOINT Bip001_Spine2
    //      JOINT Bip001_Neck
    //        JOINT Bip001_L_Clavicle
    //          JOINT Bip001_L_UpperArm
    //            JOINT Bip001_L_Forearm
    //              JOINT Bip001_L_Hand
    //                JOINT Bip001_L_Finger0
    //                JOINT Bip001_L_Finger1
    //        JOINT Bip001_R_Clavicle
    //          JOINT Bip001_R_UpperArm
    //            JOINT Bip001_R_Forearm
    //              JOINT Bip001_R_Hand
    //                JOINT Bip001_R_Finger0
    //                JOINT Bip001_R_Finger1
    //        JOINT Bip001_Head
    public class PredefinedMaps
    {
        // Full body as a single part
        public static SkeletonMap CreateFlubberFullMap(Skeleton skeleton, JointsGroup.DataChannelsMode channelsMode)
        {
            var map = new SkeletonMap(skeleton);
            map.AddChannelsGroup(new JointsGroup("Full body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Pelvis", JointChannels.Channels.All),
                new JointChannels("Bip001_Spine", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_L_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_L_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_Toe0", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_R_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_R_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_Toe0", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_Spine1", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_Spine2", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_Neck", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_Clavicle", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_L_UpperArm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_L_Forearm", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Hand", JointChannels.Channels.Rotation),
                                                new JointChannels("Bip001_L_Finger0", JointChannels.Channels.Rotation),
                                                new JointChannels("Bip001_L_Finger1", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_Clavicle", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_R_UpperArm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_R_Forearm", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Hand", JointChannels.Channels.Rotation),
                                                new JointChannels("Bip001_R_Finger0", JointChannels.Channels.Rotation),
                                                new JointChannels("Bip001_R_Finger1", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_Head", JointChannels.Channels.Rotation),
            })));

            return map;
        }

        public static SkeletonMap CreateFlubberUpperLowerMap(Skeleton skeleton, JointsGroup.DataChannelsMode channelsMode)
        {
            var map = new SkeletonMap(skeleton);
            map.AddChannelsGroup(new JointsGroup("Upper body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Spine1", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_Spine2", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_Neck", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Clavicle", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_UpperArm", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_L_Forearm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_L_Hand", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Finger0", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Finger1", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Clavicle", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_UpperArm", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_R_Forearm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_R_Hand", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Finger0", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Finger1", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_Head", JointChannels.Channels.Rotation),
            })));
            map.AddChannelsGroup(new JointsGroup("Lower body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Pelvis", JointChannels.Channels.All),
                new JointChannels("Bip001_Spine", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_L_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_L_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_Toe0", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_R_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_R_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_Toe0", JointChannels.Channels.Rotation),
            })));
            
            return map;
        }

        public static SkeletonMap CreateFlubberUpperLowerPelvisMap(Skeleton skeleton, JointsGroup.DataChannelsMode channelsMode)
        {
            var map = new SkeletonMap(skeleton);
            map.AddChannelsGroup(new JointsGroup("Upper body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Spine1", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_Spine2", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_Neck", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Clavicle", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_UpperArm", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_L_Forearm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_L_Hand", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Finger0", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Finger1", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Clavicle", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_UpperArm", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_R_Forearm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_R_Hand", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Finger0", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Finger1", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_Head", JointChannels.Channels.Rotation),
            })));
            map.AddChannelsGroup(new JointsGroup("Pelvis position", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Pelvis", JointChannels.Channels.Position),
                new JointChannels("Bip001_Pelvis", JointChannels.Channels.Rotation),
            })));
            map.AddChannelsGroup(new JointsGroup("Lower body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Spine", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_L_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_L_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_Toe0", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_R_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_R_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_Toe0", JointChannels.Channels.Rotation),
            })));
            
            return map;
        }

        public static SkeletonMap CreateFlubberHeadUpperLowerPelvisMap(Skeleton skeleton, JointsGroup.DataChannelsMode channelsMode)
        {
            var map = new SkeletonMap(skeleton);
            map.AddChannelsGroup(new JointsGroup("Head", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Head", JointChannels.Channels.Rotation),
            })));
            map.AddChannelsGroup(new JointsGroup("Upper body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Spine1", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_Spine2", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_Neck", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Clavicle", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_UpperArm", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_L_Forearm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_L_Hand", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Finger0", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_L_Finger1", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Clavicle", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_UpperArm", JointChannels.Channels.Rotation),
                                    new JointChannels("Bip001_R_Forearm", JointChannels.Channels.Rotation),
                                        new JointChannels("Bip001_R_Hand", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Finger0", JointChannels.Channels.Rotation),
                                            new JointChannels("Bip001_R_Finger1", JointChannels.Channels.Rotation),
            })));
            map.AddChannelsGroup(new JointsGroup("Pelvis position", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Pelvis", JointChannels.Channels.Position),
                new JointChannels("Bip001_Pelvis", JointChannels.Channels.Rotation),
            })));
            map.AddChannelsGroup(new JointsGroup("Lower body part", skeleton, channelsMode, new List<JointChannels>(new JointChannels[] { 
                new JointChannels("Bip001_Spine", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_L_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_L_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_L_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_L_Toe0", JointChannels.Channels.Rotation),
                    new JointChannels("Bip001_R_Thigh", JointChannels.Channels.Rotation),
                        new JointChannels("Bip001_R_Calf", JointChannels.Channels.Rotation),
                            new JointChannels("Bip001_R_Foot", JointChannels.Channels.Rotation),
                                new JointChannels("Bip001_R_Toe0", JointChannels.Channels.Rotation),
            })));

            return map;
        }
    }
}
