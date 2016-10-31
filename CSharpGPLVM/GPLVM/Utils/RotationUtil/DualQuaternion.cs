namespace GPLVM
{
    /// <summary>
    /// Dual-quaternions to represent the rotations and translations in character-based hierarchies.
    /// </summary>
    /// <remarks>
    /// Dual-quaternions are interesting and important because they cut down
    /// the volume of algebra. They make the solution more straightforward and robust.
    /// They allow us to unify the translation and rotation into a single state; 
    /// instead of having to define separate vectors. While matrices offer a comparable
    /// alternative to dual-quaternions. In fact, dual-quaternions give us a compact, 
    /// unambiguous, singularity-free, and computational minimalistic rigid transform.
    /// In addition, dual-quaternions have been shown to be the most efficient and most 
    /// compact form of representing rotation and translation. Dual-quaternions can 
    /// easily take the place of matrices in hierarchies at noadditional cost.
    /// </remarks>
    /// <author>Nick Taubert</author>
    /// <created>Oct-25</created>
    public class DualQuaternion : RotationType
    {
        public Quaternion m_real;
        public Quaternion m_dual;

        private RotType _rotType = RotType.dualquaternion;

        #region Setters and Getters
        public RotType Type
        {
            get { return _rotType; }
        }
        #endregion

        public DualQuaternion()
        {
            m_real = new Quaternion(1,0,0,0);
            m_dual = new Quaternion(0,0,0,0);
        }

        public DualQuaternion(Quaternion r, Quaternion d)
        {
            m_real = Quaternion.Normalize(r);
            m_dual = d;
        }

        public DualQuaternion(Quaternion r, Vector3 t)
        {
            m_real = Quaternion.Normalize(r);
            m_dual = (new Quaternion(new Radian(0), t) * m_real ) * 0.5f;
        }
            
        public static double Dot(DualQuaternion a, DualQuaternion b)
        {
            return Quaternion.Dot(a.m_real, b.m_real);
        }

        public static DualQuaternion operator*(DualQuaternion q, float scale)
        {
            DualQuaternion ret = q;
            ret.m_real *= scale;
            ret.m_dual *= scale;
            return ret;
        }
        
        public static DualQuaternion Normalize(DualQuaternion q)
        {
            double mag = Quaternion.Dot(q.m_real, q.m_real);

            DualQuaternion ret = q;
            ret.m_real *= 1.0f / mag;
            ret.m_dual *= 1.0f / mag;
            return ret;
        }

        public static DualQuaternion operator+ (DualQuaternion lhs, DualQuaternion rhs)
        {
            return new DualQuaternion(lhs.m_real + rhs.m_real, lhs.m_dual + rhs.m_dual);
        }

        // Multiplication order-left to right
        public static DualQuaternion operator* (DualQuaternion lhs, DualQuaternion rhs)
        {
            return new DualQuaternion(rhs.m_real*lhs.m_real, rhs.m_dual*lhs.m_real + rhs.m_real*lhs.m_dual);
        }

        public static DualQuaternion Conjugate(DualQuaternion q)
        {
            return new DualQuaternion(Quaternion.Conjugate(q.m_real), Quaternion.Conjugate(q.m_dual));
        }

        public static Quaternion QuaternionGetRotation(DualQuaternion q)
        {
            return q.m_real;
        }
        
        public static Vector3 GetTranslation(DualQuaternion q)
        {
            Quaternion t = ( q.m_dual * 2.0f ) * Quaternion.Conjugate(q.m_real);
            return new Vector3(t.x, t.y, t.z);
        }

        /// <summary>
        /// Converts dual quaternion to a 4x4 transformation/rotation matrix.
        /// </summary>
        public Matrix4 ToMatrix()
        {
            DualQuaternion q = DualQuaternion.Normalize(this);

            Matrix4 M = new Matrix4(q.m_real.ToRotationMatrix());

            // Extract translation information
            M.setTransform(DualQuaternion.GetTranslation(q));

            return M;
        }

        /*public static Matrix DualQuaternionToMatrix(DualQuaternion q)
        {
            q = DualQuaternion.Normalize(q);
            //Matrix M = Matrix.Identity;
            double w = q.m_real.w;
            double x = q.m_real.x;
            double y = q.m_real.y;
            double z = q.m_real.z;
            // Extract rotational information
            M.M11 = w*w + x*x - y*y - z*z;
            M.M12 = 2*x*y + 2*w*z;
            M.M13 = 2*x*z - 2*w*y;
            M.M21 = 2*x*y - 2*w*z;
            M.M22 = w*w + y*y - x*x - z*z;
            M.M23 = 2*y*z + 2*w*x;
            M.M31 = 2*x*z + 2*w*y;
            M.M32 = 2*y*z - 2*w*x;
            M.M33 = w*w + z*z - x*x - y*y;
            // Extract translation information
            Quaternion t = (q.m_dual * 2.0f) * Quaternion.Conjugate(q.m_real);
            M.M41 = t.X;
            M.M42 = t.Y;
            M.M43 = t.Z;
            return M;
        }*/
    }
}
