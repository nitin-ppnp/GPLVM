using System;
using System.Xml;
using ILNumerics;

namespace GPLVM.Prior
{
    public class LocallyLinear : IPrior
    {
        private PriorType _type = PriorType.PriorTypeLocallyLinear;

        private int _q;                     // dimension of latent space
        private int _N;                     // dimension of data space
        private int numNeighbors = 50;

        private ILArray<double> _X;
        private ILArray<double> _Xnew;
        private ILArray<double> _Y;
        private ILArray<double> _W;
        private double _priorPrec = 1;

        private ILArray<double> idx;

        private Guid _key = Guid.NewGuid();

        public LocallyLinear()
        {
            _X = ILMath.empty();
            _Y = ILMath.empty();
            _W = ILMath.empty();

            idx = ILMath.empty();
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

        public ILArray<double> M
        {
            set
            {
                _Y = value;
            }
        }

        public ILArray<double> Index
        {
            get
            {
                return idx;
            }

            set
            {
                idx = value;
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
        #endregion

        public void Initialize(Data _data)
        {
            Initialize(_data.GetData("X"), 0);
        }

        public void Initialize(ILInArray<double> _data, ILInArray<double> segments)
        {
            using (ILScope.Enter(_data))
            {
                ILArray<double> d = ILMath.check(_data);
                _X.a = d;

                _q = _X.Size[1];
                _N = _X.Size[0];

                if (!_Y.IsEmpty)
                {
                    int D = _Y.Size[1];
                    _Y = _Y.T;
                    
                    // STEP1: COMPUTE PAIRWISE DISTANCES & FIND NEIGHBORS 
                    System.Console.WriteLine("\t-->Finding {0} nearest neighbours.", numNeighbors);

                    ILArray<double> Y2 = ILMath.sum(ILMath.pow(_Y, 2), 0);
                    ILArray<double> distance = ILMath.repmat(Y2, _N, 1) + ILMath.repmat(Y2.T, 1, _N) - 2 * ILMath.multiply(_Y.T, _Y);

                    ILArray<int> index = ILMath.empty<int>();
                    ILArray<double> sorted = ILMath.sort(distance, index);

                    ILArray<int> neighborhood = index[ILMath.r(1, numNeighbors), ILMath.full];

                    // STEP2: SOLVE FOR RECONSTRUCTION WEIGHTS
                    System.Console.WriteLine("\t-->Solving for reconstruction weights.");

                    double tol;
                    if (numNeighbors > D)
                    {
                        System.Console.WriteLine("\t   [note: numNeighbors > D; regularization will be used]");
                        tol = 1e-3; // regularlizer in case constrained fits are ill conditioned
                    }
                    else
                    {
                        tol = 0;
                    }

                    ILArray<double> w = ILMath.zeros(_N, _N);
                    ILArray<double> z, C;
                    for (int ii = 0; ii < _N; ii++)
                    {
                        z = _Y[ILMath.full, neighborhood[ILMath.full, ii]] -
                            ILMath.repmat(_Y[ILMath.full, ii], 1, numNeighbors);                                // shift ith pt to origin
                        C = ILMath.multiply(z.T, z);                                                            // local covariance
                        C = C + ILMath.eye(numNeighbors, numNeighbors) * tol * ILMath.trace(C);                 // regularlization (K>D)
                        w[neighborhood[ILMath.full, ii], ii] = ILMath.linsolve(C, ILMath.ones(numNeighbors, 1));                   // solve Cw=1
                        w[neighborhood[ILMath.full, ii], ii] = ILMath.divide(w[neighborhood[ILMath.full, ii], ii], ILMath.sum(w[ILMath.full, ii]));    // enforce sum(w)=1
                    }
                    _W.a = w.T;
                }
                else
                    Console.WriteLine("Prior needs observation data for initialisation!");
            }

            if (idx.IsEmpty)
                idx = ILMath.counter(_q) - 1;
        }

        public double LogLikelihood()
        {
            return -_priorPrec * (double)ILMath.sum(ILMath.sum(ILMath.multiplyElem(_X[ILMath.full, idx], _X[ILMath.full, idx]), 1)
                - 2 * ILMath.sum(ILMath.multiplyElem(_X[ILMath.full, idx], ILMath.multiply(_W, _X[ILMath.full, idx])), 1)
                + ILMath.multiply(ILMath.multiplyElem(_W, _W), ILMath.sum(ILMath.multiplyElem(_X[ILMath.full, idx], _X[ILMath.full, idx]), 1)), 0);
        }

        public double PostLogLikelihood()
        {
            // Todo
            double L = 0;

            return L;
        }

        public ILRetArray<double> LogLikGradient()
        {
            ILArray<double> gX = ILMath.zeros(_X.S);
            gX[ILMath.full, idx] = -2 * _priorPrec * (_X[ILMath.full, idx] + ILMath.multiply(_W, _X[ILMath.full, idx]));
            return gX;
        }

        public ILRetArray<double> PostLogLikGradient()
        {
            // Todo
            return 0;
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
                _X.a = d;
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
                        if (reader.Name == "W")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _W = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _W[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _W = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _W[i] = Double.Parse(tokens[i]);
                            }
                            _W = _W.Reshape(_N, _N).T;
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

            writer.WriteStartElement("W");
            writer.WriteAttributeString("data", _W.ToString().Remove(0, _W.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
