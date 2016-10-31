using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;

namespace MotionPrimitives.MotionStream
{
    // Matrix reader motion ganerator
    public class MatrixGenerator : Generator
    {
        protected ILArray<double> aMotionData = ILMath.empty();
        protected Representation representationType;
        public double FrameTime = 1/30;

        // Accepts data in exponential form
        public MatrixGenerator(Skeleton characterSkeleton, ILInArray<double> inMotionData, Representation representationType)
            : base(characterSkeleton)
        {
            using (ILScope.Enter(inMotionData))
            {
                ILArray<double> motionData = ILMath.check(inMotionData);
                aMotionData.a = motionData;
                this.representationType = representationType;
            }
        }

        public override bool GenerateFrame(ILOutArray<double> frameData, out TimeSpan timeMarker, out Representation representationType)
        {
            timeMarker = TimeSpan.FromSeconds(nFramesGenerated * FrameTime);
            representationType = this.representationType;
            if (nFramesGenerated < aMotionData.S[0])
            {
                frameData.a = aMotionData[nFramesGenerated, ILMath.full];
                nFramesGenerated++;
                return true;                
            }
            else
                return false; 
        }
    }
}
