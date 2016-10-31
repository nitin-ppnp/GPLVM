using System;
using System.Xml;
using ILNumerics;
using GPLVM.Prior;
using GPLVM.Kernel;
using GPLVM.GPLVM;

namespace GPLVM.Dynamics
{
    public class GPVelocity : IDynamics
    {
        const double W_VARIANCE = 1e6;
        const double MARGINAL_DW = 0;
        const double BALLANCE = 1;

        private int _numInducing = 200;   // number of inducing inputs

        private DynamicType _type;          // the type of the kernel
        private PriorType _priorType;
        private ApproximationType _approxType;
        private int _q;                     // dimension of latent space
        private int _D;                     // dimension of the data
        private int _N;                     // number of data points

        private ILArray<double> _X = ILMath.localMember<double>();
        private ILArray<double> _Xin = ILMath.localMember<double>();       // Nxq matrix of latent points
        private ILArray<double> _Xout = ILMath.localMember<double>();      // Nxq matrix of data points
        private ILArray<double> _Xu = ILMath.localMember<double>();        // numInducingxq matrix of latent inducing inputs
        private ILArray<double> _Xnew = ILMath.localMember<double>();
        private ILArray<double> _Xold = ILMath.localMember<double>();
        private ILArray<double> _Xvar = ILMath.localMember<double>();
        private ILArray<double> _Xstar = ILMath.localMember<double>();
        private ILArray<double> _scale = ILMath.localMember<double>();     // 1xD vector of weights for the data
        private ILArray<double> _segments = ILMath.localMember<double>();

        private ILArray<double> _Kuf = ILMath.localMember<double>();       // NxN kernel covariance matrix
        private ILArray<double> _A = ILMath.localMember<double>();         // help matrix for rearranging log-likelihood
        private ILArray<double> _invA = ILMath.localMember<double>();
        private double _logDetA;

        private ILArray<double> _diagD = ILMath.localMember<double>();     // help diagonal of the kernel for rearranging fitc log-likelihood
        private ILArray<double> _invD = ILMath.localMember<double>();
        private ILArray<double> _detDiff = ILMath.localMember<double>();

        private double _beta;               // precission term of inducing mapping

        private IKernel _kern;
        private ILArray<double> _K = ILMath.localMember<double>();         // NxN kernel covariance matrix
        private ILArray<double> _invK = ILMath.localMember<double>();      // NxN inverse kernel covariance matrix
        private double _logDetK;            // log determinant of K
        private ILArray<double> _alpha = ILMath.localMember<double>();     // part needed for mean prediction

        private Guid _key = Guid.NewGuid();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// GPVelocity is a continius first order non-linear autoregressiv model provided by a Gaussian process. The actual time step x_n 
        /// were mapped from the time step before x_n-1 via non-linear mapping function g(x_n-1) drawn from a Gaussian 
        /// process and some noise e (x_n = g(x_n-1) + e; g(x_n-1) ~ GP(m(x_n-1),k(x_n-1,x')); where m(x_n-1) is the mean function
        /// and k(x_n-1,x') the kernel function).
        /// This class provides the full fit (FTC) of the data.
        /// </remarks>
        /// <param name="dimension">The dimension of the latent space.</param>
        public GPVelocity(ApproximationType aType = ApproximationType.ftc)
        {
            this._type = DynamicType.DynamicTypeVelocity;
            this._priorType = PriorType.PriorTypeDynamics;

            _beta = 1e3;

            _kern = new CompoundKern();
            ((CompoundKern)_kern).AddKern(new RBFKern());
            ((CompoundKern)_kern).AddKern(new LinearKern());
            ((CompoundKern)_kern).AddKern(new WhiteKern());
        }

        #region Setters and Getters
        /// <summary>
        /// Returns the number of parameters wants to bee optimized. 
        /// </summary>
        public int NumParameter
        {
            get
            {
                return _kern.NumParameter;
            }
        }

        /// <summary>
        /// Returns the prior type. 
        /// </summary>
        public PriorType Type
        {
            get
            {
                return _priorType;
            }
        }

        /// <summary>
        /// Returns the dynamics type. 
        /// </summary>
        public DynamicType DynamicsType
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// The log of the parameter in the object wants to be optimized. 
        /// </summary>
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

