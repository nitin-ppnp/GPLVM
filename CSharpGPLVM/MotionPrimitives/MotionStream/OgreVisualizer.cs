using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ILNumerics;
using DataFormats;
using GPLVM;
using GPLVM.Utils.Character;

namespace MotionPrimitives.MotionStream
{
    public class OgreVisualizer : Visualizer
    {
        private string sServerName;
        private int iServerPort;
        private TcpClient mTcpClient;
        private Stream mOgreStream;
        private Skeleton mSkeleton;

        public OgreVisualizer(string serverName = "127.0.0.1", int serverPort = 5150)
        {
            sServerName = serverName;
            iServerPort = serverPort;
        }

        public override void Start()
        {
            base.Start();
            try
            {
                mTcpClient = new TcpClient();
                mTcpClient.Connect(new IPEndPoint(IPAddress.Parse(sServerName), Convert.ToInt16(iServerPort)));
                mOgreStream = mTcpClient.GetStream();
            }
            catch (Exception e)
            {
                mTcpClient.Close();
                throw e;
            }
        }

        public override void Stop()
        {
            if (mOgreStream != null)
                mOgreStream.Close();
            if (mTcpClient != null)
                mTcpClient.Close();
            base.Stop();
        }

        public override void SetSkeleton(Skeleton characterSkeleton)
        {
            if (mOgreStream == null)
                throw new Exception("Ogre data stream is not initialized");
            mSkeleton = characterSkeleton;
            Tree tree = new Tree();
            tree.CreateStreamTree(mSkeleton);
            
            byte[] buffer = new byte[tree.byteSize()];
            buffer = tree.writeToByte(buffer);
            mOgreStream.Write(buffer, 0, buffer.Length);
            
            mOgreStream.Read(buffer, 0, 1);
        }

        public override void SetFrame(ILInArray<double> aFrameData, Representation representationType)
        {
            if (mOgreStream == null)
                throw new Exception("Ogre data stream is not initialized");
            Frame frame = new Frame();
            frame.CreateStreamFrame(aFrameData, mSkeleton, representationType);
            frame.PID = 3;

            byte[] buffer = new byte[frame.byteSize()];
            frame.writeToByte(buffer);
            mOgreStream.Write(buffer, 0, buffer.Length);
            
            mOgreStream.Read(buffer, 0, 1);
        }
    }
}
