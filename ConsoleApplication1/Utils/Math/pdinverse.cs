using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Computing the inverse of a positiv definit matrix. 
        /// </summary>
        /// <remarks>
        /// Using colesky factorisation for faster computing.
        /// </remarks>
        /// <param name="inA">The positiv semidefinite square matrix.</param>
        public static ILRetArray<double> pdinverse(ILInArray<double> inA)
        {
            using (ILScope.Enter(inA))
            {
                ILArray<double> A = ILMath.check(inA);
                ILArray<double> AInv = ILMath.empty();
                ILArray<double> U = ILMath.empty();
                ILArray<double> invU = ILMath.empty();

                U = jitChol(A);

                MatrixProperties propA = new MatrixProperties();
                propA |= MatrixProperties.UpperTriangular;  // matrix setting for faster computing

                // solving the linear equation with the identity matrix to get the inverse
                invU.a = ILMath.linsolve(U, ILMath.eye(U.Size[0], U.Size[1]), ref propA);
                AInv.a = ILMath.multiply(invU, invU.T);

                //ILArray<double> U2 = ILMath.empty();
                //System.Console.WriteLine();
                //for (int i = 0; i < 20; i++)
                //{
                //    U2 = jitChol(A);                    
                //    if (ILMath.allall(ILMath.eq(U, U2)))
                //        System.Console.Write("=");
                //    else
                //        System.Console.Write("!");
                //}
                //System.Console.WriteLine();

                return AInv;
            }
        }
    }
}
