using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Computing log determinant. 
        /// </summary>
        /// <param name="A">The positiv semidefinite square matrix.</param>
        public static double logdet(ILInArray<double> inA)
        {
            using (ILScope.Enter(inA))
            {
                ILArray<double> A = ILMath.check(inA);

                ILArray<double> U = jitChol(A);
                ILArray<double> logdet_ = 2 * ILMath.sum(ILMath.log(ILMath.diag(U)), 0);
                return logdet_.GetValue(0);
            }
        }
    }
}
