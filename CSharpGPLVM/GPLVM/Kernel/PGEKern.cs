using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;
using System.Collections.Generic;

namespace GPLVM.Kernel
{
    // Product of Gaussian experts kernel
    [DataContract()]
    [KnownType(typeof(BiasKern))]
    [KnownType(typeof(CompoundKern))]
    [KnownType(typeof(LinearAccelerationKern))]
    [KnownType(typeof(LinearKern))]
    [KnownType(typeof(RBFAccelerationKern))]
    [KnownType(typeof(RBFKern))]
    [KnownType(typeof(RBFKernBack))]
    [KnownType(typeof(StyleKern))]
    [KnownType(typeof(StyleAccelerationKern))]
    [KnownType(typeof(TensorKern))]
    [KnownType(typeof(WhiteKern))]
    [KnownType(typeof(ModulationKern))]
    [KnownType(typeof(DirectionKern))]
    [KnownType(typeof(PGEKern))]

    public class PGEKern : IKernel
    {
        [DataMember()]
        private KernelType _type;       // the type of the kernel
        [DataMember()]
        private int _numParameter;      // number of kernel parameter, M sigmas + inner kernel parameters
        [DataMember()]
        private ILArray<double> _parameter = ILMath.localMember<double>(); // M sigmas + inner kernel parameters

        //[DataMember()]
        private ILArray<double> _K = ILMath.localMember<double>();         // NxN kernel covariance matrix, combined
        //[DataMember()]
        private ILArray<double> _Kuf = ILMath.localMember<double>();

        private ILArray<double> _Krec = ILMath.localMember<double>();

        private Guid _key = Guid.NewGuid();

        [DataMember()]
        private List<IKernel> _kernels;             // M inner kernels for predictions from every part
        [DataMember()]
        private List<ILArray<int>> _indexes;     // list of M arrays of indexes for every kernel

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// This type of kernel is a collection of kernels, which were individualy added by the user.
        /// Style kernels can also be part of the list.
        /// </remarks>
        public PGEKern()
        {
            this._type = KernelType.KernelTypePGE;
            this._numParameter = 0;
            _kernels = new List<IKernel>();
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
            set
            {
                _K.a = value.C;
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
            set
            {
                _Kuf.a = value.C;
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

        public List<ILArray<int>> Indexes
        {
            get
            {
                return _indexes;
            }
            set
            {
                _indexes = value;
            }
        }

        ///<summary>
        ///Gets the kernel parameters of type ILArray<double>.
        ///</summary>
        public ILArray<double> Parameter
        {
            get
            {
                int startVal = _kernels.Count, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal = startVal + kern.NumParameter;
                    _parameter[ILMath.r(startVal, endVal - 1)] = kern.Parameter;
                    startVal = endVal;
                }
                return _parameter.C;
            }
            set
            {
                _parameter.a = value.C;
                int startVal = _kernels.Count, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal = startVal + kern.NumParameter;
                    kern.Parameter = _parameter[ILMath.r(startVal, endVal - 1)];
                    startVal = endVal;
                }
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                ILArray<double> res = ILMath.zeros(_parameter.S);
                res[ILMath.r(0, _kernels.Count - 1)] = ILMath.log(_parameter[ILMath.r(0, _kernels.Count - 1)]);
                int startVal = _kernels.Count, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal = startVal + kern.NumParameter;
                    res[ILMath.r(startVal, endVal - 1)] = kern.LogParameter;
                    startVal = endVal;
                }
                return res.C;
            }
            set
            {
                _parameter.a = value.C;
                _parameter[ILMath.r(0, _kernels.Count - 1)] = Util.atox(_parameter[ILMath.r(0, _kernels.Count - 1)]);
                int startVal = _kernels.Count, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal = startVal + kern.NumParameter;
                    kern.LogParameter = _parameter[ILMath.r(startVal, endVal - 1)];
                    startVal = endVal;
                }
            }
        }

