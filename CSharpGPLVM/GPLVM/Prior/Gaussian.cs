using System;
using System.Xml;
using ILNumerics;

namespace GPLVM.Prior
{
    public class Gaussian : IPrior
    {
        private int _q;                     // dimension of latent space
        private int _N;                     // dimension of data space

        private PriorType _type;
        private ILArray<double> _X;
        private ILArray<double> _Xnew;
        private double _priorPrec = 1;

        private Guid _key = Guid.NewGuid();

        public Gaussian()
        {
            _type = PriorType.PriorTypeGauss;
            _X = ILMath.empty();
        }

        public PriorType Type
        {
            get
            {
                return _type;
            }
        }

        public int NumParameter
        {
            get
            {
                return 0;
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                return ILMath.empty();
            }
            set
            {

            }
        }

        public ILArray<double> X
        {
            get { return _X; }
        }

        public ILArray<double> Xnew
        {
            get { return _Xnew; }
            set { _Xnew = value; }
        }

        ///<summary>
        ///Gets the unique ID of the object.<double>.
        ///</summary>
        public Guid Key
        {
            get
            {
                return _key;
            }
        }

        public void Initialize(Data _data)
        {
            _X = _data.GetData("X");
            _q = _X.Size[1];
            _N = _X.Size[0];
        }

        public void Initialize(ILInArray<double> _data, ILInArray<double> segments)
        {
            using (ILScope.Enter(_data))
            {
                ILArray<double> d = ILMath.check(_data);
                _X.a = d.C;
            }
            _q = _X.Size[1];
            _N = _X.Size[0];
        }

        public double LogLikelihood()
        {
            double L = 0;
            for (int i = 0; i < _X.Size[0]; i++)
                L -= (double)(0.5 * (_priorPrec * ILMath.multiply(_X[i, ILMath.full], _X[i, ILMath.full].T) + ILMath.log(2 * ILMath.pi) - ILMath.log(_priorPrec)));

            return L;
        }

        public double PostLogLikelihood()
        {
            double L = 0;
            for (int i = 0; i < _Xnew.Size[0]; i++)
                L -= (double)(0.5 * (_priorPrec * ILMath.multiply(_Xnew[i, ILMath.full], _Xnew[i, ILMath.full].T) + ILMath.log(2 * ILMath.pi) - ILMath.log(_priorPrec)));
            
            return L;
        }

        public ILRetArray<double> LogLikGradient()
        {
            return -_priorPrec * _X;
        }

        public ILRetArray<double> PostLogLikGradient()
        {
            return -_priorPrec * _Xnew;
        }

        public void UpdateParameter(Data data)
        {
            _X = data.GetData("X");
        }

        public void UpdateParameter(ILInArray<double> _data)
        {
            using (ILScope.Enter(_data))
            {
                ILArray<double> d = ILMath.check(_data);
                _X.a = d.C;
            }
        }

        // Read and Write Data to a XML file
        public void Read(ref XmlReader reader)
        {
            string[] tokens;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Read();
                if (reader.Name == "Data")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "q")
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                    _q = (int)Double.Parse(reader.Value);

                            }
                            reader.Read();
                        }
                        
                        if (reader.Name == "N")
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                    _N = (int)Double.Parse(reader.Value);

                            }
                            reader.Read();
                        }
                        if (reader.Name == "X")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _X = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _X[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _X = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _X[i] = Double.Parse(tokens[i]);
                            }
                            _X = _X.Reshape(_q, _N).T;
                        }
                    }
                    reader.Read();
                }
            }
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("Prior");
            writer.WriteAttributeString("type", "PriorTypeGauss");
                writer.WriteStartElement("Data", null);
                    writer.WriteElementString("q", _q.ToString());

                    writer.WriteElementString("N", _N.ToString());
                    writer.WriteStartElement("X");
                        writer.WriteAttributeString("data", _X.ToString().Remove(0, _X.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();

                writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
