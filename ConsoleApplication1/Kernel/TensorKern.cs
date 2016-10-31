using System;
using System.Xml;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ILNumerics;

namespace GPLVM.Kernel
{
    [DataContract()]
    public class TensorKern : IKernel
    {
        [DataMember()]
        private KernelType _type;       // the type of the kernel
        [DataMember()]
        private int _numParameter;      // number of kernel parameter
        [DataMember()]
        private ILArray<double> _parameter = ILMath.localMember<double>();

        [DataMember()]
        private ILArray<double> _K = ILMath.localMember<double>();         // NxN kernel covariance matrix
        [DataMember()]
        private ILArray<double> _Kuf = ILMath.localMember<double>();

        [DataMember()]
        private ILArray<double> _Krec = ILMath.localMember<double>();         // reconstruction kernel covariance matrix

        [DataMember()]
        private Guid _key = Guid.NewGuid();

        [DataMember()]
        private List<IKernel> _kernels;

        [DataMember()]
        List<ILArray<double>> _indexes;

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// The tensor product (TENSOR) kernel is a container kernel for
        /// allowing tensor products kernels of separate component kernels.
        /// </remarks>
        public TensorKern()
        {
            this._type = KernelType.KernelTypeTensor;
            this._numParameter = 0;
            _kernels = new List<IKernel>();
            _indexes = null;
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

        public List<ILArray<double>> Indexes
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

        public void AddIndex(ILInArray<double> inIndex)
        {
            if (_indexes == null) _indexes = new List<ILArray<double>>();

            ILArray<double> index = ILMath.check(inIndex);

            _indexes.Add(ILMath.localMember<double>());
            _indexes[_indexes.Count - 1].a = index.C;
        }

        ///<summary>
        ///Gets the kernel parameters of type ILArray<double>.
        ///</summary>
        public ILArray<double> Parameter
        {
            get
            {
                _parameter.a = ILMath.zeros(1, NumParameter);

                int startVal = 0, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal += kern.NumParameter;
                    _parameter[ILMath.r(startVal, endVal - 1)] = kern.Parameter;
                    startVal = endVal;
                }
                return _parameter.C;
            }
            set
            {
                _parameter.a = value;
                int startVal = 0, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal += kern.NumParameter;
                    kern.Parameter = _parameter[ILMath.r(startVal, endVal - 1)];
                    startVal = endVal;
                }
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                _parameter = ILMath.zeros(1, NumParameter);

                int startVal = 0, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal += kern.NumParameter;
                    _parameter[ILMath.r(startVal, endVal - 1)] = kern.LogParameter;
                    startVal = endVal;
                }
                return _parameter.C;
            }
            set
            {
                _parameter.a = value;
                int startVal = 0, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal += kern.NumParameter;
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
            get
            {
                double noise = 0;
                foreach (IKernel kern in _kernels)
                {
                    double cnoise = kern.Noise;
                    if (cnoise > noise)
                        noise = cnoise;
                }

                return noise;
            }
        }

        ///<summary>
        ///Adds a new kern to the List.
        ///</summary>
        public void AddKern(IKernel newKern)
        {
            _kernels.Add(newKern);
            _numParameter += newKern.NumParameter;
        }

        public void RemoveKern(string key)
        {
            foreach (IKernel kern in _kernels)
            {
                if (kern.Key.ToString() == key)
                    _kernels.Remove(kern);
            }
        }