        ///<summary>
        ///Gets the kernel list of the kernel.
        ///</summary>
        public List<IKernel> Kernels
        {
            get
            {
                return _kernels;
            }
            set
            {
                _kernels = value;
                _numParameter = _kernels.Count;     // sigmas
                foreach (var k in _kernels)         // inner kernels parameters
                    _numParameter += k.NumParameter;
                _parameter.a = ILMath.zeros(1, _numParameter);
                _parameter[ILMath.r(0, _kernels.Count - 1)] = (double)ILMath.exp((double)-2); // initialize sigmas
            }
        }

        /// <summary>
        /// Returns arays of variances for every kernel
        /// </summary>
        protected ILRetArray<double> SigmaSqr
        {
            get
            {
                return _parameter[ILMath.r(0, _kernels.Count - 1)];
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

        public double Noise
        {
            get { return 0.0; }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <remarks>
        /// Computing the weigted sum of kernels to form a Product of Gaussian experts kernel
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the identity kernel matrix times noise of type ILArray<double>.
        /// </returns>
        public ILRetArray<double> ComputeKernelMatrix(ILInArray<double> inX1, ILInArray<double> inX2, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                ILArray<double> k = ILMath.zeros<double>(X1.Size[0], X2.Size[0]);
                
                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                double sigmaSqrTotal = 1 / (double)ILMath.sum(1 / sigmaSqr);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);
                
                // Init indexes if they are not set externally yet
                List<ILArray<int>> indexes = _indexes;
                if (indexes == null)
                {
                    indexes = new List<ILArray<int>>();
                    foreach (IKernel kern in _kernels)
                    {
                        ILArray<int> indFull = ILMath.localMember<int>();
                        indFull.a = ILMath.counter<int>(0, 1, X1.S[1]);
                        indexes.Add(indFull);
                    }
                }
                
                if (flag == Flag.learning)
                {
                    if (X1.S[0] == X2.S[0]) // check whether it is a full fit or an approximation
                    {
                        // For FTC
                        int cnt = 0;
                        foreach (IKernel kern in _kernels)
                        {
                            // For normal kernels; should be extended for the style kernels
                            k.a = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, indexes[cnt]], X2[ILMath.full, indexes[cnt]], flag) 
                                / (sigmaSqr[cnt] * sigmaSqr[cnt])); 
                            cnt++;
                        }
                        k.a = sigmaSqrTotalSqr * k + sigmaSqrTotal * ILMath.eye<double>(X1.S[0], X2.S[0]);
                        _K.a = k.C;
                    }
                    else 
                    {
                        // For approximate learning with inducing variables
                        int cnt = 0;
                        foreach (IKernel kern in _kernels)
                        {
                            // For normal kernels; should be extended for the style kernels
                            k.a = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, indexes[cnt]], X2[ILMath.full, indexes[cnt]], flag)
                                / (sigmaSqr[cnt] * sigmaSqr[cnt]));
                            cnt++;
                        }
                        k.a = sigmaSqrTotalSqr * k;
                        _Kuf.a = k.C;
                    }
                }
                else
                {
                    int cnt = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        // For normal kernels; should be extended for the style kernels
                        k.a = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, indexes[cnt]], X2[ILMath.full, indexes[cnt]], flag) 
                            / (sigmaSqr[cnt] * sigmaSqr[cnt]));
                        cnt++;
                    }
                    k.a = sigmaSqrTotalSqr * k;
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

        protected List<ILArray<int>> GetIndexes(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);

                // Init indexes if they are not set externally yet
                List<ILArray<int>> indexes = _indexes;
                if (indexes == null)
                {
                    indexes = new List<ILArray<int>>();
                    foreach (IKernel kern in _kernels)
                    {
                        ILArray<int> indFull = ILMath.counter<int>(0, 1, X.S[1]);
                        indexes.Add(indFull);
                    }
                }
                return indexes;
            }
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

                List<ILArray<int>> indexes = GetIndexes(X);
                
                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                double sigmaSqrTotal = 1 / (double)ILMath.sum(1 / sigmaSqr);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);
                
                ILArray<double> k = ILMath.zeros<double>(X.Size[0], 1);
                int cnt = 0;
                foreach (IKernel kern in _kernels)
                {
                    // For normal kernels; should be extended for the style kernels
                    k.a = ILMath.add(k, kern.ComputeDiagonal(X[ILMath.full, indexes[cnt]])
                        / (sigmaSqr[cnt] * sigmaSqr[cnt]));
                    cnt++;
                }
                k.a = sigmaSqrTotalSqr * k + ILMath.repmat(sigmaSqrTotal, X.S[0], 1);
                return k;
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

                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                ILArray<double> sigmaSqrInv = 1 / sigmaSqr;
                double sigmaSqrTotal = 1 / (double)ILMath.sum(sigmaSqrInv);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);
                double sigmaSqrInvSum = (double)ILMath.sum(sigmaSqrInv);
                
                ILArray<double> dL_dX = ILMath.zeros(X.Size);
                int i = 0;
                foreach (IKernel kern in _kernels)
                {
                    dL_dX.a = dL_dX + kern.LogLikGradientX(X, dL_dK)
                                    / Math.Pow((double)sigmaSqr[i], 2);
                    i++;
                }
                dL_dX.a = sigmaSqrTotalSqr * dL_dX;
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

                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                ILArray<double> sigmaSqrInv = 1 / sigmaSqr;
                double sigmaSqrTotal = 1 / (double)ILMath.sum(sigmaSqrInv);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);
                double sigmaSqrInvSum = (double)ILMath.sum(sigmaSqrInv);

                ILCell tmp = ILMath.cell();
                int i = 0;
                foreach (IKernel kern in _kernels)
                {
                    tmp = kern.LogLikGradientX(Xu, dL_dKuu, X, dL_dKuf);
                    double factor = sigmaSqrTotalSqr * Math.Pow(sigmaSqrInvSum, -2) / Math.Pow((double)sigmaSqr[i], 2);
                    dL_dXu.a = ILMath.add(dL_dXu, tmp.GetArray<double>(0) * factor);
                    dL_dX.a = ILMath.add(dL_dX, tmp.GetArray<double>(1) * factor);
                    i++;
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

                List<ILArray<int>> indexes = GetIndexes(X);
                ILArray<double> tmp = ILMath.zeros(X.S);

                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                ILArray<double> sigmaSqrInv = 1 / sigmaSqr;
                double sigmaSqrTotal = 1 / (double)ILMath.sum(sigmaSqrInv);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);
                double sigmaSqrInvSum = (double)ILMath.sum(sigmaSqrInv);

                int cnt = 0;
                foreach (IKernel kern in _kernels)
                {
                    double factor = sigmaSqrTotalSqr * Math.Pow(sigmaSqrInvSum, -2) / Math.Pow((double)sigmaSqr[cnt], 2);
                    tmp[ILMath.full, indexes[cnt]] += factor * kern.DiagGradX(X[ILMath.full, indexes[cnt]]);
                    cnt++;
                }

                return tmp;
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

                ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                // Gradients of sigmas squared
                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                ILArray<double> sigmaSqrInv = 1 / sigmaSqr;
                double sigmaSqrInvSum = (double)ILMath.sum(sigmaSqrInv);
                double sigmaSqrTotal = 1 / (double)ILMath.sum(sigmaSqrInv);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);

                int i;
                ILArray<double> kernSum = ILMath.empty();
                if (dL_dK.S[0] == dL_dK.S[1])
                {
                    kernSum = ILMath.zeros(_K.S);
                    i = 0;
                    foreach (var kern in _kernels)
                    {
                        kernSum.a = kernSum + kern.K / (double)(sigmaSqr[i] * sigmaSqr[i]);
                        i++;
                    }

                    i = 0;
                    foreach (var kern in _kernels)
                    {
                        double sigmaCurr = (double)sigmaSqr[i];
                        ILArray<double> dK_dSigmaSqr = ILMath.empty();
                        dK_dSigmaSqr.a = 2 * sigmaCurr
                                         / Math.Pow((sigmaCurr * (sigmaSqrInvSum - 1 / sigmaCurr) + 1), 3) * kernSum
                                         - 2 * Math.Pow(sigmaSqrInvSum, -2) / Math.Pow(sigmaCurr, 3) * kern.K
                                         + ILMath.eye(_K.S[0], _K.S[1]) * Math.Pow((sigmaCurr * (sigmaSqrInvSum - 1 / sigmaCurr) + 1), -2);

                        gParam[i] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, dK_dSigmaSqr)));
                        gParam[i] = gParam[i] * sigmaCurr; // because of the log space
                        i++;
                    }
                }
                else
                {
                    kernSum = ILMath.zeros(_Kuf.S);
                    i = 0;
                    foreach (var kern in _kernels)
                    {
                        kernSum.a = kernSum + kern.Kuf / (double)(sigmaSqr[i] * sigmaSqr[i]);
                        i++;
                    }

                    i = 0;
                    foreach (var kern in _kernels)
                    {
                        double sigmaCurr = (double)sigmaSqr[i];
                        ILArray<double> dK_dSigmaSqr = ILMath.empty();
                        dK_dSigmaSqr.a = 2 * sigmaCurr
                                         / Math.Pow((sigmaCurr * (sigmaSqrInvSum - 1 / sigmaCurr) + 1), 3) * kernSum
                                         - 2 * Math.Pow(sigmaSqrInvSum, -2) / Math.Pow(sigmaCurr, 3) * kern.Kuf
                                         + ILMath.eye(_Kuf.S[0], _Kuf.S[1]) * Math.Pow((sigmaCurr * (sigmaSqrInvSum - 1 / sigmaCurr) + 1), -2);

                        gParam[i] = ILMath.sum(ILMath.sum(ILMath.multiplyElem(dL_dK, dK_dSigmaSqr)));
                        gParam[i] = gParam[i] * sigmaCurr; // because of the log space
                        i++;
                    }
                }

                // Parameters gradients of the inner kernels
                int startVal = _kernels.Count;
                int endVal = 0;
                i = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal = startVal + kern.NumParameter;
                    double kernelFactor = sigmaSqrTotalSqr / Math.Pow((double)sigmaSqr[i], 2);
                    gParam[ILMath.r(startVal, endVal - 1)] = kernelFactor * kern.LogLikGradientParam(dL_dK);
                    startVal = endVal;
                    i++;
                }

                return gParam;
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
                ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                List<ILArray<int>> indexes = GetIndexes(X);

                ILArray<double> sigmaSqr = SigmaSqr; // for faster access
                ILArray<double> sigmaSqrInv = 1 / sigmaSqr;
                double sigmaSqrTotal = 1 / (double)ILMath.sum(sigmaSqrInv);
                double sigmaSqrTotalSqr = Math.Pow(sigmaSqrTotal, 2);
                double sigmaSqrInvSum = (double)ILMath.sum(sigmaSqrInv);

                int i = 0;
                foreach (IKernel kern in _kernels)
                {
                    gParam[i] = ILMath.sum(CovDiag[indexes[i]]);
                    i++;
                }

                int startVal = _kernels.Count, endVal = 0;
                int cnt = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal = startVal + kern.NumParameter;
                    double factor = sigmaSqrTotalSqr * Math.Pow(sigmaSqrInvSum, -2) / Math.Pow((double)sigmaSqr[cnt], 2);
                    gParam[ILMath.r(startVal, endVal - 1)] = factor * kern.DiagGradParam(X[ILMath.full, _indexes[cnt++]], CovDiag);
                    startVal = endVal;
                }

                return gParam;
            }
        }

        public ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int q, Flag flag = Flag.learning)
        {
            throw new NotImplementedException();
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

        #region Private Functions

        #endregion

        [OnDeserialized()]
        public void OnDeserializedMethod(StreamingContext context)
        {
            _K = ILMath.localMember<double>();
            _Kuf = ILMath.localMember<double>();
        }
    }
}
