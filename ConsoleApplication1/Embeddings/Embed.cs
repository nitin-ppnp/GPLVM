namespace GPLVM.Embeddings
{
    // possible X init modes
    public enum XInit
    {
        pca,
        kernelPCA,
        lle,
        isomap,
        smallRand
    };

    public static partial class Embed
    {
        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// When some object X is said to be embedded in another object Y, the embedding 
        /// is given by some injective and structure-preserving map f : X → Y. The precise 
        /// meaning of "structure-preserving" depends on the kind of mathematical structure 
        /// of which X and Y are instances.
        /// </remarks>
    }
}
