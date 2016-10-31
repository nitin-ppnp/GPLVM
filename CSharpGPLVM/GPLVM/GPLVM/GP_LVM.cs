using GPLVM.Graph;
using GPLVM.Kernel;
using GPLVM.Dynamics;
using GPLVM.Optimisation;
using GPLVM.Prior;
using GPLVM.Backconstraint;
using GPLVM.Embeddings;
using ILNumerics;
using System.Xml;
using System;

namespace GPLVM.GPLVM
{
    public class GP_LVM : Node, IGPLVM
    {
        private GPLVMType _type = GPLVMType.normal;
        protected BackConstType _backtype;
        protected ApproximationType _approxType;

        protected XInit _initX;

        protected int _q;                     // dimension of latent space
        protected int _D;                     // dimension of data space
        protected int _N;                     // length of the data set

        protected int _numInducing = 500;           // number of inducing inputs

        protected ILArray<double> _parameter = ILMath.localMember<double>();

        protected ILArray<double> _X = ILMath.localMember<double>();        // Nxq matrix of latent points
        protected ILArray<double> _Xu = ILMath.localMember<double>();        // numInducingxq matrix of latent inducing inputs
        protected ILArray<double> _Y = ILMath.localMember<double>();         // NxD matrix of data points
        protected ILArray<double> _M = ILMath.localMember<double>();         // normalized data
        protected ILArray<double> _scale = ILMath.localMember<double>();     // 1xD vector of weights for the data
        protected ILArray<double> _segments = ILMath.localMember<double>();

        // for learning self produced data
        protected ILArray<double> _Ynew = ILMath.localMember<double>();
        protected ILArray<double> _YPostMean = ILMath.localMember<double>();
        protected ILArray<double> _Yvar = ILMath.localMember<double>();
        protected ILArray<double> _Xnew = ILMath.localMember<double>();

        protected ILArray<double> _bias = ILMath.localMember<double>();     // 1xD vector of weights for the data

        protected ILArray<double> _latentgX = ILMath.localMember<double>();  // top down prior of the latents
        protected ILArray<double> _latentPointgX = ILMath.localMember<double>();

        protected ILArray<double> _K = ILMath.localMember<double>();        // NxN kernel covariance matrix
        protected ILArray<double> _Kuf = ILMath.localMember<double>();      // NxN kernel covariance matrix
        protected ILArray<double> _invK = ILMath.localMember<double>();     // NxN inverse kernel covariance matrix
        protected ILArray<double> _k = ILMath.localMember<double>();        // reconstruction matrix
        protected double _logDetK;                                          // log determinant of K
        protected ILArray<double> _A = ILMath.localMember<double>();        // help matrix for rearranging log-likelihood
        protected ILArray<double> _invA = ILMath.localMember<double>();
        protected double _logDetA;

        protected ILArray<double> _diagK = ILMath.localMember<double>();    // diagonal of the kernel given _X
        protected ILArray<double> _diagD = ILMath.localMember<double>();    // help diagonal of the kernel for rearranging fitc log-likelihood
        protected ILArray<double> _invD = ILMath.localMember<double>();
        protected ILArray<double> _detDiff = ILMath.localMember<double>();

        protected double _beta;               // precission term of inducing mapping

        protected IKernel _kern;               // the kernel object
        protected ILArray<double> _innerProducts = ILMath.localMember<double>();
        protected ILArray<double> _alpha = ILMath.localMember<double>();     // part needed for mean prediction

        protected IPrior _prior;              // prior of the latents (gaussian or dynamics)

        //private Data _data;                 // the data dictoinary

        protected BackProjection _backProject;
        protected IBackconstraint _backConst;

        protected LerningMode _mode;
        protected Mask _mask;

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// In a Gaussian process latent variable model is a high dimensional data set Y represented by a
        /// lower dimensional latent variable X. It is a non-linear mapping from the latent space to the data space
        /// with a non-linear mapping function f(x) drawn from a Gaussian process and some noise e
        /// (y = f(x) + e; f(x) ~ GP(m(x),k(x,x')); where m(x) is the mean function and k(x,x') the kernel function).
        /// This class provides the full fit (FTC) of the data.
        /// </remarks>
        /// <param name="dimension">The dimension of the latent space.</param>
        public GP_LVM(int latentDimension = 3, ApproximationType aType = ApproximationType.ftc, BackConstType btype = BackConstType.none, XInit initX = XInit.pca)
            : base()
        {
            _q = latentDimension;
            _D = 0;                     // dimension of data space
            _N = 0;

            _initX = initX;
            _backtype = btype;
            _approxType = aType;

            _beta = 1e3;

            _backConst = null;

            _kern = new CompoundKern();
            ((CompoundKern)_kern).AddKern(new RBFKern());
            ((CompoundKern)_kern).AddKern(new LinearKern());
            //((CompoundKern)_kern).AddKern(new BiasKern());
            ((CompoundKern)_kern).AddKern(new WhiteKern());

            _mode = LerningMode.prior; //set learning flag as default
            _mask = Mask.full;
        }


        #region Setters and Getters
        /// <summary>
        /// Gets the number of parameters of the objects wants to be optimized. 
        /// </summary>
        public virtual int NumParameter
        {
            get
            {
                int numParam = 0;
                switch (Mode)
                {
                    case LerningMode.prior:
                        switch (Masking)
                        {
                            case Mask.full:
                                if (_backConst != null)
                                    numParam = _backConst.NumParameter;
                                else
                                    numParam = _q * _N; // number of latent points

                                numParam += _scale.Length;

                                if (_prior != null)
                                    numParam += _prior.NumParameter;

                                numParam += _kern.NumParameter;

                                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                {
                                    numParam += 1;
                                    numParam += NumInducing * _q;
                                }
                                break;
                            case Mask.latents:
                                if (_backConst != null)
                                    numParam = _backConst.NumParameter;
                                else
                                    numParam = _q * _N; // number of latent points

                                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    numParam += NumInducing * _q;
                                break;

                            case Mask.kernel:
                                if (_prior != null)
                                    numParam += _prior.NumParameter;

                                numParam += _kern.NumParameter;

                                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    numParam += 1;
                                break;
                        }
                        break;

                    case LerningMode.posterior:
                        numParam = _Xnew.S[0] * _q;
                        break;

                    case LerningMode.selfposterior:
                        if (Nodes.Count == 0)
                            numParam = Ynew.S[0] * _D;// + _Xnew.S[0] * _q;
                        else
                            numParam = _Xnew.S[0] * _q;

                        break;
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

                // going down the hierarchy
                foreach (IGPLVM node in Nodes)
                    numParam += node.NumParameterInHierarchy;

                return numParam;
            }
        }


        public GPLVMType Type
        {
            get
            {
                return _type;
            }
        }
        /// <summary>
        /// Dimension of latent space. 
        /// </summary>
        public virtual int LatentDimension
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
                return _latentgX;
            }
            set
            {
                _latentgX = value;
            }
        }

