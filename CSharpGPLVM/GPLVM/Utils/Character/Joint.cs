using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Utils.Character
{
    [DataContract(IsReference = true)]
    public class Joint
    {
        [DataMember()]
        public string Name;
        [DataMember()]
        public int ParentID;
        [DataMember()]
        public int ID;
        [DataMember()]
        public ILArray<double> Offset = ILMath.localMember<double>();
        [DataMember()]
        public ILArray<int> rotInd = ILMath.localMember<int>();
        [DataMember()]
        public ILArray<int> posInd = ILMath.localMember<int>();
        [DataMember()]
        public string order = null;
        [DataMember()]
        public ILArray<int> orderInt = ILMath.localMember<int>();

        public void SwitchOrder(ILArray<int> newOrder)
        {
            if (order != null)
            {
                // Rearrange rotInd.
                ILArray<int> newInd = new int[3];
                char[] newOrd = new char[3];
                for (int k = 0; k < 3; k++)
                {
                    switch ((int)newOrder[k])
                    {
                        case 0:
                            newOrd[k] = 'x';
                            break;
                        case 1:
                            newOrd[k] = 'y';
                            break;
                        case 2:
                            newOrd[k] = 'z';
                            break;
                    }
                }

                // Switch to new order.
                order = new string(newOrd);
                orderInt[0] = (int)newOrder[0].C;
                orderInt[1] = (int)newOrder[1].C;
                orderInt[2] = (int)newOrder[2].C;
            }
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("Entry", null);
            writer.WriteElementString("Name", Name);
            writer.WriteElementString("ID", ID.ToString());
            writer.WriteElementString("ParentID", ParentID.ToString());

            writer.WriteStartElement("Offset");
            writer.WriteAttributeString("data", Offset[0].ToString() + " " + Offset[1].ToString() + " " + Offset[2].ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("RotInd");
            writer.WriteAttributeString("data", rotInd[0].ToString() + " " + rotInd[1].ToString() + " " + rotInd[2].ToString());
            writer.WriteEndElement();

            writer.WriteElementString("Order", order);

            writer.WriteStartElement("OrderInt");
            writer.WriteAttributeString("data", orderInt[0].ToString() + " " + orderInt[1].ToString() + " " + orderInt[2].ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public void Read(ref XmlReader reader)
        {
            string[] tokens;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Read();
                if (reader.Name == "Name")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                            Name = reader.Value;

                    }
                    reader.Read();
                }
                if (reader.Name == "ID")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                            ID = (int)Double.Parse(reader.Value);

                    }
                    reader.Read();
                }
                if (reader.Name == "ParentID")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                            ParentID = (int)Double.Parse(reader.Value);

                    }
                    reader.Read();
                }
                if (reader.Name == "Offset")
                {
                    reader.MoveToAttribute("data");
                    tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                    Offset = new double[tokens.Length];
                    for (int i = 0; i < tokens.Length; i++)
                        Offset[i] = Double.Parse(tokens[i]);
                }
                if (reader.Name == "RotInd")
                {
                    reader.MoveToAttribute("data");
                    tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                    rotInd = new int[tokens.Length];
                    for (int i = 0; i < tokens.Length; i++)
                        rotInd[i] = (int)Double.Parse(tokens[i]);
                }
                if (reader.Name == "Order")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                            order = reader.Value;

                    }
                    reader.Read();
                }
                if (reader.Name == "OrderInt")
                {
                    reader.MoveToAttribute("data");
                    tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                    orderInt = new int[tokens.Length];
                    for (int i = 0; i < tokens.Length; i++)
                        orderInt[i] = (int)Double.Parse(tokens[i]);
                }
            }
            reader.Read();
        }
    }
}
