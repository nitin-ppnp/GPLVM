using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Sorted eigendecomposition.
        /// </summary>
        /// <remarks>
        /// computes the largest N eigenvalues of the matrix x in descending order.  It also
        /// computes the corresponding eigenvectors.
        /// </remarks>
        /// <param name="x">Covariance of the data.</param>
        /// <param name="N">Dimension of the data set.</param>
        public static ILRetArray<double> EigDec(ILInArray<double> inX, double N, ILOutArray<double> evec)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);

                if (N != ILMath.round(N) || N < 1 || N > x.Size[1])
                    System.Console.WriteLine("Number of PCs must be integer, >0, < dim");

                // Find the eigenvalues of the data covariance matrix
                // Use eig function unless fraction of eigenvalues required is tiny
                ILArray<double> temp_evec = ILMath.empty<double>();

                ILArray<double> temp_evals = ILMath.eigSymm(x, temp_evec);

                temp_evals = ILMath.diag(temp_evals);

                // Eigenvalues nearly always returned in descending order, but just

                ILArray<int> perm = ILMath.empty<int>();
                ILArray<double> evals = ILMath.sort(-temp_evals, perm);
                evals = -evals[ILMath.r(0, N - 1)];

                evec.a = temp_evec;
                if (ILMath.Equals(evals, temp_evals[ILMath.r(0, N - 1)]))
                    // Originals were in order
                    evec.a = temp_evec[ILMath.full, ILMath.r(0, N - 1)];
                else
                    // Need to reorder the eigenvectors
                    for (int i = 0; i < N; i++)
                        evec[ILMath.full, i] = temp_evec[ILMath.full, perm[i]];

                return evals;
            }
        }
    }
}
