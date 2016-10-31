using System;
using GPLVM.Kernel;
using GPLVM.Prior;
using GPLVM.Graph;
using System.Xml;
using ILNumerics;

namespace GPLVM.GPLVM
{
    public class BackProjection : Node, IGPLVM
    {
        private GPLVMType _type = GPLVMType.back;
        private ApproximationType _approxType;

        private int _q;                     // dimension of latent space
        private int _D;                     // dimension of data space
        private int _N;                     // length of the data set

        private int _numInducing = 200;   // number of inducing inputs

        private ILArray<double> _X;         // Nxq matrix of latent points
        private ILArray<double> _Xu;        // numInducingxq matrix of latent inducing inputs
        public ILArray<double> _Y;          // NxD matrix of data points
        public ILArray<double> _bias;
        private ILArray<double> _scale;     // 1xD vector of weights for the data
        private ILArray<double> _segments;

        // for learning self produced data
        private ILArray<double> _YPostMeanOld;
        private ILArray<double> _YPostMean;
        private ILArray<double> _Yvar;
        private ILArray<double> _Xnew;
        private ILArray<double> _Ynew;

        private ILArray<double> _latentgX;  // top down prior of the latents

        private ILArray<double> _K;         // NxN kernel covariance matrix
        private ILArray<double> _invK;      // NxN inverse kernel covariance matrix
        private double _logDetK;            // log determinant of K

        protected ILArray<double> _Kuf;       // NxN kernel covariance matrix
        protected ILArray<double> _A;         // help matrix for rearranging log-likelihood
        protected ILArray<double> _invA;
        protected double _logDetA;

        protected ILArray<double> _diagD;     // help diagonal of the kernel for rearranging fitc log-likelihood
        protected ILArray<double> _invD;
        protected ILArray<double> _detDiff;

        protected double _beta;               // precission term of inducing mapping

        private IKernel _kern;

        private ILArray<double> _alpha;     // part needed for mean prediction

        private IPrior _prior;
        private Data _data;                 // the data dictoinary

        private LerningMode _mode;
        protected Mask _mask;

        public BackProjection(ILArray<double> Y, ILArray<double> X, ILArray<double> bias, ILArray<double> scale, ApproximationType aType = ApproximationType.ftc)
        {
            _X = Y;
            _Y = X;
            _bias = bias;
            _scale = scale;
            _approxType = aType;

            _D = 0;                     // dimension of data space
            _N = 0;

            _data = new Data();

            _K = ILMath.empty();
            _invK = ILMath.empty();

            _beta = 1e3;

            _alpha = ILMath.empty();

            _kern = new CompoundKern();
            ((CompoundKern)_kern).AddKern(new RBFKern());
            //((CompoundKern)_kern).AddKern(new LinearKern());
            //((CompoundKern)_kern).AddKern(new BiasKern());
            ((CompoundKern)_kern).AddKern(new WhiteKern());

            _mask = Mask.full;
        }

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// Gaussian process which maps from data space to latent space for fast inference in the hierarchical structure.
        /// </remarks>
        public BackProjection()
        {
            _X = ILMath.empty();
            _Y = ILMath.empty();
            _bias = ILMath.empty();

            _D = 0;                     // dimension of data space
            _N = 0;

            _data = new Data();

            _K = ILMath.empty();
            _invK = ILMath.empty();

            _alpha = ILMath.empty();
        }

        #region Setters and Getters
        public GPLVMType Type
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
                int numParam = _kern.NumParameter;

                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                {
                    numParam += 1;
                    //numParam += NumInducing;
                }

