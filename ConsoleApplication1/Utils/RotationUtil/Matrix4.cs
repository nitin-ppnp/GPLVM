using ILNumerics;

namespace GPLVM
{
    public class Matrix4 : RotationType
    {
        private RotType _rotType = RotType.matrix4;
        private ILArray<double> rotm;

        #region Setters and Getters
        public RotType Type
        {
            get { return _rotType; }
        }

        public ILArray<double> Matrix
        {
            get
            {
                return rotm;
            }
            set
            {
                rotm = value.C;
            }
        }
        #endregion

        public Matrix4()
        {
            rotm = ILMath.zeros(4, 4);
        }

        public Matrix4(double e00, double e01, double e02, double e03, 
            double e10, double e11, double e12, double e13,
            double e20, double e21, double e22, double e23,
            double e30, double e31, double e32, double e33)
        {
            rotm = ILMath.zeros(4,4);

            rotm[0,0] = e00;
            rotm[0,1] = e01;
            rotm[0,2] = e02;
            rotm[0,3] = e03;
            rotm[1,0] = e10;
            rotm[1,1] = e11;
            rotm[1,2] = e12;
            rotm[1,3] = e13;
            rotm[2,0] = e20;
            rotm[2,1] = e21;
            rotm[2,2] = e22;
            rotm[2,3] = e23;
            rotm[3,0] = e30;
            rotm[3,1] = e31;
            rotm[3,2] = e32;
            rotm[3,3] = e33;
        }

        /// <summary>
        /// Creates a standard 4x4 transformation matrix with a zero translation part from a rotation/scaling 3x3 matrix.
        /// </summary>
        public Matrix4(Matrix3 m3x3)
        {
          rotm = ILMath.eye(4,4);
          rotm[ILMath.r(0,2), ILMath.r(0,2)] = m3x3.Matrix;
        }

        /// <summary>
        /// Creates a standard 4x4 transformation matrix with a zero translation part from a rotation/scaling Quaternion.
        /// </summary>
        public Matrix4(Quaternion rot)
        {
          Matrix3 m3x3 = rot.ToRotationMatrix();
          rotm = ILMath.eye(4, 4);
          rotm[ILMath.r(0, 2), ILMath.r(0, 2)] = m3x3.Matrix;
        }

        /// <summary>
        /// Creates a standard 4x4 identity matrix.
        /// </summary>
        public static Matrix4 Identity()
        {
            Matrix4 m = new Matrix4();
            m.Matrix = ILMath.eye(4, 4);
            return m;
        }

        public static Matrix4 operator* (Matrix4 a, Matrix4 b)
        {
            Matrix4 kProd = new Matrix4();
            kProd.Matrix = ILMath.multiply(a.Matrix, b.Matrix);
            return kProd;
        }

        /// <summary>
        /// Vector transformation using '*'.
        /// </summary>
        /// <remarks>
        /// Transforms the given 3-D vector by the matrix, projecting the result back into <i>w</i> = 1.
        /// </remarks>
        /// <note>
        /// This means that the initial <i>w</i> is considered to be 1.0, 
        /// and then all the tree elements of the resulting 3-D vector are
        /// divided by the resulting <i>w</i>.
        /// </note>
        public static Vector3 operator* (Matrix4 m, Vector3 v)
        {
            Vector3 r = new Vector3();

            double fInvW = 1.0f / (double)( m.Matrix[3,0] * v.X + m.Matrix[3,1] * v.Y + m.Matrix[3,2] * v.Z + m.Matrix[3,3]);

            r.X = (double)(m.Matrix[0,0] * v.X + m.Matrix[0,1] * v.Y + m.Matrix[0,2] * v.Z + m.Matrix[0,3]) * fInvW;
            r.Y = (double)(m.Matrix[1,0] * v.X + m.Matrix[1,1] * v.Y + m.Matrix[1,2] * v.Z + m.Matrix[1,3]) * fInvW;
            r.Z = (double)(m.Matrix[2,0] * v.X + m.Matrix[2,1] * v.Y + m.Matrix[2,2] * v.Z + m.Matrix[2,3]) * fInvW;

            return r;
        }

