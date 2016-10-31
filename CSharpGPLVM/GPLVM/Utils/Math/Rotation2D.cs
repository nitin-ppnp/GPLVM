using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        
        /// <summary>
        /// Construct a 2D counter clock wise rotation from the angle a in radian. 
        /// </summary>
        /// <param name="radAngle">Angle in radian.</param>
        /// <returns>1x2 ILArray<double></returns>
        public static ILRetArray<double> Rotation2DCounterClock(double radAngle)
        {
            ILArray<double> matrix2d = ILMath.zeros(2, 2);

            double cs = (double)ILMath.cos(radAngle);
            double sn = (double)ILMath.sin(radAngle);

            matrix2d[0, 0] = cs;
            matrix2d[0, 1] = -sn;
            matrix2d[1, 0] = sn;
            matrix2d[1, 1] = cs;

            return matrix2d;
        }

        /// <summary>
        /// Construct a 2D clock wise rotation from the angle a in radian. 
        /// </summary>
        /// <param name="radAngle">Angle in radian.</param>
        /// <returns>1x2 ILArray<double></returns>
        public static ILRetArray<double> Rotation2DClock(double radAngle)
        {
            return Rotation2DCounterClock(radAngle).T;
        }
    }
}
