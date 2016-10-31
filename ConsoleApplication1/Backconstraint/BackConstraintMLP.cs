using GPLVM.Embeddings;
using ILNumerics;
using System.Xml;

namespace GPLVM.Backconstraint
{
    // Possible types of activation functions.
    public enum MLPFunction
    {
        Linear,
        Logistic,
        Softmax
    };

    public class BackConstraintMLP : IBackconstraint
    {
        private BackConstType _type;
        private XInit _initX;

        private int _N;
        private int _q;
        private int _D;
        private int _h;

        private int _numParam;

        private int _hiddenUnits;

        private ILArray<double> _Y = ILMath.localMember<double>();
        private ILArray<double> _X = ILMath.localMember<double>();
        private ILArray<double> _segments = ILMath.localMember<double>();

        private MLPFunction _activationFunction; // Activation function.

        private ILArray<double> _W1 = ILMath.localMember<double>();       // First layer weights.
        private ILArray<double> _b1 = ILMath.localMember<double>();        // First layer biases.
        private ILArray<double> _W2 = ILMath.localMember<double>();        // Second layer weights.
        private ILArray<double> _b2 = ILMath.localMember<double>();        // Second layer biases.

        private ILArray<double> _A = ILMath.localMember<double>();         // Temporary storage for last evaluation summed inputs.
        private ILArray<double> _Z = ILMath.localMember<double>();         // Temporary storage for last evaluation hidden unit activations.
        private ILArray<double> _Xtarget = ILMath.localMember<double>();   // Target X positions during initialization.
        private ILArray<double> _Xerror = ILMath.localMember<double>();    // Temporary storage for error gradient during initialization.
        // Optimization algorithm to use for initialization.
        //GPCMOptAlgorithm* algorithm;

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// A multilayer perceptron (MLP) is a feedforward artificial neural network model that maps sets 
        /// of input data onto a set of appropriate output. An MLP consists of multiple layers of nodes 
        /// in a directed graph, with each layer fully connected to the next one. Except for the input nodes, 
        /// each node is a neuron (or processing element) with a nonlinear activation function. 
        /// MLP utilizes a supervised learning technique called backpropagation for training the network. 
        /// MLP is a modification of the standard linear perceptron and can distinguish data that is not 
        /// linearly separable.
        /// </remarks>     
        public BackConstraintMLP()
        {
            _type = BackConstType.mlp;

            _hiddenUnits = 20;
            _activationFunction = MLPFunction.Linear;
        }

        #region Setters and Getters
        public BackConstType Type
        {
            get
            {
                return _type;
            }
        }

        public MLPFunction ActivationFunction
        {
            get
            {
                return _activationFunction;
            }
            set
            {
                _activationFunction = value;
            }
        }

        public int HiddenUnits
        {
            get
            {
                return _hiddenUnits;
            }
            set
            {
                _hiddenUnits = value;
            }
        }

        /// <summary>
        /// Gets the number of parameters of the objects wants to be optimized. 
        /// </summary>
        public int NumParameter
        {
            get
            {
                return _numParam;
            }
        }

        /// <summary>
        /// The log of the parameter in the model wants to be optimized. 
        /// </summary>
        public ILArray<double> LogParameter
        {
            get
            {
                ILArray<double> param = _W1[ILMath.full].T.C;
                param[ILMath.r(ILMath.end + 1, ILMath.end + _b1.Length)] = _b1.C;
                param[ILMath.r(ILMath.end + 1, ILMath.end + _W2[ILMath.full].Length)] = _W2[ILMath.full].T.C;
                param[ILMath.r(ILMath.end + 1, ILMath.end + _b2.Length)] = _b2.C;

                return param.C;
            }
            set
            {
                ILArray<double> param = ILMath.empty();
                param.a = value;

                int startVal = 0;
                int endVal = _W1.Size[0] * _W1.Size[1] - 1;
                _W1.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _W1.Size[0], _W1.Size[1]);

                startVal = endVal + 1;
                endVal += _b1.Length;
                _b1.a = param[ILMath.r(startVal, endVal)].T;

                startVal = endVal + 1;
                endVal += _W2.Size[0] * _W2.Size[1];
                _W2.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _W2.Size[0], _W2.Size[1]);

