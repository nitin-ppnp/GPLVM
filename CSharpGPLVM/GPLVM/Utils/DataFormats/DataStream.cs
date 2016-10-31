using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GPLVM.Utils.Character;
using GPLVM;
using ILNumerics;

namespace DataFormats
{
    public class TreeEntry
    {
        public int NODE_ID;
        public int PARENT_ID;
        public short NAME_LEN;
        public string NAME;

        public TreeEntry()
        {
            NODE_ID = 0;
            PARENT_ID = 0;
            NAME_LEN = 0;
            NAME = null;
        }

        public int byteSize()
        {
            return 2 * sizeof(int) + sizeof(short) + NAME_LEN * sizeof(byte);
        }

        public int readFromByte(byte[] pBuffer, int index)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            byte[] tmpBuffer = new byte[sizeof(int)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(int));
            NODE_ID = (int)DataStream.FromByteToType(tmpBuffer, typeof(int));
            index += sizeof(int);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(int));
            PARENT_ID = (int)DataStream.FromByteToType(tmpBuffer, typeof(int));
            index += sizeof(int);

            tmpBuffer = new byte[sizeof(short)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(short));
            NAME_LEN = (short)DataStream.FromByteToType(tmpBuffer, typeof(short));
            index += sizeof(short);

            tmpBuffer = new byte[NAME_LEN - 1];
            Array.Copy(pBuffer, index, tmpBuffer, 0, NAME_LEN - 1);
            NAME = enc.GetString(tmpBuffer);
            index += NAME_LEN;

