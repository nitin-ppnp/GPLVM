using System.Collections.Generic;
using System.Xml;
using System.Runtime.Serialization;
using DataFormats;

namespace GPLVM.Utils.Character
{
    [DataContract(IsReference = true)]
    public class Skeleton
    {
        [DataMember()]
        private List<Joint> joints = new List<Joint>();

        public List<Joint> Joints
        {
            get { return joints; }
        }

        public void AddJoint(BVHNode node)
        {
            Joint joint = new Joint();

            joint.Name = node.Name;
            joint.ID = node.ID;
            joint.ParentID = node.ParentID;
            joint.Offset.a = node.Offset.C;
            joint.order = node.order;

            joint.orderInt[0] = (int)node.orderInt[0];
            joint.orderInt[1] = (int)node.orderInt[1];
            joint.orderInt[2] = (int)node.orderInt[2];

            if (!node.posInd.IsEmpty)
            {
                joint.posInd.a = node.posInd.C;
            }

            if (!node.rotInd.IsEmpty)
            {
                joint.rotInd.a = node.rotInd.C;
            }

            joints.Add(joint);
        }

        public void Read(ref XmlReader reader)
        {
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Read();
                if (reader.Name == "SkeletonTree")
                {
                    joints = new List<Joint>();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "Entry")
                        {
                            joints.Add(new Joint());
                            joints[joints.Count - 1].Read(ref reader);
                        }
                    }
                    reader.Read();
                }
            }
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            if (joints != null)
            {
                writer.WriteStartElement("Skeleton", null);
                foreach (Joint entry in joints)
                    entry.Write(ref writer);
                writer.WriteEndElement();
            }
        }
    }
}
