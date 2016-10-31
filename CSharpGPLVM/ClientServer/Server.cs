using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using DataFormats;

namespace ClientServer
{
    public class Server
    {

        private TcpClient _client;
        private TcpListener tcpListener;
        private Thread listenThread;
        private NetworkStream clientStream;

        private int MAX_BYTE_SIZE = 1024;
        private int DEFAULT_PORT = 3000;

        public bool Go()
        {
            bool isConnect = false;
            try
            {
                this.tcpListener = new TcpListener(IPAddress.Any, DEFAULT_PORT);
                this.tcpListener.Start();

                _client = this.tcpListener.AcceptTcpClient();

                clientStream = _client.GetStream();
                isConnect = true;
            }
            catch (Exception ex)
            {
                _client.Close();
                isConnect = false;
            }
            return isConnect;
        }

        public void Send(byte[] buffer)
        {
            // Build the package
            byte[] dataLength = DataStream.ToByteArray(buffer.Length);

            clientStream.Write(dataLength, 0, dataLength.Length);

            // Send to server
            int bytesSent = 0;
            int bytesLeft = buffer.Length;

            while (bytesLeft > 0)
            {
                int nextPacketSize = (bytesLeft > MAX_BYTE_SIZE) ? MAX_BYTE_SIZE : bytesLeft;

                clientStream.Write(buffer, bytesSent, nextPacketSize);
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
            iResult = clientStream.Read(dataLengthByte, 0, sizeof(int));
            int bytesLeft = (int)DataStream.FromByteToType(dataLengthByte, typeof(int));

            while (bytesLeft > 0)
            {
                int nextPacketSize = (bytesLeft > MAX_BYTE_SIZE) ? MAX_BYTE_SIZE : bytesLeft;

                iResult = clientStream.Read(buffer, total_size, nextPacketSize);

                total_size += iResult;
                bytesLeft -= iResult;
            }

            return buffer;
        }

        public bool Readable()
        {
            return clientStream.DataAvailable;
        }

        public void CloseSockets()
        {
            clientStream.Close();
            _client.Close();
        }
    }
}
