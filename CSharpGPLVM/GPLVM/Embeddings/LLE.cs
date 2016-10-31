using ILNumerics;

namespace GPLVM.Embeddings
{
    public static partial class Embed
    {
        /// <summary>
        /// Embed data set with LLE.
        /// </summary>
        /// <remarks>
        /// Locally-Linear Embeddinghas begins by finding a set of the nearest neighbors of each point. 
        /// It then computes a set of weights for each point that best describe the point as a linear 
        /// combination of its neighbors. Finally, it uses an eigenvector-based optimization technique 
        /// to find the low-dimensional embedding of points, such that each point is still described with 
        /// the same linear combination of its neighbors. LLE tends to handle non-uniform sample densities 
        /// poorly because there is no fixed unit to prevent the weights from drifting as various regions 
        /// differ in sample densities. LLE has no internal model.
        /// 
        /// Translation of Sam Roweis' Matlab LLE code.
        /// </remarks>
        /// <param name="inY">N by D data matrix.</param>
        /// <param name="numNeighbors">Number of neighbors.</param>
        /// <param name="q">Max embedding dimensionality.</param>
        /// <returns>
        /// The method returns the N by q latent matrix of type ILArray<double>.
        /// </returns>
        public static ILRetArray<double> LLE(ILInArray<double> inY, int numNeighbors, int q)
        {
            using (ILScope.Enter(inY))
            {
                ILArray<double> Y = ILMath.check(inY).T;
                ILArray<double> X = ILMath.empty();

                int N = Y.Size[1];
                int D = Y.Size[0];

                System.Console.WriteLine("LLE running on {0} points in {1} dimensions", N, D);

                // STEP1: COMPUTE PAIRWISE DISTANCES & FIND NEIGHBORS 
                System.Console.WriteLine("\t-->Finding {0} nearest neighbours.", numNeighbors);

                ILArray<double> Y2 = ILMath.sum(ILMath.pow(Y, 2), 0);
                ILArray<double> distance = ILMath.repmat(Y2, N, 1) + ILMath.repmat(Y2.T, 1, N) - 2 * ILMath.multiply(Y.T, Y);

                ILArray<int> index = ILMath.empty<int>();
                ILArray<double> sorted = ILMath.sort(distance, index);
                
                ILArray<int> neighborhood = index[ILMath.r(1,numNeighbors),ILMath.full];

                // STEP2: SOLVE FOR RECONSTRUCTION WEIGHTS
                System.Console.WriteLine("\t-->Solving for reconstruction weights.");

                double tol;
                if (numNeighbors > D)
                {
                    System.Console.WriteLine("\t   [note: numNeighbors > D; regularization will be used]"); 
                    tol = 1e-3; // regularlizer in case constrained fits are ill conditioned
                }
                else
                {
                    tol = 0;
                }

                ILArray<double> W = ILMath.zeros(numNeighbors, N);
                ILArray<double> z, C;
                for (int ii = 0; ii < N; ii++)
                {
                    z = Y[ILMath.full,neighborhood[ILMath.full,ii]] - 
                        ILMath.repmat(Y[ILMath.full,ii], 1, numNeighbors);                                  // shift ith pt to origin
                    C = ILMath.multiply(z.T, z);                                                            // local covariance
                    C = C + ILMath.eye(numNeighbors, numNeighbors) * tol * ILMath.trace(C);                 // regularlization (K>D)
                    W[ILMath.full,ii] = ILMath.linsolve(C, ILMath.ones(numNeighbors, 1));                   // solve Cw=1
                    W[ILMath.full,ii] = ILMath.divide(W[ILMath.full,ii], ILMath.sum(W[ILMath.full,ii]));    // enforce sum(w)=1
                }


                // STEP 3: COMPUTE EMBEDDING FROM EIGENVECTS OF COST MATRIX M=(I-W)'(I-W)
                System.Console.WriteLine("\t-->Computing embedding.");

                ILArray<double> M = ILMath.eye(N, N);
                ILArray<double> w;
                ILArray<int> jj;
                //ILArray<double> M = ILMath.sparse(1:N,1:N,ones(1,N),N,N,4*K*N); // use a sparse matrix with storage for 4KN nonzero elements
                for (int ii = 0; ii < N; ii++)
                {
                    w = W[ILMath.full,ii];
                    jj = neighborhood[ILMath.full,ii];
                    M[ii,jj] = M[ii,jj] - w.T;
                    M[jj,ii] = M[jj,ii] - w;
                    M[jj,jj] = M[jj,jj] + ILMath.multiply(w, w.T);
                }

                // CALCULATION OF EMBEDDING
                ILArray<double> Xtmp = ILMath.empty<double>(); 
                ILArray<double> temp_evals = ILMath.eigSymm(M, Xtmp);

                temp_evals = ILMath.diag(temp_evals);

                // Eigenvalues nearly always returned in descending order, but just
                ILArray<int> perm = ILMath.empty<int>();
                ILArray<double> evals = ILMath.sort(temp_evals, perm);
                evals = evals[ILMath.r(0, q)];

                X.a = Xtmp;
                if (ILMath.Equals(evals, temp_evals[ILMath.r(0, q)]))
                    // Originals were in order
                    X.a = Xtmp[ILMath.full, ILMath.r(0, q)];
                else
                    // Need to reorder the eigenvectors
                    for (int i = 0; i < q; i++)
                        X[ILMath.full, i] = Xtmp[ILMath.full, perm[i]];

                X = X[ILMath.full,ILMath.r(1,q)] * ILMath.sqrt((double)N); // bottom evect is [1,1,1,1...] with eval 0

                System.Console.WriteLine("\nDone.");
                
                return X;
            }
        }
    }
}
