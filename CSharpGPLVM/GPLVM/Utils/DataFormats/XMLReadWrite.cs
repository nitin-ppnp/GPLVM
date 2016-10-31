using System.Xml;
using GPLVM.GPLVM;
using GPLVM.Dynamics;

namespace GPLVM
{
    public class XMLReadWrite
    {
        public XMLReadWrite()
        {

        }

        public void write(ref GP_LVM gplvm, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.NewLineOnAttributes = true;

            XmlWriter writer = XmlWriter.Create(path, settings);
            writer.WriteStartDocument();
            gplvm.Write(ref writer);
            writer.WriteEndDocument();
            writer.Close();
        }

        public void read(string path, ref GP_LVM gplvm)
        {
            XmlReader reader = XmlReader.Create(path);
            while(reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "GPLVM")
                {
                    gplvm = new GP_LVM();
                    gplvm.Read(ref reader);
                }
            }
        }

        public void write(ref StyleGPLVM gplvm, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.NewLineOnAttributes = true;

            XmlWriter writer = XmlWriter.Create(path, settings);
            writer.WriteStartDocument();
            gplvm.Write(ref writer);
            writer.WriteEndDocument();
            writer.Close();
        }

        public void write(ref StyleGPLVM2 gplvm, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.NewLineOnAttributes = true;

            XmlWriter writer = XmlWriter.Create(path, settings);
            writer.WriteStartDocument();
            gplvm.Write(ref writer);
            writer.WriteEndDocument();
            writer.Close();
        }

        public void write(ref BackProjection backproj, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.NewLineOnAttributes = true;

            XmlWriter writer = XmlWriter.Create(path, settings);
            writer.WriteStartDocument();
            backproj.Write(ref writer);
            writer.WriteEndDocument();
            writer.Close();
        }

        public void write(ref GPAccelerationNode gpAcc, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.NewLineOnAttributes = true;

            XmlWriter writer = XmlWriter.Create(path, settings);
            writer.WriteStartDocument();
            gpAcc.Write(ref writer);
            writer.WriteEndDocument();
            writer.Close();
        }

        public void read(string path, ref StyleGPLVM gplvm)
        {
            XmlReader reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "StyleGPLVM")
                {
                    gplvm = new StyleGPLVM();
                    gplvm.Read(ref reader);
                }
            }
        }

        public void read(string path, ref StyleGPLVM2 gplvm)
        {
            XmlReader reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "StyleGPLVM2")
                {
                    gplvm = new StyleGPLVM2();
                    gplvm.Read(ref reader);
                }
            }
        }

        public void read(string path, ref BackProjection backproj)
        {
            XmlReader reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "BackProject")
                {
                    backproj = new BackProjection();
                    backproj.Read(ref reader);
                }
            }
        }

        public void read(string path, ref GPAccelerationNode gpAcc)
        {
            XmlReader reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "BackProject")
                {
                    gpAcc = new GPAccelerationNode();
                    gpAcc.Read(ref reader);
                }
            }
        }
    }
}
