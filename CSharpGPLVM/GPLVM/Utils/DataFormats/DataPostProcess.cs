using ILNumerics;
using GPLVM.Utils.Character;
using GPLVM;
//using System.Math;

namespace DataFormats
{
    public class DataPostProcess
    {
        private double frameTime;
        private Skeleton _skeleton;

        private const double MIN_ANGLE = 1e-7;


        public double FrameTime
        {
            get { return frameTime; }
        }

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// Data post processing for animation of a character or data export.
        /// </remarks>
        /// <param name="skeleton">Skeleton structure of the character.</param>
        /// <param name="frameTime">Frame time of the animation.</param>
        public DataPostProcess(Skeleton skeleton, double _frameTime)
        {
            _skeleton = skeleton;
            frameTime = _frameTime;
        }

        public ILRetArray<double> getFullPose(ILInArray<double> inPose, Representation _type)
        {
            using (ILScope.Enter(inPose))
            {
                ILArray<double> pose = ILMath.check(inPose);

                switch (_type)
                {
                    case Representation.exponential:
                        Quaternion q1 = new Quaternion();
                        q1.FromAngleAxis(new double[] { (double)Util.deg2rad(pose[0, 3]), 0, 1, 0 });

                        Quaternion q2 = Util.expToQuat(pose[0, ILMath.r(4, 6)]);

                        q1 = q1 * q2;

                        pose[ILMath.full, 3] = ILMath.empty();
                        pose[ILMath.full, ILMath.r(3, 5)] = Util.QuaternionToExp(q1);
                        break;
                    case Representation.quaternion:
                        Quaternion q3 = new Quaternion();
                        q3.FromAngleAxis(new double[] { (double)Util.deg2rad(pose[0, 3]), 0, 1, 0 });

                        Quaternion q4 = new Quaternion((double)pose[0, 7], (double)pose[0, 4], (double)pose[0, 5], (double)pose[0, 6]);

                        q3 = q3 * q4;

                        pose[ILMath.full, 3] = ILMath.empty();
                        pose[ILMath.full, 3] = q3.x;
                        pose[ILMath.full, 4] = q3.y;
                        pose[ILMath.full, 5] = q3.z;
                        pose[ILMath.full, 6] = q3.w;

                        break;
                }

                ILRetArray<double> ret = pose.C;

                return ret;
            }
        }

        /// <summary>
        /// Translates the root position from relative to absolute. 
        /// </summary>
        /// <param name="inOldPose">Old pose of the character.</param>
        /// <param name="outNewPose">New pose of the character.</param>
        /// <returns>
        /// New pose of the character of type ILRetArray<T>.
        /// </returns>
        public ILRetArray<double> AbsoluteRoot(ILInArray<double> inOldPose, ILInArray<double> inNewPose, Representation _type)
        {
            using (ILScope.Enter(inOldPose, inNewPose))
            {
                ILArray<double> oldPose = ILMath.check(inOldPose);
                ILArray<double> newPose = ILMath.check(inNewPose);

                ILArray<double> absRoot = newPose[ILMath.full, ILMath.r(0, 2)].C;

                switch (_type)
                {
                    case Representation.radian:
                        newPose[ILMath.full, 4] = oldPose[ILMath.full, 4] + frameTime * newPose[ILMath.full, 4];

                        // Clamping angles
                        for (int i = 0; i < newPose.S[0]; i++)
                            newPose[i, 4] = Util.modAngle((double)newPose[i, 4]);

                        absRoot[ILMath.full, 0] = oldPose[ILMath.full, 0] + frameTime * (newPose[ILMath.full, 0] * ILMath.cos(newPose[ILMath.full, 4])
                            + newPose[ILMath.full, 2] * ILMath.sin(newPose[ILMath.full, 4]));
                        absRoot[ILMath.full, 2] = oldPose[ILMath.full, 2] + frameTime * (newPose[ILMath.full, 2] * ILMath.cos(newPose[ILMath.full, 4])
                            - newPose[ILMath.full, 0] * ILMath.sin(newPose[ILMath.full, 4]));
                        break;

                    default:
                        newPose[ILMath.full, 3] = oldPose[ILMath.full, 3] + frameTime * newPose[ILMath.full, 3];

                        // Clamping angles
                        for (int i = 0; i < newPose.S[0]; i++)
                            newPose[i, 3] = Util.modDeg((double)newPose[i, 3]);

                        absRoot[ILMath.full, 0] = oldPose[ILMath.full, 0] + frameTime * (newPose[ILMath.full, 0] * ILMath.cos(Util.deg2rad(newPose[ILMath.full, 3]))
                            + newPose[ILMath.full, 2] * ILMath.sin(Util.deg2rad(newPose[ILMath.full, 3])));
                        absRoot[ILMath.full, 2] = oldPose[ILMath.full, 2] + frameTime * (newPose[ILMath.full, 2] * ILMath.cos(Util.deg2rad(newPose[ILMath.full, 3]))
                            - newPose[ILMath.full, 0] * ILMath.sin(Util.deg2rad(newPose[ILMath.full, 3])));

                        break;
                }



                newPose[ILMath.full, 0] = absRoot[ILMath.full, 0].C;
                newPose[ILMath.full, 2] = absRoot[ILMath.full, 2].C;

                ILRetArray<double> ret = newPose;

                return ret;
            }
        }

