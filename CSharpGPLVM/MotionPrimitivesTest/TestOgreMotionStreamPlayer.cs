using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;
using DataFormats;
using MotionPrimitives.MotionStream;

namespace MotionPrimitivesTest
{
    partial class Program 
    {
        public static void TestOgreMotionStreamPlayer()
        {
            Representation representation = Representation.radian;
            BVHData bvh = new BVHData(representation);
            ILArray<double> motionData = bvh.LoadFile("../../../../Data/Drawer/drawer_Walk01_01.bvh");
            Skeleton skeleton = bvh.skeleton;

            var generator = new RelativeRootGenerator(skeleton, motionData, bvh.FrameTime, representation);
            var visualiser = new OgreVisualizer();
            var player = new Player(generator, visualiser);
            player.PlayAll();

        }
    }
}