        /// <summary>
        /// Vector transformation using '*'.
        /// </summary>
        /// <remarks>
        /// Transforms the given 3-D vector by the matrix, projecting the result back into <i>w</i> = 1.
        /// </remarks>
        /// <note>
        /// This means that the initial <i>w</i> is considered to be 1.0, 
        /// and then all the tree elements of the resulting 3-D vector are
        /// divided by the resulting <i>w</i>.
        /// </note>
        public static Vector3 operator* (Vector3 v, Matrix4 m)
        {
            Vector3 r = new Vector3();

            double fInvW = 1.0f / (double)( m.Matrix[3,0] * v.X + m.Matrix[3,1] * v.Y + m.Matrix[3,2] * v.Z + m.Matrix[3,3]);

            r.X = (double)(m.Matrix[0,0] * v.X + m.Matrix[0,1] * v.Y + m.Matrix[0,2] * v.Z + m.Matrix[0,3]) * fInvW;
            r.Y = (double)(m.Matrix[1,0] * v.X + m.Matrix[1,1] * v.Y + m.Matrix[1,2] * v.Z + m.Matrix[1,3]) * fInvW;
            r.Z = (double)(m.Matrix[2,0] * v.X + m.Matrix[2,1] * v.Y + m.Matrix[2,2] * v.Z + m.Matrix[2,3]) * fInvW;

            return r;
        }

        /*public static Vector4 operator* (Matrix4 m, Vector4 v)
        {
            return Vector4(
                m.Matrix[0,0] * v.x + m.Matrix[0,1] * v.y + m.Matrix[0,2] * v.z + m.Matrix[0,3] * v.w, 
                m.Matrix[1,0] * v.x + m.Matrix[1,1] * v.y + m.Matrix[1,2] * v.z + m.Matrix[1,3] * v.w,
                m.Matrix[2,0] * v.x + m.Matrix[2,1] * v.y + m.Matrix[2,2] * v.z + m.Matrix[2,3] * v.w,
                m.Matrix[3,0] * v.x + m.Matrix[3,1] * v.y + m.Matrix[3,2] * v.z + m.Matrix[3,3] * v.w
                );
        }

        public static Plane operator* (Matrix4 m,Plane p)
        {
            Plane ret;
            Matrix4 invTrans = inverse().transpose();
            Vector4 v4( p.normal.x, p.normal.y, p.normal.z, p.d );
            v4 = invTrans * v4;
            ret.normal.x = v4.x; 
            ret.normal.y = v4.y; 
            ret.normal.z = v4.z;
            ret.d = v4.w / ret.normal.normalise();

            return ret;
        }*/

        public static Matrix4 operator+ (Matrix4 a, Matrix4 b)
        {
            Matrix4 r = new Matrix4();

            r.Matrix = a.Matrix + b.Matrix;

            return r;
        }

        public static Matrix4 operator- (Matrix4 a, Matrix4 b)
        {
            Matrix4 r = new Matrix4();

            r.Matrix = a.Matrix - b.Matrix;

            return r;
        }

        /// <summary>
        /// Sets the translation transformation part of the matrix.
        /// </summary>
        public void setTransform(Vector3 v)
        {
            Matrix[0,3] = v.X;
            Matrix[1,3] = v.Y;
            Matrix[2,3] = v.Z;
        }

        /// <summary>
        /// Gets the translation transformation part of the matrix.
        /// </summary>
        public Vector3 getTransform()
        {
            return new Vector3((double)Matrix[0, 3], (double)Matrix[1, 3], (double)Matrix[2, 3]);
        }

        /// <summary>
        /// Builds a translation matrix
        /// </summary>
        public void makeTransform(Vector3 v)
        {
            Matrix = ILMath.eye(4, 4); 
            Matrix[0,3] = v.X;
            Matrix[1,3] = v.Y;
            Matrix[2,3] = v.Z;
        }

        /// <summary>
        /// Builds a translation matrix
        /// </summary>
        public void makeTransform(double tx, double ty, double tz)
        {
            Matrix = ILMath.eye(4, 4); 
            Matrix[0,3] = tx;
            Matrix[1,3] = ty;
            Matrix[2,3] = tz;
        }

        /// <summary>
        /// Creates a x-rotation matrix.
        /// </summary>
        public static Matrix4 makeRotationX(double ax)
        {
            Matrix4 m = Identity();

            m.Matrix[1, 1] = ILMath.cos(-ax);   m.Matrix[1, 2] = -ILMath.sin(-ax);
            m.Matrix[2, 1] = ILMath.sin(-ax);   m.Matrix[2, 2] = ILMath.cos(-ax); 

            return m;
        }

