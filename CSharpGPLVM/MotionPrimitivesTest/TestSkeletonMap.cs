using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using DataFormats;
using GPLVM;
using GPLVM.Utils.Character;
using MotionPrimitives.SkeletonMap;

namespace MotionPrimitivesTest
{
    partial class Program
    {
        public static void TestSkeletonMap()
        {
            string sBVHFileName = @"..\..\..\..\..\..\Data\Emotional Walks\Niko\BVH\NicoMapped_NeutralWalk01.bvh";
            Representation representation = Representation.exponential;
            BVHData bvh = new BVHData(representation);
            ILArray<double> motionData = bvh.LoadFile(sBVHFileName);
            Skeleton skeleton = bvh.skeleton;
            JointsGroup.DataChannelsMode dataMode = (representation == Representation.exponential ? JointsGroup.DataChannelsMode.RootRotation4Channels : JointsGroup.DataChannelsMode.RootRotation3Channels);
            var map = PredefinedMaps.CreateFlubberUpperLowerMap(skeleton, dataMode);
            var groups = map.SplitDataIntoGroups(motionData);
            ILArray<double> motionDataMerged = map.MergeDataFromGroups(groups);

            if (ILMath.allall(ILMath.eq(motionData, motionDataMerged)))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Skeleton data split->merge test PASSED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Skeleton data split->merge test FAILED");
            }
            Console.ResetColor();
        }
    }
}