                return numParam;
            }
        }

        /// <summary>
        /// Number of parameters in the model wants to be optimized. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going down the hierarchy to get the
        /// number of all parameters of the object and its children.
        /// </remarks>
        public int NumParameterInHierarchy
        {
            get
            {
                int numParam = this.NumParameter;

                return numParam;
            }
        }

        /// <summary>
        /// Dimension of latent space. 
        /// </summary>
        public int LatentDimension
        {
            get
            {
                return _q;
            }
        }

        public ILArray<double> LatentGradient
        {
            get
            {
                return _latentgX.C;
            }
            set
            {
                _latentgX.a = value;
            }
        }

        public ILArray<double> PostLatentGradient
        {
            get
            {
                return 0;
            }
            set
            {
                
            }
        }

        /// <summary>
        /// The log of the parameter in the model wants to be optimized. 
        /// </summary>
        public ILArray<double> LogParameter
        {
            get
            {
                ILArray<double> param = ILMath.empty();
                
                param = _kern.LogParameter.C;

                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                    param[ILMath.end + 1] = ILMath.log(_beta).C;

                return param;
            }
            set
            {
                ILArray<double> param = ILMath.empty();
                param.a = value;

                int startVal = 0;
                int endVal = _kern.NumParameter - 1;

                _kern.LogParameter = param[ILMath.r(startVal, endVal)];

                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                {
                    startVal = endVal + 1;
                    endVal += 1;
                    _beta = (double)Util.atox(param[startVal]);
                }

                UpdateParameter();
            }
        }

        public virtual ILArray<double> LogPostParameter
        {
            get
            {
                return 0;
            }
            set
            {
                
            }
        }

        public ILArray<double> Y
        {
            get
            {
                return _Y;
            }
            set
            {
                _Y.a = value;
            }
        }

        public ILArray<double> Segments
        {
            get
            {
                return _segments;
            }
        }

        public ILArray<double> X
        {
            get
            {
                return _X;
            }
            set
            {
                _X.a = value;
            }
        }

        public ILArray<double> Xu
        {
            get
            {
                return _Xu;
            }
            set
            {
                _Xu.a = value;
            }
        }

        public ILArray<double> Ynew
        {
            get { return _Ynew; }
            set { _Ynew = value; }
        }

        public ILArray<double> PostMean
        {
            get { return _YPostMean; }
            set { _YPostMean = value.C; }
        }

        public ILArray<double> PostVar
        {
            get { return _Yvar; }
            set
            {
                _Yvar = value.C;
                _YPostMeanOld = value.C;
            }
        }

        public ILArray<double> TestInput
        {
            get { return _Xnew; }
            set { _Xnew = value.C; }
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

        public IPrior Prior
        {
            get
            {
                return _prior;
            }
            set
            {
                _prior = value;
            }
        }

        public ILArray<double> Bias
        {
            get { return _bias; }
        }

        public ILArray<double> Scale
        {
            get { return _scale; }
        }

        public ApproximationType ApproxType
        {
            get
            {
                return _approxType;
            }
            set
            {
                _approxType = value;
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

        public LerningMode Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                foreach (IGPLVM node in Nodes)
                    node.Mode = _mode;
            }
        }

        public Mask Masking
        {
            get { return _mask; }
            set
            {
                _mask = value;
                foreach (IGPLVM node in Nodes)
                    node.Masking = _mask;
            }
        }

        public double Noise
        {
            get { return _kern.Noise; }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Constructs the model. 
        /// </summary>
        public void Initialize()
        {
            //_data.AddData("X", _X);
            //_data.AddData("Y", _Y);

            _N = _Y.Size[0];
            _q = _X.Size[1];
            _D = _Y.Size[1];

            _latentgX = ILMath.zeros(_X.Size);

            ILArray<int> ind = ILMath.empty<int>();
            switch (_approxType)
            {
                case ApproximationType.ftc:
                    _Xu = ILMath.empty();
                    _numInducing = 0;
                    break;
                case ApproximationType.dtc:
                    ILMath.sort(ILMath.rand(1, _N), ind);
                    ind = ind[ILMath.r(0, _numInducing - 1)];
                    _Xu = _X[ind, ILMath.full].C;
                    break;
                case ApproximationType.fitc:
                    ILMath.sort(ILMath.rand(1, _N), ind);
                    ind = ind[ILMath.r(0, _numInducing - 1)];
                    _Xu = _X[ind, ILMath.full].C;
                    break;
            }

            UpdateKernelMatrix();

            if (_prior != null)
                _prior.Initialize(_X, _segments);
        }

        /// <summary>
        /// Adds data to the object. 
        /// </summary>
        /// <param name="data">Sequence length by D ILArray<double>.</param>
        public void AddData(ILInArray<double> data)
        {
            if (!_data.Dataset.ContainsKey("segments"))
                _data.AddData("segments", new double[] { 0f });
            else
                _data.AddData("segments", new double[] { _data.GetData("segments").GetValue(_data.GetData("segments").Length - 1) + data.Length });

            _data.AddData("Y", data);
            _Y.a = _data.GetData("Y");
        }

        public ILRetArray<double> PredictData(ILInArray<double> testInputs, ILOutArray<double> yVar = null)
        {
            using (ILScope.Enter(testInputs))
            {
                ILArray<double> test = ILMath.check(testInputs);
                ILArray<double> k = ILMath.empty();

                for (int i = 0; i < _bias.Length; i++)
                    test[ILMath.full, i] = (test[ILMath.full, i] - _bias[i]) / _scale[i];

                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        k = _kern.ComputeKernelMatrix(test, _X, Flag.reconstruction);
                        break;
                    case ApproximationType.dtc:
                        k = _kern.ComputeKernelMatrix(test, _Xu, Flag.reconstruction);
                        break;
                    case ApproximationType.fitc:
                        k = _kern.ComputeKernelMatrix(test, _Xu, Flag.reconstruction);
                        break;
                }
                
                return ILMath.multiply(k, _alpha);
            }
        }

        /// <summary>
        /// Computes the negative log likelihood of the model. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to get the
        /// log likelihood of the model.
        /// </remarks>
        /// <returns>Negative log likelihood of the model.</returns>
        public double LogLikelihood()
        {
            double L = 0;

            // going through the hierarchy
            foreach (IGPLVM node in Nodes)
                L += node.LogLikelihood();

            switch (_approxType)
            {
                case ApproximationType.ftc:
                    // log normalisation term
                    L -= (double)(_D * _N / 2 * ILMath.log(2 * ILMath.pi) + _D / 2 * _logDetK);
                    // log of the exponent; likelihood for each dimension of Y
                    for (int i = 0; i < _D; i++)
                        L -= (double)(0.5 * ILMath.multiply(ILMath.multiply(_Y[ILMath.full, i].T, _invK), _Y[ILMath.full, i]));

                    break;
                case ApproximationType.dtc:
                    ILArray<double> KufM = ILMath.multiply(_Kuf, _Y);
                    ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);

                    L -= (double)(.5 * (_D * (-(_N - _numInducing) * ILMath.log(_beta) - _logDetK + _logDetA)
                        - (ILMath.sum(ILMath.sum(ILMath.multiplyElem(_invA, KufMKufM)))
                        - ILMath.sum(ILMath.sum(ILMath.multiplyElem(_Y, _Y)))) * _beta));
                    break;

                case ApproximationType.fitc:
                    L -= (double)(.5 * _N * _D * ILMath.log(2 * ILMath.pi));
                    ILArray<double> DinvM = ILMath.multiplyElem(ILMath.repmat(_invD, 1, _D), _Y);
                    ILArray<double> KufDinvM = ILMath.multiply(_Kuf, DinvM);

                    L -= (double)(.5 * (_D * (ILMath.sum(ILMath.log(_diagD))
                        - (_N - _numInducing) * ILMath.log(_beta) + _detDiff) + (ILMath.sum(ILMath.sum(ILMath.multiplyElem(DinvM, _Y)))
                        - ILMath.sum(ILMath.sum(ILMath.multiplyElem(ILMath.multiply(_invA, KufDinvM), KufDinvM)))) * _beta));

                    break;
            }

            // prior of kernel parameters
            L -= (double)(ILMath.sum(_kern.LogParameter));

            // prior of the latents (dynamics or gaussian)
            if (_prior != null)
                L += _prior.LogLikelihood();

            return L;
        }

        /// <summary>
        /// Computes the posterior log likelihood of the model. 
        /// </summary>
        /// <returns>Posterior log likelihood of the model.</returns>
        public double PostLogLikelihood()
        {
            double L = 0;
            ILArray<double> yHat = _YPostMeanOld - _YPostMean;
            L = (double)(-_D / 2 * ILMath.log(_Yvar) - _D / 2 * ILMath.log(2 * ILMath.pi) - ILMath.multiply(yHat, yHat.T) / (2 * _Yvar));

            // prior of the latents (dynamics or gaussian)
            if (_prior != null)
                L += _prior.PostLogLikelihood();
            return L;
        }

        /// <summary>
        /// Computes the gradients of the latents and kernel parameters of the model. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to get the
        /// gradients of the latents and kernel parameters of the model.
        /// </remarks>
        /// <returns>Gradients of the latents and kernel parameters of the model.</returns>
        public ILRetArray<double> LogLikGradient()
        {
            ILArray<double> dL_dK = ILMath.empty(); // derivative of log likelihood w.r.t K
            ILArray<double> dL_dKuf = ILMath.empty(); // derivative of log likelihood w.r.t Kuf

            double gBeta = 0;

            ILArray<double> gParam = ILMath.empty(); // gradient of the kernel parameters

            ILArray<double> g = ILMath.empty();

            switch (_approxType)
            {
                case ApproximationType.ftc:
                    dL_dK = -_D / 2 * _invK + .5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(_invK, _Y), _Y.T), _invK);
                    gParam = _kern.LogLikGradientParam(dL_dK);
                    break;

                case ApproximationType.dtc:
                    ILArray<double> KufM = ILMath.multiply(_Kuf, _Y);
                    ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);
                    ILArray<double> invAKufMKufM = ILMath.multiply(_invA, KufMKufM);
                    ILArray<double> invAKufMKufMinvA = ILMath.multiply(invAKufMKufM, _invA);

                    dL_dK = .5 * (_D * (_invK - (1 / _beta) * _invA) - invAKufMKufMinvA);

                    ILArray<double> invAKuf = ILMath.multiply(_invA, _Kuf);

                    dL_dKuf = -_D * invAKuf - _beta * (ILMath.multiply(invAKufMKufM, invAKuf) - (ILMath.multiply(ILMath.multiply(_invA, KufM), _Y.T)));

                    gParam = _kern.LogLikGradientParam(dL_dKuf) + _kern.LogLikGradientParam(dL_dK);

                    gBeta = (double)(.5 * (_D * ((_N - _numInducing) / _beta + ILMath.sum(ILMath.sum(ILMath.multiplyElem(_invA, _K))) / (_beta * _beta))
                        + ILMath.sum(ILMath.sum(ILMath.multiplyElem(invAKufMKufMinvA, _K))) / _beta
                        + (ILMath.trace(invAKufMKufM) - ILMath.sum(ILMath.sum(ILMath.multiplyElem(Y, Y))))));

                    gBeta *= _beta; // because of the log
                    break;

                case ApproximationType.fitc:
                    ILArray<double> KufDinvM = ILMath.multiply(ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1)), _Y);
                    ILArray<double> AinvKufDinvM = ILMath.multiply(_invA, KufDinvM);
                    ILArray<double> diagKufAinvKufDinvMMT = ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(ILMath.multiply(_invA, KufDinvM), _Y.T)), 0).T;
                    ILArray<double> AinvKufDinvMKufDinvMAinv = ILMath.multiply(AinvKufDinvM, AinvKufDinvM.T);
                    ILArray<double> diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf = ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(_D * _invA + _beta * AinvKufDinvMKufDinvMAinv, _Kuf)), 0).T;
                    ILArray<double> invKuuKuf = ILMath.multiply(_invK, _Kuf);
                    ILArray<double> invKuuKufDinv = ILMath.multiplyElem(invKuuKuf, ILMath.repmat(_invD.T, _numInducing, 1));
                    ILArray<double> diagMMT = ILMath.sum(ILMath.multiplyElem(_Y, _Y), 1);

                    ILArray<double> diagQ = -_D * _diagD + _beta * diagMMT + diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf - 2 * _beta * diagKufAinvKufDinvMMT;
                    
                    dL_dK = .5 * (_D * (_invK - _invA / _beta) - AinvKufDinvMKufDinvMAinv
                        + _beta * ILMath.multiply(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, _numInducing, 1)), invKuuKufDinv.T));

                    dL_dKuf = -_beta * ILMath.multiplyElem(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, _numInducing, 1)), ILMath.repmat(_invD.T, _numInducing, 1))
                        - _D * ILMath.multiplyElem(ILMath.multiply(_invA, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1))
                        - _beta * ILMath.multiplyElem(ILMath.multiply(AinvKufDinvMKufDinvMAinv, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1))
                        + _beta * ILMath.multiplyElem(ILMath.multiply(ILMath.multiply(_invA, KufDinvM), _Y.T), ILMath.repmat(_invD.T, _numInducing, 1));

                    ILArray<double> Kstar = ILMath.divide(.5 * diagQ * _beta, ILMath.multiplyElem(_diagD, _diagD));

                    gParam = _kern.LogLikGradientParam(dL_dKuf) + _kern.LogLikGradientParam(dL_dK);
                    gParam += _kern.DiagGradParam(_X, Kstar);

                    gBeta = (double)-ILMath.sum(Kstar) / (_beta * _beta);

                    gBeta *= _beta; // because of the log
                    break;
            }

            if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                gParam[ILMath.end + 1] = gBeta;

            return gParam.C;
        }

        public ILArray<double> PostLogLikGradient()
        {
            using (ILScope.Enter())
            {
                return 0;
            }
        }


        /// <summary>
        /// Updates the parameters after each optimization step. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to update the parameters.
        /// </remarks>
        /// <returns>The new latent points of the object.</returns>
        public ILRetArray<double> UpdateParameter()
        {
            UpdateKernelMatrix();

            return _X;
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
                                    _beta = Double.Parse(reader.Value);

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
                            _Y = _Y.Reshape(_D, _N).T;
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
                        if (reader.Name == "scale")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens.Length > 3)
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
                            else
                            {
                                if (tokens[0] == "<Double>")
                                {
                                    _scale = ILMath.zeros(1, tokens.Length - 1);
                                    for (int i = 1; i < tokens.Length; i++)
                                        _scale[i] = Double.Parse(tokens[i]);
                                }
                                else
                                {
                                    _scale = ILMath.zeros(1, tokens.Length);
                                    for (int i = 0; i < tokens.Length; i++)
                                        _scale[i] = Double.Parse(tokens[i]);
                                }
                            }

                        }
                        /*if (reader.Name == "segments")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            _segments = ILMath.zeros(1, tokens.Length);
                            for (int i = 0; i < tokens.Length; i++)
                                _segments[i] = Double.Parse(tokens[i]);
                            _segments = _segments.T;
                        }*/
                        if (reader.Name == "bias")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _bias = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _bias[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _bias = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _bias[i] = Double.Parse(tokens[i]);
                            }
                        }
                        /*if (reader.Name == "innerProducts")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _innerProducts = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _innerProducts[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _innerProducts = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _innerProducts[i] = Double.Parse(tokens[i]);
                            }
                        }*/
                        if (reader.Name == "alpha")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _alpha = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _alpha[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _alpha = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _alpha[i] = Double.Parse(tokens[i]);
                            }
                            _alpha = _alpha.Reshape(_D, _N).T;
                        }
                    }
                    reader.Read();
                }
                if (reader.Name == "Kernel")
                {
                    reader.MoveToAttribute("type");
                    string type = (string)reader.ReadContentAs(typeof(string), null);
                    switch (type)
                    {
                        case "KernelTypeLinear":
                            _kern = new LinearKern();
                            _kern.Read(ref reader);
                            break;
                        case "KernelTypeRBF":
                            _kern = new RBFKern();
                            _kern.Read(ref reader);
                            break;
                        case "KernelTypeBias":
                            _kern = new BiasKern();
                            _kern.Read(ref reader);
                            break;
                        case "KernelTypeWhite":
                            _kern = new WhiteKern();
                            _kern.Read(ref reader);
                            break;
                        case "KernelTypeCompound":
                            _kern = new CompoundKern();
                            _kern.Read(ref reader);
                            break;
                        case "KernelTypeTensor":
                            _kern = new TensorKern();
                            _kern.Read(ref reader);
                            break;
                    }
                }
                if (reader.Name == "Prior")
                {
                    reader.MoveToAttribute("type");
                    switch ((string)reader.ReadContentAs(typeof(string), null))
                    {
                        case "PriorTypeNoPrior":
                            _prior = null;
                            break;
                        case "PriorTypeGauss":
                            _prior = new Gaussian();
                            _prior.Read(ref reader);
                            break;
                        case "PriorTypeConnectivity":
                            _prior = new Connectivity();
                            _prior.Read(ref reader);
                            break;

                    }
                    reader.Read();
                }
                
                if (reader.Name == "GPLVM")
                {
                    Nodes.Add(new GP_LVM());
                    ((GP_LVM)Nodes[Nodes.Count - 1]).Read(ref reader);
                }
            }
            UpdateKernelMatrix();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("BackProject", null);
            writer.WriteAttributeString("approximation", _approxType.ToString());
            writer.WriteStartElement("Data", null);
                writer.WriteElementString("q", _q.ToString());
                writer.WriteElementString("D", _D.ToString());
                writer.WriteElementString("N", _N.ToString());
                writer.WriteElementString("NumInducing", _numInducing.ToString());
                writer.WriteElementString("beta", _beta.ToString());

                writer.WriteStartElement("Y");
                    writer.WriteAttributeString("data", _Y.ToString().Normalize().Remove(0, _Y.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                writer.WriteEndElement();

                writer.WriteStartElement("X");
                    writer.WriteAttributeString("data", _X.ToString().Remove(0, _X.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                writer.WriteEndElement();

                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                {
                    writer.WriteStartElement("Xu");
                        writer.WriteAttributeString("data", _Xu.ToString().Remove(0, _Xu.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();
                }

                writer.WriteStartElement("scale");
                    writer.WriteAttributeString("data", _scale.ToString().Remove(0, _scale.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                writer.WriteEndElement();

            /*if (_segments.Length > 1)
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
            }*/

                writer.WriteStartElement("bias");
                    writer.WriteAttributeString("data", _bias.ToString().Remove(0, _bias.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                writer.WriteEndElement();

            //writer.WriteStartElement("innerProducts");
            //writer.WriteAttributeString("data", _innerProducts.ToString().Remove(0, _innerProducts.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            //writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("Kernel", null);
                writer.WriteAttributeString("type", _kern.Type.ToString());
                _kern.Write(ref writer);
            writer.WriteEndElement();

            if (_prior != null)
                _prior.Write(ref writer);

            foreach (IGPLVM node in Nodes)
                node.Write(ref writer);

            writer.WriteEndElement();
        }

        public bool ReadMat(string filename)
        {
            try 
            {
                using (ILMatFile reader = new ILMatFile(filename))
                {

                    _alpha.a = reader.GetArray<double>("alphaBackProjection");

                    _kern.Parameter = reader.GetArray<double>("kernelParamsBackProjection");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Updates the kernel matrix after each optimization step. 
        /// </summary>
        private void  UpdateKernelMatrix()
        {
            if (_kern != null)
            {
                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        _K = _kern.ComputeKernelMatrix(_X, _X);
                        _invK = Util.pdinverse(_K);
                        _logDetK = Util.logdet(_K);

                        _alpha = ILMath.multiply(_invK, _Y);

                        //for (int i = 0; i < _D; i++)
                        //    _innerProducts[i] = ILMath.multiply(ILMath.multiply(_M[ILMath.full, i].T, _invK), _M[ILMath.full, i]);
                        break;

                    case ApproximationType.dtc:
                        _K = _kern.ComputeKernelMatrix(_Xu, _Xu);
                        _Kuf = _kern.ComputeKernelMatrix(_Xu, _X);
                        _invK = Util.pdinverse(_K);
                        _logDetK = Util.logdet(_K);

                        _A = (1 / _beta) * _K + ILMath.multiply(_Kuf, _Kuf.T);
                        // This can become unstable when K_uf2 is low rank.
                        _invA = Util.pdinverse(_A);
                        _logDetA = Util.logdet(_A);

                        //for (int i = 0; i < _D; i++)
                        //    _innerProducts[i] = _beta * (ILMath.multiply(_M[ILMath.full, i].T, _M[ILMath.full, i])
                        //        - ILMath.multiply(ILMath.multiply(ILMath.multiply(_Kuf, _M[ILMath.full, i]).T, _invA), ILMath.multiply(_Kuf, _M[ILMath.full, i])));

                        _alpha = ILMath.multiply(ILMath.multiply(_invA, _Kuf), _Y);
                        break;

                    case ApproximationType.fitc:
                        _K = _kern.ComputeKernelMatrix(_Xu, _Xu);
                        _Kuf = _kern.ComputeKernelMatrix(_Xu, _X);
                        _invK = Util.pdinverse(_K);
                        _logDetK = Util.logdet(_K);

                        ILArray<double >_diagK = _kern.ComputeDiagonal(_X);

                        _diagD = 1 + _beta * _diagK -_beta * ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(_invK, _Kuf)), 0).T;
                        _diagD[_diagD == 0.0] = 1e-10;
                        _invD = 1 / _diagD; //ILMath.diag(1 / _diagD);
                        //ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiply(_Kuf, _invD), _Kuf.T);
                        ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1)), _Kuf.T);
                        _A = (1 / _beta) * _K + KufDinvKuf;    

                        // This can become unstable when K_ufDinvK_uf is low rank.
                        _invA = Util.pdinverse(_A);
                        _logDetA = Util.logdet(_A);

                        _detDiff = - ILMath.log(_beta) * _numInducing + ILMath.log(ILMath.det(ILMath.eye(_numInducing, _numInducing) + _beta * ILMath.multiply(KufDinvKuf, _invK)));

                        _alpha = ILMath.multiply(ILMath.multiplyElem(ILMath.multiply(_invA, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1)), _Y);
                        break;
                }
            }
            else
                System.Console.WriteLine("No kernel function found! Please add a kernel object!");
        }
        #endregion

    }
}
