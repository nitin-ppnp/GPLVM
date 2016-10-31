using ILNumerics;

namespace GPLVM
{
    public class Matrix3 : RotationType
    {
        private RotType _rotType = RotType.matrix3;
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

        public Matrix3()
        {
            rotm = rotm = ILMath.zeros(3, 3);
        }

        public Matrix3(double e00, double e01, double e02, double e10, double e11, double e12, double e20, double e21, double e22)
        {
            rotm = ILMath.zeros(3,3);

            rotm[0,0] = e00;
            rotm[0,1] = e01;
            rotm[0,2] = e02;
            rotm[1,0] = e10;
            rotm[1,1] = e11;
            rotm[1,2] = e12;
            rotm[2,0] = e20;
            rotm[2,1] = e21;
            rotm[2,2] = e22;
        }
        

        public void FromEulerAnglesXYZ(Radian fYAngle, Radian fPAngle, Radian fRAngle)
        {
            double fCos, fSin;

            fCos = (double)ILMath.cos(fYAngle.Value);
            fSin = (double)ILMath.sin(fYAngle.Value);
            Matrix3 kXMat = new Matrix3(1.0,0.0,0.0,0.0,fCos,-fSin,0.0,fSin,fCos);

            fCos = (double)ILMath.cos(fPAngle.Value);
            fSin = (double)ILMath.sin(fPAngle.Value);
            Matrix3 kYMat = new Matrix3(fCos,0.0,fSin,0.0,1.0,0.0,-fSin,0.0,fCos);

            fCos = (double)ILMath.cos(fRAngle.Value);
            fSin = (double)ILMath.sin(fRAngle.Value);
            Matrix3 kZMat = new Matrix3(fCos,-fSin,0.0,fSin,fCos,0.0,0.0,0.0,1.0);

            rotm = ILMath.multiply(kXMat.Matrix, ILMath.multiply(kYMat.Matrix, kZMat.Matrix));
        }

        public void FromEulerAnglesXZY (Radian fYAngle, Radian fPAngle, Radian fRAngle)
        {
            double fCos, fSin;

            fCos = (double)ILMath.cos(fYAngle.Value);
            fSin = (double)ILMath.sin(fYAngle.Value);
            Matrix3 kXMat = new Matrix3(1.0,0.0,0.0,0.0,fCos,-fSin,0.0,fSin,fCos);

            fCos = (double)ILMath.cos(fPAngle.Value);
            fSin = (double)ILMath.sin(fPAngle.Value);
            Matrix3 kZMat = new Matrix3(fCos,-fSin,0.0,fSin,fCos,0.0,0.0,0.0,1.0);

            fCos = (double)ILMath.cos(fRAngle.Value);
            fSin = (double)ILMath.sin(fRAngle.Value);
            Matrix3 kYMat = new Matrix3(fCos,0.0,fSin,0.0,1.0,0.0,-fSin,0.0,fCos);

            rotm = ILMath.multiply(kXMat.Matrix, ILMath.multiply(kZMat.Matrix, kYMat.Matrix));
        }

