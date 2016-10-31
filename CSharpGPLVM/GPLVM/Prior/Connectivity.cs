using System;
using System.Xml;
using ILNumerics;

namespace GPLVM.Prior
{
    public class Connectivity : IPrior
    {
        private PriorType _type;
        private ILArray<double> _X;
        private ILArray<double> _Xnew;
        private double _priorPrec = 1.0f;

        private double genPower = 1.0f;
        private double neighborPower;
        private double neighborFactor = 1.0f;
        private double weight = 1.0;
        private ILArray<double> temporalNeighborFactor;
        private ILArray<double> power;

        private ILArray<double> _K;
        private ILArray<double> _invK;

        private ILArray<double> n2;
        private ILArray<double> costs;
        private ILArray<double> H;
        private ILArray<double> Lambda;
        private ILArray<double> betaLambdaExp;
        private ILArray<double> D;
        private ILArray<double> invD;
        private ILArray<double> invSqrtD;
        private ILArray<double> U;

        private ILArray<double> _segments;

        private Guid _key = Guid.NewGuid();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// Models based on the GPLVM/GPDM tend to embed different motion
        /// trajectories far apart (even if similar poses exist), leading to
        /// poor pose reconstruction across trajectories [Wang et al. 2007].
        /// This is because the GPLVM places dissimilar poses far apart, but
        /// makes no effort to place similar poses close together [Lawrence and
        /// Quinonero Candela 2006]. This is a serious problem for control, because
        /// the agility of the controller depends on its ability to rapidly
        /// reach a variety of poses. If two portions of the example data are too
        /// far apart, one or the other will be not be used by the controller.
        /// This prior on the latent points X that encodes our
        /// preference for well-connected embeddings, see [Levine et al. 2012], 
        /// and does not depend on the training poses. Specifically, we model 
        /// the degree of connectivity in X using graph diffusion kernels, 
        /// which are closely related to random walk processes on graphs 
        /// [Kondor and Vert 2004].
        /// </remarks>
        public Connectivity()
        {
            _type = PriorType.PriorTypeConnectivity;
            _X = ILMath.empty();
            _segments = ILMath.empty();

            _K = ILMath.empty();
            _invK = ILMath.empty();

            n2 = ILMath.empty();
            costs = ILMath.empty();
            H = ILMath.empty();
            Lambda = ILMath.empty();
            betaLambdaExp = ILMath.empty();
            D = ILMath.empty();
            invD = ILMath.empty();
            invSqrtD = ILMath.empty();
            U = ILMath.empty<double>();

            neighborPower = genPower;
        }

        #region Setters and Getters

        public PriorType Type
        {
            get
            {
                return _type;
            }
        }

        public int NumParameter
        {
            get
            {
                return 0;
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                return ILMath.empty();
            }
            set
            {

            }
        }

        public ILArray<double> X
        {
            get { return _X; }
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

        #region Public Computations

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
            }

            temporalNeighborFactor = ILMath.ones(_X.Size[0], _X.Size[0]);
            power = ILMath.ones(_X.Size[0], _X.Size[0]) * genPower;

            int start = 0, end;
            for (int i = 0; i < _segments.Length; i++)
            {
                if (i < _segments.Length - 1)
                    end = (int)_segments[i + 1] - 1;
                else
                    end = _X.Size[0] - 1;

                // Set weight on all temporal neighbors.
                for (int t = start + 1; t < end; t++)
                {
                    temporalNeighborFactor[t, t - 1] = neighborFactor;
                    temporalNeighborFactor[t - 1, t] = neighborFactor;
                    power[t, t - 1] = neighborPower;
                    power[t - 1, t] = neighborPower;
                }

                // Set new start.
                start = end;
            }

            weight *= 250000.0 / (double)ILMath.pow((double)_X.Size[0], 2);
            _priorPrec *= ((double)_X.Size[0]) / 400.0;

            UpdateParameter();
        }

        public double LogLikelihood()
        {
            return (double)(weight * ILMath.sum(ILMath.sum(ILMath.log(_K))));
        }

        public double PostLogLikelihood()
        {
            // Todo
            double L = 0;

            return L;
        }

