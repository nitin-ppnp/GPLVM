using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Kernel
{
    [DataContract()]
    public class WhiteKern : IKernel
    {
        [DataMember()]
        private KernelType _type;
        [DataMember()]
        private int _numParameter;        // number of kernel parameter
        [DataMember()]
        private ILArray<double> _parameter = ILMath.localMember<double>();

        [DataMember()]
        private Guid _key = Guid.NewGuid();

        [DataMember()]
        private ILArray<double> _K = ILMath.localMember<double>();         // NxN kernel covariance matrix
        [DataMember()]
        private ILArray<double> _Kuf = ILMath.localMember<double>();         // NxN kernel covariance matrix
        [DataMember()]
        private ILArray<double> _Krec = ILMath.localMember<double>();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// This type of kernel adds the noise on the diagonal.
        /// </remarks>
        public WhiteKern()
        {
            this._type = KernelType.KernelTypeWhite;
            _parameter.a = ILMath.exp((double)-2);
            this._numParameter = 1;
        }
        
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
        ///The kernel matri of type ILArray<double>.
        ///</summary>
        public ILArray<double> Krec
        {
            get
            {
                return _Krec.C;
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

        public double Noise
        {
            get { return (double)_parameter.C; }
        }

        /// <summary>
        /// Computes the kernel matrix.
        /// </summary>
        /// <remarks>
        /// Computing the kernel matrix with kernel function 'k(x_i, x_j) = noise; iff x_i == x_j, 0 otherwise'.
        /// </remarks>
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<double>.
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
                        if (ILMath.isequal(X1, X2))
                        {
                            _K.a = _parameter * ILMath.eye<double>(X1.Size[0], X2.Size[0]);
                            k = _K;
                        }
                        else
                        {
                            _Kuf.a = ILMath.zeros<double>(X1.Size[0], X2.Size[0]);
                            k = _Kuf;
                        }
                        break;
                    case Flag.postlearning:
                        _Krec.a = ILMath.zeros<double>(X1.Size[0], X2.Size[0]);
                        k = _Krec.C;
                        break;

                    default:
                        k = ILMath.zeros<double>(X1.Size[0], X2.Size[0]);
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

                return ILMath.repmat(_parameter, X.S[0], 1);
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
                return ILMath.zeros(X.Size);
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

                ILRetCell ret = ILMath.cell(ILMath.zeros(Xu.Size), ILMath.zeros(X.Size));

                return ret;
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
                ILArray<double> gParam = ILMath.empty();

                if (dL_dK.S[0] == dL_dK.S[1])
                    gParam = ILMath.trace(dL_dK);
                else
                    gParam = 0;

                return gParam * _parameter; // because of the log
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
                ILArray<double> gParam = ILMath.empty();

                gParam = ILMath.sum(CovDiag);

                return gParam * _parameter; // because of the log space
            }
        }

        public ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int q, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                return ILMath.zeros(X1.Size[0], X2.Size[0]);
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
                        if (tokens[2] == "*")
                        {
                            _parameter.a = ILMath.zeros(1, tokens.Length - 3);
                            for (int i = 0; i < tokens.Length - 3; i++)
                                _parameter[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                        }
                    }
                    else
                    {
                        _parameter.a = ILMath.zeros(1, tokens.Length);
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
            writer.WriteAttributeString("data", _parameter[0].ToString().Replace("\n", "").Replace("\r", "").Remove(0, 9));//
            writer.WriteEndElement();
        }

        #region Private Computations
        
        #endregion
    }
}
