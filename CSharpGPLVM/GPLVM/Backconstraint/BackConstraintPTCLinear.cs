using System;
using GPLVM.Kernel;
using GPLVM.Embeddings;
using ILNumerics;
using System.Xml;

namespace GPLVM.Backconstraint
{
    public class BackConstraintPTCLinear : IBackconstraint
    {
        private int _N;
        private int _q;
        private int _D;

        private XInit _initX;

        private BackConstType _type;

        private ILArray<double> _Y = ILMath.localMember<double>();
        private ILArray<double> _X = ILMath.localMember<double>();
        private ILArray<double> _segments = ILMath.localMember<double>();

        private ILArray<double> _K1 = ILMath.localMember<double>(); // cosine phase kernel for dimension 1
        private ILArray<double> _K2 = ILMath.localMember<double>(); // sine phase kernel for dimension 2
        private ILArray<double> _K3 = ILMath.localMember<double>(); // Euclidean distance for the rest dimensions
        private ILArray<double> _invK1 = ILMath.localMember<double>();
        private ILArray<double> _invK2 = ILMath.localMember<double>();
        private ILArray<double> _invK3 = ILMath.localMember<double>();

        private ILArray<double> _phase = ILMath.localMember<double>();

        private ILArray<double> _A = ILMath.localMember<double>();

        //private ILArray<double> _bias;

        private IKernel _kernPhase;
        private IKernel _kernData;


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
        public BackConstraintPTCLinear()
        {
            _type = BackConstType.ptcLinear;

            _kernPhase = new RBFKernBack();
            _kernData = new RBFKernBack();

            _kernPhase.Parameter = 3e-4f;
            _kernData.Parameter = 1e-6f;
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
                return _A.Length;
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
                _A.a = value.T;
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
                _Y.a = ILMath.check(inY);
                _X.a = ILMath.check(inX).C;
            }
            CreatePhase();
            UpdateKernelMatrix();

            //_A = ILMath.zeros(_X.Size);

            //_A[ILMath.full, 0] = ILMath.multiply(_invK1, _X[ILMath.full, 0]);
            //_A[ILMath.full, 1] = ILMath.multiply(_invK2, _X[ILMath.full, 1]);

            //if (_X.S[1] > 2)
            //    _A[ILMath.full, ILMath.r(2, ILMath.end)] = ILMath.multiply(_invK3, _X[ILMath.full, ILMath.r(2, ILMath.end)]);

            _D = _Y.Size[1];
            _N = _A.Size[0];
            _q = _A.Size[1];

            UpdateParameter();
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
                ILArray<double> dX_dA = ILMath.zeros(gX.S[0], gX.S[1], _segments.Length * 2);
                ILRetArray<double> gA = ILMath.empty();
                ILArray<double> dL_dA = ILMath.zeros(_segments.Length * 2, 1);

                //gAtmp[ILMath.full, 0] = ILMath.multiply(_K1, gX[ILMath.full, 0]);
                //gAtmp[ILMath.full, 1] = ILMath.multiply(_K2, gX[ILMath.full, 1]);

                double theta_0, delta;
                int numSeg = _segments.Length;
                for (int n = 0; n < numSeg - 1; n++)
                {
                    theta_0 = (double)_A[n]; 
                    delta = (double)_A[numSeg  + n]; 
                    for (int j = (int)_segments[n]; j < _segments[n + 1]; j++)
                    {
                        int j_1 = j - (int)_segments[n];

                        dX_dA[j,0,n] = -ILMath.sin(theta_0 + (j_1)*delta);
                        dX_dA[j,1,n] = ILMath.cos(theta_0 + (j_1)*delta);

                        dX_dA[j, 0, numSeg + n] = -ILMath.sin(theta_0 + (j_1)*delta);
                        dX_dA[j, 1, numSeg + n] = ILMath.cos(theta_0 + (j_1)*delta);
                    }
                }

