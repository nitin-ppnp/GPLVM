
using ILNumerics;
using GPLVM;
using DataFormats;
using GPLVM.Utils.Character;

using ClientServer;

namespace GPLVMTest
{
    public class OgreClient
    {
        // Ogre Stream
        private string ServerName = "88.130.169.147";
        private string Port = "5150";

        Client client;

        private ILArray<double> data = ILMath.localMember<double>();
        private Representation repType;

        private Skeleton skeleton;

        public OgreClient(Skeleton _skeleton, Representation type)
        {
            repType = type;
            skeleton = _skeleton;
            client = new Client();
        }

        public void Connect()
        {
            client.Connect(ServerName, Port);

            byte[] buffer = null;
            Tree tree = new Tree();
            tree.CreateStreamTree(skeleton);
            buffer = new byte[tree.byteSize()];
            buffer = tree.writeToByte(buffer);

            client.Send(buffer);
            buffer = client.Receive();
        }

        public void Stream(Frame frame)
        {
            byte[] buffer = null;
            buffer = new byte[frame.byteSize()];
            frame.writeToByte(buffer);
            client.Send(buffer);
            buffer = client.Receive();
            frame = null;
        }

        public void Stream(ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                data.a = ILMath.check(inData);

                Frame frame = new Frame();
                frame.CreateStreamFrame(data, skeleton, repType);
                frame.PID = 3;

                byte[] buffer = null;
                buffer = new byte[frame.byteSize()];
                frame.writeToByte(buffer);
                client.Send(buffer);
                buffer = client.Receive();
                frame = null;
            }
        }
    }
}
