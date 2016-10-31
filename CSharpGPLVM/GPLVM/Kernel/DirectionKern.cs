using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Kernel
{
    [DataContract()]
    public class DirectionKern : IKernel
    {
        [DataMember()]
        private KernelType _type;
        [DataMember()]
        private int _numParameter;        // number of kernel parameter

        [DataMember()]
        private ILArray<double> _parameter = ILMath.localMember<double>();

        //[DataMember()]
        private ILArray<double> _K;         // NxN kernel covariance matrix
        
        //[DataMember()]
        private ILArray<double> _Kuf;

        private ILArray<double> _Krec;
        
        private Guid _key = Guid.NewGuid();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// This type of kernel computes a non-linear correlation between the data points.
        /// </remarks>
        public DirectionKern()
        {
            this._type = KernelType.KernelTypeDirection;

            double _variance = 1f;
            
            this._numParameter = 1;

            _parameter.a = new double[] { _variance };
            _parameter.a = _parameter.T;

            _K = ILMath.empty();
            
            _Kuf = ILMath.empty();
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

        public bool VarianceScaleParameterEnabled
        {
            get
            {
                return (_numParameter == 1);
            }
            set
            {
                _numParameter = (value ? 1 : 0);
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
                if (VarianceScaleParameterEnabled)
                    return _parameter.C;
                else
                    return ILMath.empty();
            }
            set
            {
                if (VarianceScaleParameterEnabled)
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
                if (VarianceScaleParameterEnabled)
                    return ILMath.log(_parameter).C;
                else
                    return ILMath.empty();
            }
            set
            {
                if (VarianceScaleParameterEnabled)
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
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <remarks>
        /// Computing the kernel matrix with kernel function 
        /// 'k({x_i1, x_i2}, {x_j1, x_j2}) = sigma * (x_i2 - x_i1) * (x_j2 - x_j1)^T'.
        /// </remarks>
        /// <param name="X1">First n times 2q matrix of latent points.</param>
        /// <param name="X2">Second m times 2q matrix of latent points.</param>
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
                            _K.a = _parameter[0] * ILMath.multiply(X1_2 - X1_1, (X2_2 - X2_1).T);
                            k = _K;
                        }
                        else
                        {
                            _Kuf.a = _parameter[0] * ILMath.multiply(X1_2 - X1_1, (X2_2 - X2_1).T);
                            k = _Kuf;
                        }
                        break;

                    case Flag.postlearning:
                        _Krec.a = _parameter[0] * ILMath.multiply(X1_2 - X1_1, (X2_2 - X2_1).T);
                        k = _Krec;
                        break;

                    default:
                        k = _parameter[0] * ILMath.multiply(X1_2 - X1_1, (X2_2 - X2_1).T);
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
                
                // Get subarrays of X
                int q = X.Size[1] / 2;
                ILArray<double> X1 = X[ILMath.full, ILMath.r(0, q - 1)];
                ILArray<double> X2 = X[ILMath.full, ILMath.r(q, 2 * q - 1)];
                ILArray<double> vX = X2 - X1;

                return _parameter[0] * ILMath.sum(ILMath.multiplyElem(vX, vX), 1);
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

                // Get subarrays of X
                int q = X.Size[1] / 2;
                ILArray<double> X1 = X[ILMath.full, ILMath.r(0, q - 1)];
                ILArray<double> X2 = X[ILMath.full, ILMath.r(q, 2 * q - 1)];
                ILArray<double> res = ILMath.zeros(X.S);
                res[ILMath.full, ILMath.r(0, q - 1)] = 2 * (X1 - X2);
                res[ILMath.full, ILMath.r(q, 2 * q - 1)] = 2 * (X2 - X1);
                return res;
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
                if (!VarianceScaleParameterEnabled)
                {
                    return ILMath.empty();
                }
                else
                {
                    ILArray<double> gParam = new double[] { 0 };

                    if (dL_dK.S[0] == dL_dK.S[1])
                        gParam[0] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, _K))) / _parameter[0];
                    else
                        gParam[0] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, _Kuf))) / _parameter[0];

                    return ILMath.multiplyElem(gParam, _parameter); // because of the log space
                }
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

                if (!VarianceScaleParameterEnabled)
                {
                    return ILMath.empty();
                }
                else
                {
                    ILArray<double> gParam = 0;

                    for (int i = 0; i < X.S[0]; i++)
                        gParam += ILMath.multiply(X[i, ILMath.full], X[i, ILMath.full].T) * CovDiag[i] / _parameter[0];

                    return gParam * _parameter; // because of the log space
                }
            }
        }

        public ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int j, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                int numData = X1.Size[0];
                int numData2 = X2.Size[0];

                int q = X1.Size[1] / 2;
                int j2 = (j + q) % (2 * q);
                ILArray<double> k = _parameter[0] * (ILMath.repmat(X2[ILMath.full, j].T - X2[ILMath.full, j2].T, numData, 1));
                if (numData == numData2)
                {
                    k += ILMath.diag(ILMath.diag(k));
                }
                return k;                
            }
        }

        public void Read(ref XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(ref XmlWriter writer)
        {
            throw new NotImplementedException();
        }
        #endregion

        [OnDeserialized()]
        public void OnDeserializedMethod(StreamingContext context)
        {
            _K = ILMath.empty();
            _Kuf = ILMath.empty();            
        }
    }
}