                startVal = endVal + 1;
                endVal += _b2.Length;
                _b2.a = param[ILMath.r(startVal, endVal)].T;

                UpdateParameter();
            }
        }
        #endregion

        #region Public Functions
        public void Initialize(ILInArray<double> inY, ILInArray<double> inX, ILInArray<double> inSegments, XInit initX = XInit.pca)
        {
            _initX = initX;

            using (ILScope.Enter(inY, inX, inSegments))
            {
                _X.a = ILMath.check(inX);
                _Y.a = ILMath.check(inY);
                _segments.a = ILMath.check(inSegments);

                if (_segments.Length > 1)
                    _X.a = NormalizeSegments();
            }

            int inputUnits = _Y.Size[1];
            int outputUnits = _X.Size[1];

            _Xtarget.a = _X.C;

            // Initialize matrices.
            _W1.a = ILMath.randn(inputUnits, _hiddenUnits) / ILMath.sqrt((double)(inputUnits + 1));
            //_W1 -= 0.5;

            _b1.a = ILMath.randn(1, _hiddenUnits) / ILMath.sqrt((double)(inputUnits + 1));
            //_b1 -= 0.5;

            _W2.a = ILMath.randn(_hiddenUnits, outputUnits) / ILMath.sqrt((double)(_hiddenUnits + 1));
            //_W2 -= 0.5;

            _b2.a = ILMath.randn(1, outputUnits) / ILMath.sqrt((double)(_hiddenUnits + 1));
            //_b2 -= 0.5;

            _N = _X.Size[0];
            _q = _X.Size[1];
            _D = _Y.Size[1];
            _h = _W1.Size[1];

            _numParam = _W1.Size[0] * _W1.Size[1];
            _numParam += _W2.Size[0] * _W2.Size[1];
            _numParam += _b1.Length;
            _numParam += _b2.Length;

            UpdateParameter();
        }

        public ILRetArray<double> BackConstraintsGradient(ILInArray<double> ingX)
        {
            using (ILScope.Enter(ingX))
            {
                ILArray<double> gX = ILMath.check(ingX);

                // Layer two weights are just linear.
                ILArray<double> gW2 = ILMath.multiply(_Z.T, gX);
                ILArray<double> gb2 = ILMath.multiply(ILMath.ones(1, _N), gX);

                // Layer one weights are a bit more complicated.
                ILArray<double> gW1 = ILMath.multiply(_Y.T, ILMath.multiplyElem(ILMath.multiply(gX, _W2.T), 
                    ILMath.pow(-_Z, 2) + 1.0));
                ILArray<double> gb1 = ILMath.multiply(ILMath.ones(1, _N), ILMath.multiplyElem(ILMath.multiply(gX, _W2.T), 
                    ILMath.pow(-_Z, 2) + 1.0));

                ILArray<double> g = gW1[ILMath.full].T.C;
                g[ILMath.r(ILMath.end + 1, ILMath.end + gb1.Length)] = gb1.C;
                g[ILMath.r(ILMath.end + 1, ILMath.end + gW2[ILMath.full].Length)] = gW2[ILMath.full].T.C;
                g[ILMath.r(ILMath.end + 1, ILMath.end + gb2.Length)] = gb2.C;

                return g;
            }
        }

        public double MLPError()
        {
            double value = 0;
            switch (_activationFunction)
            {
                case MLPFunction.Linear:
                    _Xerror.a = _X - _Xtarget;
                    value = 0.5 * (double)(ILMath.sum(ILMath.sum(ILMath.pow(_Xerror,2))));
                    break;
                case MLPFunction.Logistic:
                    value = -(double)(ILMath.sum(ILMath.sum(ILMath.multiplyElem(_Xtarget, ILMath.log(_X)) + 
                        ILMath.multiplyElem((1 - _Xtarget), ILMath.log(1 - _X)))));
                    break;
                case MLPFunction.Softmax:
                    value = -(double)ILMath.sum(ILMath.sum(ILMath.multiplyElem(_Xtarget, ILMath.log(_X))));
                    break;
            }
            return value;
        }

        public ILArray<double> BackPropagateGradient()
        {
            _Xerror.a = _X - _Xtarget;

            // Evaluate second-layer gradients.
            ILArray<double> gW2 = ILMath.multiply(_Z.T, _Xerror);
            ILArray<double> gb2 = ILMath.sum(_Xerror, 0);

            // Now do the backpropagation.
            ILArray<double> delhid = ILMath.multiply(_Xerror, _W2.T);
            delhid = ILMath.multiplyElem(delhid, 1.0 - ILMath.multiplyElem(_Z, _Z));

            // Finally, evaluate the first-layer gradients.
            ILArray<double> gW1 = ILMath.multiply(_Y.T, delhid);
            ILArray<double> gb1 = ILMath.sum(delhid, 0);

            ILArray<double> g = gW1[ILMath.full].T.C;
            g[ILMath.r(ILMath.end + 1, ILMath.end + gb1.Length)] = gb1.C;
            g[ILMath.r(ILMath.end + 1, ILMath.end + gW2[ILMath.full].Length)] = gW2[ILMath.full].T.C;
            g[ILMath.r(ILMath.end + 1, ILMath.end + gb2.Length)] = gb2.C;
            
            return g;
        }

        public ILArray<double> GetBackConstraints()
        { 
            return _X;
        }

        // Read and Write Data to a XML file
        public void Read(ref XmlReader reader)
        {

        }

        public void Write(ref XmlWriter writer)
        {

        }
        #endregion

        #region Private Functions
        private void UpdateParameter()
        {
            // Compute hidden unit activations.
            _Z.a = ILMath.tanh(ILMath.multiply(_Y, _W1) + ILMath.multiply(ILMath.ones(_N, 1), _b1));

            // Compute summed inputs into output units.
            _A.a = ILMath.multiply(_Z, _W2) + ILMath.multiply(ILMath.ones(_N, 1), _b2);

            // Apply transformation.
            switch (_activationFunction)
            {
                case MLPFunction.Linear:
                    _X.a = _A.C;
                    break;
                case MLPFunction.Logistic:
                    _X.a = ILMath.divide(1, 1 + ILMath.exp(-_A));
                    //_X = ILMath.linsolve(ILMath.exp(-_A) + 1.0, ILMath.eye(_A.Size[0], _A.Size[1]));
                    break;
                case MLPFunction.Softmax:
                    ILArray<double> temp = ILMath.exp(_A);
                    _X.a = ILMath.divide(temp, ILMath.multiply(ILMath.sum(temp, 2), ILMath.ones(1, _A.Size[1])));
                    break;
            }
        }

        private ILArray<double> NormalizeSegments()
        {
            ILArray<double> curY = ILMath.empty();
            ILArray<double> X = ILMath.zeros(_X.Size);

            for (int i = 0; i < _segments.Length - 1; i++)
            {
                curY = _Y[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full];
                curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

                X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full] = EstimateX(curY);
                _Y[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full] = curY;
            }

            curY = _Y[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full];
            curY = curY - ILMath.repmat(ILMath.mean(curY), curY.Size[0], 1); // substracting the centroid

            X[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full] = EstimateX(curY);
            _Y[ILMath.r(_segments[ILMath.end], _Y.Size[0] - 1), ILMath.full] = curY;

            return X;
        }

        /// <summary>
        /// Estimates latent points through PCA. 
        /// </summary>
        /// <param name="data">The data Y.</param>
        private ILRetArray<double> EstimateX(ILInArray<double> inCurY)
        {
            using (ILScope.Enter(inCurY))
            {
                ILArray<double> curY = ILMath.check(inCurY);
                ILRetArray<double> X = ILMath.empty();

                switch (_initX)
                {
                    case XInit.pca:
                        X = Embed.PCA(curY, _X.Size[1]);
                        break;
                    case XInit.kernelPCA:
                        X = Embed.KernelPCA(curY, _X.Size[1]);
                        break;
                    case XInit.lle:
                        X = Embed.LLE(curY, 15, _X.Size[1]);
                        break;
                    case XInit.smallRand:
                        X = Embed.SmallRand(curY, _X.Size[1]);
                        break;
                    case XInit.isomap:
                        X = Embed.Isomap(curY, _X.Size[1]);
                        break;
                }

                return X;
            }
        }
        #endregion
    }
}
