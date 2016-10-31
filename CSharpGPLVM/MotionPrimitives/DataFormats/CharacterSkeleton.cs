using System;
using System.Collections.Generic;
using System.Xml;
using System.Runtime.Serialization;

namespace MotionPrimitives.DataFormats
{
    [DataContract(IsReference = true)]
    public class CharacterSkeleton
    {
        [DataMember()]
        private List<CharacterJoint> joints = new List<CharacterJoint>();

        public List<CharacterJoint> Joints
        {
            get { return joints; }
        }

        public void AddJoint(BVHNode node)
        {
            var joint = new CharacterJoint();

            joint.Name = node.Name;
            joint.ID = node.ID;
            joint.ParentID = node.ParentID;
            joint.RawOffset = node.Offset;
            joint.RawRotOrder = node.RotOrder;

            joint.RawRotOrderInt[0] = (int)node.RotOrderInt[0];
            joint.RawRotOrderInt[1] = (int)node.RotOrderInt[1];
            joint.RawRotOrderInt[2] = (int)node.RotOrderInt[2];

            if (!node.PosInd.IsEmpty)
            {
                joint.RawPosInd = new int[3];
                joint.RawPosInd = node.PosInd.C;
            }

            if (!node.RotInd.IsEmpty)
            {
                joint.RawRotInd = new int[3];
                joint.RawRotInd = node.RotInd.C;
            }

            joints.Add(joint);
        }
    }
}
