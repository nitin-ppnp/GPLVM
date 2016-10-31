using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;
using DataFormats;
using MotionPrimitives.Experiments;
using FactorGraph.Core;
using FactorGraph.DataNodes;

namespace MotionPrimitives.MotionStream
{
    public class ExperimentGenerator : Generator
    {
        protected Experiment pExperiment;
        private DataPostProcess postProcess;
        private ILArray<double> prevFrame = ILMath.empty();
        public int MaxFrameCounter = 60;
        public bool StationaryRoot = true;

        public ExperimentGenerator(Skeleton characterSkeleton, Experiment experiment)
            : base(characterSkeleton)
        {
            pExperiment = experiment;
            postProcess = new DataPostProcess(characterSkeleton, pExperiment.FrameTime);
            var dnX = pExperiment.GraphModel.FindDataNodeByName("X");
            MaxFrameCounter = (int)dnX.GetValues().S[0];
        }

        public override void Reset()
        {
            base.Reset();
            if (pExperiment != null)
                pExperiment.InitGeneration();
        }

        public override bool GenerateFrame(ILOutArray<double> frameData, out TimeSpan timeMarker, out Representation representationType)
        {
            timeMarker = TimeSpan.FromSeconds(nFramesGenerated * pExperiment.FrameTime);
            representationType = Representation.exponential;
            if (nFramesGenerated < MaxFrameCounter)
            {
                pExperiment.GenerateFrame(frameData, out representationType);
                //if (representationType == Representation.exponential)
                //{
                //    frameData.a = ExponentialToEulerRad(frameData);
                //    representationType = Representation.radian;
                //}
                if (nFramesGenerated > 0) // Not first frame
                {
                    frameData.a = postProcess.AbsoluteRoot(prevFrame, frameData, representationType).C;
                    if (StationaryRoot)
                    {
                        frameData[ILMath.full, 0] = 0;
                        frameData[ILMath.full, 2] = 0;
                    }
                }
                prevFrame = frameData.C;
                frameData.a = GetFullPose(frameData);
                nFramesGenerated++;
                return true;
            }
                return false;
        }

        
    }
}
