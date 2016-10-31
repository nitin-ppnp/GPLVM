using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ILNumerics;
using GPLVM;

namespace MotionPrimitives.MotionStream
{
    public class Player
    {
        private Generator mGenerator;
        private Visualizer mVisualiser;

        public Player(Generator generator, Visualizer visualiser)
        {
            mGenerator = generator;
            mVisualiser = visualiser;
        }

        public void Reset()
        {
            mGenerator.Reset();
            mVisualiser.Stop();
            mVisualiser.Start();
            mVisualiser.SetSkeleton(mGenerator.CharacterSkeleton);
        }

        public void PlayAll()
        {
            Reset();
            ILArray<double> frameData = ILMath.empty();
            TimeSpan timeMarker;
            Representation representationType;
            DateTime timeStart = DateTime.Now;
            while (mGenerator.GenerateFrame(frameData, out timeMarker, out representationType))
            {
                mVisualiser.SetFrame(frameData, representationType);
                TimeSpan toSleep = timeMarker - (DateTime.Now - timeStart);
                if (toSleep.TotalMilliseconds > 0)
                    Thread.Sleep(timeMarker - (DateTime.Now - timeStart));
            }
        }

    }
}