        /// <summary>
        /// Computes absolute position for all the joints. 
        /// </summary>
        /// <param name="inPoseQuaternions">Pose of the character in Quaternions, ExpMap or Radian.</param>
        /// <param name="transform">translation matrix of dimension 4x4xnumberOfJoints.</param>
        /// <param name="bUseRoot">Root is involved.</param>
        /// <param name="bUseRelative">Root is relative.</param>
        /// <returns>
        /// ILOutArray<T> transform.
        /// </returns>
        public void ForwardKinematics(ILInArray<double> inPose, ILOutCell transforms, bool bUseRoot, bool bUseRelative, Representation type)
        {
            using (ILScope.Enter(inPose))
            {
                ILArray<double> pose = ILMath.check(inPose);

                ILArray<double> transform = ILMath.empty();

                ILArray<double> afnMat = ILMath.eye(3, 3);
                ILArray<double> rotMat = ILMath.eye(4, 4);

                transforms.a = ILMath.cell(ILMath.size(1, _skeleton.Joints.Count));

                int jntIdx, xidx, yidx, zidx, widx;
                Quaternion q;

                for (int i = 0; i < _skeleton.Joints.Count; i++)
                {
                    transform = ILMath.eye(4, 4);

                    // Rotate by joint rotation.
                    ILArray<int> rotInd = _skeleton.Joints[i].rotInd;
                    if (!rotInd.IsEmpty)
                    {
                        if (type == Representation.quaternion)
                        {
                            if (bUseRelative)
                                jntIdx = (int)((rotInd[0] - 3) / 3) * 4 + 4;
                            else
                                jntIdx = (int)((rotInd[0] - 3) / 3) * 4 + 3;

                            // Compute indices.
                            xidx = jntIdx + 0;
                            yidx = jntIdx + 1;
                            zidx = jntIdx + 2;
                            widx = jntIdx + 3;

                            q = new Quaternion((double)pose[0, widx], (double)pose[0, xidx], (double)pose[0, yidx], (double)pose[0, zidx]);
                        }
                        else
                        {
                            if (bUseRelative)
                                jntIdx = (int)rotInd[0] + 1;
                            else
                                jntIdx = (int)rotInd[0];

                            xidx = jntIdx + 0;
                            yidx = jntIdx + 1;
                            zidx = jntIdx + 2;

                            if (type == Representation.exponential)
                                q = Util.expToQuat(pose[0, ILMath.r(xidx, zidx)]);
                            else
                                q = Util.eulerToQuaternion(pose[0, ILMath.r(xidx, zidx)], _skeleton.Joints[i].order);
                        }

                        double norm = Quaternion.Norm(q);

                        if (i == 0 && bUseRelative)
                        {
                            double angle0 = (double)ILMath.asin(2.0 * ((q.w * q.z + q.y * q.x) / norm));
                            double angle1 = (double)ILMath.atan2(2.0 * ((q.w * q.x - q.y * q.z) / norm),
                                (ILMath.pow(q.w, 2) - ILMath.pow(q.x, 2) + ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2)) / norm);
                            
                            // Apply roll about X axis.
                            afnMat[0, 0] = 1.0;
                            afnMat[0, 1] = 0.0;
                            afnMat[0, 2] = 0.0;
                            afnMat[1, 0] = 0.0;
                            afnMat[2, 0] = 0.0;
                            afnMat[1, 1] = ILMath.cos(angle1);
                            afnMat[1, 2] = -ILMath.sin(angle1);
                            afnMat[2, 1] = ILMath.sin(angle1);
                            afnMat[2, 2] = ILMath.cos(angle1);
                            
                            // Apply pitch about Z axis.
                            transform[0, 0] = ILMath.cos(angle0);
                            transform[0, 1] = -ILMath.sin(angle0);
                            transform[1, 0] = ILMath.sin(angle0);
                            transform[1, 1] = ILMath.cos(angle0);

                            transform[ILMath.r(0, 2), ILMath.r(0, 2)] = ILMath.multiply(transform[ILMath.r(0, 2), ILMath.r(0, 2)], afnMat);
                        }
                        else
                        {
                            //// For standard joints, just use the quaternion.
                            //Matrix3 rot = q.ToRotationMatrix();
                            //afnMat = rot.Matrix;
                            //transforms[ILMath.r(0, 2), ILMath.r(0, 2), i] = afnMat;
                            // Simply convert the quaternion to a matrix.
                            afnMat[0, 0] = (ILMath.pow(q.x, 2) - ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2) + ILMath.pow(q.w, 2)) / norm;
                            afnMat[1, 0] = (2.0 * q.x * q.y + 2.0 * q.z * q.w) / norm;
                            afnMat[2, 0] = (2.0 * q.x * q.z - 2.0 * q.y * q.w) / norm;

                            afnMat[0, 1] = (2.0 * q.x * q.y - 2.0 * q.z * q.w) / norm;
                            afnMat[1, 1] = (-ILMath.pow(q.x, 2) + ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2) + ILMath.pow(q.w, 2)) / norm;
                            afnMat[2, 1] = (2.0 * q.y * q.z + 2.0 * q.x * q.w) / norm;

                            afnMat[0, 2] = (2.0 * q.x * q.z + 2.0 * q.y * q.w) / norm;
                            afnMat[1, 2] = (2.0 * q.y * q.z - 2.0 * q.x * q.w) / norm;
                            afnMat[2, 2] = (-ILMath.pow(q.x, 2) - ILMath.pow(q.y, 2) + ILMath.pow(q.z, 2) + ILMath.pow(q.w, 2)) / norm;

                            transform[ILMath.r(0, 2), ILMath.r(0, 2)] = afnMat;
                        }
                    }

                    // Apply joint translation.
                    if (i != 0)
                        transform[ILMath.r(0, 2), 3] += _skeleton.Joints[i].Offset;
                    else if (i == 0 && bUseRelative)
                    {
                        transform[0, 3] += _skeleton.Joints[i].Offset[0];
                        transform[2, 3] += _skeleton.Joints[i].Offset[2];
                    }

                    // Apply root transformation if necessary.
                    if (i == 0 && bUseRoot)
                    { 
                        // For root joints, apply root rotation and translation.
                        Quaternion qtmp = new Quaternion();
                        qtmp.FromAngleAxis(new double[] { (double)(Util.deg2rad(pose[0, 3]) * frameTime), 0, 1, 0 });
                        Matrix3 rot = qtmp.ToRotationMatrix();
                        afnMat = rot.Matrix;
                        rotMat[ILMath.r(0, 2), ILMath.r(0, 2)] = afnMat;

                        if (bUseRelative)
                        {
                            transform = ILMath.multiply(rotMat, transform);
                            transform[0, 3] += frameTime * (pose[0, 0] * ILMath.cos(Util.deg2rad(pose[0, 3] * frameTime)) + pose[0, 2] *
                                ILMath.sin(Util.deg2rad(pose[0, 3] * frameTime)));
                            transform[1, 3] += pose[0, 1];
                            transform[2, 3] += frameTime * (pose[0, 2] * ILMath.cos(Util.deg2rad(pose[0, 3] * frameTime)) - pose[0, 0] *
                                ILMath.sin(Util.deg2rad(pose[0, 3] * frameTime)));

                        }
                        else
                        {
                            //transform = ILMath.multiply(rotMat, transform);
                            transform[0, 3] += pose[0, 0];
                            transform[1, 3] += pose[0, 1];
                            transform[2, 3] += pose[0, 2];
                        }
                    }

                    // Apply parent transformation.
                    int parent = _skeleton.Joints[i].ParentID;
                    if (parent != -1)
                        transform = ILMath.multiply(transforms.GetArray<double>(parent), transform);