        /// <summary>
        /// Computes the gradient of the log likelihood w.r.t X.
        /// </summary>
        /// <returns>
        /// The gradient of X.
        /// </returns>
        public ILRetArray<double> LogLikGradient()
        {
            // Compute derivative with respect to K.
            int N = _X.Size[0];
            ILArray<double> gX = ILMath.zeros(_X.Size);

            ILArray<double> dL_dK = weight * _invK;

            // Compute difference of eigenvalue exponentials, difference of eigenvalues denominator, and divide to get Q.
            ILArray<double> Q = ILMath.repmat(ILMath.diag(betaLambdaExp), 1, N) 
                - ILMath.repmat(ILMath.diag(betaLambdaExp).T, N, 1);
            ILArray<double> temp = ILMath.repmat(ILMath.diag(Lambda), 1, N) 
                - ILMath.repmat(ILMath.diag(Lambda).T, N, 1) + ILMath.eye(N, N);
            if (ILMath.any(ILMath.any(temp <= 0f)))
                    temp[temp <= 0f] = 1.0e-128; // This ensures that the absolute value of each denominator entry is not too low.
            Q = ILMath.divide(Q, temp);
            Q += _priorPrec * betaLambdaExp; // This places the correct values on the diagonal.
        
            // Compute intermediate matrix M.
            ILArray<double> M = U * (ILMath.multiplyElem((U.T * dL_dK * U), Q)) * U.T;

            // Now compute factor in front of dD/dX and dC/dX.
            ILArray<double> MC = ILMath.diag(invSqrtD) * M * ILMath.diag(invSqrtD);
            ILArray<double> MD = -MC.C;
            MD -= 0.5 * (ILMath.diag(invD) * (H * M));
            MD -= 0.5 * (M * H) * ILMath.diag(invD);

            // Compute gradient of costs (without the difference of Xs in front).
			ILArray<double> dC_dX = n2 + ILMath.eye(N, N);
			for (int i = 0; i < costs.Size[0]; i++)
			{
				for (int j = 0; j < costs.Size[1]; j++)
				{
					dC_dX[i,j] = (2.0 * power[i,j]) / ILMath.pow(dC_dX[i,j], power[i,j] + 1.0);
				}
			}
            dC_dX -= (2.0* power[0,0]) * ILMath.eye(N, N);

            // Multiply by MC and MD matrices.
            temp = ILMath.multiplyElem(MC + MC.T + ILMath.repmat(ILMath.diag(MD), 1, N) + ILMath.repmat(ILMath.diag(MD).T, N, 1), dC_dX);

            // Multiply by temporal neighbor factor.
            temp = ILMath.multiplyElem(temp, temporalNeighborFactor);

            for (int k = 0; k < _X.Size[1]; k++)
            {
                // Now just compute the column-wise gradients.
                gX[ILMath.full, k] += ILMath.sum(ILMath.multiplyElem(temp, ILMath.repmat(_X[ILMath.full, k].T, N, 1)
                    - ILMath.repmat(_X[ILMath.full, k], 1, N)), 1);
            }

            return gX;
        }

        public ILRetArray<double> PostLogLikGradient()
        {
            // Todo
            return 0;
        }

        public void UpdateParameter(Data data)
        {
            _X = data.GetData("X");
            UpdateParameter();
        }

        public void UpdateParameter(ILInArray<double> _data)
        {
            using (ILScope.Enter(_data))
            {
                ILArray<double> d = ILMath.check(_data);
                _X.a = d;
                UpdateParameter();
            }
        }

        // Read and Write Data to a XML file
        public void Read(ref XmlReader reader)
        {
            
        }

        public void Write(ref XmlWriter writer)
        {

        }
        #endregion

        #region Private Computations
        /// <summary>
        /// Computes the square distance between two q-dimensional arrays of points.
        /// </summary>
        /// <remarks>
        /// Computing the square distance '(x_i - x_j) * (x_i - x_j)^T'.
        /// </remarks>
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<double>.
        /// </returns>
        private ILRetArray<double> SqareDistance(ILInArray<double> inX1, ILInArray<double> inX2)
        {
            using (ILScope.Enter(inX1, inX2))
            {
                ILArray<double> X1 = ILMath.check(inX1);
                ILArray<double> X2 = ILMath.check(inX2);

                int ndata = X1.Size[0]; // number of rows
                int dimx = X1.Size[1];  // number of collums
                int ncentres = X2.Size[0];
                int dimc = X2.Size[1];

                if (dimx != dimc)
                {
                    System.Console.WriteLine("Data dimension does not match dimension of centres");
                    return 0;
                }

                ILArray<double> n2 = ILMath.multiply(ILMath.ones<double>(ncentres, 1), ILMath.sum(ILMath.pow(X1, 2).T, 0)).T +
                    ILMath.multiply(ILMath.ones<double>(ndata, 1), ILMath.sum(ILMath.pow(X2, 2).T, 0)) -
                    2.0f * ILMath.multiply(X1, X2.T);

                // Rounding errors occasionally cause negative entries in n2
                if (ILMath.any(ILMath.any(n2 < 0f)))
                    n2[n2 < 0f] = 0f;

                return n2;
            }
        }

        private void UpdateParameter()
        {
            int N = _X.Size[0];

            n2.a = SqareDistance(_X, _X);

            // Computing the costs
            // Subtracting the identity ensures the diagonal has all 0s.
            costs.a = n2 + ILMath.eye(N, N);
            for (int i = 0; i < costs.Size[0]; i++)
            {
                for (int j = 0; j < costs.Size[1]; j++)
                {
                    costs[i, j] = 1.0 / ILMath.pow(costs[i, j], power[i, j]);
                }
            }
            costs.a -= ILMath.eye(N, N);

            // Multiply costs by temporal neighbor factor.
            costs.a = ILMath.multiplyElem(costs, temporalNeighborFactor);

            // Build negative Laplacian.
            D.a = ILMath.sum(costs, 1);
            H.a = costs;
            H.a -= ILMath.diag(D);
            invD.a = 1.0f / D;
            invSqrtD.a = ILMath.sqrt(invD);
            H.a = ILMath.diag(invSqrtD) * H * ILMath.diag(invSqrtD);

            // Use eigendecomposition to compute matrix exponential.
            Lambda.a = ILMath.eigSymm(H, U);

            // Exponentiate the eigenvalues.
            betaLambdaExp.a = ILMath.exp(_priorPrec * Lambda);

            // Compute kernel matrix.
            _K.a = U * betaLambdaExp * U.T;
            if (ILMath.any(ILMath.any(_K <= 0f)))
                _K[_K <= 0f] = 1.0e-16;

            _invK.a = Util.pdinverse(_K);
        }
        #endregion
    }
}
