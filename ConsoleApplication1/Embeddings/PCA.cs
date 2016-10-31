using ILNumerics;

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
        public static ILRetArray<double> PCA(ILInArray<double> inY, int q)
        {
            using (ILScope.Enter(inY))
            {
                ILArray<double> Y = ILMath.check(inY);
                ILArray<double> X = ILMath.empty();

                if (!ILMath.any(ILMath.any(ILMath.isnan(Y))))
                {
                    ILArray<double> u = ILMath.empty();
                    ILArray<double> v = ILMath.empty();
                    v = Util.EigDec(ILMath.cov(Y.T).T, Y.Size[1], u);

                    v[ILMath.find(v < 0)] = 0;
                    X = u[ILMath.full, ILMath.r(0, q - 1)];
                    X = ILMath.multiply(ILMath.multiply(Y, X), ILMath.diag(1 / ILMath.sqrt(v[ILMath.r(0, q - 1)])));
                    return X;
                }
                else
                    return 0;
            }
        }
    }
}
