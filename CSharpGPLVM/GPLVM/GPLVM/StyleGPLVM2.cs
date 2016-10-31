using System;
using System.Collections.Generic;
using ILNumerics;
using GPLVM.Kernel;
using GPLVM.Styles;
using GPLVM.Backconstraint;
using GPLVM.Prior;
using GPLVM.Dynamics;
using GPLVM.Optimisation;
using GPLVM.Embeddings;
using System.Xml;
using GPLVM.Utils.Character;

namespace GPLVM.GPLVM
{
    public class StyleGPLVM2 : GP_LVM
    {
        private GPLVMType _type = GPLVMType.style;
        protected Skeleton _skeletonTree;
        protected Dictionary<string, IStyle> _styles;
        protected Representation _repType;

        private List<ILArray<double>> _indexes;

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
        public StyleGPLVM2(int latentDimension = 2, ApproximationType aType = ApproximationType.ftc, Representation type = Representation.exponential, BackConstType btype = BackConstType.none, XInit initX = XInit.pca)
            : base(latentDimension, aType, btype, initX)
        {
            _repType = type;
            _skeletonTree = null;
        }

        #region Setters and Getters
        public Skeleton SkeletonTree
        {
            get
            {
                return _skeletonTree;
            }
            set
            {
                _skeletonTree = value;
            }
        }

        public Dictionary<string, IStyle> Styles
        {
            get
            {
                return _styles;
            }
        }

        public override int LatentDimension
        {
            get
            {
                return _q;
            }
        }

        public override int NumParameter
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

                                if (_styles != null)
                                    foreach (KeyValuePair<string, IStyle> style in _styles)
                                        numParam += style.Value.NumParameter;

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

                                if (_styles != null)
                                    foreach (KeyValuePair<string, IStyle> style in _styles)
                                        numParam += style.Value.NumParameter;

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
                            numParam = _Xnew.S[0] * _q + PostMean.S[0] * _D;
                        else
                            numParam = _Xnew.S[0] * _q;

                        break;
                }

