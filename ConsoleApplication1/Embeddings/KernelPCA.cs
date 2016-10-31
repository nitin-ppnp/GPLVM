using ILNumerics;
using GPLVM.Kernel;

namespace GPLVM.Embeddings
{
    public static partial class Embed
    {
        /// <summary>
        /// Embed data set with kernel PCA.
        /// </summary>
        /// <remarks>
        /// Kernel principal component analysis (kernel PCA) is an extension of 
        /// principal component analysis (PCA) using techniques of kernel methods. 
        /// Using a kernel, the originally linear operations of PCA are done in 
        /// a reproducing kernel Hilbert space with a non-linear mapping.
        /// </remarks>
        /// <param name="inY">N by D data matrix.</param>
        /// <param name="q">Max embedding dimensionality.</param>
        /// <returns>
        /// The method returns the N by q latent matrix of type ILArray<double>.
        /// </returns>
        public static ILRetArray<double> KernelPCA(ILInArray<double> inY, int q)
        {
            using (ILScope.Enter(inY))
            {
                ILArray<double> Y = ILMath.check(inY) ;
                ILArray<double> X = ILMath.empty();

                RBFKernBack kern = new RBFKernBack();
                kern.Parameter[0] = 1e-2f;

                ILArray<double> K = kern.ComputeKernelMatrix(Y, Y);

                ILArray<double> Xtmp = ILMath.empty<double>();
                ILArray<double> temp_evals = ILMath.eigSymm(K, Xtmp);

                temp_evals = ILMath.diag(temp_evals);

                ILArray<int> perm = ILMath.empty<int>();
                ILArray<double> evals = ILMath.sort(-temp_evals, perm);
                evals = -evals[ILMath.r(0, q - 1)];

                X.a = Xtmp[ILMath.full, ILMath.r(0, q - 1)];
                if (ILMath.Equals(evals, temp_evals[ILMath.r(0, q - 1)]))
                    // Originals were in order
                    X.a = Xtmp[ILMath.full, ILMath.r(0, q - 1)];
                else
                    // Need to reorder the eigenvectors
                    for (int i = 0; i < q; i++)
                        X[ILMath.full, i] = Xtmp[ILMath.full, perm[i]];

                evals = ILMath.diag(evals);
                X = ILMath.multiply(X, ILMath.sqrt(evals));

                return X;
            }
        }
    }
}