        public void FromEulerAnglesYXZ (Radian fYAngle, Radian fPAngle, Radian fRAngle)
        {
            double fCos, fSin;

            fCos = (double)ILMath.cos(fYAngle.Value);
            fSin = (double)ILMath.sin(fYAngle.Value);
            Matrix3 kYMat = new Matrix3(fCos,0.0,fSin,0.0,1.0,0.0,-fSin,0.0,fCos);

            fCos = (double)ILMath.cos(fPAngle.Value);
            fSin = (double)ILMath.sin(fPAngle.Value);
            Matrix3 kXMat = new Matrix3(1.0,0.0,0.0,0.0,fCos,-fSin,0.0,fSin,fCos);

            fCos = (double)ILMath.cos(fRAngle.Value);
            fSin = (double)ILMath.sin(fRAngle.Value);
            Matrix3 kZMat = new Matrix3(fCos,-fSin,0.0,fSin,fCos,0.0,0.0,0.0,1.0);

            rotm = ILMath.multiply(kYMat.Matrix, ILMath.multiply(kXMat.Matrix, kZMat.Matrix));
        }
        //-----------------------------------------------------------------------
        public void FromEulerAnglesYZX (Radian fYAngle, Radian fPAngle, Radian fRAngle)
        {
            double fCos, fSin;

            fCos = (double)ILMath.cos(fYAngle.Value);
            fSin = (double)ILMath.sin(fYAngle.Value);
            Matrix3 kYMat = new Matrix3(fCos,0.0,fSin,0.0,1.0,0.0,-fSin,0.0,fCos);

            fCos = (double)ILMath.cos(fPAngle.Value);
            fSin = (double)ILMath.sin(fPAngle.Value);
            Matrix3 kZMat = new Matrix3(fCos,-fSin,0.0,fSin,fCos,0.0,0.0,0.0,1.0);

            fCos = (double)ILMath.cos(fRAngle.Value);
            fSin = (double)ILMath.sin(fRAngle.Value);
            Matrix3 kXMat = new Matrix3(1.0,0.0,0.0,0.0,fCos,-fSin,0.0,fSin,fCos);

            rotm = ILMath.multiply(kYMat.Matrix, ILMath.multiply(kZMat.Matrix, kXMat.Matrix));
        }
        //-----------------------------------------------------------------------
        public void FromEulerAnglesZXY (Radian fYAngle, Radian fPAngle, Radian fRAngle)
        {
            double fCos, fSin;

            fCos = (double)ILMath.cos(fYAngle.Value);
            fSin = (double)ILMath.sin(fYAngle.Value);
            Matrix3 kZMat = new Matrix3(fCos,-fSin,0.0,fSin,fCos,0.0,0.0,0.0,1.0);

            fCos = (double)ILMath.cos(fPAngle.Value);
            fSin = (double)ILMath.sin(fPAngle.Value);
            Matrix3 kXMat = new Matrix3(1.0,0.0,0.0,0.0,fCos,-fSin,0.0,fSin,fCos);

            fCos = (double)ILMath.cos(fRAngle.Value);
            fSin = (double)ILMath.sin(fRAngle.Value);
            Matrix3 kYMat = new Matrix3(fCos,0.0,fSin,0.0,1.0,0.0,-fSin,0.0,fCos);

            rotm = ILMath.multiply(kZMat.Matrix, ILMath.multiply(kXMat.Matrix, kYMat.Matrix));
        }
        //-----------------------------------------------------------------------
        public void FromEulerAnglesZYX (Radian fYAngle, Radian fPAngle, Radian fRAngle)
        {
            double fCos, fSin;

            fCos = (double)ILMath.cos(fYAngle.Value);
            fSin = (double)ILMath.sin(fYAngle.Value);
            Matrix3 kZMat = new Matrix3(fCos,-fSin,0.0,fSin,fCos,0.0,0.0,0.0,1.0);

            fCos = (double)ILMath.cos(fPAngle.Value);
            fSin = (double)ILMath.sin(fPAngle.Value);
            Matrix3 kYMat = new Matrix3(fCos,0.0,fSin,0.0,1.0,0.0,-fSin,0.0,fCos);

            fCos = (double)ILMath.cos(fRAngle.Value);
            fSin = (double)ILMath.sin(fRAngle.Value);
            Matrix3 kXMat = new Matrix3(1.0, 0.0, 0.0, 0.0, fCos, -fSin, 0.0, fSin, fCos);

            rotm = ILMath.multiply(kZMat.Matrix, ILMath.multiply(kYMat.Matrix, kXMat.Matrix));
        }

        public bool ToEulerAnglesXYZ (ref Radian rfYAngle, ref Radian rfPAngle, ref Radian rfRAngle)
        {
            // rot = cy*cz -cy*sz sy
            // cz*sx*sy+cx*sz cx*cz-sx*sy*sz -cy*sx
            // -cx*cz*sy+sx*sz cz*sx+cx*sy*sz cx*cy

            rfPAngle = new Radian((double)ILMath.asin(rotm[0,2]));
            if ( rfPAngle.Value < (double)(ILMath.pi * 0.5))
            {
                if ( rfPAngle.Value > (double)(-ILMath.pi * 0.5))
                {
                    rfYAngle.Value = (double)ILMath.atan2(-rotm[1,2], rotm[2,2]);
                    rfRAngle.Value = (double)ILMath.atan2(-rotm[0,1], rotm[0,0]);
                    return true;
                }
                else
                {
                    // WARNING. Not a unique solution.
                    Radian fRmY = new Radian((double)ILMath.atan2(rotm[1,0], rotm[1,1]));
                    rfRAngle.Value = 0.0; // any angle works
                    rfYAngle.Value = rfRAngle.Value - fRmY.Value;
                    return false;
                }
            }
            else
            {
                // WARNING. Not a unique solution.
                Radian fRpY = new Radian((double)ILMath.atan2(rotm[1,0], rotm[1,1]));
                rfRAngle.Value = 0.0; // any angle works
                rfYAngle.Value = fRpY.Value - rfRAngle.Value;
                return false;
            }
        }

