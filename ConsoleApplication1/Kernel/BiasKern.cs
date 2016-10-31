using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Kernel
{
    [DataContract()]
    public class BiasKern : IKernel
    {
        [DataMember()]
        private KernelType _type;
        [DataMember()]
        private int _numParameter;        // number of kernel parameter
        [DataMember()]
        private ILArray<double> _parameter = ILMath.localMember<double>();

        [DataMember()]
        private ILArray<double> _K = ILMath.localMember<double>();         // NxN kernel covariance matrix
        [DataMember()]
        private ILArray<double> _Kuf = ILMath.localMember<double>();

        [DataMember()]
        private ILArray<double> _Krec = ILMath.localMember<double>(); 

        [DataMember()]
        private Guid _key = Guid.NewGuid();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// This type of kernel computes a linear correlation between the data points.
        /// </remarks>
        public BiasKern()
        {
            this._type = KernelType.KernelTypeBias;
            _parameter.a = ILMath.exp((double)-2);//new double[] { _variance };
            this._numParameter = 1;
        }

        #region Setters and Getters
        ///<summary>
        ///Gets the kernel type.
        ///</summary>
        public KernelType Type
        {
            get
            {
                return _type;
            }
        }

        ///<summary>
        ///The kernel matri of type ILArray<double>.
        ///</summary>
        public ILArray<double> K
        {
            get
            {
                return _K.C;
            }
        }

        ///<summary>
        ///The kernel matrix of type ILArray<double>.
        ///</summary>
        public ILArray<double> Krec
        {
            get
            {
                return _Krec.C;
            }
        }

        ///<summary>
        ///The kernel matri of type ILArray<double>.
        ///</summary>
        public ILArray<double> Kuf
        {
            get
            {
                return _Kuf.C;
            }
        }

        ///<summary>
        ///Gets the number of parameters.
        ///</summary>
        public int NumParameter
        {
            get
            {
                return _numParameter;
            }
        }

        ///<summary>
        ///Gets the kernel parameters of type ILArray<double>.
        ///</summary>
        public ILArray<double> Parameter
        {
            get
            {
                return _parameter.C;
            }
            set
            {
                _parameter.a = value;
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                return ILMath.log(_parameter).C;
            }
            set
            {
                _parameter.a = Util.atox(value);
            }
        }

        public double Noise
        {
            get { return 0.0; }
        }

        ///<summary>
        ///Gets the unique ID of the object.
        ///</summary>
        public Guid Key
        {
            get
            {
                return _key;
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <remarks>
        /// Computing the kernel matrix with kernel function 'k(x_i, x_j) = sigma * x_i * x_j^T'.
        /// </remarks>
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<T>.
        /// </returns>
        public ILRetArray<double> ComputeKernelMatrix(ILInArray<double> inX1, ILInArray<double> inX2, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                ILRetArray<double> k = ILMath.empty();

                switch (flag)
                {
                    case Flag.learning:
                        if (X1.S[0] == X2.S[0])
                        {
                            _K.a = ILMath.repmat(_parameter[0], X1.S[0], X2.S[0]);
                            k = _K;
                        }
                        else
                        {
                            _Kuf.a = ILMath.repmat(_parameter[0], X1.S[0], X2.S[0]);
                            k = _Kuf;
                        }
                        break;
                    case Flag.postlearning:
                        _Krec.a = ILMath.repmat(_parameter[0], X1.S[0], X2.S[0]);
                        k = _Krec;
                        break;

                    default:
                        k = ILMath.repmat(_parameter[0], X1.S[0], X2.S[0]);
                        break;
                }

                return k;
            }
        }

        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <param name="data">The data dictionary.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> ComputeKernelMatrix(Data _data)
        {
            return ComputeKernelMatrix(_data.GetData("X"), _data.GetData("X"));
        }

        /// <summary>
        /// Computes the diagonal of the kernel given a design matrix ofinputs.
        /// </summary>
        /// <param name="X1">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the diagonal matrix of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> ComputeDiagonal(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);

                return ILMath.repmat(_parameter[0], X.S[0], 1);
            }
        }

        /// <summary>
        /// Computes the gradient w.r.t the latents.
        /// </summary>
        /// <param name="X">The N times q matrix of latent points.</param>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradients of the latent points of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> LogLikGradientX(ILInArray<double> inX, ILInArray<double> indL_dK)
        {
            using (ILScope.Enter(inX, indL_dK))
            {
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> dL_dK = ILMath.check(indL_dK);

                ILArray<double> dL_dX = ILMath.zeros(X.Size);
                ILArray<double> dK_dX = ILMath.empty();
                for (int q = 0; q < dL_dX.Size[1]; q++)
                {
                    dK_dX = GradX(X, X, q);
                    dL_dX[ILMath.full, q] = 2f * ILMath.sum(ILMath.multiplyElem(dL_dK, dK_dX), 1) - ILMath.multiplyElem(ILMath.diag(dL_dK), ILMath.diag(dK_dX));
                }
                return dL_dX;
            }
        }

        /// <summary>
        /// Computes the gradient of L w.r.t the latents.
        /// </summary>
        /// <param name="X">The N times q matrix of latent points.</param>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradients of the latent points of type ILCell (1st cell dL_dXu, 2nd cell dL_dX)<double>.
        /// </returns>
        public ILRetCell LogLikGradientX(ILInArray<double> inXu, ILInArray<double> indL_dKuu, ILInArray<double> inX, ILInArray<double> indL_dKuf)
        {
            using (ILScope.Enter(inXu, indL_dKuu, inX, indL_dKuf))
            {
                ILArray<double> Xu = ILMath.check(inXu);
                ILArray<double> dL_dKuu = ILMath.check(indL_dKuu);
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> dL_dKuf = ILMath.check(indL_dKuf);

                ILArray<double> dL_dXu = ILMath.zeros(Xu.Size);
                ILArray<double> dL_dX = ILMath.zeros(X.Size);

                ILArray<double> dK_dX = ILMath.empty();
                for (int q = 0; q < dL_dXu.Size[1]; q++)
                {
                    // dL_dKuu_dXu
                    dK_dX = GradX(Xu, Xu, q);
                    dL_dXu[ILMath.full, q] = (2 * ILMath.sum(ILMath.multiplyElem(dL_dKuu, dK_dX), 1) - ILMath.multiplyElem(ILMath.diag(dL_dKuu), ILMath.diag(dK_dX)));

                    // dL_dKuf_dXu
                    dK_dX = GradX(Xu, X, q);
                    dL_dXu[ILMath.full, q] += ILMath.sum(ILMath.multiplyElem(dL_dKuf, dK_dX), 1);

                    // dL_dKuf_dX
                    dK_dX = GradX(X, Xu, q);
                    dL_dX[ILMath.full, q] += ILMath.sum(ILMath.multiplyElem(dL_dKuf.T, dK_dX), 1);
                }

                ILRetCell ret = ILMath.cell(dL_dXu, dL_dX);

                return ret;
            }
        }

        /// <summary>
        /// Computes the gradient of the diagonal w.r.t the latent points.
        /// </summary>
        /// <param name="inX">Given latent points.</param>
        /// <returns>
        /// The method returns the gradients of the diagonal of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> DiagGradX(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);
                return ILMath.zeros(X.S);
            }
        }

        /// <summary>
        /// Computes the gradient w.r.t the latents.
        /// </summary>
        /// <param name="data">The data dictionary.</param>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradients of the latent points of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> LogLikGradientX(Data data, ILInArray<double> indL_dK)
        {
            using (ILScope.Enter(indL_dK))
            {
                ILArray<double> dL_dK = ILMath.check(indL_dK);
                return LogLikGradientX(data.GetData("X"), dL_dK);
            }
        }

        /// <summary>
        /// Computes the gradient of the kernel parameters.
        /// </summary>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradient of the kernel parameters of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> LogLikGradientParam(ILInArray<double> indL_dK)
        {
            using (ILScope.Enter(indL_dK))
            {
                ILArray<double> dL_dK = ILMath.check(indL_dK);
                ILArray<double> gParam = new double[] { 0 };

                gParam[0] = ILMath.sum(ILMath.sum(dL_dK));

                return ILMath.multiplyElem(gParam, _parameter); // because of the log space
            }
        }

        /// <summary>
        /// Computes the gradient of the kernel parameters.
        /// </summary>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradient of the kernel parameters of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> LogLikGradientParamTensor(ILInArray<double> inX, ILInArray<double> indL_dK)
        {
            using (ILScope.Enter(inX, indL_dK))
            {
                // Todo
                return ILMath.empty();
            }
        }

        public ILRetArray<double> DiagGradParam(ILInArray<double> inX, ILInArray<double> inCovDiag)
        {
            using (ILScope.Enter(inX, inCovDiag))
            {
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> CovDiag = ILMath.check(inCovDiag);
                ILArray<double> gParam = 0;

                gParam = ILMath.sum(CovDiag);

                return gParam * _parameter - 1; // because of the log space
            }
        }

        public ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int q, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                int numData = X1.Size[0];
                int numData2 = X2.Size[0];

                return ILMath.zeros(numData, numData2);
            }
        }

        public void Read(ref XmlReader reader)
        {
            string[] tokens;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Read();

                if (reader.Name == "numParameter")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                            _numParameter = (int)Double.Parse(reader.Value);

                    }
                    reader.Read();
                }
                if (reader.Name == "parameter")
                {
                    reader.MoveToAttribute("data");
                    tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                    if (tokens.Length > 1)
                    {
                        if (tokens[1] == "*")
                        {
                            _parameter = ILMath.zeros(1, tokens.Length - 2);
                            for (int i = 0; i < tokens.Length - 2; i++)
                                _parameter[i] = Double.Parse(tokens[i + 2]) * Double.Parse(tokens[0]);
                        }
                    }
                    else
                    {
                        _parameter = ILMath.zeros(1, tokens.Length);
                        for (int i = 0; i < tokens.Length; i++)
                            _parameter[i] = Double.Parse(tokens[i]);
                    }
                }
            }
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteElementString("numParameter", _numParameter.ToString());

            writer.WriteStartElement("parameter", null);
            writer.WriteAttributeString("data", _parameter.ToString().Remove(0, 14).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();
        }
        #endregion

        #region Private Functions
        
        #endregion
    }
}
