using System;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;
using System.Collections.Generic;

namespace GPLVM.Kernel
{
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

    public class CompoundKern : IKernel
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
        private ILArray<double> _Krec = ILMath.localMember<double>(); 
        [DataMember()]
        private ILArray<double> _Kuf = ILMath.localMember<double>();

        [DataMember()]
        private Guid _key = Guid.NewGuid();

        [DataMember()]
        private List<IKernel> _kernels;
        [DataMember()]
        private List<ILArray<double>> _indexes;

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// This type of kernel is a collection of kernels, which were individualy added by the user.
        /// Style kernels can also be part of the list.
        /// </remarks>
        public CompoundKern()
        {
            this._type = KernelType.KernelTypeCompound;
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
                _parameter = ILMath.zeros(1, NumParameter);

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
                _parameter.a = ILMath.zeros(1, NumParameter);

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

        public double Noise
        {
            get
            {
                double noise = 0;
                foreach (IKernel kern in _kernels)
                    noise += kern.Noise;
                return noise;
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <remarks>
        /// Computing the elementwise sum 'K = K_1 + ... + K_N', where '[K_1, ..., K_N]' are the kernel matrices computed 
        /// by the corresponding kernel functions in the list to get a logical 'OR' effect of point correlations.
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
                                    k = ILMath.add(k, kern.ComputeKernelMatrix(X1, X2, flag)); // for normal kernels; should be extended for the style kernels
                                }
                                _K.a = k;
                                k_ = k;
                            }
                            else
                            {
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.add(k, kern.ComputeKernelMatrix(X1, X2, flag)); // for normal kernels; should be extended for the style kernels
                                }
                                _Kuf.a = k;
                                k_ = k;
                            }
                            break;

                        case Flag.postlearning:
                            foreach (IKernel kern in _kernels)
                            {
                                k = ILMath.add(k, kern.ComputeKernelMatrix(X1, X2, flag)); // for normal kernels; should be extended for the style kernels
                            }
                            _Krec.a = k;
                            k_ = k;
                            break;

                        default:
                            foreach (IKernel kern in _kernels)
                            {
                                k = ILMath.add(k, kern.ComputeKernelMatrix(X1, X2, flag)); // for normal kernels; should be extended for the style kernels
                            }
                            k_ = k;
                            break;
                    }
                }
                else
                {
                    int cnt = 0;
                    switch(flag)
                    {
                        case Flag.learning:
                            if (X1.S[0] == X2.S[0])
                            {
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag)); // for normal kernels; should be extended for the style kernels
                                    cnt++;
                                }
                                _K.a = k;
                                k_ = k;
                            }
                            else
                            {
                                foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag)); // for normal kernels; should be extended for the style kernels
                                    cnt++;
                                }
                                _Kuf.a = k;
                                k_ = k;
                            }
                            break;

                        case Flag.postlearning:
                            foreach (IKernel kern in _kernels)
                                {
                                    k = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag)); // for normal kernels; should be extended for the style kernels
                                    cnt++;
                                }
                                _Krec.a = k;
                                k_ = k;
                                break;

                        default:
                            foreach (IKernel kern in _kernels)
                            {
                                k = ILMath.add(k, kern.ComputeKernelMatrix(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], flag)); // for normal kernels; should be extended for the style kernels
                                cnt++;
                            }
                            k_ = k;
                            break;
                    }
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

                ILArray<double> k = ILMath.zeros<double>(X.Size[0], 1);

                if (_indexes == null)
                {
                    foreach (IKernel kern in _kernels)
                    {
                        k = ILMath.add(k, kern.ComputeDiagonal(X)); // for normal kernels; should be extended for the style kernels
                    }
                }
                else
                {
                    int cnt = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        k = ILMath.add(k, kern.ComputeDiagonal(X[ILMath.full, _indexes[cnt]])); // for normal kernels; should be extended for the style kernels
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
                foreach (IKernel kern in _kernels)
                {
                    dL_dX = ILMath.add(dL_dX, kern.LogLikGradientX(X, dL_dK));
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

                ILCell tmp = ILMath.cell();
                foreach (IKernel kern in _kernels)
                {
                    tmp = kern.LogLikGradientX(Xu, dL_dKuu, X, dL_dKuf);
                    dL_dXu = ILMath.add(dL_dXu, tmp.GetArray<double>(0));
                    dL_dX = ILMath.add(dL_dX, tmp.GetArray<double>(1));
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

                ILArray<double> tmp = ILMath.zeros(X.S);

                if (_indexes == null)
                {
                    foreach (IKernel kern in _kernels)
                    {
                        tmp += kern.DiagGradX(X);
                    }
                }
                else
                {
                    int cnt = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        tmp[ILMath.full, _indexes[cnt]] += kern.DiagGradX(X[ILMath.full, _indexes[cnt]]);
                        cnt++;
                    }

                }

                ILRetArray<double> XDiagGrad = tmp;

                return XDiagGrad;
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

                int startVal = 0, endVal = 0;
                foreach (IKernel kern in _kernels)
                {
                    endVal += kern.NumParameter;
                    gParam[ILMath.r(startVal, endVal - 1)] = kern.LogLikGradientParam(dL_dK);
                    startVal = endVal;
                }

                ILRetArray<double> ret = gParam;

                return ret;
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
                using (ILScope.Enter(indL_dK))
                {
                    ILArray<double> dL_dK = ILMath.check(indL_dK);
                    ILArray<double> X = ILMath.check(inX);

                    ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                    int startVal = 0, endVal = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        endVal += kern.NumParameter;
                        gParam[ILMath.r(startVal, endVal - 1)] = kern.LogLikGradientParamTensor(X, dL_dK);
                        startVal = endVal;
                    }

                    ILRetArray<double> ret = gParam;

                    return ret;
                }
            }
        }

        public ILRetArray<double> DiagGradParam(ILInArray<double> inX, ILInArray<double> inCovDiag)
        {
            using (ILScope.Enter(inX, inCovDiag))
            {
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> CovDiag = ILMath.check(inCovDiag);
                ILArray<double> gParam = ILMath.zeros(1, NumParameter);

                int startVal = 0, endVal = 0;
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
                }

                ILRetArray<double> ret = gParam;

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

                if (_indexes == null)
                {
                    foreach (IKernel kern in _kernels)
                    {
                        dK_dX_tmp += kern.GradX(X1, X2, q, flag);
                    }
                }
                else
                {
                    int cnt = 0;
                    foreach (IKernel kern in _kernels)
                    {
                        dK_dX_tmp += kern.GradX(X1[ILMath.full, _indexes[cnt]], X2[ILMath.full, _indexes[cnt]], q, flag);
                        cnt++;
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
                                case "KernelTypeBias":
                                    AddKern(new BiasKern());
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

        #endregion
    }
}