        public IKernel GetKern(string key)
        {
            foreach (IKernel kern in _kernels)
            {
                if (kern.Key.ToString() == key)
                    return kern;
            }

            return null;
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <remarks>
        /// Computing the tensor product 'K = K_1 .* ... .* K_J', where '[K_1, ..., K_J]' are the kernel matrices computed 
        /// by the corresponding kernel functions in the list to get a logical 'AND' effect of point correlations.
        /// <param name="X1">First N times q matrix of latent points.</param>
        /// <param name="X2">Second M times q matrix of latent points.</param>
        /// <returns>
        /// Computed NxM tensor kernel matrix.<double>.
        /// </returns>
        public ILRetArray<double> ComputeKernelMatrix(ILInArray<double> inX1, ILInArray<double> inX2, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                ILArray<double> k = ILMath.ones<double>(X1.Size[0], X2.Size[0]);

                ILRetArray<double> k_ = ILMath.empty();

                if (_indexes == null)
                {
                    switch(flag)
                    {
                        case Flag.learning:
                            if (X1.S[0] == X2.S[0])
                            {
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1, X2, flag));
                                }
                                _K.a = k;
                                k_ = k;
                            }
                            else
                            {
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1, X2, flag));
                                }
                                _Kuf.a = k;
                                k_ = k;
                            }
                            break;
                        case Flag.postlearning:
                            foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1, X2, flag));
                                }
                                _Krec.a = k;
                                k_ = k;
                            break;

                        default:
                            foreach (IKernel kern in _kernels)
                            {
                                k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1, X2, flag));
                            }
                            k_ = k;
                            break;
                    }
                }
                else
                {
                    if (_indexes.Count == _kernels.Count)
                    {
                        int cnt = 0;
                        switch(flag)
                        {
                            case Flag.learning:
                                if (X1.S[0] == X2.S[0])
                                {
                                    foreach (IKernel kern in _kernels)
                                    {
                                        k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag));
                                        cnt++;
                                    }
                                    _K.a = k;
                                    k_ = k;
                                }
                                else
                                {
                                    foreach (IKernel kern in _kernels)
                                    {
                                        k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag));
                                        cnt++;
                                    }
                                    _Kuf.a = k;
                                    k_ = k;
                                }
                                break;
                            case Flag.postlearning:
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag));
                                    cnt++;
                                }
                                _Krec.a = k;
                                k_ = k;
                                break;

                            default:
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.multiplyElem(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag));
                                    cnt++;
                                }
                                k_ = k;
                                break;
                        }
                    }
                    else
                        Console.WriteLine("\nIndex list and kernel list must have the same length!");
                }
                return k_;
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

                ILArray<double> k = ILMath.ones<double>(X.Size[0], 1);

                if (_indexes == null)
                {
                    foreach (IKernel kern in _kernels)
                    {
                        k = ILMath.multiplyElem(k, kern.ComputeDiagonal(X));
                    }
                }
                else
                {
                    int cnt = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        k = ILMath.multiplyElem(k, kern.ComputeDiagonal(X[ILMath.full, _indexes[cnt]]));
                        cnt++;
                    }
                }

                ILRetArray<double> _k = k;

                return _k;
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
                    dL_dX[ILMath.full, q] = (2 * ILMath.sum(ILMath.multiplyElem(dL_dK, dK_dX), 1) - ILMath.multiplyElem(ILMath.diag(dL_dK), ILMath.diag(dK_dX)));
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

        // TODO
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

                ILArray<double> tmp = ILMath.zeros(X.S);
                ILArray<double> Ktmp;

                List<IKernel> tmpKernels;

                if (_indexes == null)
                {
                    Ktmp = ILMath.ones(X.Size[0], 1);
                    for (int cnt = 0; cnt < _kernels.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);
                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.ComputeDiagonal(X));
                        }
                        tmp += ILMath.multiplyElem(_kernels[cnt].DiagGradX(X), ILMath.repmat(Ktmp, 1, X.S[1]));
                    }
                }
                else
                {
                    for (int cnt = 0; cnt < _indexes.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);
                        List<ILArray<double>> tmpIndex = new List<ILArray<double>>(_indexes);
                        tmpIndex.RemoveAt(cnt);

                        Ktmp = ILMath.ones(X.Size[0], 1);
                        int cnt2 = 0;
                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.ComputeDiagonal(X[ILMath.full, tmpIndex[cnt2++]]));
                        }

                        tmp[ILMath.full, _indexes[cnt]] += ILMath.multiplyElem(_kernels[cnt].DiagGradX(X[ILMath.full, _indexes[cnt]]), ILMath.repmat(Ktmp, 1, X[ILMath.full, _indexes[cnt]].S[1]));
                    }
                }

                ILRetArray<double> XDiagGrad = tmp;

                return XDiagGrad;
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

                ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                ILArray<double> Ktmp = ILMath.empty();
                ILArray<double> tempdL_dK;

                List<IKernel> tmpKernels;

                int startVal = 0, endVal = 0;
                for (int cnt = 0; cnt < _kernels.Count; cnt++)
                {
                    tmpKernels = KSlash(_kernels, cnt);

                    if (dL_dK.S[0] == dL_dK.S[1])
                    {
                        Ktmp = ILMath.ones(_K.Size);
                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.K);
                        }
                    }
                    else
                    {
                        Ktmp = ILMath.ones(_Kuf.Size);
                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.Kuf);
                        }
                    }
                    tempdL_dK = ILMath.multiplyElem(dL_dK, Ktmp);

                    endVal += _kernels[cnt].NumParameter;
                    gParam[ILMath.r(startVal, endVal - 1)] = _kernels[cnt].LogLikGradientParam(tempdL_dK);
                    startVal = endVal;
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
                ILArray<double> dL_dK = ILMath.check(indL_dK);
                ILArray<double> X = ILMath.check(inX);

                ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                ILArray<double> Ktmp = ILMath.empty();
                ILArray<double> tempdL_dK;

                List<IKernel> tmpKernels;

                int startVal = 0, endVal = 0;
                

                if (_indexes == null)
                {
                    for (int cnt = 0; cnt < _kernels.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);
                        Ktmp = ILMath.ones(X.S[0], X.S[0]);

                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.ComputeKernelMatrix(X, X, Flag.reconstruction));
                        }

                        tempdL_dK = ILMath.multiplyElem(dL_dK, Ktmp);
                        endVal += _kernels[cnt].NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = _kernels[cnt].LogLikGradientParamTensor(X, tempdL_dK);
                        startVal = endVal;
                    }
                }
                else
                {
                    for (int cnt = 0; cnt < _kernels.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);
                        List<ILArray<double>> tmpIndex = new List<ILArray<double>>(_indexes);
                        tmpIndex.RemoveAt(cnt);

                        Ktmp = ILMath.ones(X.S[0], X.S[0]);

                        int cnt2 = 0;
                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.ComputeKernelMatrix(X[ILMath.full, tmpIndex[cnt2]], X[ILMath.full, tmpIndex[cnt2]], Flag.reconstruction));
                            cnt2++;
                        }

                        tempdL_dK = ILMath.multiplyElem(dL_dK, Ktmp);
                        endVal += _kernels[cnt].NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = _kernels[cnt].LogLikGradientParamTensor(X[ILMath.full, _indexes[cnt]], tempdL_dK);
                        startVal = endVal;
                    }
                }

                return gParam;
            }
        }

        // TODO
        public ILRetArray<double> DiagGradParam(ILInArray<double> inX, ILInArray<double> inCovDiag)
        {
            using (ILScope.Enter(inX, inCovDiag))
            {
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> CovDiag = ILMath.check(inCovDiag);
                ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                List<IKernel> tmpKernels;
                ILArray<double> Ktmp = ILMath.empty();
                ILArray<double> tempdL_dK = ILMath.empty();

                /*int startVal = 0, endVal = 0;
                if (_indexes == null)
                {
                    for (int cnt = 0; cnt < _kernels.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);
                        Ktmp = ILMath.ones(X.S[0], 1);

                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.ComputeDiagonal(X));
                        }

                        tempdL_dK = ILMath.multiplyElem(CovDiag, Ktmp);

                        endVal += _kernels[cnt].NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = _kernels[cnt].DiagGradParam(X, tempdL_dK);
                        startVal = endVal;
                    }
                }
                else
                {
                    for (int cnt = 0; cnt < _kernels.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);
                        List<ILArray<double>> tmpIndex = new List<ILArray<double>>(_indexes);
                        tmpIndex.RemoveAt(cnt);

                        Ktmp = ILMath.ones(X.S[0], 1);

                        int cnt2 = 0;
                        foreach (IKernel kern_ in tmpKernels)
                        {
                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.ComputeDiagonal(X[ILMath.full, tmpIndex[cnt2]]));
                            cnt2++;
                        }

                        tempdL_dK = ILMath.multiplyElem(CovDiag, Ktmp);

                        endVal += _kernels[cnt].NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = _kernels[cnt].DiagGradParam(X[ILMath.full, _indexes[cnt]], tempdL_dK);
                        startVal = endVal;
                    }
                }*/

                /*int startVal = 0, endVal = 0;
                if (_indexes == null)
                {
                    foreach (IKernel kern in _kernels)
                    {
                        endVal += kern.NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = kern.DiagGradParam(X, CovDiag);
                        startVal = endVal;
                    }
                }
                else
                {
                    int cnt = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        endVal += kern.NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = kern.DiagGradParam(X[ILMath.full, _indexes[cnt++]], CovDiag);
                        startVal = endVal;
                    }
                }*/

                for (int i = 0; i < X.S[0]; i++)
                {
                    gParam += LogLikGradientParamTensor(X[i, ILMath.full], CovDiag[i]);
                }
				
                ILRetArray<double> ret = ILMath.multiplyElem(gParam, Parameter);

                return ret;
            }
        }

        public ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int q, Flag flag = Flag.learning)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                ILArray<double> dK_dX_tmp = ILMath.zeros(X1.Size[0], X2.Size[0]);
                ILArray<double> Ktmp;

                List<IKernel> tmpKernels;

                if (_indexes == null)
                    for (int cnt = 0; cnt < _kernels.Count; cnt++)
                    {
                        tmpKernels = KSlash(_kernels, cnt);

                        Ktmp = ILMath.ones(X1.Size[0], X2.Size[0]);
                        if (flag == Flag.learning)
                        {
                            if (X1.Size[0] == X2.Size[0])
                                foreach (IKernel kern_ in tmpKernels)
                                    Ktmp = ILMath.multiplyElem(Ktmp, kern_.K);

                            else
                                foreach (IKernel kern_ in tmpKernels)
                                    if (Ktmp.S[0] == kern_.Kuf.S[0])
                                        Ktmp = ILMath.multiplyElem(Ktmp, kern_.Kuf);
                                    else
                                        Ktmp = ILMath.multiplyElem(Ktmp, kern_.Kuf.T);
                        }
                        else
                            foreach (IKernel kern_ in tmpKernels)
                                Ktmp = ILMath.multiplyElem(Ktmp, kern_.Krec);

                        dK_dX_tmp += ILMath.multiplyElem(_kernels[cnt].GradX(X1, X2, q, flag), Ktmp);
                    }
                else
                {
                    for (int cnt = 0; cnt < _indexes.Count; cnt++)
                        for (int i = 0; i < _indexes[cnt].Length; i++)
                            if (_indexes[cnt].GetValue(i) == q)
                            {
                                tmpKernels = KSlash(_kernels, cnt);

                                Ktmp = ILMath.ones(X1.Size[0], X2.Size[0]);
                                if (flag == Flag.learning)
                                {
                                    if (X1.Size[0] == X2.Size[0])
                                        foreach (IKernel kern_ in tmpKernels)
                                            Ktmp = ILMath.multiplyElem(Ktmp, kern_.K);

                                    else
                                        foreach (IKernel kern_ in tmpKernels)
                                            if (Ktmp.S[0] == kern_.Kuf.S[0])
                                                Ktmp = ILMath.multiplyElem(Ktmp, kern_.Kuf);
                                            else
                                                Ktmp = ILMath.multiplyElem(Ktmp, kern_.Kuf.T);

                                }
                                else
                                    foreach (IKernel kern_ in tmpKernels)
                                        Ktmp = ILMath.multiplyElem(Ktmp, kern_.Krec);

                                dK_dX_tmp += ILMath.multiplyElem(_kernels[cnt].GradX(X1, X2, q, flag), Ktmp);
                            }
                }
                return dK_dX_tmp;
            }  
        }

        public void Read(ref XmlReader reader)
        {
            string[] tokens;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Read();
                if (reader.Name == "Kernels")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "Kernel")
                        {
                            reader.MoveToAttribute("type");
                            switch (reader.ReadContentAsString())
                            {
                                case "KernelTypeLinear":
                                    AddKern(new LinearKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeRBF":
                                    AddKern(new RBFKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeWhite":
                                    AddKern(new WhiteKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeCompound":
                                    AddKern(new CompoundKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeTensor":
                                    AddKern(new TensorKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeLinearAcceleration":
                                    AddKern(new LinearAccelerationKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeRBFAcceleration":
                                    AddKern(new RBFAccelerationKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                                case "KernelTypeStyle":
                                    AddKern(new StyleKern());
                                    _kernels[_kernels.Count - 1].Read(ref reader);
                                    break;
                            }
                        }
                    }
                    reader.Read();
                }
                if (reader.Name == "Indexes")
                {
                    _indexes = new List<ILArray<double>>();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "Index")
                        {
                            _indexes.Add(ILMath.localMember<double>());
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);

                            _indexes[_indexes.Count - 1] = ILMath.zeros(1, tokens.Length);
                            for (int i = 0; i < tokens.Length; i++)
                                _indexes[_indexes.Count - 1][i] = Double.Parse(tokens[i]);

                        }
                    }
                    reader.Read();
                }
            }
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("Kernels", null);
            foreach (IKernel kern in _kernels)
            {
                writer.WriteStartElement("Kernel", null);
                writer.WriteAttributeString("type", kern.Type.ToString());
                kern.Write(ref writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            if (_indexes != null)
            {
                writer.WriteStartElement("Indexes", null);
                for (int i = 0; i < _indexes.Count; i++)
                {
                    writer.WriteStartElement("Index", null);
                    writer.WriteAttributeString("data", _indexes[i].ToString().Remove(0, _indexes[i].ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }
        #endregion

        #region Private Functions
        private List<IKernel> KSlash(List<IKernel> list, int index)
        {
            List<IKernel> tmpKernels = new List<IKernel>(list);
            tmpKernels.RemoveAt(index);

            return tmpKernels;
        }
        #endregion
    }
}