        //-----------------------------------------------------------------------
        public bool ToEulerAnglesXZY (ref Radian rfYAngle, ref Radian rfPAngle, ref Radian rfRAngle)
        {
            // rot = cy*cz -sz cz*sy
            // sx*sy+cx*cy*sz cx*cz -cy*sx+cx*sy*sz
            // -cx*sy+cy*sx*sz cz*sx cx*cy+sx*sy*sz

            rfPAngle.Value = (double)ILMath.asin(-rotm[0,1]);
            if ( rfPAngle.Value < ILMath.pi * 0.5)
            {
                if ( rfPAngle.Value > -ILMath.pi * 0.5)
                {
                    rfYAngle.Value = (double)ILMath.atan2(rotm[2,1], rotm[1,1]);
                    rfRAngle.Value = (double)ILMath.atan2(rotm[0,2], rotm[0,0]);
                    return true;
                }
                else
                {
                    // WARNING. Not a unique solution.
                    Radian fRmY = new Radian((double)ILMath.atan2(-rotm[2,0], rotm[2,2]));
                    rfRAngle.Value = 0.0; // any angle works
                    rfYAngle.Value = rfRAngle.Value - fRmY.Value;
                    return false;
                }
            }
            else
            {
                // WARNING. Not a unique solution.
                Radian fRpY = new Radian((double)ILMath.atan2(-rotm[2,0], rotm[2,2]));
                rfRAngle.Value = 0.0; // any angle works
                rfYAngle.Value = fRpY.Value - rfRAngle.Value;
                return false;
            }
        }

        //-----------------------------------------------------------------------
        public bool ToEulerAnglesYXZ (ref Radian rfYAngle, ref Radian rfPAngle, ref Radian rfRAngle)
        {
            // rot = cy*cz+sx*sy*sz cz*sx*sy-cy*sz cx*sy
            // cx*sz cx*cz -sx
            // -cz*sy+cy*sx*sz cy*cz*sx+sy*sz cx*cy

            rfPAngle.Value = (double)ILMath.asin(-rotm[1,2]);
            if ( rfPAngle.Value < ILMath.pi * 0.5)
            {
                if ( rfPAngle.Value > -ILMath.pi * 0.5)
                {
                    rfYAngle.Value = (double)ILMath.atan2(rotm[0,2],rotm[2,2]);
                    rfRAngle.Value = (double)ILMath.atan2(rotm[1,0],rotm[1,1]);
                    return true;
                }
                else
                {
                    // WARNING. Not a unique solution.
                    Radian fRmY = new Radian((double)ILMath.atan2(-rotm[0,1],rotm[0,0]));
                    rfRAngle.Value = 0.0; // any angle works
                    rfYAngle.Value = rfRAngle.Value - fRmY.Value;
                    return false;
                }
            }
            else
            {
                // WARNING. Not a unique solution.
                Radian fRpY = new Radian((double)ILMath.atan2(-rotm[0,1],rotm[0,0]));
                rfRAngle.Value = 0.0; // any angle works
                rfYAngle.Value = fRpY.Value - rfRAngle.Value;
                return false;
            }
        }