            return index;
        }

        public int writeToByte(ref byte[] pBuffer, int index)
        {
            byte[] tmpBuffer;
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            tmpBuffer = DataStream.ToByteArray(NODE_ID);
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            tmpBuffer = DataStream.ToByteArray(PARENT_ID);
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            tmpBuffer = DataStream.ToByteArray(NAME_LEN);
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            tmpBuffer = enc.GetBytes(NAME + "\0");
            Array.Copy(tmpBuffer, 0, pBuffer, index, NAME_LEN);
            index += NAME_LEN;

            return index;
        }
    }

    public class Tree
    {
        public byte PID;
        public int NUM_ENTRIES;
        public TreeEntry[] ENTRIES;

        public Tree()
        {
            PID = 1;
            NUM_ENTRIES = 0;
            ENTRIES = null;
        }

        public int byteSize()
        {
            int lSum = 0;
            for (int i = 0; i < NUM_ENTRIES; i++)
                lSum += ENTRIES[i].byteSize();
            return sizeof(byte) + 2 * sizeof(int) + lSum;
        }

        public byte[] readFromByte(byte[] pBuffer)
		{
            int index = 0;
            byte[] tmpBuffer = new byte[sizeof(byte)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(byte));
            PID = (byte)DataStream.FromByteToType(tmpBuffer, typeof(byte));
            index += sizeof(byte);

            /* PACKAGE SIZE */
            index += sizeof(int);

            tmpBuffer = new byte[sizeof(int)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(int));
            NUM_ENTRIES = (int)DataStream.FromByteToType(tmpBuffer, typeof(int));
            index += sizeof(int);

			if (ENTRIES != null) ENTRIES = null;
			ENTRIES = new TreeEntry[NUM_ENTRIES];

            for (int i = 0; i < NUM_ENTRIES; i++)
            {
                ENTRIES[i] = new TreeEntry();
                index = ENTRIES[i].readFromByte(pBuffer, index);
            }

            return pBuffer;
		}

        public byte[] writeToByte(byte[] pBuffer)
        {
            int index = 0;
            byte[] tmpBuffer;

            pBuffer[0] = PID;
            index += sizeof(byte);

            tmpBuffer = DataStream.ToByteArray(byteSize());
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            tmpBuffer = DataStream.ToByteArray(NUM_ENTRIES);
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            for (int i = 0; i < NUM_ENTRIES; i++)
                index = ENTRIES[i].writeToByte(ref pBuffer, index);

            return pBuffer;
        }

        public void CreateStreamTree(Skeleton skeleton)
        {
            this.ENTRIES = new TreeEntry[skeleton.Joints.Count];
            this.NUM_ENTRIES = skeleton.Joints.Count;
            int cnt = 0;
            foreach (Joint entry in skeleton.Joints)
            {
                this.ENTRIES[cnt] = new TreeEntry();
                this.ENTRIES[cnt].NAME = entry.Name;
                this.ENTRIES[cnt].NODE_ID = entry.ID;
                this.ENTRIES[cnt].PARENT_ID = entry.ParentID;
                this.ENTRIES[cnt].NAME_LEN = (short)(entry.Name.Length + 1);
                cnt++;
            }
        }
    }

    public class FrameEntry
    {
        public int NODE_ID;
        public double[] TRANSLATION;    // x,y,z
        public double[] QUATERNION;     // w,x,y,z
        public double[] SCALE;

        public FrameEntry()
        {
            NODE_ID = 0;
            TRANSLATION = new double[3];    // x,y,z
            QUATERNION = new double[4];     // w,x,y,z
            SCALE = new double[3] {1, 1, 1};
        }

        public int byteSize()
        {
            return 1 * sizeof(int) + 10 * sizeof(double);
        }

        public void writeToByte(ref byte[] pBuffer, ref int index)
        {
            byte[] tmpBuffer;
            tmpBuffer = DataStream.ToByteArray(NODE_ID);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(int));
            index += sizeof(int);

            // Translation
            tmpBuffer = DataStream.ToByteArray(TRANSLATION[0]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(TRANSLATION[1]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(TRANSLATION[2]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            // Scale
            tmpBuffer = DataStream.ToByteArray(SCALE[0]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(SCALE[1]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(SCALE[2]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            // Quaternion
            tmpBuffer = DataStream.ToByteArray(QUATERNION[0]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(QUATERNION[1]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(QUATERNION[2]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);

            tmpBuffer = DataStream.ToByteArray(QUATERNION[3]);
            Array.Copy(tmpBuffer, 0, pBuffer, index, sizeof(double));
            index += sizeof(double);
        }

        public void readFromByte(byte[] pBuffer, ref int index)
        {
            byte[] tmpBuffer = new byte[sizeof(int)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(int));
            NODE_ID = (int)DataStream.FromByteToType(tmpBuffer, typeof(int));
            index += sizeof(int);

            // Translation
            tmpBuffer = new byte[sizeof(double)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            TRANSLATION[0] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            TRANSLATION[1] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            TRANSLATION[2] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            // Scale
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            SCALE[0] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            SCALE[1] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            SCALE[2] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            // Quaternion
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            QUATERNION[0] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            QUATERNION[1] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            QUATERNION[2] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(double));
            QUATERNION[3] = (double)DataStream.FromByteToType(tmpBuffer, typeof(double));
            index += sizeof(double);
        }
    }

    public class Frame
    {
        public byte PID;
        public int TIMESTAMP;
        public int NUM_ENTRIES;
        public FrameEntry[] ENTRIES;

        public Frame()
        {
            PID = 2;
            TIMESTAMP = 0;
            NUM_ENTRIES = 0;
            ENTRIES = null;
        }

        public int byteSize()
        {
            int lSum = 0;
            for (int i = 0; i < NUM_ENTRIES; i++)
                lSum += ENTRIES[i].byteSize();
            return sizeof(byte) + 3 * sizeof(int) + lSum;
        }

        public byte[] readFromByte(byte[] pBuffer)
        {
            int index = 0;
            PID = pBuffer[0];
            index += sizeof(byte);

            /* PACKAGE SIZE */
            index += sizeof(int);

            byte[] tmpBuffer = new byte[sizeof(int)];
            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(int));
            TIMESTAMP = (int)DataStream.FromByteToType(tmpBuffer, typeof(int));
            index += sizeof(int);

            Array.Copy(pBuffer, index, tmpBuffer, 0, sizeof(int));
            NUM_ENTRIES = (int)DataStream.FromByteToType(tmpBuffer, typeof(int));
            index += sizeof(int);

            if (ENTRIES != null) ENTRIES = null;
            ENTRIES = new FrameEntry[NUM_ENTRIES];

            for (int i = 0; i < NUM_ENTRIES; i++)
            {
                ENTRIES[i] = new FrameEntry();
                ENTRIES[i].readFromByte(pBuffer, ref index);
            }

            return pBuffer;
        }

        public byte[] writeToByte(byte[] pBuffer)
        {
            int index = 0;
            byte[] tmpBuffer;

            pBuffer[0] = PID;
            index += sizeof(byte);

            tmpBuffer = DataStream.ToByteArray(byteSize());
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            tmpBuffer = DataStream.ToByteArray(TIMESTAMP);
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            tmpBuffer = DataStream.ToByteArray(NUM_ENTRIES);
            Array.Copy(tmpBuffer, 0, pBuffer, index, tmpBuffer.Length);
            index += tmpBuffer.Length;

            for (int i = 0; i < NUM_ENTRIES; i++)
                ENTRIES[i].writeToByte(ref pBuffer, ref index);

            return pBuffer;
        }

        public void CreateStreamFrame(ILArray<double> inFrameAngles, Skeleton skeleton, Representation type)
        {
            this.NUM_ENTRIES = skeleton.Joints.Count;
            this.ENTRIES = new FrameEntry[this.NUM_ENTRIES];

            ILArray<double> ind;

            for (int i = 0; i < skeleton.Joints.Count; i++)
            {
                this.ENTRIES[i] = new FrameEntry();
                this.ENTRIES[i].NODE_ID = skeleton.Joints[i].ID;

                if (type == Representation.quaternion)
                {
                    ind = new double[4];
                    int jntIdx = (int)(((skeleton.Joints[i].rotInd[0] - 3) / 3) * 4 + 4) - 1;
                    ind[0] = jntIdx;
                    ind[1] = jntIdx + 1;
                    ind[2] = jntIdx + 2;
                    ind[3] = jntIdx + 3;
                }
                else
                {
                    ind = new double[3];
                    ind[0] = (int)skeleton.Joints[i].rotInd[0];
                    ind[1] = (int)skeleton.Joints[i].rotInd[1];
                    ind[2] = (int)skeleton.Joints[i].rotInd[2];
                }

                if (i == 0)
                {
                    this.ENTRIES[i].TRANSLATION[0] = (double)inFrameAngles[0, 0];
                    this.ENTRIES[i].TRANSLATION[1] = (double)inFrameAngles[0, 1];
                    this.ENTRIES[i].TRANSLATION[2] = (double)inFrameAngles[0, 2];
                }

                switch (type)
                {
                    case Representation.exponential:
                        this.ENTRIES[i].QUATERNION = dbExpToQuat(inFrameAngles[ind]);
                        break;
                    case Representation.quaternion:
                        this.ENTRIES[i].QUATERNION = dbQuaternion(inFrameAngles[ind]);
                        break;
                    case Representation.radian:
                        this.ENTRIES[i].QUATERNION = dbEulerToQuaternion(inFrameAngles[ind], skeleton.Joints[i].order);
                        break;
                }   
            }
        }

        private double[] dbEulerToQuaternion(ILInArray<double> radAngles, string order)
        {
            using (ILScope.Enter(radAngles))
            {
                ILArray<double> angles = ILMath.check(radAngles);

                Matrix3 rotMatrix = Util.GetRotationMatrix(radAngles, order);

                Quaternion quat = new Quaternion();
                quat.FromRotationMatrix(rotMatrix);

                double[] retQuaternion = new double[4];

                retQuaternion[0] = quat.w;
                retQuaternion[1] = quat.x;
                retQuaternion[2] = quat.y;
                retQuaternion[3] = quat.z;

                return retQuaternion;
            }
        }

        private double[] dbExpToQuat(ILInArray<double> inFrameAngles)
        {
            using (ILScope.Enter(inFrameAngles))
            {
                ILArray<double> angles = ILMath.check(inFrameAngles);
                double[] quat = new double[4];

                Quaternion q = Util.expToQuat(inFrameAngles);

                quat[0] = (double)q.w;
                quat[1] = (double)q.x;
                quat[2] = (double)q.y;
                quat[3] = (double)q.z;

                return quat;
            }
        }

        private double[] dbQuaternion(ILInArray<double> inFrameAngles)
        {
            using (ILScope.Enter(inFrameAngles))
            {
                ILArray<double> angles = ILMath.check(inFrameAngles);
                double[] quat = new double[4];

                Quaternion q = new Quaternion((double)inFrameAngles[3], (double)inFrameAngles[0], (double)inFrameAngles[1], (double)inFrameAngles[2]);

                quat[0] = (double)q.w;
                quat[1] = (double)q.x;
                quat[2] = (double)q.y;
                quat[3] = (double)q.z;

                return quat;
            }
        }
    }

    

    public static class DataStream
    {
        public static byte[] ToByteArray(object anyObject)
        {
            int rawsize = Marshal.SizeOf(anyObject);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(anyObject, buffer, false);
            byte[] streamdatas = new byte[rawsize];
            Marshal.Copy(buffer, streamdatas, 0, rawsize);
            Marshal.FreeHGlobal(buffer);
            return streamdatas;
        }

        public static Object FromByteToType(byte[] rawdatas, Type type)
        {
            Object res;
            int rawsize = Marshal.SizeOf(type);
            //if (rawsize > rawdatas.Length) return null;
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.Copy(rawdatas, 0, buffer, rawsize);
            res = Marshal.PtrToStructure(buffer, type);
            Marshal.FreeHGlobal(buffer);
            return res;
        }

        public static Tree CreateStructTree(List<BVHNode> tree)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            Tree structTree = new Tree();
            structTree.ENTRIES = new TreeEntry[tree.Count];
            structTree.NUM_ENTRIES = tree.Count;
            structTree.PID = 1;
            for (int i = 0; i < tree.Count; i++)
            {
                    structTree.ENTRIES[i] = new TreeEntry();
                    structTree.ENTRIES[i].NAME = tree[i].Name;
                    structTree.ENTRIES[i].NAME_LEN = (short)enc.GetBytes(structTree.ENTRIES[i].NAME + "\0").Length;
                    structTree.ENTRIES[i].NODE_ID = tree[i].ID;
                    structTree.ENTRIES[i].PARENT_ID = tree[i].ParentID;
            }
            return structTree;
        }
    }
}
