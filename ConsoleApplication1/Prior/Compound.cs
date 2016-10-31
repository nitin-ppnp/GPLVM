using System;
using System.Collections.Generic;
using System.Xml;
using ILNumerics;
using GPLVM.Dynamics;

namespace GPLVM.Prior
{
    public class Compound : IPrior
    {
        private int _q;                     // dimension of latent space
        private int _N;                     // dimension of data space

        private List<IPrior> _priors;
        private PriorType _type;

        private ILArray<double> _X;
        private ILArray<double> _Xnew;
        private ILArray<double> _segments;

        private int _numParameter;
        ILArray<double> _parameter;

        private Guid _key = Guid.NewGuid();

        public Compound()
        {
            _type = PriorType.PriorTypeCompound;
            _priors = new List<IPrior>();
            _numParameter = 0;

            _X = ILMath.empty();
            _segments = ILMath.empty();
        }

        #region Setters and Getters
        ///<summary>
        ///Gets the Prior type.
        ///</summary>
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
                return _numParameter;
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                _parameter = ILMath.zeros(1, NumParameter);

                int startVal = 0, endVal = 0;
                foreach (IPrior prior in _priors)
                {
                    endVal += prior.NumParameter;
                    _parameter[ILMath.r(startVal, endVal - 1)] = prior.LogParameter;
                    startVal = endVal;
                }
                return _parameter.C;
            }
            set
            {
                _parameter.a = value;
                int startVal = 0, endVal = 0;
                foreach (IPrior prior in _priors)
                {
                    endVal += prior.NumParameter;
                    prior.LogParameter = _parameter[ILMath.r(startVal, endVal - 1)];
                    startVal = endVal;
                }
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
        ///Gets the kernel list of the kernel.
        ///</summary>
        public List<IPrior> Priors
        {
            get
            {
                return _priors;
            }
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
        #endregion

        ///<summary>
        ///Adds a new kern to the List.
        ///</summary>
        public void AddPrior(IPrior newPrior)
        {
            _priors.Add(newPrior);
            _numParameter += newPrior.NumParameter;
        }

        public void RemovePrior(string key)
        {
            foreach (IPrior prior in _priors)
            {
                if (prior.Key.ToString() == key)
                    _priors.Remove(prior);
            }
        }

        public void Initialize(Data _data)
        {
            _N = _data.GetData("X").Size[0];
            _q = _data.GetData("X").Size[1];

            foreach (IPrior prior in _priors)
                prior.Initialize(_data);
        }

        public void Initialize(ILInArray<double> _data, ILInArray<double> segments)
        {
            using (ILScope.Enter(_data, segments))
            {
                ILArray<double> data = ILMath.check(_data);
                ILArray<double> seg = ILMath.check(segments);

                _X.a = data;
                _segments.a = seg;
            }

            foreach (IPrior prior in _priors)
                prior.Initialize(_X, _segments);

            _N = _X.Size[0];
            _q = _X.Size[1];
        }

        public double LogLikelihood()
        {
            double L = 0;
            foreach (IPrior prior in _priors)
                L += prior.LogLikelihood();

            return L;
        }

        public double PostLogLikelihood()
        {
            double L = 0;
            foreach (IPrior prior in _priors)
                L += prior.PostLogLikelihood();

            return L;
        }

        public ILRetArray<double> LogLikGradient()
        {
            ILArray<double> gX = ILMath.zeros(_N,_q);

            foreach (IPrior prior in _priors)
                gX += prior.LogLikGradient();

            return gX;
        }

        public ILRetArray<double> PostLogLikGradient()
        {
            ILArray<double> gX = ILMath.zeros(_N, _q);

            foreach (IPrior prior in _priors)
                gX += prior.PostLogLikGradient();

            return gX;
        }

        public void UpdateParameter(Data data)
        {
            foreach (IPrior prior in _priors)
                prior.UpdateParameter(data);
        }

        public void UpdateParameter(ILInArray<double> _data)
        {
            using (ILScope.Enter(_data))
            {
                ILArray<double> d = ILMath.check(_data);
                _X.a = d;
            }

            foreach (IPrior prior in _priors)
                prior.UpdateParameter(_X);
        }

        // Read and Write Data to a XML file
        public void Read(ref XmlReader reader)
        {
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
                        
                    }
                    reader.Read();
                }
                if (reader.Name == "Priors")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "Prior")
                        {
                            reader.MoveToAttribute("type");
                            switch (reader.ReadContentAsString())
                            {
                                case "PriorTypeGauss":
                                    AddPrior(new Gaussian());
                                    _priors[_priors.Count - 1].Read(ref reader);
                                    break;
                                case "PriorTypeConnectivity":
                                    AddPrior(new Connectivity());
                                    _priors[_priors.Count - 1].Read(ref reader);
                                    break;
                                case "PriorTypeDynamics":
                                    reader.MoveToAttribute("DynamicType");
                                    switch ((string)reader.ReadContentAs(typeof(string), null))
                                    {
                                        case "DynamicTypeVelocity":
                                            AddPrior(new GPVelocity());
                                            _priors[_priors.Count - 1].Read(ref reader);
                                            break;
                                        case "DynamicTypeAcceleration":
                                            AddPrior(new GPAcceleration());
                                            _priors[_priors.Count - 1].Read(ref reader);
                                            break;
                                    }
                                    break;
                                    
                            }
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
            writer.WriteAttributeString("type", "PriorTypeCompound");

            writer.WriteStartElement("Data", null);

            writer.WriteElementString("q", _q.ToString());
            writer.WriteElementString("N", _N.ToString());

            writer.WriteEndElement();

            writer.WriteStartElement("Priors");
            foreach (IPrior prior in _priors)
                prior.Write(ref writer);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
