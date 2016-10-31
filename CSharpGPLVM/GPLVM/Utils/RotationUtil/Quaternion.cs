using ILNumerics;

namespace GPLVM
{
    public class Quaternion : RotationType
    {
        public ILArray<double> _q;
        private RotType _rotType = RotType.quaternion;

        #region Setters and Getters
        public RotType Type
        {
            get { return _rotType; }
        }

        public double w
        {
            get
            {
                return (double)_q[0];
            }
            set
            {
                _q[0] = value;
            }
        }

        public double x
        {
            get
            {
                return (double)_q[1];
            }
            set
            {
                _q[1] = value;

            }
        }

        public double y
        {
            get
            {
                return (double)_q[2];
            }
            set
            {
                _q[2] = value;

            }
        }

        public double z
        {
            get
            {
                return (double)_q[3];
            }
            set
            {
                _q[3] = value;

            }
        }
        #endregion

        #region Constructors
        public Quaternion(double w, double x, double y, double z)
        {
            _q = new double[4];
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Quaternion(Radian angle, Vector3 axis)
        {
            _q = new double[4];
            this.w = angle.Value;
            this.x = axis.X;
            this.y = axis.Y;
            this.z = axis.Z;
        }

        public Quaternion(Matrix3 mat)
        {
            _q = new double[4];
            FromRotationMatrix(mat);
        }

        public Quaternion()
        {
            _q = new double[4];
            this.w = 1;
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }
        #endregion

        //-----------------------------------------------------------------------
        public void FromRotationMatrix(Matrix3 rotm)
        {
            ILArray<double> kRot = rotm.Matrix;

            double fTrace = (double)ILMath.trace(kRot);//kRot[0,0] + kRot[1,1] + kRot[2,2];
            double fRoot;

            if ( fTrace > 0.0 )
            {
                // |w| > 1/2, may as well choose w > 1/2
                fRoot = (double)ILMath.sqrt(fTrace + 1.0f);  // 2w
                w = 0.5f*fRoot;
                fRoot = 0.5f/fRoot;  // 1/(4w)
                x = (double)(kRot[2,1]-kRot[1,2])*fRoot;
                y = (double)(kRot[0,2]-kRot[2,0])*fRoot;
                z = (double)(kRot[1,0]-kRot[0,1])*fRoot;
            }
            else
            {
                // |w| <= 1/2
                int[] s_iNext = new int[3]{ 1, 2, 0 };
                int i = 0;
                if ( kRot[1,1] > kRot[0,0] )
                    i = 1;
                if ( kRot[2,2] > kRot[i,i] )
                    i = 2;
                int j = s_iNext[i];
                int k = s_iNext[j];

                fRoot = (double)ILMath.sqrt(kRot[i,i]-kRot[j,j]-kRot[k,k] + 1.0f);
                _q[i] = 0.5f*fRoot;
                fRoot = 0.5f/fRoot;
                w = (double)(kRot[k,j]-kRot[j,k])*fRoot;
                _q[j] = (kRot[j,i] + kRot[i,j]) * fRoot;
                _q[k] = (kRot[k,i] + kRot[i,k]) * fRoot;
            }
        }

        //-----------------------------------------------------------------------
        public Matrix3 ToRotationMatrix()
        {
            Matrix3 kRot = new Matrix3();

            double fTx  = x+x;
            double fTy = y + y;
            double fTz = z + z;
            double fTwx = fTx * w;
            double fTwy = fTy * w;
            double fTwz = fTz * w;
            double fTxx = fTx * x;
            double fTxy = fTy * x;
            double fTxz = fTz * x;
            double fTyy = fTy * y;
            double fTyz = fTz * y;
            double fTzz = fTz * z;

            kRot.Matrix[0, 0] = 1.0f-(fTyy+fTzz);
            kRot.Matrix[0, 1] = fTxy - fTwz;
            kRot.Matrix[0, 2] = fTxz + fTwy;
            kRot.Matrix[1, 0] = fTxy + fTwz;
            kRot.Matrix[1, 1] = 1.0f - (fTxx + fTzz);
            kRot.Matrix[1, 2] = fTyz - fTwx;
            kRot.Matrix[2, 0] = fTxz - fTwy;
            kRot.Matrix[2, 1] = fTyz + fTwx;
            kRot.Matrix[2, 2] = 1.0f - (fTxx + fTyy);

            return kRot;
        }

        //-----------------------------------------------------------------------
        public ILArray<double> ToAngleAxis()
        {
            // The quaternion representing the rotation is
            //   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

            ILArray<double> ret = ILMath.zeros(1,4);

            double fSqrLength = x*x+y*y+z*z;
            if ( fSqrLength > 0.0 )
            {
                ret[0] = 2.0 * ILMath.acos(w);
                double fInvLength = 1 / (double)ILMath.sqrt(fSqrLength);
                ret[1] = x * fInvLength;
                ret[2] = y * fInvLength;
                ret[3] = z * fInvLength;
            }
            else
            {
                // angle is 0 (mod 2*pi), so any axis will do
                ret[0] = 0.0;
                ret[1] = 1.0;
                ret[2] = 0.0;
                ret[3] = 0.0;
            }

            return ret;
        }

        //-----------------------------------------------------------------------
        public void FromAngleAxis(ILArray<double> rfAngles)
        {
            // assert:  axis[] is unit length
            //
            // The quaternion representing the rotation is
            //   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

            double fHalfAngle = 0.5 * (double)rfAngles[0];
            double fSin = (double)ILMath.sin(fHalfAngle);
            w = (double)ILMath.cos(fHalfAngle);
            x = fSin * (double)rfAngles[1];
            y = fSin * (double)rfAngles[2];
            z = fSin * (double)rfAngles[3];
        }

        //-----------------------------------------------------------------------
        public static Quaternion Invert(Quaternion q)
        {
            double fNorm = Quaternion.Norm(q);
            if ( fNorm > 0.0 )
            {
                double fInvNorm = 1.0f/fNorm;
                return new Quaternion(q.w * fInvNorm,-q.x * fInvNorm,-q.y * fInvNorm,-q.z * fInvNorm);
            }
            else
            {
                // return an invalid result to flag the error
                return new Quaternion(0, 0, 0, 0);
            }
        }

        //-----------------------------------------------------------------------
        public static Quaternion Conjugate(Quaternion q)
        {
            return new Quaternion(q.w, -q.x, -q.y, -q.z);
        }

        //-----------------------------------------------------------------------
        public static Quaternion Exp(Quaternion q)
        {
            // If q = A*(x*i+y*j+z*k) where (x,y,z) is unit length, then
            // exp(q) = cos(A)+sin(A)*(x*i+y*j+z*k).  If sin(A) is near zero,
            // use exp(q) = cos(A)+A*(x*i+y*j+z*k) since A/sin(A) has limit 1.

            double fAngle = (double)ILMath.sqrt(q.x*q.x + q.y*q.y + q.z*q.z);
            double fSin = (double)ILMath.sin(fAngle);

            Quaternion kResult = new Quaternion();
            kResult.w = (double)ILMath.cos(fAngle);

            if (ILMath.abs(fSin) >= 1e-3)
            {
                double fCoeff = fSin / fAngle;
                kResult.x = fCoeff * q.x;
                kResult.y = fCoeff * q.y;
                kResult.z = fCoeff * q.z;
            }
            else
            {
                kResult.x = q.x;
                kResult.y = q.y;
                kResult.z = q.z;
            }
            return kResult;
        }

        //-----------------------------------------------------------------------
        public static Quaternion Log(Quaternion q)
        {
            // If q = cos(A)+sin(A)*(x*i+y*j+z*k) where (x,y,z) is unit length, then
            // log(q) = A*(x*i+y*j+z*k).  If sin(A) is near zero, use log(q) =
            // sin(A)*(x*i+y*j+z*k) since sin(A)/A has limit 1.

            Quaternion kResult = new Quaternion();
            kResult.w = 0.0;

            if (ILMath.abs(q.w) < 1.0 )
            {
                double fAngle = (double)ILMath.acos(q.w);
                double fSin = (double)ILMath.sin(fAngle);
                if (ILMath.abs(fSin) >= 1e-3)
                {
                    double fCoeff = fAngle / fSin;
                    kResult.x = fCoeff * q.x;
                    kResult.y = fCoeff * q.y;
                    kResult.z = fCoeff * q.z;
                    return kResult;
                }
            }

            kResult.x = q.x;
            kResult.y = q.y;
            kResult.z = q.z;

            return kResult;
        }

        //-----------------------------------------------------------------------
        /// <summary>
        /// Smoothly interpolate between the two given quaternions using Spherical
        /// Linear Interpolation (SLERP).
        /// </summary>
        /// <param name="rkP">First quaternion for interpolation.
        /// <param name="rkQ">Second quaternion for interpolation.
        /// <param name="fT">Interpolation coefficient.
        /// <param name="shortestPath">If true, Slerp will automatically flip the sign of
        ///     the destination Quaternion to ensure the shortest path is taken.
        /// <returns>SLERP-interpolated quaternion between the two given quaternions.</returns>
        public static Quaternion Slerp(double fT, Quaternion rkP, Quaternion rkQ, bool shortestPath = false)
        {
            double fCos = Quaternion.Dot(rkP, rkQ);
            Quaternion rkT;

            // Do we need to invert rotation?
            if (fCos < 0.0f && shortestPath)
            {
                fCos = -fCos;
                rkT = -rkQ;
            }
            else
            {
                rkT = rkQ;
            }

            if (ILMath.abs(fCos) < 1 - 1e-3)
            {
                // Standard case (slerp)
                double fSin = (double)ILMath.sqrt(1 - ILMath.sqrt(fCos));
                double fAngle = (double)ILMath.atan2(fSin, fCos);
                double fInvSin = 1.0f / fSin;
                double fCoeff0 = (double)ILMath.sin((1.0f - fT) * fAngle) * fInvSin;
                double fCoeff1 = (double)ILMath.sin(fT * fAngle) * fInvSin;
                return fCoeff0 * rkP + fCoeff1 * rkT;
            }
            else
            {
                // There are two situations:
                // 1. "rkP" and "rkQ" are very close (fCos ~= +1), so we can do a linear
                //    interpolation safely.
                // 2. "rkP" and "rkQ" are almost inverse of each other (fCos ~= -1), there
                //    are an infinite number of possibilities interpolation. but we haven't
                //    have method to fix this case, so just use linear interpolation here.
                return Quaternion.Normalize((1.0f - fT) * rkP + fT * rkT); // taking the complement requires renormalisation
            }
        }

        /// <summary>
        /// @see Slerp. It adds extra "spins" (i.e. rotates several times) specified
        /// by parameter 'iExtraSpins' while interpolating before arriving to the final values
        /// </summary>
        /// <param name="rkP">First quaternion for interpolation.
        /// <param name="rkQ">Second quaternion for interpolation.
        /// <param name="fT">Interpolation coefficient.
        /// <param name="iExtraSpins">Extra spins.
        /// <returns>SLERP-interpolated quaternion between the two given quaternions and extra spin.</returns>
        public static Quaternion SlerpExtraSpins(double fT, Quaternion rkP, Quaternion rkQ, int iExtraSpins)
        {
            double fCos = Quaternion.Dot(rkP, rkQ);
            double fAngle = (double)ILMath.acos(fCos);

            if (ILMath.abs(fAngle) < 1e-3)
                return rkP;

            double fSin = (double)ILMath.sin(fAngle);
            double fPhase = ILMath.pi * iExtraSpins* fT;
            double fInvSin = 1.0f / fSin;
            double fCoeff0 = (double)ILMath.sin((1.0f-fT)*fAngle - fPhase)*fInvSin;
            double fCoeff1 = (double)ILMath.sin(fT*fAngle + fPhase)*fInvSin;
            return fCoeff0*rkP + fCoeff1*rkQ;
        }

        /// <summary>
        /// Setup for spherical quadratic interpolation
        /// </summary>
        /// <param name="rkQ0">First quaternion for interpolation.
        /// <param name="rkQ1">Second quaternion for interpolation.
        /// <param name="rkQ2">Third quaternion for interpolation.
        /// <returns>
        /// <param name="rkA">Quaternion A.
        /// <param name="rkB">Quaternion B.
        /// </returns>
        public static void Intermediate(Quaternion rkQ0, Quaternion rkQ1, Quaternion rkQ2, ref Quaternion rkA, ref Quaternion rkB)
        {
            // assert:  q0, q1, q2 are unit quaternions

            Quaternion kQ0inv = Quaternion.Conjugate(rkQ0);
            Quaternion kQ1inv = Quaternion.Conjugate(rkQ1);
            Quaternion rkP0 = kQ0inv * rkQ1;
            Quaternion rkP1 = kQ1inv * rkQ2;
            Quaternion kArg = 0.25 * (Quaternion.Log(rkP0) - Quaternion.Log(rkP1));
            Quaternion kMinusArg = -kArg;

            rkA = rkQ1 * Quaternion.Exp(kArg);
            rkB = rkQ1 * Quaternion.Exp(kMinusArg);
        }

        /// <summary>
        /// Spherical quadratic interpolation
        /// </summary>
        /// <param name="fT">Interpolation coefficient.
        /// <param name="rkP">First quaternion for interpolation.
        /// <param name="rkA">Second quaternion for interpolation.
        /// <param name="rkB">Third quaternion for interpolation.
        /// <param name="rkQ">Fourth quaternion for interpolation.
        /// <param name="shortestPath">If true, Slerp will automatically flip the sign of
        ///     the destination Quaternion to ensure the shortest path is taken.
        /// <returns>Squad-interpolated quaternion between the four given quaternions.</returns>
        public static Quaternion Squad(double fT, Quaternion rkP, Quaternion rkA, Quaternion rkB, Quaternion rkQ, bool shortestPath)
        {
            double fSlerpT = 2.0f * fT * (1.0f - fT);
            Quaternion kSlerpP = Quaternion.Slerp(fT, rkP, rkQ, shortestPath);
            Quaternion kSlerpQ = Quaternion.Slerp(fT, rkA, rkB);
            return Quaternion.Slerp(fSlerpT, kSlerpP, kSlerpQ);
        }

        //-----------------------------------------------------------------------
        public double getRoll(bool reprojectAxis)
        {
            if (reprojectAxis)
            {
                    // roll = atan2(localx.y, localx.x)
                    // pick parts of xAxis() implementation that we need Real fTx  = 2.0*x;
                    double fTy  = 2.0f * y;
                    double fTz  = 2.0f * z;
                    double fTwz = fTz * w;
                    double fTxy = fTy * x;
                    double fTyy = fTy * y;
                    double fTzz = fTz * z;

                    return (double)ILMath.atan2(fTxy+fTwz, 1.0f-(fTyy+fTzz));
            }
            else
                    return (double)ILMath.atan2(2*(x*y + w*z), w*w + x*x - y*y - z*z);
        }

        //-----------------------------------------------------------------------
        public double getPitch(bool reprojectAxis)
        {
                if (reprojectAxis)
                {
                        // pitch = atan2(localy.z, localy.y)
                        // pick parts of yAxis() implementation that we need
                        double fTx  = 2.0f * x;
                        double fTz  = 2.0f * z;
                        double fTwx = fTx * w;
                        double fTxx = fTx * x;
                        double fTyz = fTz * y;
                        double fTzz = fTz * z;

                        return (double)ILMath.atan2(fTyz+fTwx, 1.0f-(fTxx+fTzz));
                }
                else
                {
                        // internal version
                        return (double)ILMath.atan2(2*(y*z + w*x), w*w - x*x - y*y + z*z);
                }
        }

        //-----------------------------------------------------------------------
        public double getYaw(bool reprojectAxis)
        {
                if (reprojectAxis)
                {
                        // yaw = atan2(localz.x, localz.z)
                        // pick parts of zAxis() implementation that we need
                        double fTx  = 2.0f*x;
                        double fTy  = 2.0f*y;
                        double fTz  = 2.0f*z;
                        double fTwy = fTy*w;
                        double fTxx = fTx*x;
                        double fTxz = fTz*x;
                        double fTyy = fTy*y;

                        return (double)ILMath.atan2(fTxz+fTwy, 1.0f-(fTxx+fTyy));

                }
                else
                {
                        // internal version
                        return (double)ILMath.asin(-2*(x*z - w*y));
                }
        }

        public static Quaternion nlerp(double fT, Quaternion rkP, Quaternion rkQ, bool shortestPath)
        {
            Quaternion result;
            double fCos = Quaternion.Dot(rkP, rkQ);
            if (fCos < 0.0f && shortestPath)
            {
                    result = rkP + fT * ((-rkQ) - rkP);
            }
            else
            {
                    result = rkP + fT * (rkQ - rkP);
            }

            return Quaternion.Normalize(result);
        }

        //-----------------------------------------------------------------------
       public bool equals(Quaternion rhs, double tolerance)
        {
            double fCos = Quaternion.Dot(this, rhs);
            double angle = (double)ILMath.acos(fCos);

            return (ILMath.abs(angle) <= tolerance) || (ILMath.abs(angle - ILMath.pi) <= tolerance);
        }

        //-----------------------------------------------------------------------
        public static double Norm(Quaternion q)
        {
            return q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z;
        }

        //-----------------------------------------------------------------------
        public static double Dot(Quaternion q1, Quaternion q2)
        {
            return q1.w * q2.w + q1.x * q2.x + q1.y * q2.y + q1.z * q2.z;
        }

        //-----------------------------------------------------------------------
        public static Quaternion Normalize(Quaternion q)
        {
            double mag = (double)ILMath.sqrt(Quaternion.Dot(q, q));
            mag = 1 / mag;
            return q *= mag;
        }

        //-----------------------------------------------------------------------
        public static Quaternion operator* (Quaternion q, double scalar)
        {
            return new Quaternion(scalar * q.w, scalar * q.x, scalar * q.y, scalar * q.z);
        }

        //-----------------------------------------------------------------------
        public static Quaternion operator* (double scalar, Quaternion q)
        {
            return new Quaternion(scalar * q.w, scalar * q.x, scalar * q.y, scalar * q.z);
        }

        //-----------------------------------------------------------------------
        public static Quaternion operator+ (Quaternion a, Quaternion b)
        {
            return new Quaternion(a.w + b.w, a.x + b.x, a.y + b.y, a.z + a.z);
        }

        //-----------------------------------------------------------------------
        public static Quaternion operator- (Quaternion a, Quaternion b)
        {
            return new Quaternion(a.w - b.w, a.x - b.x, a.y - b.y, a.z - b.z);
        }

        //-----------------------------------------------------------------------
        public static Quaternion operator- (Quaternion a)
        {
            return new Quaternion(-a.w, -a.x, -a.y, -a.z);
        }

        public static Quaternion operator* (Quaternion a, Quaternion b)
        {
            // NOTE:  Multiplication is not generally commutative, so in most
            // cases p*q != q*p.

            return new Quaternion
            (
                a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z,
                a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
                a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
                a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x
            );
        }

        // Rotation of a vector by a quaternion
        public static Vector3 operator* (Quaternion q, Vector3 v)
        {
            // nVidia SDK implementation
            Vector3 uv, uuv;
            Vector3 qvec = new Vector3(q.x, q.y, q.z);
            uv = Vector3.CrossProduct(qvec, v);
            uuv = Vector3.CrossProduct(qvec, uv);
            uv *= (2.0f * q.w);
            uuv *= 2.0f;

            return v + uv + uuv;
        }
  
        #region Private Fields and Properties
  
        
  
        #endregion Private Fields and Properties
    }
}