        //-----------------------------------------------------------------------
        public bool ToEulerAnglesYZX (ref Radian rfYAngle, ref Radian rfPAngle, ref Radian rfRAngle)
        {
            // rot = cy*cz sx*sy-cx*cy*sz cx*sy+cy*sx*sz
            // sz cx*cz -cz*sx
            // -cz*sy cy*sx+cx*sy*sz cx*cy-sx*sy*sz

            rfPAngle.Value = (double)ILMath.asin(rotm[1,0]);
            if ( rfPAngle.Value < ILMath.pi * 0.5)
            {
                if ( rfPAngle.Value > -ILMath.pi * 0.5)
                {
                    rfYAngle.Value = (double)ILMath.atan2(-rotm[2,0],rotm[0,0]);
                    rfRAngle.Value = (double)ILMath.atan2(-rotm[1,2],rotm[1,1]);
                    return true;
                }
                else
                {
                    // WARNING. Not a unique solution.
                    Radian fRmY = new Radian((double)ILMath.atan2(rotm[2,1],rotm[2,2]));
                    rfRAngle.Value = 0.0; // any angle works
                    rfYAngle.Value = rfRAngle.Value - fRmY.Value;
                    return false;
                }
            }
            else
            {
                // WARNING. Not a unique solution.
                Radian fRpY = new Radian((double)ILMath.atan2(rotm[2,1],rotm[2,2]));
                rfRAngle.Value = 0.0; // any angle works
                rfYAngle.Value = fRpY.Value - rfRAngle.Value;
                return false;
            }
        }
        //-----------------------------------------------------------------------
        public bool ToEulerAnglesZXY (ref Radian rfYAngle, ref Radian rfPAngle, ref Radian rfRAngle)
        {
            // rot = cy*cz-sx*sy*sz -cx*sz cz*sy+cy*sx*sz
            // cz*sx*sy+cy*sz cx*cz -cy*cz*sx+sy*sz
            // -cx*sy sx cx*cy

            rfPAngle.Value = (double)ILMath.asin(rotm[2,1]);
            if ( rfPAngle.Value < ILMath.pi * 0.5)
            {
                if ( rfPAngle.Value > (-ILMath.pi * 0.5) )
                {
                    rfYAngle.Value = (double)ILMath.atan2(-rotm[0,1],rotm[1,1]);
                    rfRAngle.Value = (double)ILMath.atan2(-rotm[2,0],rotm[2,2]);
                    return true;
                }
                else
                {
                    // WARNING. Not a unique solution.
                    Radian fRmY = new Radian((double)ILMath.atan2(rotm[0,2],rotm[0,0]));
                    rfRAngle.Value = 0.0; // any angle works
                    rfYAngle.Value = rfRAngle.Value - fRmY.Value;
                    return false;
                }
            }
            else
            {
                // WARNING. Not a unique solution.
                Radian fRpY = new Radian((double)ILMath.atan2(rotm[0,2],rotm[0,0]));
                rfRAngle.Value = 0.0; // any angle works
                rfYAngle.Value = fRpY.Value - rfRAngle.Value;
                return false;
            }
        }
        //-----------------------------------------------------------------------
        public bool ToEulerAnglesZYX (ref Radian rfYAngle, ref Radian rfPAngle, ref Radian rfRAngle)
        {
            // rot = cy*cz cz*sx*sy-cx*sz cx*cz*sy+sx*sz
            // cy*sz cx*cz+sx*sy*sz -cz*sx+cx*sy*sz
            // -sy cy*sx cx*cy

            rfPAngle.Value = (double)ILMath.asin(-rotm[2,0]);
            if ( rfPAngle.Value < ILMath.pi * 0.5 )
            {
                if ( rfPAngle.Value > (-ILMath.pi * 0.5) )
                {
                    rfYAngle.Value = (double)ILMath.atan2(rotm[1,0],rotm[0,0]);
                    rfRAngle.Value = (double)ILMath.atan2(rotm[2,1],rotm[2,2]);
                    return true;
                }
                else
                {
                    // WARNING. Not a unique solution.
                    Radian fRmY = new Radian((double)ILMath.atan2(-rotm[0,1],rotm[0,2]));
                    rfRAngle.Value = 0.0; // any angle works
                    rfYAngle.Value = rfRAngle.Value - fRmY.Value;
                    return false;
                }
            }
            else
            {
                // WARNING. Not a unique solution.
                Radian fRpY = new Radian((double)ILMath.atan2(-rotm[0,1],rotm[0,2]));
                rfRAngle.Value = 0.0; // any angle works
                rfYAngle.Value = fRpY.Value - rfRAngle.Value;
                return false;
            }
        }

        //-----------------------------------------------------------------------
        public static Matrix3 operator+ (Matrix3 a, Matrix3 b)
        {
            Matrix3 kSum = new Matrix3();
            
            kSum.Matrix = a.Matrix + b.Matrix;

            return kSum;
        }

        //-----------------------------------------------------------------------
        public static Matrix3 operator- (Matrix3 a, Matrix3 b)
        {
            Matrix3 kDiff = new Matrix3();
            
            kDiff.Matrix = a.Matrix - b.Matrix;

            return kDiff;
        }

        //-----------------------------------------------------------------------
        public static Matrix3 operator* (Matrix3 a, Matrix3 b)
        {
            Matrix3 kProd = new Matrix3();
            kProd.Matrix = ILMath.multiply(a.Matrix, b.Matrix);
            return kProd;
        }

        //-----------------------------------------------------------------------
        public static Vector3 operator* (Matrix3 a, Vector3 b)
        {
            Vector3 kProd = new Vector3();
            kProd.Vector = ILMath.multiply(a.Matrix, b.Vector);
            return kProd;
        }

        //-----------------------------------------------------------------------
        public static Vector3 operator* (Vector3 a, Matrix3 b)
        {
            Vector3 kProd = new Vector3();
            kProd.Vector = ILMath.multiply(a.Vector.T, b.Matrix);
            return kProd;
        }

        //-----------------------------------------------------------------------
        public static Matrix3 operator- (Matrix3 a)
        {
            Matrix3 kNeg = new Matrix3();
            kNeg.Matrix = -a.Matrix;
            return kNeg;
        }

        //-----------------------------------------------------------------------
        public static Matrix3 operator* (Matrix3 a, double fScalar)
        {
            Matrix3 kProd = new Matrix3();
            kProd.Matrix = a.Matrix * fScalar;
            return kProd;
        }

        //-----------------------------------------------------------------------
        public static Matrix3 operator* (double fScalar, Matrix3 a)
        {
            Matrix3 kProd = new Matrix3();
            kProd.Matrix = a.Matrix * fScalar;
            return kProd;
        }
    }
}