        public ILArray<double> PostLatentGradient
        {
            get
            {
                return _latentPointgX;
            }
            set
            {
                _latentPointgX = value;
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

        public ILArray<double> M
        {
            get
            {
                return _M;
            }
        }

        public ILArray<double> PostMean
        {
            get { return _YPostMean; }
            set { _YPostMean = value; }
        }

        public ILArray<double> PostVar
        {
            get { return _Yvar; }
            set 
            { 
                _Yvar = value;
            }
        }

        public ILArray<double> Ynew
        {
            get { return _Ynew.C; }
            set { _Ynew.a = value; }
        }

        public ILArray<double> TestInput
        {
            get { return _Xnew; }
            set 
            { 
                _Xnew.a = value;
                if (_prior != null)
                    _prior.Xnew = _Xnew.C;
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

        /// <summary>
        /// The log of the parameter in the model wants to be optimized. 
        /// </summary>
        public virtual ILArray<double> LogParameter
        {
            get
            {
                    ILArray<double> param = ILMath.empty();
                    switch(Mode)
                    {
                        case LerningMode.prior:
                            switch (Masking)
                            {
                                case Mask.full:
                                    if (_backConst != null)
                                        param = _backConst.LogParameter;
                                    else
                                        param = _X[ILMath.full].T.C;

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                        param[ILMath.r(param.Length, param.Length + _Xu[ILMath.full].Length - 1)] = _Xu[ILMath.full].T.C;

                                    param[ILMath.r(param.Length, param.Length + _scale.Length - 1)] = ILMath.log(_scale).C;
                                    param[ILMath.r(param.Length, param.Length + _kern.LogParameter.Length - 1)] = _kern.LogParameter.C;

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                        param[ILMath.end + 1] = ILMath.log(_beta).C;

                                    if (_prior != null)
                                        if (_prior.NumParameter != 0)
                                            param[ILMath.r(param.Length, param.Length + _prior.LogParameter.Length - 1)] = _prior.LogParameter.C;
                                    break;

                                case Mask.latents:
                                    if (_backConst != null)
                                        param = _backConst.LogParameter;
                                    else
                                        param = _X[ILMath.full].T.C;

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                        param[ILMath.r(param.Length, param.Length + _Xu[ILMath.full].Length - 1)] = _Xu[ILMath.full].T.C;
                                    break;

                                case Mask.kernel:
                                    param = _kern.LogParameter.C;
                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                        param[ILMath.end + 1] = ILMath.log(_beta).C;
                                    if (_prior != null)
                                        if (_prior.NumParameter != 0)
                                            param[ILMath.r(param.Length, param.Length + _prior.LogParameter.Length - 1)] = _prior.LogParameter.C;
                                    break;
                            }
                            break;
                            

                        case LerningMode.posterior:
                            param = _Xnew[ILMath.full].T.C;
                            break;

                        case LerningMode.selfposterior:
                            if (Nodes.Count == 0)
                            {
                                param = _Ynew[ILMath.full].T.C;
                                //param[ILMath.r(ILMath.end + 1, ILMath.end + _Xnew.S[1])] = _Xnew[ILMath.full].T.C;
                            }
                            else
                                param = _Xnew[ILMath.full].T.C;
                            break;
                    }

                    foreach (IGPLVM node in Nodes)
                    {
                        ILArray<double> tmp = node.LogParameter;
                        param[ILMath.r(param.Length, param.Length + tmp.Length - 1)] = tmp.C;
                    }

                    return param.C;
            }
            set
            {
                    ILArray<double> param = ILMath.empty();
                    param.a = value;

                    int startVal = 0;
                    int endVal = 0;

                    switch (Mode)
                    {
                        case LerningMode.prior:
                            switch (Masking)
                            {
                                case Mask.full:
                                    if (_backConst != null)
                                    {
                                        endVal = _backConst.NumParameter - 1;
                                        _backConst.LogParameter = param[ILMath.r(startVal, endVal)];
                                        _X.a = _backConst.GetBackConstraints();
                                    }
                                    else
                                    {
                                        endVal = _q * _N - 1;
                                        _X.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _N, _q);
                                    }

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    {
                                        startVal = endVal + 1;
                                        endVal += NumInducing * _q;
                                        _Xu.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], NumInducing, _q);
                                    }

                                    startVal = endVal + 1;
                                    endVal += _scale.Length;
                                    _scale.a = Util.atox(param[ILMath.r(startVal, endVal)]);

                                    startVal = endVal + 1;
                                    endVal += _kern.NumParameter;
                                    _kern.LogParameter = param[ILMath.r(startVal, endVal)];

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    {
                                        startVal = endVal + 1;
                                        endVal += 1;
                                        _beta = (double)Util.atox(param[startVal]);
                                    }

                                    if (_prior != null)
                                    {
                                        if (_prior.NumParameter != 0)
                                        {
                                            startVal = endVal + 1;
                                            endVal += _prior.NumParameter;
                                            _prior.LogParameter = param[ILMath.r(startVal, endVal)];
                                        }
                                        _prior.UpdateParameter(_X);
                                    }
                                    break;
                                case Mask.latents:
                                    if (_backConst != null)
                                    {
                                        endVal = _backConst.NumParameter - 1;
                                        _backConst.LogParameter = param[ILMath.r(startVal, endVal)];
                                        _X.a = _backConst.GetBackConstraints();
                                    }
                                    else
                                    {
                                        endVal = _q * _N - 1;
                                        _X.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _N, _q);
                                    }

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    {
                                        startVal = endVal + 1;
                                        endVal += NumInducing * _q;
                                        _Xu.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], NumInducing, _q);
                                    }
                                    if (_prior != null)
                                        _prior.UpdateParameter(_X);

                                    break;
                                case Mask.kernel:
                                    endVal += _kern.NumParameter - 1;
                                    _kern.LogParameter = param[ILMath.r(startVal, endVal)];

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    {
                                        startVal = endVal + 1;
                                        endVal += 1;             
                                        _beta = (double)Util.atox(param[startVal]);
                                    }
                                    if (_prior != null)
                                    {
                                        if (_prior.NumParameter != 0)
                                        {
                                            startVal = endVal + 1;
                                            endVal += _prior.NumParameter;
                                            _prior.LogParameter = param[ILMath.r(startVal, endVal)];
                                        }
                                        _prior.UpdateParameter(_X);
                                    }
                                    break;
                            }
                            break;

                        case LerningMode.posterior:
                            _Xnew.a = ILMath.reshape(param, _Xnew.S[0], _q);
                            if (_prior != null)
                                _prior.Xnew = _Xnew.C;
                            break;

                        case LerningMode.selfposterior:
                            if (Nodes.Count == 0)
                            {
                                endVal = _D * _Ynew.S[0] - 1;
                                _Ynew.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _Ynew.S[0], _D);
                            }

