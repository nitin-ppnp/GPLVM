using System;
using System.Net;
using System.Net.Sockets;

using DataFormats;

namespace ClientServer
{
    public class Client
    {
        // Ogre Stream
        private int MAX_BYTE_SIZE = 1024;

        private TcpClient tcpClient;
        private NetworkStream serverStream;
        private IPEndPoint ServerEndPoint;

        public bool Connect(string serverName, string port)
        {
            bool isConnect = false;
            try
            {
                tcpClient = new TcpClient();
                ServerEndPoint = new IPEndPoint(IPAddress.Parse(serverName), Convert.ToInt16(port));

                try
                {
                    tcpClient.Connect(ServerEndPoint);
                    serverStream = tcpClient.GetStream();
                    isConnect = true;
                }

                catch (Exception ex)
                {

                }
            }
            catch (Exception ex)
            {
                tcpClient.Close();
                isConnect = false;
            }
            return isConnect;
        }

        public void CloseSockets()
        {
            serverStream.Close();
            tcpClient.Close();
        }

        public void Send(byte[] buffer)
        {
            // Build the package
            byte[] dataLength = DataStream.ToByteArray(buffer.Length);

            serverStream.Write(dataLength, 0, dataLength.Length);

            // Send to server
            int bytesSent = 0;
            int bytesLeft = buffer.Length;

            while (bytesLeft > 0)
            {
                int nextPacketSize = (bytesLeft > MAX_BYTE_SIZE) ? MAX_BYTE_SIZE : bytesLeft;

                serverStream.Write(buffer, bytesSent, nextPacketSize);
                bytesSent += nextPacketSize;
                bytesLeft -= nextPacketSize;

            }

        }

        public byte[] Receive()
        {
            int total_size = 0;
            int iResult;
            byte[] buffer = new byte[MAX_BYTE_SIZE];

            byte[] dataLengthByte = new byte[sizeof(int)];
            iResult = serverStream.Read(dataLengthByte, 0, sizeof(int));
            int bytesLeft = (int)DataStream.FromByteToType(dataLengthByte, typeof(int));

            while (bytesLeft > 0)
            {
                int nextPacketSize = (bytesLeft > MAX_BYTE_SIZE) ? MAX_BYTE_SIZE : bytesLeft;

                iResult = serverStream.Read(buffer, total_size, nextPacketSize);

                total_size += iResult;
                bytesLeft -= iResult;
            }

            return buffer;
        }

        public bool Readable()
        {
            return serverStream.DataAvailable;
        }
    }
}