        /// <summary>
        /// Creates a x-rotation matrix.
        /// </summary>
        public static Matrix4 makeRotationY(double ay)
        {
            Matrix4 m = Identity();

            m.Matrix[0, 0] = ILMath.cos(-ay);   m.Matrix[0, 2] = ILMath.sin(-ay);
            m.Matrix[2, 0] = ILMath.sin(-ay);   m.Matrix[2, 2] = ILMath.cos(-ay);

            return m;
        }

        /// <summary>
        /// Creates a x-rotation matrix.
        /// </summary>
        public static Matrix4 makeRotationZ(double az)
        {
            Matrix4 m = Identity();

            m.Matrix[0, 0] = ILMath.cos(-az);   m.Matrix[0, 1] = -ILMath.sin(-az);
            m.Matrix[1, 0] = ILMath.sin(-az);   m.Matrix[1, 1] = ILMath.cos(-az);

            return m;
        }

        /// <summary>
        /// Gets a translation matrix.
        /// </summary>
        public static Matrix4 getTransform(Vector3 v)
        {
            Matrix4 r = Identity();

            r.Matrix[0,3] = v.X;
            r.Matrix[1,3] = v.Y;
            r.Matrix[2,3] = v.Z;

            return r;
        }

        /// <summary>
        /// Gets a translation matrix - variation for not using a vector.
        /// </summary>
        public static Matrix4 getTransform(double tx, double ty, double tz)
        {
            Matrix4 r = Identity();

            r.Matrix[0,3] = tx;
            r.Matrix[1,3] = ty;
            r.Matrix[2,3] = tz;

            return r;
        }

        /// <summary>
        /// Gets a translation matrix - variation for not using a vector.
        /// </summary>
        public static Matrix4 Transpose(Matrix4 m)
        {
            Matrix4 r = Identity();

            r.Matrix = m.Matrix.T;
             
            return r;
        }

        /// <summary>
        /// Sets the scale part of the matrix.
        /// </summary>
        public void setScale(Vector3 v)
        {
            Matrix[0,0] = v.X;
            Matrix[1,1] = v.Y;
            Matrix[2,2] = v.Z;
        }

        /// <summary>
        /// Gets a scale matrix.
        /// </summary>
        public static Matrix4 getScale(Vector3 v)
        {
            Matrix4 r = Identity();
            r.setScale(v);

            return r;
        }

        /// <summary>
        /// Gets a scale matrix - variation for not using a vector.
        /// </summary>
        public static Matrix4 getScale(double s_x, double s_y, double s_z)
        {
            Matrix4 r = Identity();
            r.setScale(new Vector3( s_x, s_y, s_z));

            return r;
        }

        /// <summary>
        /// Extracts the rotation / scaling part of the Matrix as a 3x3 matrix.
        /// </summary>
        public Matrix3 extract3x3Matrix()
        {
            Matrix3 m3x3 = new Matrix3();
            m3x3.Matrix = Matrix[ILMath.r(0,2), ILMath.r(0,2)];
            return m3x3;
        }

        /// <summary>
        /// Determines if this matrix involves a scaling.
        /// </summary>
        public bool hasScale()
        {
            // check magnitude of column vectors (==local axes)
            double t = (double)(Matrix[0,0] * Matrix[0,0] + Matrix[1,0] * Matrix[1,0] + Matrix[2,0] * Matrix[2,0]);
            if (!(ILMath.abs(t - 1.0) <= 1e-04))
                    return true;
            t = (double)(Matrix[0,1] * Matrix[0,1] + Matrix[1,1] * Matrix[1,1] + Matrix[2,1] * Matrix[2,1]);
            if (!(ILMath.abs(t - 1.0) <= 1e-04))
                    return true;
            t = (double)(Matrix[0,2] * Matrix[0,2] + Matrix[1,2] * Matrix[1,2] + Matrix[2,2] * Matrix[2,2]);
            if (!(ILMath.abs(t - 1.0) <= 1e-04))
                    return true;

            return false;
        }

        /// <summary>
        /// Determines if this matrix involves a negative scaling.
        /// </summary>
        public bool hasNegativeScale()
        {
            return ILMath.det(this.Matrix) < 0;
        }

        /// <summary>
        /// Extracts the rotation / scaling part as a quaternion from the Matrix.
        /// </summary>
        public Quaternion extractQuaternion()
        {
          return new Quaternion(extract3x3Matrix());
        }

        /// <summary>
        /// Converts Trasformation/Rotation matrix to a dual-quaternion.
        /// </summary>
        public DualQuaternion extractDualQuaternion()
        {
            return new DualQuaternion(extractQuaternion(), getTransform());
        }
    }
}