                            startVal = endVal + 1;
                            endVal += _q * _Xnew.S[0];

                            //_Xnew.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _Xnew.S[0], _q);

                            //if (_prior != null)
                            //    _prior.Xnew = _Xnew.C;
                            break;
                    }

                    foreach (IGPLVM node in Nodes)
                    {
                        startVal = endVal + 1;
                        endVal += node.NumParameterInHierarchy;
                        node.LogParameter = param[ILMath.r(startVal, endVal)];
                    }

                    if (!isChild)
                        UpdateParameter();
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

        public ILArray<double> Bias
        {
            get { return _bias; }
        }

        public ILArray<double> Scale
        {
            get { return _scale; }
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

        public IBackconstraint BackConstraint
        {
            get
            {
                return _backConst;
            }
            set
            {
                _backConst = value;
            }
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
        #endregion

        #region Public Functions
        /// <summary>
        /// Constructs the model. 
        /// </summary>
        public virtual void Initialize()
        {
            using (ILScope.Enter())
            {
                ILArray<double> ytmp = ILMath.empty();
                int startVal = 0;
                foreach (IGPLVM node in Nodes)
                {
                    node.Initialize();
                    if (_Y.IsEmpty)
                    {
                        _Y.a = node.UpdateParameter().C;
                        _segments.a = node.Segments.C;
                    }
                    else
                    {
                        ytmp = _Y.C;
                        ytmp[ILMath.full, ILMath.r(startVal, startVal + node.LatentDimension - 1)] = node.UpdateParameter().C;
                        _Y.a = ytmp;
                    }

                    startVal += node.LatentDimension;
                }

                _N = _Y.Size[0];
                _D = _Y.Size[1];

                _scale.a = ILMath.ones(1, _D);
                _bias.a = ILMath.mean(_Y);

                _latentPointgX.a = ILMath.zeros(1, _q);

                _M.a = ILMath.zeros(_Y.Size);
                for (int i = 0; i < _bias.Length; i++)
                    _M[ILMath.full, i] = ((_Y[ILMath.full, i] - _bias[i]) / _scale[i]).C;

                if (_X.IsEmpty)
                    EstimateX();

                ILArray<int> ind = ILMath.empty<int>();
                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        _Xu.a = ILMath.empty();
                        _numInducing = 0;
                        break;
                    case ApproximationType.dtc:
                        ILMath.sort(ILMath.rand(1, _N), ind);
                        ind = ind[ILMath.r(0, _numInducing - 1)];
                        _Xu.a = _X[ind, ILMath.full].C;
                        break;
                    case ApproximationType.fitc:
                        ILMath.sort(ILMath.rand(1, _N), ind);
                        ind = ind[ILMath.r(0, _numInducing - 1)];
                        _Xu.a = _X[ind, ILMath.full].C;
                        break;
                }

                switch (_backtype)
                {
                    case BackConstType.kbr:
                        _backConst = new BackConstraintKBR();
                        _backConst.Initialize(_Y, _X, _segments, _initX);
                        _X.a = _backConst.GetBackConstraints();
                        break;

                    case BackConstType.ptc:
                        _backConst = new BackConstraintPTC();
                        _backConst.Initialize(_Y, _X, _segments, _initX);
                        _X.a = _backConst.GetBackConstraints();
                        break;

                    case BackConstType.mlp:
                        // Preoptimizing multi layer perceptron
                        BackConstraintMLP bc = new BackConstraintMLP();
                        bc.Initialize(_Y, _X, _segments, _initX);
                        bc.ActivationFunction = MLPFunction.Linear;
                        Console.WriteLine("Preoptimizing backconstraint...");
                        SCG.Optimize(ref bc, 200, true);

                        _backConst = bc;

                        _X.a = _backConst.GetBackConstraints();
                        break;
                }

                if (!isChild)
                    UpdateParameter();

                if (_prior != null)
                {
                    if (_prior.Type == PriorType.PriorTypeCompound)
                        foreach (IPrior prior in ((Compound)_prior).Priors)
                            if (prior.Type == PriorType.PriorTypeLocallyLinear)
                                ((LocallyLinear)prior).M = _M;

                    if (_prior.Type == PriorType.PriorTypeLocallyLinear)
                        ((LocallyLinear)_prior).M = _M;

                    _prior.Initialize(_X, _segments);
                }
            }
        }

        /// <summary>
        /// Adds data to the object. 
        /// </summary>
        /// <param name="data">Sequence length by D ILArray<double>.</param>
        public virtual void AddData(ILInArray<double> data)
        {
            if (_segments.IsEmpty)
                _segments.a = 0;
            else
                _segments[ILMath.end + 1] = _Y.Size[0];

            if (_Y.IsEmpty)
                _Y.a = data;
            else
                _Y[ILMath.r(ILMath.end + 1, ILMath.end + data.Size[0]), ILMath.full] = data;  
        }

        public ILRetArray<double> PredictData(ILInArray<double> testInputs, ILOutArray<double> yVar = null)
        {
            using (ILScope.Enter(testInputs))
            {
                _Xnew.a = ILMath.check(testInputs);

                //ILArray<double> k = ILMath.empty();

                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        if (Mode == LerningMode.selfposterior)
                            _k.a = _kern.ComputeKernelMatrix(_Xnew, _X, Flag.postlearning);
                        else
                            _k.a = _kern.ComputeKernelMatrix(_Xnew, _X, Flag.reconstruction);
                        break;
                    case ApproximationType.dtc:
                        if (Mode == LerningMode.selfposterior)
                            _k.a = _kern.ComputeKernelMatrix(_Xnew, _Xu, Flag.postlearning);
                        else
                            _k.a = _kern.ComputeKernelMatrix(_Xnew, _Xu, Flag.reconstruction);
                        break;
                    case ApproximationType.fitc:
                        if (Mode == LerningMode.selfposterior)
                            _k.a = _kern.ComputeKernelMatrix(_Xnew, _Xu, Flag.postlearning);
                        else
                            _k.a = _kern.ComputeKernelMatrix(_Xnew, _Xu, Flag.reconstruction);
                        break;
                }

                _YPostMean.a = ILMath.multiply(_k, _alpha);

                for (int i = 0; i < _bias.Length; i++)
                    _YPostMean[ILMath.full, i] = _YPostMean[ILMath.full, i] * _scale[i] + _bias[i];

                if (yVar != null)
                {
                    //ILArray<double> kv = _kern.ComputeDiagonal(_Xnew);
                    ILArray<double> kv = _kern.ComputeKernelMatrix(_Xnew, _Xnew, Flag.reconstruction);
                    ILArray<double> var = ILMath.empty();
                    switch (_approxType)
                    {
                        case ApproximationType.ftc:
                            //var = kv - ILMath.sum(ILMath.multiplyElem(_k.T, ILMath.multiply(_invK, _k.T)), 0);
                            var = kv - ILMath.multiply(ILMath.multiply(_k, _invK), _k.T);
                            break;
                        case ApproximationType.dtc:
                            var = kv - ILMath.sum(ILMath.multiplyElem(_k.T, ILMath.multiply(_invK - (1 / _beta) * _invA, _k.T)), 0).T;
                            var = var + (1 / _beta);
                            break;
                        case ApproximationType.fitc:
                            //var = kv - ILMath.sum(ILMath.multiplyElem(_k.T, ILMath.multiply(_invK - (1 / _beta) * _invA, _k.T)), 0).T;
                            var = kv - ILMath.multiply(ILMath.multiply(_k, _invK - (1 / _beta) * _invA), _k.T);
                            var = var + (1 / _beta);
                            break;
                    }

                    //var = ILMath.repmat(var, 1, _D);
                    yVar.a = var.C;//ILMath.multiplyElem(var, ILMath.repmat(ILMath.multiplyElem(_scale, _scale), _Xnew.S[0], 1));
                    _Yvar.a = yVar.C;
                }

                return _YPostMean;
            }
        }


        /// <summary>
        /// Computes the log likelihood of the model. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to get the
        /// log likelihood of the model.
        /// </remarks>
        /// <returns>Log likelihood of the model.</returns>
        public double LogLikelihood()
        {
            if (Mode == LerningMode.prior)
                return PreLogLikelihood();
            else
                return PostLogLikelihood();
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
            if (Mode == LerningMode.prior)
                return PreLogLikGradient();
            else
                return PostLogLikGradient();
        }

        /// <summary>
        /// Updates the parameters after each optimization step. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to update the parameters.
        /// </remarks>
        /// <returns>The new latent points of the object.</returns>
        public virtual ILRetArray<double> UpdateParameter()
        {
            using (ILScope.Enter())
            {
                int startVal = 0;
                ILArray<double> ret = ILMath.empty();

                switch (Mode)
                {
                    case LerningMode.prior:
                        foreach (IGPLVM node in Nodes)
                        {
                            _Y[ILMath.full, ILMath.r(startVal, startVal + node.LatentDimension - 1)] = node.UpdateParameter().C;
                            startVal += node.LatentDimension;
                        }

                        _bias.a = ILMath.mean(_Y);
                        _M.a = ILMath.zeros(_Y.S);
                        for (int i = 0; i < _bias.Length; i++)
                            _M[ILMath.full, i] = ((_Y[ILMath.full, i] - _bias[i]) / _scale[i]).C;

                        UpdateKernelMatrix();
                        ret = _X;
                        break;

                    case LerningMode.posterior:
                        foreach (IGPLVM node in Nodes)
                        {
                            _Ynew[ILMath.full, ILMath.r(startVal, startVal + node.LatentDimension - 1)] = node.UpdateParameter().C;
                            startVal += node.LatentDimension;
                            PredictData(_Xnew, _Yvar);
                            ret = _Xnew;
                        }
                        break;

                    case LerningMode.selfposterior:
                        foreach (IGPLVM node in Nodes)
                        {
                            _Ynew[ILMath.full, ILMath.r(startVal, startVal + node.LatentDimension - 1)] = node.UpdateParameter().C;
                            startVal += node.LatentDimension;
                            //PredictData(_Xnew, _Yvar);
                            ret = _Xnew;
                        }
                        break;
                }

                return ret;
            }
        }

        public virtual void Read(ref XmlReader reader)
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
                                _Y.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _Y[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _Y.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _Y[i] = Double.Parse(tokens[i]);
                            }
                            _Y.a = _Y.Reshape(_D, _N).T;
                        }
                        if (reader.Name == "X")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _X.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _X[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _X.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _X[i] = Double.Parse(tokens[i]);
                            }
                            _X.a = _X.Reshape(_q, _N).T;
                        }
                        if (reader.Name == "Xu")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _Xu.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _Xu[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _Xu.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _Xu[i] = Double.Parse(tokens[i]);
                            }
                            _Xu.a = _Xu.Reshape(_q, _numInducing).T;
                        }
                        if (reader.Name == "scale")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _scale.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _scale[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _scale.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _scale[i] = Double.Parse(tokens[i]);
                            }
                        }
                        if (reader.Name == "segments")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            _segments.a = ILMath.zeros(1, tokens.Length);
                            for (int i = 0; i < tokens.Length; i++)
                                _segments[i] = Double.Parse(tokens[i]);
                            _segments.a = _segments.T;
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
                        case "KernelTypeWhite":
                            _kern = new WhiteKern();
                            _kern.Read(ref reader);
                            break;
                        case "KernelTypeBias":
                            _kern = new BiasKern();
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
                        case "PriorTypeDynamics":
                            reader.MoveToAttribute("DynamicType");
                            switch ((string)reader.ReadContentAs(typeof(string), null))
                            {
                                case "DynamicTypeVelocity":
                                    _prior = new GPVelocity();
                                    _prior.Read(ref reader);
                                    break;
                                case "DynamicTypeAcceleration":
                                    _prior = new GPAcceleration();
                                    _prior.Read(ref reader);
                                    break;
                            }
                            break;
                            
                    }
                    reader.Read();
                }
                if (reader.Name == "Backconstraint")
                {
                    reader.MoveToAttribute("type");
                    switch ((string)reader.ReadContentAs(typeof(string), null))
                    {
                        case "kbr":
                            _backConst = new BackConstraintKBR();
                            _backConst.Read(ref reader);
                            break;
                        case "mlp":
                            _backConst = new BackConstraintMLP();
                            _backConst.Read(ref reader);
                            break;
                        case "ptc":
                            _backConst = new BackConstraintPTC();
                            _backConst.Read(ref reader);
                            break;
                    }
                    reader.Read();
                }

                if (reader.Name == "Children")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "GPLVM")
                        {
                            AddNode(new GP_LVM());
                            ((IGPLVM)Nodes[Nodes.Count - 1]).Read(ref reader);
                        }
                        if (reader.Name == "StyleGPLVM")
                        {
                            AddNode(new StyleGPLVM());
                            ((IGPLVM)Nodes[Nodes.Count - 1]).Read(ref reader);
                        }
                        if (reader.Name == "StyleGPLVM2")
                        {
                            AddNode(new StyleGPLVM2());
                            ((IGPLVM)Nodes[Nodes.Count - 1]).Read(ref reader);
                        }
                    }
                    reader.Read();
                }
            }
            reader.Read();
            _latentgX.a = ILMath.zeros(_X.S);
            UpdateParameter();
        }

        public virtual void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("GPLVM", null);
            writer.WriteAttributeString("approximation", _approxType.ToString());
                writer.WriteStartElement("Data", null);
                    writer.WriteElementString("q", _q.ToString());
                    writer.WriteElementString("D", _D.ToString());
                    writer.WriteElementString("N", _N.ToString());
                    writer.WriteElementString("NumInducing", _numInducing.ToString());
                    writer.WriteElementString("beta", _beta.ToString());

                    writer.WriteStartElement("Y");
                        writer.WriteAttributeString("data", _Y.ToString().Normalize().Remove(0, _Y.ToString().IndexOf("]") + 1).Replace("\n","").Replace("\r","")); 
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

                if (_prior != null)
                    _prior.Write(ref writer);

                if (_backConst != null)
                    _backConst.Write(ref writer);

                if (Nodes.Count != 0)
                {
                    writer.WriteStartElement("Children", null);
                    foreach (IGPLVM node in Nodes)
                        node.Write(ref writer);
                    writer.WriteEndElement();
                }

            writer.WriteEndElement();
        }

        public bool ReadMat(string filename)
        {
            bool success = true;
            try 
            {
                using (ILMatFile reader = new ILMatFile(filename))
                {

                    _Y.a = reader.GetArray<double>("Y");
                    _X.a = reader.GetArray<double>("X");
                    _bias.a = reader.GetArray<double>("biasGPLVM");
                    _alpha.a = reader.GetArray<double>("alphaGPLVM");
                    _scale = reader.GetArray<double>("scale");

                    //_kern.Parameter = reader.GetArray<double>("kernelParamsGPLVM");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }

            return success;
        }
        #endregion

        #region Private Functions
        protected double PreLogLikelihood()
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
                        L -= (double)(0.5 * ILMath.multiply(ILMath.multiply(_M[ILMath.full, i].T, _invK), _M[ILMath.full, i]));

                    break;
                case ApproximationType.dtc:
                    ILArray<double> KufM = ILMath.multiply(_Kuf, _M);
                    ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);

                    L -= (double)(.5 * (_D * (-(_N - _numInducing) * ILMath.log(_beta) - _logDetK + _logDetA)
                        - (ILMath.sum(ILMath.sum(ILMath.multiplyElem(_invA, KufMKufM)))
                        - ILMath.sum(ILMath.sum(ILMath.multiplyElem(_M, _M)))) * _beta));
                    break;

                case ApproximationType.fitc:
                    L -= (double)(.5 * _N * _D * ILMath.log(2 * ILMath.pi));
                    ILArray<double> DinvM = ILMath.multiplyElem(ILMath.repmat(_invD, 1, _D), _M);
                    ILArray<double> KufDinvM = ILMath.multiply(_Kuf, DinvM);

                    L -= (double)(.5 * (_D * (ILMath.sum(ILMath.log(_diagD))
                        - (_N - _numInducing) * ILMath.log(_beta) + _detDiff) + (ILMath.sum(ILMath.sum(ILMath.multiplyElem(DinvM, _M)))
                        - ILMath.sum(ILMath.sum(ILMath.multiplyElem(ILMath.multiply(_invA, KufDinvM), KufDinvM)))) * _beta));

                    break;
            }

            // prior of kernel parameters
            L -= (double)(ILMath.sum(_kern.LogParameter));

            // prior of the weights
            L -= (double)(ILMath.sum(ILMath.log(_scale)));

            // prior of the latents (dynamics or gaussian)
            if (_prior != null)
                L += _prior.LogLikelihood();

            return L;
        }

        /// <summary>
        /// Computes the posterior log likelihood of the model for new observation points. 
        /// </summary>
        /// <returns>Posterior log likelihood of the model.</returns>
        protected double PostLogLikelihood()
        {
            double L = 0;
            // going through the hierarchy
            foreach (IGPLVM node in Nodes)
                L += node.LogLikelihood();

            if (_Ynew.IsEmpty || _YPostMean.IsEmpty)
                System.Console.WriteLine("Please compute posterior mean depending on new test inputs and set the new corrsponding observation point!");
            else
            {
                ILArray<double> yHat = _Ynew - _YPostMean;
                L += (double)(-.5 * ILMath.sum(ILMath.log(_Yvar) + ILMath.log(2 * ILMath.pi) + ILMath.divide(ILMath.multiply(yHat, yHat.T), _Yvar), 1));

                // prior of the latents (dynamics or gaussian)
                //if (_prior != null)
                //    L += _prior.PostLogLikelihood();
            }

            return L;
        }

        protected virtual ILRetArray<double> PreLogLikGradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> dgParam = ILMath.empty(); // dynamics kernel parameters
                ILArray<double> dL_dK = ILMath.empty(); // derivative of log likelihood w.r.t K
                ILArray<double> dL_dKuf = ILMath.empty(); // derivative of log likelihood w.r.t Kuf

                double gBeta = 0;

                ILArray<double> gParam = ILMath.empty(); // gradient of the kernel parameters
                ILArray<double> gX = ILMath.empty(); // gradient of X
                ILArray<double> gXu = ILMath.empty(); // gradient of Xu

                ILCell dL_dX_dL_dXuf = ILMath.cell();

                ILArray<double> g = ILMath.empty();

                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        dL_dK = -_D / 2 * _invK + .5 * ILMath.multiply(ILMath.multiply(ILMath.multiply(_invK, _M), _M.T), _invK);
                        gX = _kern.LogLikGradientX(_X, dL_dK);
                        gParam = _kern.LogLikGradientParam(dL_dK);
                        break;

                    case ApproximationType.dtc:
                        ILArray<double> KufM = ILMath.multiply(_Kuf, _M);
                        ILArray<double> KufMKufM = ILMath.multiply(KufM, KufM.T);
                        ILArray<double> invAKufMKufM = ILMath.multiply(_invA, KufMKufM);
                        ILArray<double> invAKufMKufMinvA = ILMath.multiply(invAKufMKufM, _invA);

                        dL_dK = .5 * (_D * (_invK - (1 / _beta) * _invA) - invAKufMKufMinvA);

                        ILArray<double> invAKuf = ILMath.multiply(_invA, _Kuf);

                        dL_dKuf = -_D * invAKuf - _beta * (ILMath.multiply(invAKufMKufM, invAKuf) - (ILMath.multiply(ILMath.multiply(_invA, KufM), _M.T)));

                        dL_dX_dL_dXuf = _kern.LogLikGradientX(_Xu, dL_dK, _X, dL_dKuf);
                        gXu = dL_dX_dL_dXuf.GetArray<double>(0);
                        gX = dL_dX_dL_dXuf.GetArray<double>(1);

                        gParam = _kern.LogLikGradientParam(dL_dKuf) + _kern.LogLikGradientParam(dL_dK);

                        gBeta = (double)(.5 * (_D * ((_N - _numInducing) / _beta + ILMath.sum(ILMath.sum(ILMath.multiplyElem(_invA, _K))) / (_beta * _beta))
                            + ILMath.sum(ILMath.sum(ILMath.multiplyElem(invAKufMKufMinvA, _K))) / _beta
                            + (ILMath.trace(invAKufMKufM) - ILMath.sum(ILMath.sum(ILMath.multiplyElem(M, M))))));

                        gBeta *= _beta; // because of the log
                        break;

                    case ApproximationType.fitc:
                        ILArray<double> KufDinvM = ILMath.multiply(ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1)), _M);
                        ILArray<double> AinvKufDinvM = ILMath.multiply(_invA, KufDinvM);
                        ILArray<double> diagKufAinvKufDinvMMT = ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(ILMath.multiply(_invA, KufDinvM), _M.T)), 0).T;
                        ILArray<double> AinvKufDinvMKufDinvMAinv = ILMath.multiply(AinvKufDinvM, AinvKufDinvM.T);
                        ILArray<double> diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf = ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(_D * _invA + _beta * AinvKufDinvMKufDinvMAinv, _Kuf)), 0).T;
                        ILArray<double> invKuuKuf = ILMath.multiply(_invK, _Kuf);
                        ILArray<double> invKuuKufDinv = ILMath.multiplyElem(invKuuKuf, ILMath.repmat(_invD.T, _numInducing, 1));
                        ILArray<double> diagMMT = ILMath.sum(ILMath.multiplyElem(_M, _M), 1);

                        ILArray<double> diagQ = -_D * _diagD + _beta * diagMMT + diagKufdAinvplusAinvKufDinvMKufDinvMAinvKuf - 2 * _beta * diagKufAinvKufDinvMMT;

                        dL_dK = .5 * (_D * (_invK - _invA / _beta) - AinvKufDinvMKufDinvMAinv
                            + _beta * ILMath.multiply(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, _numInducing, 1)), invKuuKufDinv.T));

                        dL_dKuf = -_beta * ILMath.multiplyElem(ILMath.multiplyElem(invKuuKufDinv, ILMath.repmat(diagQ.T, _numInducing, 1)), ILMath.repmat(_invD.T, _numInducing, 1))
                            - _D * ILMath.multiplyElem(ILMath.multiply(_invA, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1))
                            - _beta * ILMath.multiplyElem(ILMath.multiply(AinvKufDinvMKufDinvMAinv, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1))
                            + _beta * ILMath.multiplyElem(ILMath.multiply(ILMath.multiply(_invA, KufDinvM), _M.T), ILMath.repmat(_invD.T, _numInducing, 1));

                        ILArray<double> Kstar = ILMath.divide(.5 * diagQ * _beta, ILMath.multiplyElem(_diagD, _diagD));

                        dL_dX_dL_dXuf = _kern.LogLikGradientX(_Xu, dL_dK, _X, dL_dKuf);
                        gXu = dL_dX_dL_dXuf.GetArray<double>(0);
                        gX = dL_dX_dL_dXuf.GetArray<double>(1);

                        ILArray<double> diagGX = _kern.DiagGradX(_X);
                        for (int i = 0; i < _N; i++)
                            gX[i, ILMath.full] += diagGX[i, ILMath.full] * Kstar[i];

                        gParam = _kern.LogLikGradientParam(dL_dKuf) + _kern.LogLikGradientParam(dL_dK);
                        gParam += _kern.DiagGradParam(_X, Kstar);

                        gBeta = (double)-ILMath.sum(Kstar) / (_beta * _beta);

                        gBeta *= _beta; // because of the log
                        break;
                }

                if (_prior != null)
                {
                    if (_prior.Type == PriorType.PriorTypeDynamics)
                        dgParam = ((IDynamics)_prior).KernelGradient();
                    if (_prior.Type == PriorType.PriorTypeCompound)
                        foreach (IPrior prior in ((Compound)_prior).Priors)
                            if (prior.Type == PriorType.PriorTypeDynamics)
                                dgParam = ((IDynamics)prior).KernelGradient();

                    gX += _prior.LogLikGradient();
                }
                gX += _latentgX; // top down gradient

                if (_backConst != null)
                    gX = _backConst.BackConstraintsGradient(gX);

                // gradient of the weights
                ILArray<double> gSc = 1 / ILMath.multiplyElem(_scale, (_innerProducts - 1));
                gSc = ILMath.multiplyElem(_scale, gSc); // because of the log
                switch (Masking)
                {
                    case Mask.full:
                        g = gX[ILMath.full].T;
                        if (!gXu.IsEmpty)
                            g[ILMath.r(g.Length, g.Length + gXu[ILMath.full].Length - 1)] = gXu[ILMath.full].T;
                        g[ILMath.r(g.Length, g.Length + gSc.Length - 1)] = gSc;
                        g[ILMath.r(g.Length, g.Length + gParam.Length - 1)] = gParam;
                        if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                            g[ILMath.end + 1] = gBeta;
                        if (!dgParam.IsEmpty)
                            g[ILMath.r(g.Length, g.Length + dgParam.Length - 1)] = dgParam;
                        break;

                    case Mask.latents:
                        g = gX[ILMath.full].T;
                        if (!gXu.IsEmpty)
                            g[ILMath.r(g.Length, g.Length + gXu[ILMath.full].Length - 1)] = gXu[ILMath.full].T;
                        break;
                    case Mask.kernel:
                        g = gParam;
                        if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                            g[ILMath.end + 1] = gBeta;
                        if (!dgParam.IsEmpty)
                            g[ILMath.r(g.Length, g.Length + dgParam.Length - 1)] = dgParam;
                        break;
                }
                

                // going through the hierarchy; setting the top down gradients; collecting the gradients of the children
                int startVal = 0, endVal = 0;
                foreach (IGPLVM node in Nodes)
                {
                    startVal += endVal;
                    endVal += node.LatentDimension;

                    // computing the derivative of the log likelihood w.r.t the data
                    node.LatentGradient = ComputeLatentGradient(_M[ILMath.full, ILMath.r(startVal, endVal - 1)], _scale[ILMath.r(startVal, endVal - 1)]);

                    ILArray<double> tmp = node.LogLikGradient();
                    g[ILMath.r(g.Length, g.Length + tmp.Length - 1)] = tmp;
                }

                return g.C;
            }
        }

        protected virtual ILRetArray<double> PostLogLikGradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> df_dx;
                ILArray<double> dvar_dx;
                ILArray<double> dkx_dx;
                ILArray<double> ddiagkx_dx;
                //ILArray<double> kX;
                ILArray<double> invKdk_dx;

                ILArray<double> gX;
                ILArray<double> gY;

                ILArray<double> g;

                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        //kX = _kern.ComputeKernelMatrix(_Xnew, _X, Flag.postlearning);

                        dkx_dx = ILMath.zeros(_X.S);
                        for (int k = 0; k < _q; k++)
                            dkx_dx[ILMath.full, k] = _kern.GradX(_Xnew, _X, k, Flag.postlearning);

                        invKdk_dx = ILMath.multiply(_invK, dkx_dx);
                        break;

                    default:
                        //kX = _kern.ComputeKernelMatrix(_Xnew, _Xu, Flag.postlearning);

                        dkx_dx = ILMath.zeros(_X.S);
                        for (int k = 0; k < _q; k++)
                            dkx_dx[ILMath.full, k] = _kern.GradX(_Xnew, _Xu, k, Flag.postlearning);

                        invKdk_dx = ILMath.multiply(_invK - (1 / _beta) * _invA, dkx_dx);
                        break;
                }

                ddiagkx_dx = _kern.DiagGradX(_Xnew);

                df_dx = ILMath.multiply(dkx_dx.T, _alpha);
                dvar_dx = ILMath.repmat(ddiagkx_dx.T - 2 * ILMath.multiply(invKdk_dx.T, _k.T), 1, _D);

                df_dx = ILMath.multiplyElem(df_dx, ILMath.repmat(_scale, _q, 1));
                dvar_dx = ILMath.multiplyElem(dvar_dx, ILMath.repmat(ILMath.multiplyElem(_scale, _scale), _q, 1));

                ILArray<double> yHat = (_Ynew - _YPostMean) / _Yvar;
                //gX = -ILMath.multiply(yHat, df_dx.T) / _Yvar + ILMath.multiply(_D - ILMath.multiply(yHat, yHat.T) / _Yvar, dvar_dx.T) / (2 * _Yvar);
                gX = -ILMath.multiply(yHat, df_dx.T) + ILMath.multiply(0.5 * ILMath.multiplyElem(yHat, yHat) - 1 / _Yvar, dvar_dx.T);

                //if (_prior != null)
                //    gX += _prior.PostLogLikGradient();

                if (!_latentPointgX.IsEmpty)
                    gX += _latentPointgX;

                if (Mode == LerningMode.selfposterior && Nodes.Count == 0)
                {
                    gY = -yHat;
                    g = gY[ILMath.full].T;
                    gX = gX[ILMath.full].T;
                    //g[ILMath.r(ILMath.end + 1, ILMath.end + gX.Length)] = gX;
                }
                else
                    g = gX[ILMath.full].T;

                int startVal = 0, endVal = 0;
                foreach (IGPLVM node in Nodes)
                {
                    startVal += endVal;
                    endVal += node.LatentDimension;

                    // computing the derivative of the log likelihood w.r.t the data
                    node.PostLatentGradient = ComputeLatentPostGradient(yHat[ILMath.full, ILMath.r(startVal, endVal - 1)], _Yvar[ILMath.r(startVal, endVal - 1)]);

                    ILArray<double> tmp = node.LogLikGradient();
                    g[ILMath.r(g.Length, g.Length + tmp.Length - 1)] = tmp;
                }

                return g.C;
            }
        }

        /// <summary>
        /// Updates the kernel matrix after each optimization step. 
        /// </summary>
        protected void UpdateKernelMatrix()
        {
            using (ILScope.Enter())
            {
                _latentgX.a = ILMath.zeros(_N, _q);
                _innerProducts.a = ILMath.zeros(1, _D);

                if (_kern != null)
                {
                    switch (_approxType)
                    {
                        case ApproximationType.ftc:
                            _K.a = _kern.ComputeKernelMatrix(_X, _X);
                            _invK.a = Util.pdinverse(_K);
                            _logDetK = Util.logdet(_K);

                            _alpha.a = ILMath.multiply(_invK, _M);

                            for (int i = 0; i < _D; i++)
                                _innerProducts[i] = ILMath.multiply(ILMath.multiply(_M[ILMath.full, i].T, _invK), _M[ILMath.full, i]);
                            break;

                        case ApproximationType.dtc:
                            _K.a = _kern.ComputeKernelMatrix(_Xu, _Xu);
                            _Kuf.a = _kern.ComputeKernelMatrix(_Xu, _X);
                            _invK.a = Util.pdinverse(_K);
                            _logDetK = Util.logdet(_K);

                            _A.a = (1 / _beta) * _K + ILMath.multiply(_Kuf, _Kuf.T);
                            // This can become unstable when K_uf2 is low rank.
                            _invA.a = Util.pdinverse(_A);
                            _logDetA = Util.logdet(_A);

                            for (int i = 0; i < _D; i++)
                                _innerProducts[i] = _beta * (ILMath.multiply(_M[ILMath.full, i].T, _M[ILMath.full, i])
                                    - ILMath.multiply(ILMath.multiply(ILMath.multiply(_Kuf, _M[ILMath.full, i]).T, _invA), ILMath.multiply(_Kuf, _M[ILMath.full, i])));

                            _alpha.a = ILMath.multiply(ILMath.multiply(_invA, _Kuf), _M);
                            break;

                        case ApproximationType.fitc:
                            _K.a = _kern.ComputeKernelMatrix(_Xu, _Xu);
                            _Kuf.a = _kern.ComputeKernelMatrix(_Xu, _X);
                            _invK.a = Util.pdinverse(_K);
                            _logDetK = Util.logdet(_K);

                            double jitter = 1e-6;
                            _K.a = _K + ILMath.diag(ILMath.repmat(jitter, _K.S[0], 1));

                            _diagK.a = _kern.ComputeDiagonal(_X);

                            _diagD.a = 1 + _beta * _diagK - _beta * ILMath.sum(ILMath.multiplyElem(_Kuf, ILMath.multiply(_invK, _Kuf)), 0).T;
                            _diagD[_diagD == 0.0] = 1e-10;
                            _invD.a = 1 / _diagD; //ILMath.diag(1 / _diagD);
                            //ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiply(_Kuf, _invD), _Kuf.T);
                            ILArray<double> KufDinvKuf = ILMath.multiply(ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1)), _Kuf.T);
                            _A.a = (1 / _beta) * _K + KufDinvKuf;

                            // This can become unstable when K_ufDinvK_uf is low rank.
                            _invA.a = Util.pdinverse(_A);
                            _logDetA = Util.logdet(_A);

                            _detDiff.a = -ILMath.log(_beta) * _numInducing + ILMath.log(ILMath.det(ILMath.eye(_numInducing, _numInducing) + _beta * ILMath.multiply(KufDinvKuf, _invK)));

                            for (int i = 0; i < _D; i++)
                            {
                                ILArray<double> DinvM = ILMath.multiplyElem(_invD, _M[ILMath.full, i]);
                                ILArray<double> KufDinvM = ILMath.multiply(_Kuf, DinvM);
                                _innerProducts[i] = _beta * ILMath.multiply(DinvM.T, _M[ILMath.full, i]) - ILMath.multiply(ILMath.multiply(KufDinvM.T, _invA), KufDinvM);
                            }

                            _alpha.a = ILMath.multiply(ILMath.multiplyElem(ILMath.multiply(_invA, _Kuf), ILMath.repmat(_invD.T, _numInducing, 1)), _M);
                            break;
                    }
                }
                else
                    System.Console.WriteLine("No kernel function found! Please add a kernel object!");
            }
        }

        /// <summary>
        /// Computes the derivative of the log likelihood w.r.t the data. 
        /// </summary>
        /// <remarks>
        /// The computation is needed because of the top down influences of the latent variables from the next upper layer.
        /// </remarks>
        /// <param name="partialParentData">Collums and rows of the data which belongs to the corresponding latents of the child.</param>
        /// <param name="parentinvK">The inverse of the kernel matrix.</param>
        /// <param name="partialParentScale">Collums of the scale which belongs to the partialParentData.</param>
        /// <returns>The gradient of the data</returns>
        protected ILRetArray<double> ComputeLatentGradient(ILArray<double> partialParentData, ILArray<double> parentScale)
        {
            using (ILScope.Enter())
            {
                ILArray<double> latentgX = ILMath.zeros(_N, parentScale.Length);

                switch (_approxType)
                {
                    case ApproximationType.ftc:
                        for (int i = 0; i < parentScale.Length; i++)
                            latentgX[ILMath.full, i] = -1 / parentScale[i] * ILMath.multiply(_invK, partialParentData[ILMath.full, i]);

                        break;

                    case ApproximationType.dtc:
                        ILArray<double> AinvKuf = ILMath.multiply(Util.pdinverse((1 / _beta) * _K + ILMath.multiply(_Kuf, _Kuf.T)), _Kuf);

                        for (int i = 0; i < parentScale.Length; i++)
                            latentgX[ILMath.full, i] = -_beta / parentScale[i] * partialParentData[ILMath.full, i]
                                + _beta / parentScale[i] * ILMath.multiply(_Kuf.T, ILMath.multiply(AinvKuf, partialParentData[ILMath.full, i]));

                        break;

                    case ApproximationType.fitc:
                        ILArray<double> KufDinv = ILMath.multiplyElem(_Kuf, ILMath.repmat(_invD.T, _numInducing, 1));
                        ILArray<double> AinvKufDinv = ILMath.multiply(_invA, KufDinv);

                        for (int i = 0; i < parentScale.Length; i++)
                            latentgX[ILMath.full, i] = -_beta / parentScale[i] * ILMath.multiply(ILMath.diag(ILMath.divide(1, _diagD)), partialParentData[ILMath.full, i])
                                + _beta / parentScale[i] * ILMath.multiply(ILMath.multiply(KufDinv.T, AinvKufDinv), partialParentData[ILMath.full, i]);

                        break;
                }

                return latentgX;
            }
        }

        protected ILRetArray<double> ComputeLatentPostGradient(ILArray<double> parentYHat, ILArray<double> parentSigma)
        {
            using (ILScope.Enter())
            {
                return -(parentYHat) / parentSigma;
            }
        }

        /// <summary>
        /// Initialisation of X through different embeddings. 
        /// </summary>
        protected void EstimateX()
        {
            using (ILScope.Enter())
            {
                switch (_initX)
                {
                    case XInit.pca:
                        _X.a = Embed.PCA(_M, _q);
                        break;
                    case XInit.kernelPCA:
                        _X.a = Embed.KernelPCA(_M, _q);
                        break;
                    case XInit.lle:
                        _X.a = Embed.LLE(_M, 25, _q);
                        break;
                    case XInit.smallRand:
                        _X.a = Embed.SmallRand(_M, _q);
                        break;
                    case XInit.isomap:
                        _X.a = Embed.Isomap(_M, _q);
                        break;
                }
            }
        }

        #endregion
    }
}
