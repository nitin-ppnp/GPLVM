using ILNumerics;

namespace GPLVM.Embeddings
{
    public static partial class Embed
    {
        /// <summary>
        /// Embed data set with small random values.
        /// </summary>
        /// <param name="inY">N by D data matrix.</param>
        /// <param name="q">Max embedding dimensionality.</param>
        /// <returns>
        /// The method returns the N by q latent matrix of type ILArray<double>.
        /// </returns>
        public static ILRetArray<double> SmallRand(ILInArray<double> inY, int q)
        {
            using (ILScope.Enter(inY))
            {
                ILArray<double> Y = ILMath.check(inY);

                return ILMath.randn(Y.Size[0], q) * 0.0001;
            }
        }
    }
}
