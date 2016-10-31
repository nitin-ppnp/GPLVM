using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;

namespace MotionPrimitives.MotionStream
{
    public abstract class Visualizer
    {
        public virtual void Start() { }
        public virtual void Stop() { }
        public abstract void SetSkeleton(Skeleton characterSkeleton);
        public abstract void SetFrame(ILInArray<double> aFrameData, Representation representationType);
    }
}