        public IKernel Kernel
        {
            get
            {
                return _kern;
            }
            set
            {
                _kern = value;
            }
        }

        public int NumInducing
        {
            get
            {
                return _numInducing;
            }
            set
            {
                _numInducing = value;
            }
        }

        public ILArray<double> X
        {
            get { return _Xin; }
        }

        public ILArray<double> Xnew
        {
            get { return _Xnew; }
            set { _Xnew = value; }
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
        #endregion

        #region Public Functions
        /// <summary>
        /// Constructs the object. 
        /// </summary>
        public void Initialize(Data _data)
        {
            Initialize(_data.GetData("X"), _data.GetData("segments"));
        }

        public void Initialize(ILInArray<double> _data, ILInArray<double> segments)
        {
            using (ILScope.Enter(_data, segments))
            {
                ILArray<double> d = ILMath.check(_data);
                ILArray<double> s = ILMath.check(segments);

                _X.a = d;
                _segments.a = s;

                _D = _X.Size[1];
                _N = _X.Size[0];

                _q = _X.Size[1];

                _Xstar = ILMath.empty();
                _Xvar = ILMath.empty();

                _scale.a = ILMath.ones(1, _X.Size[1]);
                UpdateParameter();
            }
        }

        /// <summary>
        /// Computes the negative log likelihood of the object. 
        /// </summary>
        /// <returns>Negative log likelihood of the object.</returns>
        public double LogLikelihood()
        {
            double L = 0;

            switch (_approxType)
            {
                case ApproximationType.ftc:
                    L -= (double)(_q * _N / 2 * ILMath.log(2 * ILMath.pi) + _q / 2 * _logDetK);
                    for (int d = 0; d < _D; d++)
                        L -= (double)(0.5 * ILMath.multiply(ILMath.multiply(_Xout[ILMath.full, d].T, _invK), _Xout[ILMath.full, d]));
                    break;

                case ApproximationType.dtc:
                    ILArray<double> KufM = ILMath.multiply(_Kuf, _Xout);
                    ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);

                    L -= (double)(.5 * (_D * (-(_N - _numInducing) * ILMath.log(_beta) - _logDetK + _logDetA)
                        - (ILMath.sum(ILMath.sum(ILMath.multiplyElem(_invA, KufMKufM)))
                        - ILMath.sum(ILMath.sum(ILMath.multiplyElem(_Xout, _Xout)))) * _beta));
                    break;

                case ApproximationType.fitc:
                    L -= (double)(.5 * _N * _D * ILMath.log(2 * ILMath.pi));
                    ILArray<double> DinvM = ILMath.multiplyElem(ILMath.repmat(_invD, 1, _D), _Xout);
                    ILArray<double> KufDinvM = ILMath.multiply(_Kuf, DinvM);

                    L -= (double)(.5 * (_D * (ILMath.sum(ILMath.log(_diagD))
                        - (_N - _numInducing) * ILMath.log(_beta) + _detDiff) + (ILMath.sum(ILMath.sum(ILMath.multiplyElem(DinvM, _Xout)))
                        - ILMath.sum(ILMath.sum(ILMath.multiplyElem(ILMath.multiply(_invA, KufDinvM), KufDinvM)))) * _beta));

                    break;
            }

            // Gaussian prior likelihood for [0] point of every segment
            L -= (double)(_segments.Length * _q/2 * ILMath.log(2*ILMath.pi) - 
                0.5*ILMath.sum(ILMath.sum(ILMath.multiplyElem(_X[_segments,ILMath.full], _X[_segments,ILMath.full]))));

            L -= (double)(ILMath.sum(_kern.LogParameter));

            return L;
        }

        public double PostLogLikelihood()
        {
            double L = 0;

            ILArray<double> yHat = _Xnew - _Xold;
            L = (double)(-_D / 2 * ILMath.log(_Xvar) - _D / 2 * ILMath.log(2 * ILMath.pi) - ILMath.multiply(yHat, yHat.T) / (2 * _Xvar));

            return L;
        }

