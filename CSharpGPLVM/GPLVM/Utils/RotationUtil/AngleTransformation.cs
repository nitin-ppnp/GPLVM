using System;
using System.Runtime.InteropServices;
using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {
        /// <summary>
        /// Transform radians to degrees.
        /// </summary>
        /// <param name="theta">Angle in radians.</param>
        /// <returns>Angle in degrees</returns>
        public static ILRetArray<double> rad2deg(ILInArray<double> inTheta)
        {
            using (ILScope.Enter(inTheta))
            {
                ILArray<double> theta = ILMath.check(inTheta);

                return theta * 180 / ILMath.pi;
            }
        }

        /// <summary>
        /// Transform degrees to radians.
        /// </summary>
        /// <remarks>
        /// Adding jitter on the diagonal if the matrix is not positiv definite.
        /// </remarks>
        /// <param name="omega">Angle in degrees.</param>
        /// <returns>Angle in radians</returns>
        public static ILRetArray<double> deg2rad(ILInArray<double> inOmega)
        {
            using (ILScope.Enter(inOmega))
            {
                ILArray<double> omega = ILMath.check(inOmega);

                return omega / 180 * ILMath.pi;
            }
        }

        public static double Normalize(double angle)
        {
            if(angle < - ILMath.pi)
			{
				angle = fmod(angle, ILMath.pi * 2.0f);
				if(angle < -ILMath.pi)
				{
					angle += ILMath.pi * 2.0f;
				}
			}
			else if(angle > ILMath.pi)
			{
				angle = fmod(angle, ILMath.pi * 2.0f);
				if(angle > ILMath.pi)
				{
                    angle -= ILMath.pi * 2.0f;
				}
			}

            return angle;
        }

        // Normalize angle into [-180,180] range.
        public static double modDeg(double angle)
        {
            return modPositive(angle, -180.0, 180.0);
        }

        // Normalize real into [min,max] range.
        public static double modPositive(double val, double min, double max)
        {
            if (val >= min && val < max)
                return val;
            else
                return modDouble(val - min, max - min) + min;
        }

        // Normalize angle into [-PI,PI] range.
        public static double modAngle(double angle)
        {
            return modPositive(angle, -ILMath.pi, ILMath.pi);
        }

        // Double mod.
        public static double modDouble(double a, double _base)
        {
            return a < 0.0 ? Math.IEEERemainder(Math.IEEERemainder(a, _base) + _base, _base) : Math.IEEERemainder(a, _base);
            //return a < 0.0 ? fmod(fmod(a, _base) + _base, _base) : fmod(a, _base);
        }

        // Convert Euler angles between two different orders.
        public static ILArray<double> convertEuler(ILArray<double> radEuler, string oldOrder, string newOrder)
        {
            Matrix3 rot = GetRotationMatrix(radEuler, oldOrder);

            return FromRotationMatrixToEuler(rot, newOrder);
        }

        public static Quaternion eulerToQuaternion(ILArray<double> radEuler, string order)
        {
            Quaternion q = new Quaternion();
            q.FromRotationMatrix(GetRotationMatrix(radEuler, order));
            q = Quaternion.Normalize(q);
            return q;
        }

        // Convert Euler angles to exponential map.
        public static ILArray<double> eulerToExp(ILArray<double> radEuler, string order)
        {
                Quaternion q = new Quaternion();
                q.FromRotationMatrix(GetRotationMatrix(radEuler, order));

                ILArray<double> v = q.ToAngleAxis();

                return v[ILMath.r(1, 3)] * Util.rad2deg(modAngle((double)v[0])); //modAngle((double)v[0]);
        }

        public static ILArray<double> QuaternionToExp(Quaternion q)
        {
            ILArray<double> v = q.ToAngleAxis();

            return v[ILMath.r(1, 3)] * Util.rad2deg(modAngle((double)v[0])); //modAngle((double)v[0]);
        }

        // Convert exponential map to quaternion.
        public static Quaternion expToQuat(ILArray<double> exp)
        {
            if (exp.S[0] > exp.S[1]) exp = exp.T;

            ILArray<double> theta = ILMath.sqrt(ILMath.sum(ILMath.multiplyElem(exp, exp), 1));

            Quaternion quat = new Quaternion();
            if (ILMath.abs(theta) < 1.0e-16)
            {
                theta = ILMath.zeros(1, 4);
                theta[1] = 1;
                quat.FromAngleAxis(theta);
            }
            else
            {
                theta[ILMath.r(ILMath.end + 1, ILMath.end + exp.Length)] = exp / theta[0];
                theta[0] = deg2rad(theta[0]);
                quat.FromAngleAxis(theta);
            }
            return quat;
        }

        public static Matrix3 GetRotationMatrix(ILInArray<double> radAngles, string order)
        {
            ILArray<double> angles = ILMath.check(radAngles);
            double x = (double)angles[0];
            double y = (double)angles[1];
            double z = (double)angles[2];

            Matrix3 rotMatrix = new Matrix3();

            switch (order)
            {
                case "xyz":
                    rotMatrix.FromEulerAnglesXYZ(new Radian(x), new Radian(y), new Radian(z));
                    break;
                case "xzy":
                    rotMatrix.FromEulerAnglesXZY(new Radian(x), new Radian(z), new Radian(y));
                    break;
                case "yxz":
                    rotMatrix.FromEulerAnglesYXZ(new Radian(y), new Radian(x), new Radian(z));
                    break;
                case "yzx":
                    rotMatrix.FromEulerAnglesYZX(new Radian(y), new Radian(z), new Radian(x));
                    break;
                case "zxy":
                    rotMatrix.FromEulerAnglesZXY(new Radian(z), new Radian(x), new Radian(y));
                    break;
                case "zyx":
                    rotMatrix.FromEulerAnglesZYX(new Radian(z), new Radian(y), new Radian(x));
                    break;
            }

            return rotMatrix;
        }

        public static ILArray<double> FromRotationMatrixToEuler(Matrix3 rotMatrix, string order)
        {
            Radian x = new Radian(0);
            Radian y = new Radian(0);
            Radian z = new Radian(0);

            ILArray<double> angles = ILMath.zeros(1, 3);

            switch (order)
            {
                case "xyz":
                    rotMatrix.ToEulerAnglesXYZ(ref x, ref y, ref z);
                    /*angles[0] = x.Value;
                    angles[1] = y.Value;
                    angles[2] = z.Value;*/
                    break;
                case "xzy":
                    rotMatrix.ToEulerAnglesXZY(ref x, ref z, ref y);
                    /*angles[0] = x.Value;
                    angles[1] = z.Value;
                    angles[2] = y.Value;*/
                    break;
                case "yxz":
                    rotMatrix.ToEulerAnglesYXZ(ref y, ref x, ref z);
                    /*angles[0] = y.Value;
                    angles[1] = x.Value;
                    angles[2] = z.Value;*/
                    break;
                case "yzx":
                    rotMatrix.ToEulerAnglesYZX(ref y, ref z, ref x);
                    /*angles[0] = y.Value;
                    angles[1] = z.Value;
                    angles[2] = x.Value;*/
                    break;
                case "zxy":
                    rotMatrix.ToEulerAnglesZXY(ref z, ref x, ref y);
                    /*angles[0] = z.Value;
                    angles[1] = x.Value;
                    angles[2] = y.Value;*/
                    break;
                case "zyx":
                    rotMatrix.ToEulerAnglesZYX(ref z, ref y, ref x);
                    /*angles[0] = z.Value;
                    angles[1] = y.Value;
                    angles[2] = x.Value;*/
                    break;
            }

            angles[0] = x.Value;
            angles[1] = y.Value;
            angles[2] = z.Value;

            return angles;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double fmod(double x, double y);
    }
}