                theta_0 = (double)_A[numSeg - 1];
                delta = (double)_A[numSeg + numSeg - 1];

                for (int j = (int)_segments[ILMath.end]; j < _Y.Size[0]; j++)
                {
                    int j_1 = j - (int)_segments[ILMath.end];

                    dX_dA[j,0, numSeg - 1] = -ILMath.sin(theta_0 + (j_1)*delta);
                    dX_dA[j,1, numSeg - 1] = ILMath.cos(theta_0 + (j_1)*delta);

                    dX_dA[j, 0, numSeg + numSeg - 1] = -ILMath.sin(theta_0 + (j_1)*delta);
                    dX_dA[j, 1, numSeg + numSeg - 1] = ILMath.cos(theta_0 + (j_1) * delta);
                }


                for (int n = 0; n < numSeg; n++)
                {
                    dL_dA[n] = ILMath.trace(ILMath.multiply(gX.T, dX_dA[ILMath.full, ILMath.full, n]));
                    dL_dA[numSeg + n] = ILMath.trace(ILMath.multiply(gX.T, dX_dA[ILMath.full, ILMath.full, numSeg + n]));
                }

                gA = dL_dA;

                return gA;
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
                            //_Y = _Y.Reshape(_D, _N).T;
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
                            //_X = _X.Reshape(_q, _N).T;
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
                            _A = _A.T;
                        }
                        if (reader.Name == "Phase")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _phase = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _phase[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _phase = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _phase[i] = Double.Parse(tokens[i]);
                            }
                            _phase = _phase.T;
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
            writer.WriteAttributeString("type", "ptcLinear");

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

            //writer.WriteStartElement("Phase");
            //writer.WriteAttributeString("data", _phase.ToString().Remove(0, _phase.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            //writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        #endregion

        #region Private Functions
        private void UpdateParameter()
        {
            //_X[ILMath.full, 0] = ILMath.multiply(_K1, _A[ILMath.full, 0]);
            //_X[ILMath.full, 1] = ILMath.multiply(_K2, _A[ILMath.full, 1]);
            double theta_0, delta;
            int numSeg = _segments.Length;
            for (int i = 0; i < numSeg - 1; i++)
            {
                theta_0 = (double)_A[i];
                delta = (double)_A[numSeg+i];
                for (int j = (int)_segments[i]; j < _segments[i + 1]; j++)
                {
                    int j_1 = j - (int)_segments[i];
                    _X[j,0] = ILMath.cos(theta_0 + (j_1)*delta); 
                    _X[j,1] = ILMath.sin(theta_0 + (j_1)*delta); // extracting the phase and setting to x_j
                }
            }

            theta_0 = (double)_A[numSeg - 1];
            delta = (double)_A[numSeg + numSeg - 1];

            for (int j = (int)_segments[ILMath.end]; j < _Y.Size[0]; j++)
            {
                int j_1 = j - (int)_segments[ILMath.end];
                _X[j,0] = ILMath.cos(theta_0 + (j_1)*delta); 
                _X[j,1] = ILMath.sin(theta_0 + (j_1)*delta); // extracting the phase and setting to x_j
            }

            //_X[ILMath.full, 2] = ILMath.multiply(_K3, _A[ILMath.full, 2]);

            //if (_X.S[1] > 2)
            //    _X[ILMath.full, ILMath.r(2, ILMath.end)] = ILMath.multiply(_K3, _A[ILMath.full, ILMath.r(2, ILMath.end)]);

            UpdateKernelMatrix();
        }

        private void UpdateKernelMatrix()
        {
            //_K1 = _kernPhase.ComputeKernelMatrix(ILMath.cos(_phase), ILMath.cos(_phase));
            //_K2 = _kernPhase.ComputeKernelMatrix(ILMath.sin(_phase), ILMath.sin(_phase));
                

            //_invK1 = Util.pdinverse(_K1);
            //_invK2 = Util.pdinverse(_K2);

            if (_X.S[1] > 2)
            {
                _K3 = _kernData.ComputeKernelMatrix(_Y, _Y);
                _invK3 = Util.pdinverse(_K3);
            }
        }

