using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Kernel
{
    [DataContract()]
    public class ModulationKern : IKernel
    {
        [DataMember()]
        private KernelType _type;

        [DataMember()]
        private double _modConst = 1.0; // Constant kernel value when _modLevel is set to 0
        [DataMember()]
        private double _modLevel = 1.0; // Modulation level; [0..1] gives kernel values [_modConst.._K]

        [DataMember()]
        private IKernel _kern;          // Internal kernel being modulated
        private ILArray<double> _K = ILMath.localMember<double>();     // NxN kernel matrix
        private ILArray<double> _Kuf = ILMath.localMember<double>();
        private ILArray<double> _Krec = ILMath.localMember<double>();

        private Guid _key = Guid.NewGuid();

        /// <summary>
        /// The class constructor
        /// </summary>
        /// <remarks>
        /// This type of kernel computes a non-linear correlation between the data points.
        /// </remarks>
        public ModulationKern()
        {
            this._type = KernelType.KernelTypeModulation;
        }

        /// <summary>
        /// Sets inner kernel
        /// </summary>
        /// <param name="k"></param>
        public void SetInnerKern(IKernel k)
        {
            _kern = k;
        }

        public IKernel GetInnerKern()
        {
            return _kern;
        }

        public double ModulationLevel
        {
            get { return _modLevel; }
            set { _modLevel = value; }
        }

        public double ModulationConstant
        {
            get { return _modConst; }
            set { _modConst = value; }
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
        ///The kernel matrix of type ILArray<double>.
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
        [IgnoreDataMember]
        public int NumParameter
        {
            get
            {
                return _kern.NumParameter;
            }
        }

        ///<summary>
        ///Gets the kernel parameters of type ILArray<double>.
        ///</summary>
        [IgnoreDataMember]
        public ILArray<double> Parameter
        {
            get
            {
                return _kern.Parameter;
            }
            set
            {
                _kern.Parameter = value;
            }
        }

        ///<summary>
        ///Gets the the log kernel parameters of type ILArray<double>.
        ///</summary>
        [IgnoreDataMember]
        public ILArray<double> LogParameter
        {
            get
            {
                return _kern.LogParameter;
            }
            set
            {
                _kern.LogParameter = value;
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

        /// <summary>
        /// Modulates a  matrix (blending between the matrix values and _modConst)
        /// </summary>
        /// <param name="inKMatrix"></param>
        /// <returns></returns>
        private ILRetArray<double> ModulateMatrix(ILInArray<double> inKMatrix)
        {
            using (ILScope.Enter(inKMatrix))
            {
                ILArray<double> KMatrix = ILMath.check(inKMatrix);
                return (1 - _modLevel) * _modConst + _modLevel * KMatrix;
            }
        }

        #region Public Computations
        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <remarks>
        /// Computing the kernel matrix with kernel function 
        /// 'k({x_i1, x_i2}, {x_j1, x_j2}) = (1 - alpha) * C + alpha * kern({x_i1, x_i2}, {x_j1, x_j2}).
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

                ILArray<double> k = ILMath.empty();

                switch (flag)
                {
                    case Flag.learning:
                        k.a = ModulateMatrix(_kern.ComputeKernelMatrix(X1, X2, flag));
                        if (X1.S[0] == X2.S[0])
                        {
                            _K.a = k.C;
                        }
                        else
                        {
                            _Kuf.a = k.C;
                        }
                        break;

                    case Flag.postlearning:
                        k.a = ModulateMatrix(_kern.ComputeKernelMatrix(X1, X2, flag));
                        _Krec.a = k.C;
                        break;

                    default:
                        k.a = ModulateMatrix(_kern.ComputeKernelMatrix(X1, X2, flag));
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

                return ModulateMatrix(_kern.ComputeDiagonal(X));
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

                return _modLevel * _kern.LogLikGradientX(X, dL_dK);
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

                ILCell tmp = ILMath.cell();
                tmp = _kern.LogLikGradientX(Xu, dL_dKuu, X, dL_dKuf);
                dL_dXu.a = _modLevel * tmp.GetArray<double>(0);
                dL_dX.a = _modLevel * tmp.GetArray<double>(1);

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

                return _modLevel * _kern.DiagGradX(X);
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

                return _modLevel * _kern.LogLikGradientParam(dL_dK);
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

                return _modLevel * _kern.DiagGradParam(X, CovDiag);
            }
        }

        public ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int j, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                return _modLevel * _kern.GradX(X1, X2, j, flag);
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
            _K = ILMath.localMember<double>();
            _Kuf = ILMath.localMember<double>();
        }
    }
}