        /// <summary>
        /// Computes the gradients of the latents and kernel parameters of the object. 
        /// </summary>
        /// <returns>Gradients of the latents and kernel parameters of the object.</returns>
        public ILRetArray<double> LogLikGradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> dL_dK = ILMath.empty(); // derivative of log likelihood w.r.t K
                ILArray<double> dL_dKuf = ILMath.empty(); // derivative of log likelihood w.r.t Kuf

                ILArray<double> gXin = ILMath.empty(); // gradient of X
                ILArray<double> gXout = ILMath.empty(); // gradient of Xu

                ILCell dL_dX_dL_dXuf = ILMath.cell();

                for (int d = 0; d < _D; d++)
                    gXout[ILMath.full, d] = -BALLANCE * ILMath.multiply(_invK, _Xout[ILMath.full, d]);

                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        dL_dK = -_D / 2 * _invK + 0.5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(_invK, _Xout), _Xout.T), _invK);
                        gXin = _kern.LogLikGradientX(_Xin, dL_dK);

                        gXout = ILMath.zeros(_Xout.Size);
                        for (int d = 0; d < _D; d++)
                            gXout[ILMath.full, d] = -ILMath.multiply(_invK, _Xout[ILMath.full, d]);
                        break;

                    case ApproximationType.dtc:
                        ILArray<double> KufM = ILMath.multiply(_Kuf, _Xout);
                        ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);
                        ILArray<double> invAKufMKufM = ILMath.multiply(_invA, KufMKufM);
                        ILArray<double> invAKufMKufMinvA = ILMath.multiply(invAKufMKufM, _invA);

                        dL_dK = .5 * (_D * (_invK - (1 / _beta) * _invA) - invAKufMKufMinvA);

                        ILArray<double> invAKuf = ILMath.multiply(_invA, _Kuf);

                        dL_dKuf = -_D * invAKuf - _beta * (ILMath.multiply(invAKufMKufM, invAKuf) - (ILMath.multiply(ILMath.multiply(_invA, KufM), _Xout.T)));

                        dL_dX_dL_dXuf = _kern.LogLikGradientX(_Xu, dL_dK, _Xin, dL_dKuf);

                        gXin = dL_dX_dL_dXuf.GetArray<double>(1);

                        ILArray<double> AinvKuf = ILMath.multiply(Util.pdinverse((1 / _beta) * _K + ILMath.multiply(_Kuf, _Kuf.T)), _Kuf);

                        gXout = ILMath.zeros(_Xout.Size);
                        for (int i = 0; i < _D; i++)
                            gXout[ILMath.full, i] = -_beta * _Xout[ILMath.full, i]
                                + _beta * ILMath.multiply(_Kuf.T, ILMath.multiply(AinvKuf, _Xout[ILMath.full, i]));
                        break;

                    case ApproximationType.fitc:
                        ILArray<double> KufDinvM = ILMath.multiply(ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1)), _Xout);
                        ILArray<double> KufDinvMKufDinvMT = ILMath.multiply(KufDinvM, KufDinvM.T);
                        ILArray<double> AinvKufDinvM = ILMath.multiply(_invA, KufDinvM);
                        ILArray<double> diagKufAinvKufDinvMMT = ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(ILMath.multiply(_invA, KufDinvM), _Xout.T)), 0).T;
                        ILArray<double> AinvKufDinvMKufDinvMAinv = ILMath.multiply(AinvKufDinvM, AinvKufDinvM.T);
                        ILArray<double> diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf = ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(_D * _invA + _beta * AinvKufDinvMKufDinvMAinv, _Kuf)), 0).T;
                        ILArray<double> invKuuKuf = ILMath.multiply(_invK, _Kuf);
                        ILArray<double> invKuuKufDinv = ILMath.multiplyElem(invKuuKuf, ILMath.repmat(_invD.T, _numInducing, 1));
                        ILArray<double> diagMMT = ILMath.sum(ILMath.multiplyElem(_Xout, _Xout), 1);

                        ILArray<double> diagQ = -_D * _diagD + _beta * diagMMT + diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf - 2 * _beta * diagKufAinvKufDinvMMT;

                        dL_dK = .5 * (_D * (_invK - _invA / _beta) - AinvKufDinvMKufDinvMAinv
                            + _beta * ILMath.multiply(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, _numInducing, 1)), invKuuKufDinv.T));

                        dL_dKuf = -_beta * ILMath.multiplyElem(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, _numInducing, 1)), ILMath.repmat(_invD.T, _numInducing, 1))
                            - _D * ILMath.multiplyElem(ILMath.multiply(_invA, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1))
                            - _beta * ILMath.multiplyElem(ILMath.multiply(AinvKufDinvMKufDinvMAinv, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1))
                            + _beta * ILMath.multiplyElem(ILMath.multiply(ILMath.multiply(_invA, KufDinvM), _Xout.T), ILMath.repmat(_invD.T, _numInducing, 1));

                        ILArray<double> Kstar = ILMath.divide(.5 * diagQ * _beta, ILMath.multiplyElem(_diagD, _diagD));

                        dL_dX_dL_dXuf = _kern.LogLikGradientX(_Xu, dL_dK, _Xin, dL_dKuf);
                        gXin = dL_dX_dL_dXuf.GetArray<double>(1);

                        ILArray<double> diagGX = _kern.DiagGradX(_Xin);
                        for (int i = 0; i < _Xin.S[0]; i++)
                            gXin[i, ILMath.full] += diagGX[i, ILMath.full] * Kstar[i];

                        ILArray<double> KufDinv = ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1));
                        ILArray<double> AinvKufDinv = ILMath.multiply(_invA, KufDinv);

                        gXout = ILMath.zeros(_Xout.Size);
                        for (int i = 0; i < _q; i++)
                            gXout[ILMath.full, i] = -_beta * ILMath.multiplyElem(_invD, _Xout[ILMath.full, i])
                                + _beta * ILMath.multiply(ILMath.multiply(KufDinv.T, AinvKufDinv), _Xout[ILMath.full, i]);
                        break;
                }

                int qp = gXin.Size[1];
                ILArray<double> gX = ILMath.zeros(_N, qp);

                ILArray<int> S;
                if (_segments.Length > 1)
                    S = Util.setdiff(ILMath.toint32(ILMath.counter(1, _N) - 1), ILMath.toint32(_segments[ILMath.r(1, ILMath.end)] - 1));
                else
                    S = ILMath.toint32(ILMath.counter(1, _N) - 1);

                S[ILMath.find(S == _N - 1)] = ILMath.empty<int>();

                gX[S, ILMath.full] = gXin;

                S = Util.setdiff(ILMath.toint32(ILMath.counter(1, _N) - 1), ILMath.toint32(_segments));

                gX[S, ILMath.full] += gXout;

                gX[_segments, ILMath.full] -= BALLANCE * _X[_segments, ILMath.full];

                return gX.C;
            }
        }

        public ILRetArray<double> PostLogLikGradient()
        {
            using (ILScope.Enter())
            {
                return -((_Xnew - _Xold) / _Xvar);
            }
        }

        /// <summary>
        /// Computes the gradients of kernel parameters of the object. 
        /// </summary>
        /// <returns>Log gradient of the kernel parameters.</returns>
        public ILRetArray<double> KernelGradient()
        {
            using (ILScope.Enter())
            {
                return _kern.LogLikGradientParam(-_D / 2 * _invK + 0.5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(_invK, _Xout), _Xout.T), _invK));
            }
        }

        /// <summary>
        /// Creates a seqence of latent points. 
        /// </summary>
        /// <param name="inTestInput">Starting point.</param>
        /// <param name="numTimeSteps">Number of time steps the sequence has to be.</param>
        /// <returns>The computed sequence.</returns>-
        public ILRetArray<double> SimulateDynamics(ILInArray<double> inTestInput, int numTimeSteps)
        {
            using (ILScope.Enter(inTestInput))
            {
                _Xstar.a = ILMath.check(inTestInput);
                ILArray<double> Xpred = ILMath.zeros(numTimeSteps, _q);

                Xpred[0,ILMath.full] = _Xstar;

                for (int n = 1; n < numTimeSteps; n++)
                    Xpred[n,ILMath.full] = PredictData(Xpred[n-1,ILMath.full]);

                return Xpred;
            }
        }

        /// <summary>
        /// Prediction of data points. 
        /// </summary>
        /// <param name="inTestInputs">Point wants to be start</param>
        /// <returns>The predicted data.</returns>
        public ILRetArray<double> PredictData(ILInArray<double> inTestInputs, bool xVar = false)
        {
            using (ILScope.Enter(inTestInputs))
            {
                _Xstar.a = ILMath.check(inTestInputs);

                ILArray<double> k = _kern.ComputeKernelMatrix(_Xstar, _Xin, Flag.reconstruction);
                        
                _Xnew.a = ILMath.multiply(k, _alpha);

                if (xVar)
                {
                    ILArray<double> kv = _kern.ComputeKernelMatrix(_Xstar, _Xstar, Flag.reconstruction);
                    _Xvar.a = kv - ILMath.multiply(ILMath.multiply(k, _invK), k.T);

                    
                    if (_Xvar < 1e-6)
                        _Xvar.a = 1e-6;
                }

                return _Xnew;
            }
        }

        /// <summary>
        /// Updates the parameters after each optimization step. 
        /// </summary>
        public void UpdateParameter(Data data)
        {
            UpdateParameter(data.GetData("X"));
        }

        /// <summary>
        /// Updates the parameters after each optimization step. 
        /// </summary>
        public void UpdateParameter(ILInArray<double> _data)
        {
            using (ILScope.Enter(_data))
            {
                ILArray<double> d = ILMath.check(_data);

                _X.a = d;
            }
            UpdateParameter();
        }

        public void Read(ref XmlReader reader)
        {
            string[] tokens;
            reader.MoveToAttribute("approximation");
            string atype = (string)reader.ReadContentAs(typeof(string), null);
            switch (atype)
            {
                case "ftc":
                    _approxType = ApproximationType.ftc;
                    break;
                case "dtc":
                    _approxType = ApproximationType.dtc;
                    break;
                case "fitc":
                    _approxType = ApproximationType.fitc;
                    break;
            }
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
                        if (reader.Name == "NumInducing")
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                    _numInducing = (int)Double.Parse(reader.Value);

                            }
                            reader.Read();
                        }
                        if (reader.Name == "beta")
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                    _beta = (int)Double.Parse(reader.Value);

                            }
                            reader.Read();
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
                        if (reader.Name == "Xin")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _Xin = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _Xin[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _Xin = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _Xin[i] = Double.Parse(tokens[i]);
                            }
                            _Xin = _Xin.Reshape(_q, _N - 1).T;
                        }
                        if (reader.Name == "Xu")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _Xu = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _Xu[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _Xu = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _Xu[i] = Double.Parse(tokens[i]);
                            }
                            _Xu = _Xu.Reshape(_q, _numInducing).T;
                        }
                        if (reader.Name == "Xout")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _Xout = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _Xout[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _Xout = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _Xout[i] = Double.Parse(tokens[i]);
                            }
                            _Xout = _Xout.Reshape(_q, _N - 1).T;
                        }
                        if (reader.Name == "scale")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _scale = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _scale[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _scale = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _scale[i] = Double.Parse(tokens[i]);
                            }
                        }
                        if (reader.Name == "segments")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            _segments = ILMath.zeros(1, tokens.Length);
                            for (int i = 0; i < tokens.Length; i++)
                                _segments[i] = Double.Parse(tokens[i]);
                            _segments = _segments.T;
                        }
                    }
                    reader.Read();
                }
                if (reader.Name == "Kernel")
                {
                    
                    _kern = new CompoundKern();
                    _kern.Read(ref reader);
                    break;
                }
            }
            UpdateKernelMatrix();
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("Prior");
            writer.WriteAttributeString("type", "PriorTypeDynamics");
            writer.WriteAttributeString("DynamicType", "DynamicTypeVelocity");
            writer.WriteAttributeString("approximation", _approxType.ToString());
                writer.WriteStartElement("Data", null);
                    writer.WriteElementString("q", _q.ToString());
                    writer.WriteElementString("D", _D.ToString());
                    writer.WriteElementString("N", _N.ToString());
                    writer.WriteElementString("NumInducing", _numInducing.ToString());
                    writer.WriteElementString("beta", _beta.ToString());

                    writer.WriteStartElement("X");
                        writer.WriteAttributeString("data", _X.ToString().Normalize().Remove(0, _X.ToString().IndexOf("]") + 1).Replace("\n","").Replace("\r","")); 
                    writer.WriteEndElement();

                    writer.WriteStartElement("Xin");
                        writer.WriteAttributeString("data", _Xin.ToString().Remove(0, _Xin.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();

                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                    {
                        writer.WriteStartElement("Xu");
                        writer.WriteAttributeString("data", _Xu.ToString().Remove(0, _Xu.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("Xout");
                        writer.WriteAttributeString("data", _Xout.ToString().Remove(0, _Xout.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();

                    writer.WriteStartElement("scale");
                        writer.WriteAttributeString("data", _scale.ToString().Remove(0, _scale.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();


                    if (_segments.Length > 1)
                    {
                        writer.WriteStartElement("segments");
                            writer.WriteAttributeString("data", _segments.ToString().Remove(0, _segments.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement("segments");
                        writer.WriteAttributeString("data", _segments.ToString().Remove(0, 14).Replace("\n", "").Replace("\r", ""));
                        writer.WriteEndElement();
                    }
                writer.WriteEndElement();

                writer.WriteStartElement("Kernel", null);
                writer.WriteAttributeString("type", _kern.Type.ToString());
                    _kern.Write(ref writer);
                writer.WriteEndElement();

            writer.WriteEndElement();
        }
        #endregion

        #region Private Functions
        private void UpdateParameter()
        {
            using (ILScope.Enter())
            {
                _Xout.a = _X.C;

                _Xin.a = ILMath.zeros(1, _q);
                _Xin[ILMath.r(1, _X.Size[0] - 1), ILMath.full] = _X[ILMath.r(0, ILMath.end - 1), ILMath.full].C;

                _Xin[_segments, ILMath.full] = ILMath.empty();
                _Xout[_segments, ILMath.full] = ILMath.empty();

                UpdateKernelMatrix();
            }
        }

        private void UpdateKernelMatrix()
        {
            using (ILScope.Enter())
            {
                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        _K.a = _kern.ComputeKernelMatrix(_Xin, _Xin);
                        _invK.a = Util.pdinverse(_K);
                        _logDetK = Util.logdet(_K);

                        _alpha.a = ILMath.multiply(_invK, _Xout);
                        break;

                    case ApproximationType.dtc:
                        _K.a = _kern.ComputeKernelMatrix(_Xu, _Xu);
                        _Kuf.a = _kern.ComputeKernelMatrix(_Xu, _Xin);
                        _invK.a = Util.pdinverse(_K);
                        _logDetK = Util.logdet(_K);

                        _A.a = (1 / _beta) * _K + ILMath.multiply(_Kuf, _Kuf.T);
                        // This can become unstable when K_uf2 is low rank.
                        _invA.a = Util.pdinverse(_A);
                        _logDetA = Util.logdet(_A);

                        _alpha.a = ILMath.multiply(ILMath.multiply(_invA, _Kuf), _Xout);
                        break;

                    case ApproximationType.fitc:
                        _K.a = _kern.ComputeKernelMatrix(_Xu, _Xu);
                        _Kuf.a = _kern.ComputeKernelMatrix(_Xu, _Xin);
                        _invK.a = Util.pdinverse(_K);
                        _logDetK = Util.logdet(_K);

                        ILArray<double> _diagK = _kern.ComputeDiagonal(_Xin);

                        _diagD.a = 1 + _beta * _diagK - _beta * ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(_invK, _Kuf)), 0).T;
                        _invD.a = 1 / _diagD;
                        ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1)), _Kuf.T);
                        _A.a = 1 / _beta * _K + KufDinvKuf;

                        // This can become unstable when K_ufDinvK_uf is low rank.
                        _invA.a = Util.pdinverse(_A);
                        _logDetA = Util.logdet(_A);

                        _detDiff.a = -ILMath.log(_beta) * _numInducing + ILMath.log(ILMath.det(ILMath.eye(_numInducing, _numInducing) + _beta * ILMath.multiply(KufDinvKuf, _invK)));

                        _alpha.a = ILMath.multiply(ILMath.multiplyElem(ILMath.multiply(_invA, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1)), _Xout);
                        break;
                }
            }
        }
        #endregion
    }
}
