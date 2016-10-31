using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Colesky factorisation with jitter to ensure positiv definitness. 
        /// </summary>
        /// <remarks>
        /// Adding jitter on the diagonal if the matrix is not positiv definite.
        /// </remarks>
        /// <param name="A">The positiv semidefinite square matrix.</param>
        public static ILRetArray<double> jitChol(ILInArray<double> inA)
        {
            using (ILScope.Enter(inA))
            {
                ILArray<double> A = ILMath.check(inA);
                int maxTries = 10;
                ILArray<double> jitter = 0;
                ILArray<double> UC = ILMath.empty();
                for (int i = 0; i < maxTries; i++)
                {
                    try
                    {
                        // Try --- need to check A is positive definite
                        if (jitter == 0f)
                        {
                            jitter = ILMath.abs(ILMath.mean(ILMath.diag(A))) * 1e-6;
                            UC = ILMath.chol(A);
                            break;
                        }
                        else
                        {
                            //System.Console.WriteLine("Matrix is not positive definite in jitChol, adding {0} jitter.", jitter);
                            UC = ILMath.chol(A + jitter * ILMath.eye<double>(A.Size[0], A.Size[1]));
                            break;
                        }
                    }
                    catch (ILNumerics.Exceptions.ILArgumentException e)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(e.Message, "not positive definite",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            System.Console.Write("+");
                            jitter *= 10;
                        }
                    }
                }
                return UC;
            }
        }
    }
}
