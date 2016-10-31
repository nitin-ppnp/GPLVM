using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Kernel
{
    [DataContract()]
    public class RBFAccelerationKern : IKernel
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
        private ILArray<double> _n2 = ILMath.localMember<double>();
        [DataMember()]
        private ILArray<double> _m2 = ILMath.localMember<double>();

        [DataMember()]
        private ILArray<double> _Kuf = ILMath.localMember<double>();
        [DataMember()]
        private ILArray<double> _n2uf = ILMath.localMember<double>();
        [DataMember()]
        private ILArray<double> _m2uf = ILMath.localMember<double>();

        [DataMember()]
        private ILArray<double> _Krec = ILMath.localMember<double>();         // reconstruction kernel covariance matrix
        [DataMember()]
        private ILArray<double> _n2rec = ILMath.localMember<double>();
        [DataMember()]
        private ILArray<double> _m2rec = ILMath.localMember<double>();

        [DataMember()]
        private Guid _key = Guid.NewGuid();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// This type of kernel computes a non-linear correlation between the data points.
        /// </remarks>
        public RBFAccelerationKern()
        {
            this._type = KernelType.KernelTypeRBFAcceleration;

            double _variance = 1f;
            double _inverseScale = 1f;
            double _inverseScale2 = 1f;

            this._numParameter = 3;

            _parameter.a = new double[] { _inverseScale, _inverseScale2, _variance };
            _parameter.a = _parameter.T;
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

        ///<summary>
        ///Gets the the log kernel parameters of type ILArray<double>.
        ///</summary>
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
            get { return 0.0; }
        }
        #endregion

        #region Public Computations
        /// <summary>
        /// Computes the kernel matrix.
        /// </summary>
        /// <remarks>
        /// Computing the kernel matrix with kernel function 'k(x_i, x_j) = sigma * exp(-gamma/2 *(x_i - x_j) * (x_i - x_j)^T)'.
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

                int q = X1.Size[1] / 2;

                // Get subarrays of X1
                ILArray<double> X1_1 = X1[ILMath.full, ILMath.r(0, q - 1)];
                ILArray<double> X1_2 = X1[ILMath.full, ILMath.r(q, 2 * q - 1)];

                // Get subarrays of X2
                ILArray<double> X2_1 = X2[ILMath.full, ILMath.r(0, q - 1)];
                ILArray<double> X2_2 = X2[ILMath.full, ILMath.r(q, 2 * q - 1)];

                ILRetArray<double> k = ILMath.empty();

                switch(flag)
                {
                    case Flag.learning:
                        if (X1.S[0] == X2.S[0])
                        {
                            _n2.a = SqareDistance(X1_1, X2_1);
                            _m2.a = SqareDistance(X1_2, X2_2);

                            _K.a = _parameter[2] * ILMath.exp(-_n2 * 0.5f * _parameter[0] - _m2 * 0.5f * _parameter[1]);
                            k = _K;
                        }
                        else
                        {
                            _n2uf.a = SqareDistance(X1_1, X2_1);
                            _m2uf.a = SqareDistance(X1_2, X2_2);

                            _Kuf.a = _parameter[2] * ILMath.exp(-_n2uf * 0.5f * _parameter[0] - _m2uf * 0.5f * _parameter[1]);
                            k = _Kuf;
                        }
                        break;

                    case Flag.postlearning:
                        _n2rec.a = SqareDistance(X1_1, X2_1);
                        _m2rec.a = SqareDistance(X1_2, X2_2);

                        _Krec.a = _parameter[2] * ILMath.exp(-_n2rec * 0.5f * _parameter[0] - _m2rec * 0.5f * _parameter[1]);
                        k = _Krec;
                        break;

                    default:
                        ILArray<double> n2 = SqareDistance(X1_1, X2_1);
                        ILArray<double> m2 = SqareDistance(X1_2, X2_2);

                        k = _parameter[2] * ILMath.exp(-n2 * 0.5f * _parameter[0] - m2 * 0.5f * _parameter[1]);
                        break;
                }
                return k;
            }
        }

        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <param name="data">The data dictionary.</param>
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

                return ILMath.repmat(_parameter[2], X.S[0], 1);
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
                ILArray<double> gParam = new double[] { 0, 0, 0 };
                gParam = gParam.T;

                ILArray<double> tmp = ILMath.empty();
                if (dL_dK.S[0] == dL_dK.S[1])
                {
                    tmp = -0.5 * ILMath.multiplyElem(_K, _n2);
                    gParam[0] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, tmp)));
                    tmp.a = -0.5 * ILMath.multiplyElem(_K, _m2);
                    gParam[1] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, tmp)));
                    tmp.a = _K / _parameter[2];
                    gParam[2] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, tmp)));
                }
                else
                {
                    tmp = -0.5 * ILMath.multiplyElem(_Kuf, _n2uf);
                    gParam[0] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, tmp)));
                    tmp.a = -0.5 * ILMath.multiplyElem(_Kuf, _m2uf);
                    gParam[1] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, tmp)));
                    tmp.a = _Kuf / _parameter[2];
                    gParam[2] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, tmp)));
                }
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
                ILArray<double> gParam = new double[] { 0, 0, 0 };
                gParam = gParam.T;

                gParam[2] = ILMath.sum(CovDiag);

                return ILMath.multiplyElem(gParam, _parameter); // because of the log space
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

                ILArray<double> K1 = ILMath.repmat(X1[ILMath.full, q], 1, numData2);
                ILArray<double> K2 = ILMath.repmat(X2[ILMath.full, q].T, numData, 1);

                if (flag == Flag.learning)
                {
                    if (q < X1.Size[1] / 2)
                        if (numData == numData2)
                            return -_parameter[0] * ILMath.multiplyElem((K1 - K2), _K);
                        else
                            if (K1.S[0] == _Kuf.S[0])
                                return -_parameter[0] * ILMath.multiplyElem((K1 - K2), _Kuf);
                            else
                                return -_parameter[0] * ILMath.multiplyElem((K1 - K2), _Kuf.T);
                    else
                        if (numData == numData2)
                            return -_parameter[1] * ILMath.multiplyElem((K1 - K2), _K);
                        else
                            if (K1.S[0] == _Kuf.S[0])
                                return -_parameter[1] * ILMath.multiplyElem((K1 - K2), _Kuf);
                            else
                                return -_parameter[1] * ILMath.multiplyElem((K1 - K2), _Kuf.T);
                }
                else
                    if (q < X1.Size[1] / 2)
                        return -_parameter[0] * ILMath.multiplyElem((K1 - K2), _Krec);
                    else
                        return -_parameter[1] * ILMath.multiplyElem((K1 - K2), _Krec);
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
                    if (tokens.Length > 3)
                    {
                        if (tokens[2] == "*")
                        {
                            _parameter = ILMath.zeros(1, tokens.Length - 3);
                            for (int i = 0; i < tokens.Length - 3; i++)
                                _parameter[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
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
                writer.WriteAttributeString("data", _parameter.ToString().Remove(0, _parameter.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();
        }
        #endregion

        #region Private Computations
        /// <summary>
        /// Computes the square distance between two q-dimensional arrays of points.
        /// </summary>
        /// <remarks>
        /// Computing the square distance '(x_i - x_j) * (x_i - x_j)^T'.
        /// </remarks>
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<double>.
        /// </returns>
        private ILRetArray<double> SqareDistance(ILInArray<double> inX1, ILInArray<double> inX2)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                int ndata = X1.Size[0]; // number of rows
                int dimx = X1.Size[1];  // number of collums
                int ncentres = X2.Size[0];
                int dimc = X2.Size[1];

                if (dimx != dimc)
                {
                    System.Console.WriteLine("Data dimension does not match dimension of centres");
                    return 0;
                }

                ILArray<double> n2 = ILMath.multiply(ILMath.ones<double>(ncentres, 1), ILMath.sum(ILMath.pow(X1, 2).T, 0)).T +
                    ILMath.multiply(ILMath.ones<double>(ndata, 1), ILMath.sum(ILMath.pow(X2, 2).T, 0)) -
                    2.0f * ILMath.multiply(X1, X2.T);

                // Rounding errors occasionally cause negative entries in n2
                if (ILMath.any(ILMath.any(n2 < 0f)))
                {
                    n2[n2 < 0f] = 0f;
                    // or: 
                    // ILArray<int> ind = (int)ILMath.find(n2 < 0f);
                    // for (int i = 0; i < ind.Length; i++)
                    //    n2[ind[i]] = 0f;    // equivalent: n2.SetValue(0f, ind.GetValue(i));
                }

                return n2;
            }
        }
        #endregion
    }
}