                    transforms[i] = transform.C;
                }
            }
        }

        /// <summary>
        /// Returns the global joint position.
        /// </summary>
        /// <param name="jointID">Joint.</param>
        /// <param name="inPose">Input poses.</param>
        /// <param name="position">Position of the joint.</param>
        /// <param name="useRoot">If the root is involved.</param>
        /// <param name="isRelativeRoot">If the root position is relative.</param>
        /// <param name="jacobian">Jacobian of joint with respect to Y.</param>
        public void JointGlobalPosition(int jointID, ILInArray<double> inPose, ILOutArray<double> position, bool useRoot, bool isRelativeRoot, Representation type, ILOutArray<double> jacobian = null)
        {
            using (ILScope.Enter(inPose))
            {
                ILArray<double> pose = ILMath.check(inPose);

                // Initialize positions using transpose.
                ILArray<double> oldPos = ILMath.zeros(1, 3);
                ILArray<double> norm = ILMath.zeros(1, 1); // Vector used for normalizing quaternions.
                position.a = _skeleton.Joints[jointID].Offset.T;

                // Resize Jacobian.
                if (jacobian != null)
                    jacobian.a = ILMath.zeros(3, pose.S[1]);

                // Check if this is the root.
                if (jointID == 0)
                {
                    // Simply add the position.
                    if (useRoot)
                    {
                        if (isRelativeRoot)
                        {
                            position[1] += pose[0, 1];
                            position[0] += (ILMath.multiplyElem(ILMath.cos(pose[ILMath.full, 3] * (frameTime * ILMath.pi / 180.0)), pose[ILMath.full, 0]) +
                                           ILMath.multiplyElem(ILMath.sin(pose[ILMath.full, 4] * (frameTime * ILMath.pi / 180.0)), pose[ILMath.full, 2])) * frameTime;
                            position[2] += (ILMath.multiplyElem(ILMath.cos(pose[ILMath.full, 3] * (frameTime * ILMath.pi / 180.0)), pose[ILMath.full, 2]) -
                                           ILMath.multiplyElem(ILMath.sin(pose[ILMath.full, 4] * (frameTime * ILMath.pi / 180.0)), pose[ILMath.full, 0])) * frameTime;
                        }
                        else
                        {
                            position[ILMath.full, ILMath.full] += pose[ILMath.full, ILMath.r(0, 3)];
                        }
                    }
                }
                else
                {
                    // Allocate temporaries.
                    ILArray<double> angles = ILMath.zeros(1, 2);
                    ILArray<double> ce = new double[pose.S[0]];
                    ILArray<double> se = new double[pose.S[0]];
                    ILArray<double> rotParent = ILMath.empty();
                    ILArray<double> rot = ILMath.empty();

                    JointGlobalHelper(position, oldPos, angles, ce, se, norm, pose, _skeleton.Joints[jointID].ParentID,
                        useRoot, isRelativeRoot, rotParent, rot, type, jacobian);
                }
            }
        }

        /// <summary>
        /// Helper function for determining joint global position (for quaternion and exponential map input (todo radians)).
        /// </summary>
        /// <param name="position">Output matrix for storing global positions.</param>
        /// <param name="oldPos">Temporary storage for previous position.</param>
        /// <param name="angles">Temporary angles storage.</param>
        /// <param name="ce">Temporary cosine storage.</param>
        /// <param name="se">Temporary sine storage.</param>
        /// <param name="norm">Temporary normalization storage.</param>
        /// <param name="inData">Input poses.</param>
        /// <param name="i">Desired joint index.</param>
        /// <param name="useRoot">Whether to use the root position and rotation.</param>
        /// <param name="isRelativeRoot">If root is relative.</param>
        /// <param name="dPdY">Jacobian of joint with respect to Y.</param>
        /// <param name="rotParent">Storage for parent's rotation matrix.</param>
        /// <param name="rot">Storage for this joint's rotation matrix.</param>
        private void JointGlobalHelper(ILOutArray<double> position, ILOutArray<double> oldPos, ILOutArray<double> angles, ILOutArray<double> ce, ILOutArray<double> se,
            ILOutArray<double> norm, ILInArray<double> inData, int i, bool useRoot, bool isRelativeRoot, ILOutArray<double> rotParent, ILOutArray<double> rot, Representation type, ILOutArray<double> dPdY = null)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);

                // Compute position.
                ILArray<int> order = _skeleton.Joints[i].orderInt;
                ILArray<int> rotInd = _skeleton.Joints[i].rotInd;

                if (!rotInd.IsEmpty)
                {
                    // Rotate by the rotation of this joint.
                    // Compute first index for this joint's rotation.
                    int jntIdx;

                    if (type == Representation.quaternion)
                        jntIdx = (int)((rotInd[0] - 3) / 3) * 4 + 4;
                    else
                        jntIdx = (int)rotInd[0] + 1;

                    // Rotate position by this joint's rotation and compute joint space Jacobian.
                    RotatePosByJoint(position, oldPos, angles, ce, se, norm, data, i, jntIdx, type, dPdY);

                    if (i == 0 && useRoot)
                    {
                        // For the root joint, apply its rotation about the vertical axis.
                        oldPos.a = position.C;
                        if (isRelativeRoot)
                        {
                            position[0] = ILMath.multiplyElem(oldPos[ILMath.full, 0], ILMath.cos(data[ILMath.full, 3] * (frameTime * ILMath.pi / 180.0))) +
                                         ILMath.multiplyElem(oldPos[ILMath.full, 2], ILMath.sin(data[ILMath.full, 3] * (frameTime * ILMath.pi / 180.0)));
                            position[2] = -ILMath.multiplyElem(oldPos[ILMath.full, 0], ILMath.sin(data[ILMath.full, 3] * (frameTime * ILMath.pi / 180.0))) +
                                          ILMath.multiplyElem(oldPos[ILMath.full, 2], ILMath.cos(data[ILMath.full, 3] * (frameTime * ILMath.pi / 180.0)));
                        }
                        else
                        {
                            position[0] = ILMath.multiplyElem(oldPos[ILMath.full, 0], Util.deg2rad(data[ILMath.full, 3])) +
                                         ILMath.multiplyElem(oldPos[ILMath.full, 2], Util.deg2rad(data[ILMath.full, 3]));
                            position[2] = -ILMath.multiplyElem(oldPos[ILMath.full, 0], Util.deg2rad(data[ILMath.full, 3])) +
                                          ILMath.multiplyElem(oldPos[ILMath.full, 2], Util.deg2rad(data[ILMath.full, 3]));
                        }
                    }

                    // Add the offset of this joint.
                    if (i != 0)
                        position[ILMath.full, ILMath.full] += _skeleton.Joints[i].Offset.T;
                    else
                    {
                        position[ILMath.full, 0] += _skeleton.Joints[i].Offset[0];
                        position[ILMath.full, 2] += _skeleton.Joints[i].Offset[2];
                    }

                    // Check if this is the root and optionally add its position.
                    if (i == 0)
                    {
                        oldPos.a = position.C;
                        if (useRoot)
                        {
                            if (isRelativeRoot)
                            {
                                position[1] += data[ILMath.full, 1];
                                position[0] += (ILMath.multiplyElem(data[ILMath.full, 3] * ILMath.cos(frameTime * ILMath.pi / 180.0), data[ILMath.full, 0]) +
                                           (ILMath.multiplyElem(data[ILMath.full, 3] * ILMath.sin(frameTime * ILMath.pi / 180.0), data[ILMath.full, 2]))) * frameTime;
                                position[2] += (ILMath.multiplyElem(data[ILMath.full, 3] * ILMath.cos(frameTime * ILMath.pi / 180.0), data[ILMath.full, 2]) -
                                           (ILMath.multiplyElem(data[ILMath.full, 3] * ILMath.sin(frameTime * ILMath.pi / 180.0), data[ILMath.full, 0]))) * frameTime;
                            }
                            else
                            {
                                position[ILMath.full, ILMath.full] += data[ILMath.full, ILMath.r(0, 3)];
                            }
                        }

                        // Compute rotation matrix if necessary.
                        if (dPdY != null)
                        {
                            // Compute rotation without yaw.
                            RotMatrixFromJoint(norm, data, i, jntIdx, type, rot);

                            // Make sure we don't want yaw.
                            if (useRoot)
                            {
                                // Compute root rotation.
                                rotParent.a = ILMath.eye(3, 3);
                                rotParent[0, 0] = ILMath.cos(Util.deg2rad(frameTime * data[0, 3]));
                                rotParent[0, 2] = ILMath.sin(Util.deg2rad(frameTime * data[0, 3]));
                                rotParent[2, 0] = -ILMath.sin(Util.deg2rad(frameTime * data[0, 3]));
                                rotParent[2, 2] = ILMath.cos(Util.deg2rad(frameTime * data[0, 3]));

                                // Compute global rotation.
                                rot.a = ILMath.multiply(rotParent, rot);

                                // Rotate the Jacobian by the parent.
                                if (type == Representation.quaternion)
                                    dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 4)] = ILMath.multiply(rotParent, dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 4)]);
                                else
                                    dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 3)] = ILMath.multiply(rotParent, dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 3)]);

                                // Compute gradient of root terms.
                                dPdY[ILMath.full, 0] = frameTime * rotParent[ILMath.full, 0];
                                dPdY[ILMath.full, 1] = rotParent[ILMath.full, 1];
                                dPdY[ILMath.full, 2] = frameTime * rotParent[ILMath.full, 2];

                                dPdY[0, 3] = Util.deg2rad(-frameTime * frameTime * ILMath.sin(Util.deg2rad(frameTime * data[0, 3])) * data[0, 0]
                                                          + frameTime * frameTime * ILMath.cos(Util.deg2rad(frameTime * data[0, 3])) * data[0, 2]
                                                          + frameTime * oldPos[0, 2]);
                                dPdY[1, 3] = 0.0;
                                dPdY[2, 3] = Util.deg2rad(-frameTime * frameTime * ILMath.cos(Util.deg2rad(frameTime * data[0, 3])) * data[0, 0]
                                                          - frameTime * frameTime * ILMath.sin(Util.deg2rad(frameTime * data[0, 3])) * data[0, 2]
                                                          - frameTime * oldPos[0, 0]);
                            }
                        }
                    }
                    else
                    {
                        // If this is not the root, recurse.
                        JointGlobalHelper(position, oldPos, angles, ce, se, norm, data, _skeleton.Joints[i].ParentID, useRoot, isRelativeRoot, rotParent, rot, type, dPdY);

                        // Rotate Jacobian if necessary.
                        if (dPdY != null)
                        {
                            // Store parent rotation.
                            rotParent.a = rot.C;

                            // Compute rotation of this joint.
                            RotMatrixFromJoint(norm, data, i, jntIdx, type, rot);

                            // Compute global rotation.
                            rot.a = ILMath.multiply(rotParent, rot);

                            // Rotate the Jacobian by the parent.
                            if (type == Representation.quaternion)
                                dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 4)] = ILMath.multiply(rotParent, dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 4)]);
                            else
                                dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 3)] = ILMath.multiply(rotParent, dPdY[ILMath.r(0, 2), ILMath.r(jntIdx, jntIdx + 3)]);
                        }
                    }
                }
                else
                { // If this joint has no rotation indices, recurse immediately.
                    JointGlobalHelper(position, oldPos, angles, ce, se, norm, data, _skeleton.Joints[i].ParentID, useRoot, isRelativeRoot, rotParent, rot, type, dPdY);
                }
            }
        }

        /// <summary>
        /// Helper function for determining joint global position.
        /// </summary>
        /// <param name="norm">Temporary normalization storage.</param>
        /// <param name="inData">Input poses.</param>
        /// <param name="i">Joint.</param>
        /// <param name="jntIdx">First angle index.</param>
        /// <param name="rot">Rotation matrix to return.</param>
        /// <param name="norm">Temporary normalization storage.</param>
        /// <param name="inData">Input poses.</param>
        /// <param name="i">Desired joint index.</param>
        /// <param name="useRoot">Whether to use the root position and rotation.</param>
        /// <param name="isRelativeRoot">If root is relative.</param>
        /// <param name="dPdY">Jacobian of joint with respect to Y.</param>
        /// <param name="rotParent">Storage for parent's rotation matrix.</param>
        /// <param name="rot">Storage for this joint's rotation matrix.</param>
        private void RotMatrixFromJoint(ILOutArray<double> norm, ILInArray<double> inData, int i, int jntIdx, Representation type, ILOutArray<double> rot)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);

                int xidx, yidx, zidx, widx;

                Quaternion q;

                if (type == Representation.quaternion)
                {
                    // Compute indices.
                    xidx = jntIdx + 0;
                    yidx = jntIdx + 1;
                    zidx = jntIdx + 2;
                    widx = jntIdx + 3;

                    q = new Quaternion((double)data[0, widx], (double)data[0, xidx], (double)data[0, yidx], (double)data[0, zidx]);
                }
                else
                {
                    xidx = jntIdx + 0;
                    yidx = jntIdx + 1;
                    zidx = jntIdx + 2;

                    if (type == Representation.exponential)
                        q = Util.expToQuat(data[0, ILMath.r(xidx, zidx)]);
                    else
                        q = Util.eulerToQuaternion(data[0, ILMath.r(xidx, zidx)], _skeleton.Joints[i].order);
                }

                // Compute quaternion normalization.
                norm[0] = Quaternion.Norm(q);

                // Special handling for root.
                if (i == 0)
                {
                    // Compute u, v, and w (see: Quaternion.getPitch(), Quaternion.getRoll()).
                    double a = (double)((2.0 / norm[0]) * (q.w * q.z + q.y * q.x));
                    double b = (double)((2.0 / norm[0]) * (q.w * q.x - q.y * q.z));
                    double c = (double)((1.0 / norm[0]) * (ILMath.pow(q.w, 2) - ILMath.pow(q.x, 2) + ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2)));

                    // Compute sines and cosines.
                    double c0 = (double)ILMath.sqrt(1.0 - ILMath.pow(a, 2));
                    double s0 = a;
                    double c1 = c / (double)ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2));
                    double s1 = b / (double)ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2));

                    // The first matrix leaves x alone.
                    ILArray<double> r1 = ILMath.zeros(3, 3);
                    r1[0, 0] = 1.0;
                    r1[1, 0] = 0.0;
                    r1[2, 0] = 0.0;
                    r1[0, 1] = 0.0;
                    r1[1, 1] = c1;
                    r1[2, 1] = s1;
                    r1[0, 2] = 0.0;
                    r1[1, 2] = -s1;
                    r1[2, 2] = c1;

                    // The second matrix leaves z alone.
                    ILArray<double> r0 = ILMath.zeros(3, 3);
                    r0[0, 0] = c0;
                    r0[1, 0] = s0;
                    r0[2, 0] = 0.0;
                    r0[0, 1] = -s0;
                    r0[1, 1] = c0;
                    r0[2, 1] = 0.0;
                    r0[0, 2] = 0.0;
                    r0[1, 2] = 0.0;
                    r0[2, 2] = 1.0;

                    // Compute result.
                    rot.a = ILMath.multiply(r0, r1);
                }
                else
                { 
                    // Simply convert the quaternion to a matrix.
                    rot[0, 0] = (ILMath.pow(q.x, 2) - ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2) + ILMath.pow(q.w, 2)) / norm[0];
                    rot[1, 0] = (2.0 * q.x * q.y + 2.0 * q.z * q.w) / norm[0];
                    rot[2, 0] = (2.0 * q.x * q.z - 2.0 * q.y * q.w) / norm[0];

                    rot[0, 1] = (2.0 * q.x * q.y - 2.0 * q.z * q.w) / norm[0];
                    rot[1, 1] = (-ILMath.pow(q.x, 2) + ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2) + ILMath.pow(q.w, 2)) / norm[0];
                    rot[2, 1] = (2.0 * q.y * q.z + 2.0 * q.x * q.w) / norm[0];

                    rot[0, 2] = (2.0 * q.x * q.z + 2.0 * q.y * q.w) / norm[0];
                    rot[1, 2] = (2.0 * q.y * q.z - 2.0 * q.x * q.w) / norm[0];
                    rot[2, 2] = (-ILMath.pow(q.x, 2) - ILMath.pow(q.y, 2) + ILMath.pow(q.z, 2) + ILMath.pow(q.w, 2)) / norm[0];
                }
            }
        }

        /// <summary>
        /// Rotates position by a joint.
        /// </summary>
        /// <param name="globPos">Output matrix for storing global positions.</param>
        /// <param name="oldPos">Temporary storage for previous position.</param>
        /// <param name="angles">Temporary angles storage.</param>
        /// <param name="ce">Temporary cosine storage.</param>
        /// <param name="se">Temporary sine storage.</param>
        /// <param name="norm">Temporary normalization storage.</param>
        /// <param name="inData">Input poses in quatermions [x y z w].</param>
        /// <param name="i">Joint.</param>
        /// <param name="jntIdx">First angle index.</param>
        /// <param name="dPdY">Jacobian of joint with respect to Y.</param>
        private void RotatePosByJoint(ILOutArray<double> globPos, ILOutArray<double> oldPos, ILOutArray<double> angles, ILOutArray<double> ce, ILOutArray<double> se,
            ILOutArray<double> norm, ILInArray<double> inData, int i, int jntIdx, Representation type, ILOutArray<double> dPdY = null)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);

                int xidx, yidx, zidx, widx = 0;

                Quaternion q;
                ILArray<double> v = ILMath.empty();

                if (type == Representation.quaternion)
                {
                    // Compute indices.
                    xidx = jntIdx + 0;
                    yidx = jntIdx + 1;
                    zidx = jntIdx + 2;
                    widx = jntIdx + 3;

                    q = new Quaternion((double)data[0, widx], (double)data[0, xidx], (double)data[0, yidx], (double)data[0, zidx]);
                }
                else
                {
                    // Compute indices.
                    xidx = jntIdx + 0;
                    yidx = jntIdx + 1;
                    zidx = jntIdx + 2;

                    if (type == Representation.exponential)
                    {
                        q = Util.expToQuat(data[0, ILMath.r(xidx, zidx)]);
                        v = data[0, ILMath.r(xidx, zidx)];
                    }
                    else
                        q = Util.eulerToQuaternion(data[0, ILMath.r(xidx, zidx)], _skeleton.Joints[i].order);
                }

                oldPos.a = globPos.C;

                // Compute quaternion normalization.
                norm[0] = Quaternion.Norm(q);

                if (i == 0)
                {
                    // Compute the Jacobian.
                    if (dPdY != null)
                    {
                        // Compute u, v, and w (see: Quaternion.getPitch(), Quaternion.getRoll()).
                        double a = (double)((2.0 / norm[0]) * (q.w * q.z + q.y * q.x));
                        double b = (double)((2.0 / norm[0]) * (q.w * q.x - q.y * q.z));
                        double c = (double)((1.0 / norm[0]) * (ILMath.pow(q.w, 2) - ILMath.pow(q.x, 2) + ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2)));

                        // Compute sines and cosines.
                        double c0 = (double)ILMath.sqrt(1.0 - ILMath.pow(a, 2));
                        double s0 = a;
                        double c1 = c / (double)ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2));
                        double s1 = b / (double)ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2));

                        // Gradient of normalizing factor.
                        double dNormdx, dNormdy, dNormdz, dNormdw = 0;

                        // Gradients of a, b, c with respect to data and normalizing factor.
                        double dadnorm, dbdnorm, dcdnorm, dadx = 0, dady = 0, dadz = 0, dadw = 0, dbdx = 0, dbdy = 0, dbdz = 0, dbdw = 0, dcdx = 0, dcdy = 0, dcdz = 0, dcdw = 0;

                        switch (type)
                        {
                            case Representation.quaternion:
                                // Compute gradient of normalizing factor.
                                dNormdx = 2.0 * q.x;
                                dNormdy = 2.0 * q.y;
                                dNormdz = 2.0 * q.z;
                                dNormdw = 2.0 * q.w;

                                // Compute the gradients of a, b, c with respect to data and normalizing factor.
                                dadnorm = -a / (double)norm[0, 0];
                                dbdnorm = -b / (double)norm[0, 0];
                                dcdnorm = -c / (double)norm[0, 0];
                                dadx = (double)((2.0 / norm[0, 0]) * q.y) + dadnorm * dNormdx;
                                dady = (double)((2.0 / norm[0, 0]) * q.x) + dadnorm * dNormdy;
                                dadz = (double)((2.0 / norm[0, 0]) * q.w) + dadnorm * dNormdz;
                                dadw = (double)((2.0 / norm[0, 0]) * q.z) + dadnorm * dNormdw;
                                dbdx = (double)((2.0 / norm[0, 0]) * q.w) + dbdnorm * dNormdx;
                                dbdy = (double)((-2.0 / norm[0, 0]) * q.z) + dbdnorm * dNormdy;
                                dbdz = (double)((-2.0 / norm[0, 0]) * q.y) + dbdnorm * dNormdz;
                                dbdw = (double)((2.0 / norm[0, 0]) * q.x) + dbdnorm * dNormdw;
                                dcdx = (double)((-2.0 / norm[0, 0]) * q.x) + dcdnorm * dNormdx;
                                dcdy = (double)((2.0 / norm[0, 0]) * q.y) + dcdnorm * dNormdy;
                                dcdz = (double)((-2.0 / norm[0, 0]) * q.z) + dcdnorm * dNormdz;
                                dcdw = (double)((2.0 / norm[0, 0]) * q.w) + dcdnorm * dNormdw;
                                break;

                            case Representation.exponential:
                                // Compute gradient for exponentials 
                                Quaternion dqdx = Partial_Q_Partial_3V(v, 0);
                                Quaternion dqdy = Partial_Q_Partial_3V(v, 1);
                                Quaternion dqdz = Partial_Q_Partial_3V(v, 2);

                                // Compute gradient of normalizing factor.
                                dNormdx = 2.0 * (q.x * dqdx.x + q.y * dqdx.y + q.z * dqdx.z + q.w * dqdx.w);
                                dNormdy = 2.0 * (q.y * dqdy.y + q.y * dqdy.y + q.z * dqdy.z + q.w * dqdy.w);
                                dNormdz = 2.0 * (q.z * dqdz.z + q.y * dqdz.y + q.z * dqdz.z + q.w * dqdz.w);

                                // Compute the gradients of a, b, c with respect to data and normalizing factor.
                                dadnorm = -a / (double)norm[0];
                                dbdnorm = -b / (double)norm[0];
                                dcdnorm = -c / (double)norm[0];
                                dadx = (double)((2.0 / norm[0]) * (q.y * dqdx.x + q.x * dqdx.y + q.w * dqdx.z + q.z * dqdx.w)) + dadnorm * dNormdx;
                                dady = (double)((2.0 / norm[0]) * (q.x * dqdy.y + q.y * dqdy.x + q.w * dqdy.z + q.z * dqdy.w)) + dadnorm * dNormdy;
                                dadz = (double)((2.0 / norm[0]) * (q.x * dqdz.y + q.y * dqdz.x + q.w * dqdz.z + q.z * dqdz.w)) + dadnorm * dNormdz;

                                dbdx = (double)((2.0 / norm[0]) * (q.w * dqdx.x + q.x * dqdx.w - q.y * dqdx.z - q.z * dqdx.y)) + dbdnorm * dNormdx;
                                dbdy = (double)((2.0 / norm[0]) * (q.w * dqdy.x + q.x * dqdy.w - q.y * dqdy.z - q.z * dqdy.y)) + dbdnorm * dNormdy;
                                dbdz = (double)((2.0 / norm[0]) * (q.w * dqdz.x + q.x * dqdz.w - q.y * dqdz.z - q.z * dqdz.y)) + dbdnorm * dNormdz;

                                dcdx = (double)((2.0 / norm[0]) * (-q.x * dqdx.x + q.y * dqdx.y - q.z * dqdx.z + q.w * dqdx.w)) + dcdnorm * dNormdx;
                                dcdy = (double)((2.0 / norm[0]) * (-q.x * dqdy.x + q.y * dqdy.y - q.z * dqdy.z + q.w * dqdy.w)) + dcdnorm * dNormdy;
                                dcdz = (double)((2.0 / norm[0]) * (-q.x * dqdz.x + q.y * dqdz.y - q.z * dqdz.z + q.w * dqdz.w)) + dcdnorm * dNormdz;
                                break;

                            case Representation.radian:
                                //Todo
                                break;
                        }

                        // Compute the gradients of the sines and cosines with respect to a, b, c.
                        double dc0da = -a / (double)ILMath.sqrt(1.0 - ILMath.pow(a, 2));
                        double ds0da = 1;
                        double dc1dc = (double)(ILMath.pow(b, 2) / ILMath.pow(ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2)), 3));
                        double dc1db = -b * c / (double)(ILMath.pow(ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2)), 3));
                        double ds1dc = -b * c / (double)(ILMath.pow(ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2)), 3));
                        double ds1db = (double)(ILMath.pow(c, 2) / ILMath.pow(ILMath.sqrt(ILMath.pow(b, 2) + ILMath.pow(c, 2)), 3));

                        // Compute the gradients of the positions with respect to sines and cosines.
                        double dpxdc0 = (double)oldPos[0, 0];
                        double dpxds0 = -c1 * (double)oldPos[0, 1] + s1 * (double)oldPos[0, 2];
                        double dpxdc1 = -s0 * (double)oldPos[0, 1];
                        double dpxds1 = s0 * (double)oldPos[0, 2];
                        double dpydc0 = c1 * (double)oldPos[0, 1] - s1 * (double)oldPos[0, 2];
                        double dpyds0 = (double)oldPos[0, 0];
                        double dpydc1 = c0 * (double)oldPos[0, 1];
                        double dpyds1 = -c0 * (double)oldPos[0, 2];
                        double dpzdc1 = (double)oldPos[0, 2];
                        double dpzds1 = (double)oldPos[0, 1];

                        if (type == Representation.quaternion)
                        {
                            // Use chain rule to compute final gradients.
                            dPdY[0, xidx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dadx + dpxdc1 * (dc1db * dbdx + dc1dc * dcdx) + dpxds1 * (ds1db * dbdx + ds1dc * dcdx);
                            dPdY[0, yidx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dady + dpxdc1 * (dc1db * dbdy + dc1dc * dcdy) + dpxds1 * (ds1db * dbdy + ds1dc * dcdy);
                            dPdY[0, zidx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dadz + dpxdc1 * (dc1db * dbdz + dc1dc * dcdz) + dpxds1 * (ds1db * dbdz + ds1dc * dcdz);
                            dPdY[0, widx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dadw + dpxdc1 * (dc1db * dbdw + dc1dc * dcdw) + dpxds1 * (ds1db * dbdw + ds1dc * dcdw);
                            dPdY[1, xidx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dadx + dpydc1 * (dc1db * dbdx + dc1dc * dcdx) + dpyds1 * (ds1db * dbdx + ds1dc * dcdx);
                            dPdY[1, yidx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dady + dpydc1 * (dc1db * dbdy + dc1dc * dcdy) + dpyds1 * (ds1db * dbdy + ds1dc * dcdy);
                            dPdY[1, zidx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dadz + dpydc1 * (dc1db * dbdz + dc1dc * dcdz) + dpyds1 * (ds1db * dbdz + ds1dc * dcdz);
                            dPdY[1, widx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dadw + dpydc1 * (dc1db * dbdw + dc1dc * dcdw) + dpyds1 * (ds1db * dbdw + ds1dc * dcdw);
                            dPdY[2, xidx] = dpzdc1 * (dc1db * dbdx + dc1dc * dcdx) + dpzds1 * (ds1db * dbdx + ds1dc * dcdx);
                            dPdY[2, yidx] = dpzdc1 * (dc1db * dbdy + dc1dc * dcdy) + dpzds1 * (ds1db * dbdy + ds1dc * dcdy);
                            dPdY[2, zidx] = dpzdc1 * (dc1db * dbdz + dc1dc * dcdz) + dpzds1 * (ds1db * dbdz + ds1dc * dcdz);
                            dPdY[2, widx] = dpzdc1 * (dc1db * dbdw + dc1dc * dcdw) + dpzds1 * (ds1db * dbdw + ds1dc * dcdw);
                        }
                        else
                        {
                            // Use chain rule to compute final gradients.
                            dPdY[0, xidx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dadx + dpxdc1 * (dc1db * dbdx + dc1dc * dcdx) + dpxds1 * (ds1db * dbdx + ds1dc * dcdx);
                            dPdY[0, yidx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dady + dpxdc1 * (dc1db * dbdy + dc1dc * dcdy) + dpxds1 * (ds1db * dbdy + ds1dc * dcdy);
                            dPdY[0, zidx] = (dpxdc0 * dc0da + dpxds0 * ds0da) * dadz + dpxdc1 * (dc1db * dbdz + dc1dc * dcdz) + dpxds1 * (ds1db * dbdz + ds1dc * dcdz);

                            dPdY[1, xidx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dadx + dpydc1 * (dc1db * dbdx + dc1dc * dcdx) + dpyds1 * (ds1db * dbdx + ds1dc * dcdx);
                            dPdY[1, yidx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dady + dpydc1 * (dc1db * dbdy + dc1dc * dcdy) + dpyds1 * (ds1db * dbdy + ds1dc * dcdy);
                            dPdY[1, zidx] = (dpydc0 * dc0da + dpyds0 * ds0da) * dadz + dpydc1 * (dc1db * dbdz + dc1dc * dcdz) + dpyds1 * (ds1db * dbdz + ds1dc * dcdz);

                            dPdY[2, xidx] = dpzdc1 * (dc1db * dbdx + dc1dc * dcdx) + dpzds1 * (ds1db * dbdx + ds1dc * dcdx);
                            dPdY[2, yidx] = dpzdc1 * (dc1db * dbdy + dc1dc * dcdy) + dpzds1 * (ds1db * dbdy + ds1dc * dcdy);
                            dPdY[2, zidx] = dpzdc1 * (dc1db * dbdz + dc1dc * dcdz) + dpzds1 * (ds1db * dbdz + ds1dc * dcdz);
                        }
                    }

                    // If this is the root, pull out non-yaw Euler angles.
                    angles[0, 0] = ILMath.asin(2.0 * ((q.w * q.z + q.y * q.x) / norm));

                    angles[0, 1] = ILMath.atan2(2.0 * (q.w * q.x - q.y * q.z) / norm[0],
                        (ILMath.pow(q.w, 2) - ILMath.pow(q.x, 2) + ILMath.pow(q.y, 2) - ILMath.pow(q.z, 2)) / norm[0]);

                    // Apply roll about X axis.
                    ce.a = ILMath.cos(angles[ILMath.full, 1]);
                    se.a = ILMath.sin(angles[ILMath.full, 1]);
                    globPos[ILMath.full, 1] = ILMath.multiplyElem(oldPos[ILMath.full, 1], ce) - ILMath.multiplyElem(oldPos[ILMath.full, 2], se);
                    globPos[ILMath.full, 2] = ILMath.multiplyElem(oldPos[ILMath.full, 1], se) + ILMath.multiplyElem(oldPos[ILMath.full, 2], ce);

                    // Apply pitch about Z axis.
                    ce.a = ILMath.cos(angles[ILMath.full, 0]);
                    se.a = ILMath.sin(angles[ILMath.full, 0]);
                    oldPos.a = globPos;
                    globPos[ILMath.full, 0] = ILMath.multiplyElem(oldPos[ILMath.full, 0], ce) - ILMath.multiplyElem(oldPos[ILMath.full, 1], se);
                    globPos[ILMath.full, 1] = ILMath.multiplyElem(oldPos[ILMath.full, 0], se) + ILMath.multiplyElem(oldPos[ILMath.full, 1], ce);

                }
                else
                {
                    // Rotate vector by quaternion.
                    globPos[ILMath.full, 0] = ILMath.divide(ILMath.multiplyElem(oldPos[ILMath.full, 0], norm - 2.0 * ILMath.pow(q.y, 2) - 2.0 * ILMath.pow(q.z, 2)) +
                                    ILMath.multiplyElem(oldPos[ILMath.full, 1], 2.0 * ILMath.multiplyElem(q.x, q.y) - 2.0 * ILMath.multiplyElem(q.z, q.w)) +
                                    ILMath.multiplyElem(oldPos[ILMath.full, 2], 2.0 * ILMath.multiplyElem(q.x, q.z) + 2.0 * ILMath.multiplyElem(q.y, q.w)), norm[0]);

                    globPos[ILMath.full, 1] = ILMath.divide(ILMath.multiplyElem(oldPos[ILMath.full, 0], 2.0 * ILMath.multiplyElem(q.x, q.y) + 2.0 * ILMath.multiplyElem(q.z, q.w)) +
                                    ILMath.multiplyElem(oldPos[ILMath.full, 1], norm - 2.0 * (ILMath.pow(q.x, 2)) - 2.0 * (ILMath.pow(q.z, 2))) +
                                    ILMath.multiplyElem(oldPos[ILMath.full, 2], 2.0 * ILMath.multiplyElem(q.y, q.z) - 2.0 * ILMath.multiplyElem(q.x, q.w)), norm[0]);

                    globPos[ILMath.full, 2] = ILMath.divide(ILMath.multiplyElem(oldPos[ILMath.full, 0], 2.0 * ILMath.multiplyElem(q.x, q.z) - 2.0 * ILMath.multiplyElem(q.y, q.w)) +
                                    ILMath.multiplyElem(oldPos[ILMath.full, 1], 2.0 * ILMath.multiplyElem(q.y, q.z) + 2.0 * ILMath.multiplyElem(q.x, q.w)) +
                                    ILMath.multiplyElem(oldPos[ILMath.full, 2], norm - 2.0 * (ILMath.pow(q.x, 2)) - 2.0 * (ILMath.pow(q.y, 2))), norm[0]);

                    // Now compute the Jacobian.
                    if (dPdY != null)
                    {
                        switch (type)
                        {
                            case Representation.quaternion:
                                dPdY[0, xidx] = (-2.0 / norm[0]) * q.x * globPos[0, 0] + (2.0 / norm[0]) * (q.x * oldPos[0, 0] + q.y * oldPos[0, 1] + q.z * oldPos[0, 2]);
                                dPdY[0, yidx] = (-2.0 / norm[0]) * q.y * globPos[0, 0] + (2.0 / norm[0]) * (-q.y * oldPos[0, 0] + q.x * oldPos[0, 1] + q.w * oldPos[0, 2]);
                                dPdY[0, zidx] = (-2.0 / norm[0]) * q.z * globPos[0, 0] + (2.0 / norm[0]) * (-q.z * oldPos[0, 0] - q.w * oldPos[0, 1] + q.x * oldPos[0, 2]);
                                dPdY[0, widx] = (-2.0 / norm[0]) * q.w * globPos[0, 0] + (2.0 / norm[0]) * (q.w * oldPos[0, 0] - q.z * oldPos[0, 1] + q.y * oldPos[0, 2]);

                                dPdY[1, xidx] = (-2.0 / norm[0]) * q.x * globPos[0, 1] + (2.0 / norm[0]) * (q.y * oldPos[0, 0] - q.x * oldPos[0, 1] - q.w * oldPos[0, 2]);
                                dPdY[1, yidx] = (-2.0 / norm[0]) * q.y * globPos[0, 1] + (2.0 / norm[0]) * (q.x * oldPos[0, 0] + q.y * oldPos[0, 1] + q.z * oldPos[0, 2]);
                                dPdY[1, zidx] = (-2.0 / norm[0]) * q.z * globPos[0, 1] + (2.0 / norm[0]) * (q.w * oldPos[0, 0] - q.z * oldPos[0, 1] + q.y * oldPos[0, 2]);
                                dPdY[1, widx] = (-2.0 / norm[0]) * q.w * globPos[0, 1] + (2.0 / norm[0]) * (q.z * oldPos[0, 0] + q.w * oldPos[0, 1] - q.x * oldPos[0, 2]);

                                dPdY[2, xidx] = (-2.0 / norm[0]) * q.x * globPos[0, 2] + (2.0 / norm[0]) * (q.z * oldPos[0, 0] + q.w * oldPos[0, 1] - q.x * oldPos[0, 2]);
                                dPdY[2, yidx] = (-2.0 / norm[0]) * q.y * globPos[0, 2] + (2.0 / norm[0]) * (-q.w * oldPos[0, 0] + q.z * oldPos[0, 1] - q.y * oldPos[0, 2]);
                                dPdY[2, zidx] = (-2.0 / norm[0]) * q.z * globPos[0, 2] + (2.0 / norm[0]) * (q.x * oldPos[0, 0] + q.y * oldPos[0, 1] + q.z * oldPos[0, 2]);
                                dPdY[2, widx] = (-2.0 / norm[0]) * q.w * globPos[0, 2] + (2.0 / norm[0]) * (-q.y * oldPos[0, 0] + q.x * oldPos[0, 1] + q.w * oldPos[0, 2]);
                                break;

                            case Representation.exponential:
                                Matrix3 mat;

                                // Compute gradient for exponentials 
                                Quaternion dqdx = Partial_Q_Partial_3V(v, 0);
                                Quaternion dqdy = Partial_Q_Partial_3V(v, 1);
                                Quaternion dqdz = Partial_Q_Partial_3V(v, 2);

                                // Compute gradient of normalizing factor.
                                double dNormdx = 2.0 * (q.x * dqdx.x + q.y * dqdx.y + q.z * dqdx.z + q.w * dqdx.w);
                                double dNormdy = 2.0 * (q.y * dqdy.y + q.y * dqdy.y + q.z * dqdy.z + q.w * dqdy.w);
                                double dNormdz = 2.0 * (q.z * dqdz.z + q.y * dqdz.y + q.z * dqdz.z + q.w * dqdz.w);

                                // going through the indexes
                                // first x
                                mat = Partial_R_Partial_EM3(v, 0);
                                dPdY[0, xidx] = (-1.0 / norm[0]) * dNormdx * globPos[0, 0] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[0, 0] + oldPos[1] * mat.Matrix[0, 1] + oldPos[2] * mat.Matrix[0, 2]);
                                dPdY[1, xidx] = (-1.0 / norm[0]) * dNormdx * globPos[0, 1] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[1, 0] + oldPos[1] * mat.Matrix[1, 1] + oldPos[2] * mat.Matrix[1, 2]);
                                dPdY[2, xidx] = (-1.0 / norm[0]) * dNormdx * globPos[0, 2] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[2, 0] + oldPos[1] * mat.Matrix[2, 1] + oldPos[2] * mat.Matrix[2, 2]);

                                // second y
                                mat = Partial_R_Partial_EM3(v, 1);
                                dPdY[0, yidx] = (-1.0 / norm[0]) * dNormdy * globPos[0, 0] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[0, 0] + oldPos[1] * mat.Matrix[0, 1] + oldPos[2] * mat.Matrix[0, 2]);
                                dPdY[1, yidx] = (-1.0 / norm[0]) * dNormdy * globPos[0, 1] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[1, 0] + oldPos[1] * mat.Matrix[1, 1] + oldPos[2] * mat.Matrix[1, 2]);
                                dPdY[2, yidx] = (-1.0 / norm[0]) * dNormdy * globPos[0, 2] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[2, 0] + oldPos[1] * mat.Matrix[2, 1] + oldPos[2] * mat.Matrix[2, 2]);

                                // third z
                                mat = Partial_R_Partial_EM3(v, 2);
                                dPdY[0, zidx] = (-1.0 / norm[0]) * dNormdz * globPos[0, 0] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[0, 0] + oldPos[1] * mat.Matrix[0, 1] + oldPos[2] * mat.Matrix[0, 2]);
                                dPdY[1, zidx] = (-1.0 / norm[0]) * dNormdz * globPos[0, 1] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[1, 0] + oldPos[1] * mat.Matrix[1, 1] + oldPos[2] * mat.Matrix[1, 2]);
                                dPdY[2, zidx] = (-1.0 / norm[0]) * dNormdz * globPos[0, 2] + (1.0 / norm[0]) * (oldPos[0] * mat.Matrix[2, 0] + oldPos[1] * mat.Matrix[2, 1] + oldPos[2] * mat.Matrix[2, 2]);
                                break;

                            case Representation.radian:
                                //Todo
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 'Partial_R_Partial_EM3'Compute the i'th partial derivative of
        /// the rotation matrix with respect to EM parameter 'v', storing result
        /// in 'dRdvi'.  If 'v' is near a singularity, it will be dynamically
        /// reparameterized in place and the value 1 is returned; otherwise,
        /// 0 is returned.
        /// </summary>
        /// <param name="v">EM vector 'v'.</param>
        /// <param name="i">The i'th element of 'v'.</param>
        /// <returns>Partial derivative of the rotation matrix with respect to EM parameter 'v'.</returns>
        private Matrix3 Partial_R_Partial_EM3(ILArray<double> v, int i)
        {
            Quaternion q = Util.expToQuat(v);

            Quaternion dqdvi = Partial_Q_Partial_3V(v, i);
            return Partial_R_Partial_Vi(q, dqdvi);
        }

        /// <summary>
        /// Partial derivative of quaternion wrt i'th component of EM vector 'v'.
        /// </summary>
        /// <param name="v">EM vector 'v'.</param>
        /// <param name="i">Index of 'v'.</param>
        /// <returns>Partial derivative of quaternion wrt i'th component of EM vector 'v'.</returns>
        private Quaternion Partial_Q_Partial_3V(ILArray<double> v, int i)
        {
            double theta = (double)ILMath.sqrt(ILMath.sum(ILMath.multiplyElem(v, v), 1));
            //theta = (double)Util.deg2rad(theta);

            double cosp = (double)ILMath.cos(.5 * theta), sinp = (double)ILMath.sin(.5 * theta);
            ILArray<double> dqdx = ILMath.zeros(1,4);

            /* This is an efficient implementation of the derivatives given
             * in Appendix A of the paper with common subexpressions factored out */
            if (theta < MIN_ANGLE)
            {
	            int i2 = (i+1) % 3, i3 = (i+2) % 3;
	            double Tsinc = 0.5 - theta * theta / 48.0;
	            double vTerm = (double)(v[i] * (theta * theta / 40.0 - 1.0) / 24.0);
	
	            dqdx[3] = -.5*v[i] * Tsinc;
	            dqdx[i]  = v[i] * vTerm + Tsinc;
	            dqdx[i2] = v[i2] * vTerm;
	            dqdx[i3] = v[i3] * vTerm;
            }
            else
            {
	            int i2 = (i+1) % 3, i3 = (i + 2) % 3;
	            double  ang = 1.0/theta, ang2 = (double)(ang * ang * v[i]), sang = sinp * ang;
	            double  cterm = ang2 * (.5 * cosp - sang);
	
	            dqdx[i]  = cterm * v[i] + sang;
	            dqdx[i2] = cterm * v[i2];
	            dqdx[i3] = cterm * v[i3];
	            dqdx[3] = -.5*v[i] * sang;
            }

            return new Quaternion((double)dqdx[3], (double)dqdx[0], (double)dqdx[1], (double)dqdx[2]);
        }

        /// <summary>
        /// 'Partial_R_Partial_Vi' Given a quaternion 'q' computed from the 
        /// current 3 degree of freedom EM vector 'v', and the partial
        /// derivative of the quaternion with respect to the i'th element of
        /// 'v' in 'dqdvi' (computed using 'Partial_Q_Partial_3V' or
        /// 'Partial_Q_Partial_2V'), compute and store in 'dRdvi' the i'th
        /// partial derivative of the rotation matrix 'R' with respect to the
        /// i'th element of 'v'.
        /// </summary>
        /// <param name="q">Given quaternion.</param>
        /// <param name="dqdvi">Derivative of the quaternion with respect to the i'th element of 'v'.</param>
        /// <returns>Partial derivative of the rotation matrix 'R' with respect to the i'th element of 'v'.</returns>
        private Matrix3 Partial_R_Partial_Vi(Quaternion q, Quaternion dqdvi)
        {
            ILArray<double> prod = ILMath.zeros(1, 10);

            /* This efficient formulation is arrived at by writing out the
             * entire chain rule product dRdq * dqdv in terms of 'q' and 
             * noticing that all the entries are formed from sums of just
             * nine products of 'q' and 'dqdv' */
            prod[0] = 2 * q.x * dqdvi.x;
            prod[1] = 2 * q.y * dqdvi.y;
            prod[2] = 2 * q.z * dqdvi.z;
            prod[3] = 2 * q.w * dqdvi.w;
            prod[4] = 2 * (q.y * dqdvi.x + q.x * dqdvi.y);
            prod[5] = 2 * (q.w * dqdvi.z + q.z * dqdvi.w);
            prod[6] = 2 * (q.z * dqdvi.x + q.x * dqdvi.z);
            prod[7] = 2 * (q.w * dqdvi.y + q.y * dqdvi.w);
            prod[8] = 2 * (q.z * dqdvi.y + q.y * dqdvi.z);
            prod[9] = 2 * (q.w * dqdvi.x + q.x * dqdvi.w);

            Matrix3 dRdvi = new Matrix3();

            /* first row, followed by second and third */
            dRdvi.Matrix[0, 0] = prod[0] - prod[1] - prod[2] + prod[3];
            dRdvi.Matrix[0, 1] = prod[4] - prod[5];
            dRdvi.Matrix[0, 2] = prod[6] + prod[7];

            dRdvi.Matrix[1, 0] = prod[4] + prod[5];
            dRdvi.Matrix[1, 1] = -prod[0] + prod[1] - prod[2] + prod[3];
            dRdvi.Matrix[1, 2] = prod[8] - prod[9];

            dRdvi.Matrix[2, 0] = prod[6] - prod[7];
            dRdvi.Matrix[2, 1] = prod[8] + prod[9];
            dRdvi.Matrix[2, 2] = -prod[0] - prod[1] + prod[2] + prod[3];

            return dRdvi;
        }
    }
}
