using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GPLVM.Utils.Character;

namespace MotionPrimitives.MotionStream
{
    // Abstract motion data generator
    public abstract class Generator
    {
        protected int nFramesGenerated;
        public Skeleton pCharacterSkeleton;

        public Generator(Skeleton characterSkeleton)
        {
            pCharacterSkeleton = characterSkeleton;
            Reset();
        }

        public int FreamesGenerated
        {
            get { return nFramesGenerated; }
        }

        public Skeleton CharacterSkeleton
        {
            get { return pCharacterSkeleton; }
        }
        
        public virtual void Reset() 
        {
            nFramesGenerated = 0; 
        }
        
        public virtual bool GenerateFrame(ILOutArray<double> frameData, out TimeSpan timeMarker, out Representation representationType) 
        {
            timeMarker = TimeSpan.FromSeconds(nFramesGenerated / 24);
            representationType = Representation.exponential;
            return false; 
        }

        protected ILRetArray<double> ExponentialToEulerRad(ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);
                ILArray<double> euler = data.C;

                for (int i = 0; i < data.S[0]; i++)
                    for (int j = 0; j < pCharacterSkeleton.Joints.Count; j++)
                        euler[i, pCharacterSkeleton.Joints[j].rotInd] =
                            Util.FromRotationMatrixToEuler(Util.expToQuat(data[i, pCharacterSkeleton.Joints[j].rotInd]).ToRotationMatrix(), pCharacterSkeleton.Joints[j].order);

                ILRetArray<double> ret = euler;
                return ret;
            }
        }

        protected ILRetArray<double> ComputeRadian(ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);
                ILArray<double> radian = data.C;

                for (int j = 0; j < pCharacterSkeleton.Joints.Count; j++)
                    radian[ILMath.full, pCharacterSkeleton.Joints[j].rotInd] = Util.deg2rad(data[ILMath.full, pCharacterSkeleton.Joints[j].rotInd]);

                ILRetArray<double> ret = radian;

                return ret;
            }
        }

        protected ILRetArray<double> GetFullPose(ILInArray<double> inPose)
        {
            using (ILScope.Enter(inPose))
            {
                ILArray<double> pose = ILMath.check(inPose);
                ILArray<double> tmpAngle = new double[3] { 0, (double)inPose[3], 0 };

                tmpAngle = tmpAngle.T;
                Quaternion q1 = Util.eulerToQuaternion(Util.deg2rad(tmpAngle), pCharacterSkeleton.Joints[0].order);

                Quaternion q3 = Util.expToQuat(pose[0, ILMath.r(4, 6)]);

                q1 = q1 * q3;

                pose[ILMath.full, 3] = ILMath.empty();
                pose[ILMath.full, ILMath.r(3, 5)] = Util.QuaternionToExp(q1);

                pose[ILMath.full, 2] = pose[ILMath.full, 2];

                ILRetArray<double> ret = pose.C;

                return ret;
            }
        }
    }
}
