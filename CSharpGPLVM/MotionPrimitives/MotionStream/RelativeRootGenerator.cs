using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using DataFormats;
using GPLVM;
using GPLVM.Utils.Character;

namespace MotionPrimitives.MotionStream
{
    public class RelativeRootGenerator : MatrixGenerator
    {
        private DataPostProcess postProcess;
        private ILArray<double> prevFrame = ILMath.empty();

        public RelativeRootGenerator(Skeleton characterSkeleton, ILInArray<double> inMotionData, double frameTime, Representation representationType)
            : base(characterSkeleton, inMotionData, representationType)
        {
            FrameTime = frameTime;
            postProcess = new DataPostProcess(characterSkeleton, frameTime);
        }

        public override bool GenerateFrame(ILOutArray<double> frameData, out TimeSpan timeMarker, out Representation representationType)
        {
            bool res = base.GenerateFrame(frameData, out timeMarker, out representationType);
            if (nFramesGenerated > 1) // Not first frame
            {
                frameData.a = postProcess.AbsoluteRoot(prevFrame, frameData, representationType).C;
            }
            prevFrame = frameData.C;
            frameData.a = GetFullPose(frameData);
            //if (representationType == Representation.exponential)
            //{
            //    frameData.a = ExponentialToEulerRad(frameData);
            //    representationType = Representation.radian;
            //}
            return res;
        }
    }
}