        private void CreatePhase()
        {
            ILArray<double> curY = ILMath.empty();

            //_phase = ILMath.zeros(_Y.S);
            ILArray<complex> tmp = ILMath.empty<complex>();
            ILArray<double> curX = ILMath.empty();
            ILArray<double> curX_norm = ILMath.empty();
            ILArray<double> angles = ILMath.empty();
            ILArray<double> offStep = ILMath.zeros(1,_segments.Length * 2); // offset and step size

            ILArray<int> idx;

            int numSeg = _segments.Length;

            for (int i = 0; i < _segments.Length - 1; i++)
            {
                curY = _Y[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full];
                curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

                curX = EstimateX(curY);

                curX_norm = ILMath.sqrt(ILMath.multiplyElem(curX[":", 0], curX[":", 0]) + ILMath.multiplyElem(curX[":", 1], curX[":", 1])); // getting the circle
                curX[":", 0] = ILMath.divide(curX[":", 0], curX_norm);
                curX[":", 1] = ILMath.divide(curX[":", 1], curX_norm); // normalizing the circle

                angles = ILMath.atan(ILMath.divide(curX[":", 1], curX[":", 0]));

                offStep[numSeg + i] = ILMath.median(ILMath.abs(angles[ILMath.r(1, ILMath.end)] - angles[ILMath.r(0, ILMath.end - 1)])); // computing step size*/
                _Y[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full] = curY;
            }

            curY = _Y[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full];
            curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid


            curX = EstimateX(curY);

            curX_norm = ILMath.sqrt(ILMath.multiplyElem(curX[":", 0], curX[":", 0]) + ILMath.multiplyElem(curX[":", 1], curX[":", 1])); // getting the circle
            curX[":", 0] = ILMath.divide(curX[":", 0], curX_norm);
            curX[":", 1] = ILMath.divide(curX[":", 1], curX_norm); // normalizing the circle

            angles = ILMath.atan(ILMath.divide(curX[":", 1], curX[":", 0]));

            offStep[ILMath.end] = ILMath.median(ILMath.abs(angles[ILMath.r(1, ILMath.end)] - angles[ILMath.r(0, ILMath.end - 1)])); // computing step size

            _Y[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full] = curY;

            if (numSeg > 1)
            {
                int l = (int)(_segments[1] - _segments[0]);
                for (int i = 1; i < numSeg; i++)
                {
                    idx = ILMath.empty<int>();
                    ILMath.min(ILMath.sum(ILMath.pow(ILMath.repmat(_Y[_segments[i], ":"], l, 1) - _Y[ILMath.r(_segments[0], _segments[1] - 1), ":"], 2), 1), idx);

                    offStep[i] = ((double)idx - 1) * offStep[numSeg]; // applying the angles

                }
            }

            _A = offStep;

            //// set the phase
            //double theta_0, delta;
            //_A = ILMath.zeros(_Y.Size[0], 1);
            //for (int i = 0; i < numSeg - 1; i++)
            //{
            //    theta_0 = (double)offStep[i];
            //    delta = (double)offStep[numSeg + i];

            //    for (int j = (int)_segments[i]; j < _segments[i + 1]; j++)
            //    {
            //        int j_1 = j - (int)_segments[i];
            //        _A[j] = theta_0 + (j_1) * delta; // extracting the phase
            //    }
            //}

            //theta_0 = (double)offStep[numSeg - 1];
            //delta = (double)offStep[numSeg + numSeg - 1];

            //for (int j = (int)_segments[ILMath.end]; j < _Y.Size[0]; j++)
            //{
            //    int j_1 = j - (int)_segments[ILMath.end];
            //    _A[j] = theta_0 + (j_1) * delta; // extracting the phase
            //}
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