                return numParam;
            }
        }

        /// <summary>
        /// The log of the parameter in the model wants to be optimized. 
        /// </summary>
        public override ILArray<double> LogParameter
        {
            get
            {
                ILArray<double> param = ILMath.empty();
                switch (Mode)
                {
                    case LerningMode.prior:
                        switch (Masking)
                        {
                            case Mask.full:
                                ILArray<double> tmpX;
                                if (_backConst != null)
                                    param = _backConst.LogParameter;
                                else
                                {
                                    tmpX = _X[ILMath.full, ILMath.r(0, _q - 1)];

                                    param = tmpX[ILMath.full].T.C;
                                }

                                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                {
                                    tmpX = _Xu[ILMath.full, ILMath.r(0, _q - 1)];

                                    param[ILMath.r(param.Length, param.Length + tmpX[ILMath.full].Length - 1)] = tmpX[ILMath.full].T.C;
                                }

                                foreach (KeyValuePair<string, IStyle> style in _styles)
                                    param[ILMath.r(param.Length, param.Length + style.Value.NumParameter - 1)] = style.Value.LogParameter.C;

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
                                {
                                    tmpX = _Xu[ILMath.full, ILMath.r(0, _q - 1)];

                                    param[ILMath.r(param.Length, param.Length + tmpX[ILMath.full].Length - 1)] = tmpX[ILMath.full].T.C;
                                }

                                foreach (KeyValuePair<string, IStyle> style in _styles)
                                    param[ILMath.r(param.Length, param.Length + style.Value.NumParameter - 1)] = style.Value.LogParameter.C;

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
                            param = _YPostMean[ILMath.full].T.C;
                            param[ILMath.r(ILMath.end + 1, ILMath.end + _Xnew.S[1])] = _Xnew[ILMath.full].T.C;
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
                                        _X = _backConst.GetBackConstraints();
                                    }
                                    else
                                    {
                                        endVal = _q * _N - 1;
                                        _X = ILMath.reshape(param[ILMath.r(startVal, endVal)], _N, _q);
                                    }

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    {
                                        startVal = endVal + 1;
                                        endVal += NumInducing * _q;
                                        _Xu = ILMath.reshape(param[ILMath.r(startVal, endVal)], NumInducing, _q);
                                    }

                                    foreach (KeyValuePair<string, IStyle> style in _styles)
                                    {
                                        startVal = endVal + 1;
                                        endVal += style.Value.LogParameter.Length;
                                        style.Value.LogParameter = param[ILMath.r(startVal, endVal)];
                                    }

                                    startVal = endVal + 1;
                                    endVal += _scale.Length;
                                    _scale = Util.atox(param[ILMath.r(startVal, endVal)]);

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
                                        _X = _backConst.GetBackConstraints();
                                    }
                                    else
                                    {
                                        endVal = _q * _N - 1;
                                        _X = ILMath.reshape(param[ILMath.r(startVal, endVal)], _N, _q);
                                    }

                                    if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                                    {
                                        startVal = endVal + 1;
                                        endVal += NumInducing * _q;
                                        _Xu = ILMath.reshape(param[ILMath.r(startVal, endVal)], NumInducing, _q);
                                    }

                                    foreach (KeyValuePair<string, IStyle> style in _styles)
                                    {
                                        startVal = endVal + 1;
                                        endVal += style.Value.LogParameter.Length;
                                        style.Value.LogParameter = param[ILMath.r(startVal, endVal)];
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
                                endVal = _D * _YPostMean.S[0];
                                _Ynew.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _YPostMean.S[0], _D);
                            }

                            startVal = endVal + 1;
                            endVal += _q * _Xnew.S[0];

                            _Xnew.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _Xnew.S[0], _q);

                            if (_prior != null)
                                _prior.Xnew = _Xnew.C;
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

        public Representation RepresentationType
        {
            get
            {
                return _repType;
            }
        }
        #endregion

        #region Public Functions
        public override void Initialize()
        {
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
                        _Y[ILMath.full, ILMath.r(startVal, startVal + node.LatentDimension - 1)] = node.UpdateParameter().C;

                    startVal += node.LatentDimension;
                }

                _N = _Y.Size[0];
                _D = _Y.Size[1];

                _scale.a = ILMath.ones(1, _D);
                _bias.a = ILMath.mean(_Y);

                _M.a = ILMath.zeros(_Y.Size);
                for (int i = 0; i < _bias.Length; i++)
                    _M[ILMath.full, i] = ((_Y[ILMath.full, i] - _bias[i]) / _scale[i]).C;

                EstimateX();

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

                    case BackConstType.halfPtc:
                        _backConst = new BackConstraintPTChalf();
                        _backConst.Initialize(_Y, _X, _segments, _initX);
                        _X.a = _backConst.GetBackConstraints();
                        break;
                    case BackConstType.ptcLinear:
                        _backConst = new BackConstraintPTCLinear();
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

                //setting indexes for kernel computation
                _indexes = new List<ILArray<double>>();
                _indexes.Add(ILMath.localMember<double>());
                _indexes[_indexes.Count - 1].a = (ILMath.counter(_q) - 1);

                int cnt = _q - 1;
                int cntComp = 0;

                List<ILArray<double>> tmpIndex = new List<ILArray<double>>();
                tmpIndex.Add(ILMath.counter(_q) - 1);

                foreach (KeyValuePair<string, IStyle> style in _styles)
                {
                    _indexes.Add(ILMath.localMember<double>());
                    _indexes[_indexes.Count - 1].a = (ILMath.counter(style.Value.SubStyles.Count) + cnt);

                    tmpIndex.Add(ILMath.counter(style.Value.SubStyles.Count) + cnt);
                    cnt += style.Value.SubStyles.Count;
                    cntComp += style.Value.SubStyles.Count;

                    _X[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + style.Value.SubStyles.Count)] = style.Value.StyleVariable[style.Value.FactorIndex, ILMath.full];
                }

                CompoundKern circle = new CompoundKern();
                circle.AddKern(new RBFKern());
                circle.AddKern(new LinearKern());

                TensorKern mult = new TensorKern();
                mult.AddKern(circle);

                for (int i = 0; i < _styles.Count; i++)
                    mult.AddKern(new StyleKern());

                for (int i = 0; i < tmpIndex.Count; i++)
                    mult.AddIndex(tmpIndex[i]);

                CompoundKern all = new CompoundKern();
                all.AddKern(mult);
                all.AddKern(new WhiteKern());

                all.AddIndex(ILMath.counter(_q + cntComp) - 1);
                all.AddIndex(ILMath.counter(_q) - 1);

                _kern = all;

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
                        foreach (KeyValuePair<string, IStyle> style in _styles)
                            style.Value.FactorIndexInducing = style.Value.FactorIndex[ind];

                        break;
                    case ApproximationType.fitc:
                        ILMath.sort(ILMath.rand(1, _N), ind);
                        ind = ind[ILMath.r(0, _numInducing - 1)];
                        _Xu.a = _X[ind, ILMath.full].C;
                        foreach (KeyValuePair<string, IStyle> style in _styles)
                            style.Value.FactorIndexInducing = style.Value.FactorIndex[ind];

                        break;
                }

                UpdateKernelMatrix();

                if (_prior != null)
                {
                    if (_prior.Type == PriorType.PriorTypeCompound)
                        foreach (IPrior prior in ((Compound)_prior).Priors)
                            if (prior.Type == PriorType.PriorTypeLocallyLinear)
                                ((LocallyLinear)prior).M = _M;

                    if (_prior.Type == PriorType.PriorTypeLocallyLinear)
                        ((LocallyLinear)_prior).M = _M;

                    _prior.Initialize(_X[ILMath.full, ILMath.r(0, _q - 1)], _segments);
                }
        }

        /// <summary>
        /// Adds a style object. 
        /// </summary>
        public void AddStyle(string styleName, IStyle style)
        {
            if (_styles == null)
            {
                _styles = new Dictionary<string, IStyle>();
                _styles.Add(styleName, style);
            }
            else
            {
                if (!_styles.ContainsKey(styleName))
                    _styles.Add(styleName, style);
                else
                    System.Console.WriteLine("Style already exist!");
            }
        }

        /// <summary>
        /// Computes the gradients of the latents and kernel parameters of the model. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to get the
        /// gradients of the latents and kernel parameters of the model.
        /// </remarks>
        /// <returns>Gradients of the latents and kernel parameters of the model.</returns>
        protected override ILRetArray<double> PreLogLikGradient()
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

                    gX[ILMath.full, ILMath.r(0, _q - 1)] += _prior.LogLikGradient();
                }
                gX[ILMath.full, _indexes[0]] += _latentgX; // top down gradient
                //gX[ILMath.full, ILMath.r(0, _q - 1)] += _latentgX; // top down gradient

                // gradient of the weights
                ILArray<double> gSc = 1 / ILMath.multiplyElem(_scale, (_innerProducts - 1));
                gSc = ILMath.multiplyElem(_scale, gSc); // because of the log

                int cnt = 1;

                ILArray<double> gStyle = ILMath.empty();
                ILArray<double> guStyle = ILMath.empty();
                bool isFirst = true;
                foreach (KeyValuePair<string, IStyle> style in _styles)
                {
                    if (isFirst)
                    {
                        gStyle = style.Value.StyleGradient(gX[ILMath.full, _indexes[cnt]]);
                        if (!gXu.IsEmpty)
                            guStyle = style.Value.StyleInducingGradient(gXu[ILMath.full, _indexes[cnt]]);

                        isFirst = false;
                        cnt++;
                    }
                    else
                    {
                        gStyle[ILMath.r(ILMath.end + 1, ILMath.end + style.Value.SubStyles.Count * style.Value.SubStyles.Count)] = style.Value.StyleGradient(gX[ILMath.full, _indexes[cnt]]);
                        if (!gXu.IsEmpty)
                            guStyle[ILMath.r(ILMath.end + 1, ILMath.end + style.Value.SubStyles.Count * style.Value.SubStyles.Count)] = style.Value.StyleInducingGradient(gXu[ILMath.full, _indexes[cnt]]);

                        cnt++;
                    }
                }

                switch (Masking)
                {
                    case Mask.full:
                        gX = gX[ILMath.full, ILMath.r(0, _q - 1)];
                        if (_backConst != null)
                            gX = _backConst.BackConstraintsGradient(gX);

                        g = gX[ILMath.full].T;

                        if (!gXu.IsEmpty)
                        {
                            gXu = gXu[ILMath.full, ILMath.r(0, _q - 1)];
                            g[ILMath.r(g.Length, g.Length + gXu[ILMath.full].Length - 1)] = gXu[ILMath.full].T;

                        }

                        g[ILMath.r(g.Length, g.Length + gStyle.Length - 1)] = gStyle;
                        if (!guStyle.IsEmpty)
                            g[ILMath.r(g.Length, g.Length + guStyle.Length - 1)] = guStyle;

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
                        {
                            gXu = gXu[ILMath.full, ILMath.r(0, _q - 1)];
                            g[ILMath.r(g.Length, g.Length + gXu[ILMath.full].Length - 1)] = gXu[ILMath.full].T;

                        }

                        g[ILMath.r(g.Length, g.Length + gStyle.Length - 1)] = gStyle;
                        if (!guStyle.IsEmpty)
                            g[ILMath.r(g.Length, g.Length + guStyle.Length - 1)] = guStyle;
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

        /// <summary>
        /// Updates the parameters after each optimization step. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to update the parameters.
        /// </remarks>
        /// <returns>The new latent points of the object.</returns>
        public override ILRetArray<double> UpdateParameter()
        {
            using (ILScope.Enter())
            {
                int startVal = 0;
                foreach (IGPLVM node in Nodes)
                {
                    _Y[ILMath.full, ILMath.r(startVal, startVal + node.LatentDimension - 1)] = node.UpdateParameter().C;
                    startVal += node.LatentDimension;
                }

                _X.a = _X[ILMath.full, ILMath.r(0, _q - 1)];
                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                    _Xu.a = _Xu[ILMath.full, ILMath.r(0, _q - 1)];

                _bias.a = ILMath.mean(_Y);
                _M.a = ILMath.zeros(_Y.S);
                for (int i = 0; i < _bias.Length; i++)
                    _M[ILMath.full, i] = (_Y[ILMath.full, i] - _bias[i]) / _scale[i].C;

                ILArray<double> retX = _X.C;

                foreach (KeyValuePair<string, IStyle> style in _styles)
                {
                    _X[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + style.Value.SubStyles.Count)] = style.Value.StyleVariable[style.Value.FactorIndex, ILMath.full];
                    if (!_Xu.IsEmpty)
                        _Xu[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + style.Value.SubStyles.Count)] = style.Value.StyleInducingVariable[style.Value.FactorIndexInducing, ILMath.full];
                }

                UpdateKernelMatrix();

                return retX;
            }
            //return X[ILMath.full, ILMath.r(0, _q - 1)];
        }

        public override void Read(ref XmlReader reader)
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

                            _indexes[_indexes.Count - 1].a = ILMath.zeros(1, tokens.Length);
                            for (int i = 0; i < tokens.Length; i++)
                                _indexes[_indexes.Count - 1][i] = Double.Parse(tokens[i]);

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
                        case "ptcLinear":
                            _backConst = new BackConstraintPTCLinear();
                            _backConst.Read(ref reader);
                            break;
                    }
                    reader.Read();
                }

                if (reader.Name == "Styles")
                {
                    string name;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "Style")
                        {
                            reader.MoveToAttribute("name");
                            name = (string)reader.ReadContentAs(typeof(string), null);
                            AddStyle(name, new Style(name));
                            Styles[name].Read(ref reader);
                        }
                    }
                    reader.Read();
                }

                if (reader.Name == "Skeleton")
                {
                    _skeletonTree = new Skeleton();
                    _skeletonTree.Read(ref reader);
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
                    }
                    reader.Read();
                }
            }
            reader.Read();
            _latentgX = ILMath.zeros(_X.S);
            if(!isChild)
                UpdateParameter();
        }

        public override void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("StyleGPLVM2", null);
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
                ILArray<double> tmp = _X[ILMath.full, ILMath.r(0, _q - 1)];
                writer.WriteAttributeString("data", tmp.ToString().Remove(0, tmp.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                writer.WriteEndElement();

                if (_approxType == ApproximationType.dtc || _approxType == ApproximationType.fitc)
                {
                    writer.WriteStartElement("Xu");
                    tmp = _Xu[ILMath.full, ILMath.r(0, _q - 1)];
                    writer.WriteAttributeString("data", tmp.ToString().Remove(0, tmp.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
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

            writer.WriteStartElement("Kernel", null);
            writer.WriteAttributeString("type", _kern.Type.ToString());
            _kern.Write(ref writer);
            writer.WriteEndElement();

            if (_prior != null)
                _prior.Write(ref writer);

            if (_backConst != null)
                _backConst.Write(ref writer);

            if (_styles != null)
            {
                writer.WriteStartElement("Styles", null);
                foreach (KeyValuePair<string, IStyle> style in _styles)
                    style.Value.Write(ref writer);
                writer.WriteEndElement();
            }

            if (_skeletonTree != null)
            {
                _skeletonTree.Write(ref writer);
            }

            if (Nodes.Count != 0)
            {
                writer.WriteStartElement("Children", null);
                foreach (IGPLVM node in Nodes)
                    node.Write(ref writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
        #endregion
    }
}
