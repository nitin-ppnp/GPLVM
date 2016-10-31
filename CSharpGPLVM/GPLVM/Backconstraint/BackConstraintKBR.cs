using System;
using GPLVM.Kernel;
using GPLVM.Embeddings;
using ILNumerics;
using System.Xml;

namespace GPLVM.Backconstraint
{
    public class BackConstraintKBR : IBackconstraint
    {
        private int _N;
        private int _q;
        private int _D;

        private XInit _initX;

        private BackConstType _type;

        private ILArray<double> _Y = ILMath.localMember<double>();
        private ILArray<double> _X = ILMath.localMember<double>();
        private ILArray<double> _segments = ILMath.localMember<double>();

        private ILArray<double> _K = ILMath.localMember<double>();
        private ILArray<double> _invK = ILMath.localMember<double>();

        private ILArray<double> _A = ILMath.localMember<double>();
        //private ILArray<double> _bias;

        private IKernel _kern;

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// The kernel regression is a non-parametric technique in statistics to estimate 
        /// the conditional expectation of a random variable. The objective is to find a 
        /// non-linear relation between a pair of random variables X and Y. In any nonparametric 
        /// regression, the conditional expectation of a variable Y relative to a variable 
        /// X may be written: E(Y | X) = m(X) where m is an unknown function.
        /// </remarks>  
        public BackConstraintKBR()
        {
            _type = BackConstType.kbr;

            _kern = new RBFKernBack();

            _kern.Parameter = 2;
        }

        #region Setters and Getters
        public BackConstType Type
        {
            get 
            { 
                return _type; 
            }
        }

        /// <summary>
        /// Gets the number of parameters of the objects wants to be optimized. 
        /// </summary>
        public int NumParameter
        {
            get
            {
                return _A.Size[0] * _A.Size[1];
            }
        }

        /// <summary>
        /// The log of the parameter in the model wants to be optimized. 
        /// </summary>
        public ILArray<double> LogParameter
        {
            get
            {
                return _A[ILMath.full].T.C; 
            }
            set
            {
                _A.a = value;
                _A.a = ILMath.reshape(_A, _N, _q);
                UpdateParameter();
            }
        }
        #endregion

        #region Public Functions
        public void Initialize(ILInArray<double> inY, ILInArray<double> inX, ILInArray<double> inSegments, XInit initX = XInit.pca)
        {
            _initX = initX;

            using (ILScope.Enter(inY, inX, inSegments))
            {
                _segments.a = ILMath.check(inSegments);
                _Y.a = ILMath.check(inY).C;
                _X.a = ILMath.check(inX).C;

                /*if (_segments.Length > 1)
                    _X.a = NormalizeSegments();*/

                NormalizeSegments();

                UpdateKernelMatrix();
                //_bias = ILMath.mean(_X);
                _A.a = ILMath.multiply(_invK, _X);// -ILMath.repmat(_bias, _Y.Size[0], 1);

                _D = _Y.Size[1];
                _N = _A.Size[0];
                _q = _A.Size[1];

                UpdateParameter();
            }
        }

        public ILArray<double> GetBackConstraints()
        {
            if (!_A.IsEmpty)
                return _X.C;
            else
            {
                Console.WriteLine("Please initialize backconstraints!");
                return ILMath.empty();
            }
        }

        public ILRetArray<double> BackConstraintsGradient(ILInArray<double> ingX)
        {
            using (ILScope.Enter(ingX))
            {
                ILArray<double> gX = ILMath.check(ingX);
                return ILMath.multiply(_K, gX);
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
                        if (reader.Name == "D")
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                    _D = (int)Double.Parse(reader.Value);

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
                        if (reader.Name == "Y")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _Y = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _Y[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _Y = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _Y[i] = Double.Parse(tokens[i]);
                            }
                            _Y = _Y.Reshape(_D, _N).T;
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
                        if (reader.Name == "A")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _A = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _A[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _A = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _A[i] = Double.Parse(tokens[i]);
                            }
                            _A = _A.Reshape(_q, _N).T;
                        }
                    }
                    reader.Read();
                }
            }
            UpdateKernelMatrix();
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("Backconstraint");
            writer.WriteAttributeString("type", "kbr");

            writer.WriteStartElement("Data", null);

            writer.WriteElementString("q", _q.ToString());
            writer.WriteElementString("D", _D.ToString());
            writer.WriteElementString("N", _N.ToString());

            writer.WriteStartElement("Y");
            writer.WriteAttributeString("data", _Y.ToString().Normalize().Remove(0, _Y.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();

            writer.WriteStartElement("X");
            writer.WriteAttributeString("data", _X.ToString().Normalize().Remove(0, _X.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();

            writer.WriteStartElement("A");
            writer.WriteAttributeString("data", _A.ToString().Remove(0, _A.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        #endregion

        #region Private Functions
        private void UpdateParameter()
        {
            using (ILScope.Enter())
            {
                _X.a = ILMath.multiply(_K, _A);// +ILMath.multiply(ILMath.ones<double>(_Y.Size[0], 1), _bias);
                //UpdateKernelMatrix();
            }
        }

        private void UpdateKernelMatrix()
        {
            using (ILScope.Enter())
            {
                if (_kern != null)
                {
                    _K.a = _kern.ComputeKernelMatrix(_Y, _Y);
                    _invK.a = Util.pdinverse(_K);
                }
                else
                    System.Console.WriteLine("No kernel function found! Please add a kernel object!");
            }
        }

        private ILArray<double> NormalizeSegments()
        {
            using (ILScope.Enter())
            {
                ILArray<double> curY = ILMath.empty();
                ILArray<double> X = ILMath.zeros(_X.Size);

                for (int i = 0; i < _segments.Length - 1; i++)
                {
                    curY = _Y[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full];
                    curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

                    _Y[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full] = curY;
                }

                curY = _Y[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full];
                curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

                _Y[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full] = curY;

                X.a = EstimateX(_Y);

                return X;
            }
        }

        /// <summary>
        /// Estimates latent points through PCA. 
        /// </summary>
        /// <param name="data">The data Y.</param>
        private ILRetArray<double> EstimateX(ILInArray<double> inCurY)
        {
            using (ILScope.Enter(inCurY))
            {
                ILArray<double> curY = ILMath.check(inCurY);
                ILRetArray<double> X = ILMath.empty();

                switch (_initX)
                {
                    case XInit.pca:
                        X = Embed.PCA(curY, _X.Size[1]);
                        break;
                    case XInit.kernelPCA:
                        X = Embed.KernelPCA(curY, _X.Size[1]);
                        break;
                    case XInit.lle:
                        X = Embed.LLE(curY, 15, _X.Size[1]);
                        break;
                    case XInit.smallRand:
                        X = Embed.SmallRand(curY, _X.Size[1]);
                        break;
                    case XInit.isomap:
                        X = Embed.Isomap(curY, _X.Size[1]);
                        break;
                }

                return X;
            }
        }
        #endregion
    }
}
