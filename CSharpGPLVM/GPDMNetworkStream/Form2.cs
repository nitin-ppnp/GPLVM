using System;
using System.Windows.Forms;
using GPLVM;
using GPLVM.GPLVM;
using DataFormats;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ILNumerics;

namespace GPDMNetworkStream
{
    public partial class Form2 : Form
    {
        private GP_LVM subAngle;
        private GP_LVM interaction;
        private BackProjection backSub1;
        private BackProjection backInter;
        private bool computingisfinished = false;
        private ILArray<double> newY;

        private XMLReadWrite reader;
        private BVHData bvh;

        // Ogre Stream
        private string ServerName = "127.0.0.1";
        private int Port = 5150;

        TcpClient tcpClient;
        Stream serverStream;
        private IPEndPoint ServerEndPoint;

        private Thread simulator;
        private Thread syncroStream;

        private bool isConnect = false;
        private bool isLoad = false;
        private bool isSimulate = false;
        private bool isClicked = false;
        private bool isTreeSent = false;
        private bool isStreaming = false;

        private Tree tree;
        //private List<Entry> listTree;

        // Vicon Data Stream
        private TcpClient viconClient;
        private TcpListener tcpListener;
        private Thread listenThread;
        NetworkStream clientStream;

        private bool isViconConnect = false;

        private Frame c3d;

        private int maxSize = 2048;

        private ILArray<double> ratio;
        private ILArray<double> distance;
        private ILArray<double> lastPosition;
        private ILArray<double> initPosition;

        delegate void SetTextDelegate(string text);
        delegate void EnableDisableButtonRunDeleg(bool value);

        public Form2()
        {
            InitializeComponent();
            ipText.Text = ServerName;
            portText.Text = Port.ToString();

            button2.Enabled = false;

            this.tcpListener = new TcpListener(IPAddress.Any, 3000);
            this.listenThread = new Thread(ListenForClient);
            this.listenThread.Priority = ThreadPriority.Lowest;
            this.listenThread.Start();

            newY = ILMath.empty();
            lastPosition = ILMath.empty();
            initPosition = new double[] { 0, 95, 0 };
            initPosition = initPosition.T;

            ratio = ILMath.ones(1, 2);

            Thread loadModel = new Thread(LoadModels);
            loadModel.Start();
            Melde("Waiting for Vicon client...");
        }

        private void LoadModels()
        {
            Melde("Loading Model...");
            reader = new XMLReadWrite();

            reader.read(@"..\..\..\..\Data\XML\radian_interactionDTC_SCG.xml", ref interaction);
            reader.read(@"..\..\..\..\Data\XML\radian_backSub1DTC_SCG.xml", ref backSub1);
            reader.read(@"..\..\..\..\Data\XML\radian_backInterDTC_SCG.xml", ref backInter);

            subAngle = (GP_LVM)interaction.Nodes[1];

            bvh = new BVHData();
            bvh.LoadFile(@"..\..\..\..\Data\HighFive\BVH\angry\nick_angry_Ia_1.bvh");

            //listTree = DataProcess.CreateTree(ref bvh);
            //tree = DataProcess.CreateStreamTree(listTree);

            ILArray<double> tmp = ILMath.empty();
            using (ILMatFile matRead = new ILMatFile(@"..\..\..\..\Data\HighFive\TobyTPosec3d.mat"))
            {
                tmp.a = matRead.GetArray<double>(0);
            }

            tmp = tmp[0, ILMath.full];
            ILArray<double> root = (tmp[0, ILMath.r(0, 2)] + tmp[0, ILMath.r(3, 5)]) / 2;
            distance = ILMath.zeros(1, 2);
            distance[0] = ILMath.sqrt(ILMath.pow(tmp[6] - root[0], 2) + ILMath.pow(tmp[7] - root[1], 2) + ILMath.pow(tmp[8] - root[2], 2));
            distance[1] = ILMath.sqrt(ILMath.pow(tmp[9] - root[0], 2) + ILMath.pow(tmp[10] - root[1], 2) + ILMath.pow(tmp[11] - root[2], 2));

            isLoad = true;
            EnableDisableButtonRun(true);

            Melde("Model loaded.");

            //tree = DataStream.CreateStructTree(bvh.tree);
        }

        private void ListenForClient()
        {
            this.tcpListener.Start();

            viconClient = this.tcpListener.AcceptTcpClient();

            Melde("Vicon Client has connected.");
            isViconConnect = true;

            Connect2Ogre();

            syncroStream = new Thread(SyncroStream);
            syncroStream.Priority = ThreadPriority.Lowest;
            syncroStream.Start();

            this.listenThread.Abort();
        }

        // Connect
        private void Connect2Ogre()
        {
            try
            {
                // Let's connect to a listening server
                try
                {
                    Melde("Der Client started on " + ServerName.ToString() + ":" + Port + "\r\n");
                    tcpClient = new TcpClient();
                    isConnect = true;
                }

                catch (Exception ex)
                {
                    Melde("Failed to create client Socket: " + ex.Message);
                }
                ServerEndPoint = new IPEndPoint(IPAddress.Parse(ServerName), Convert.ToInt16(Port));

                try
                {
                    tcpClient.Connect(ServerEndPoint);
                    Melde("Connected to the Ogre server");
                    isConnect = true;
                }

                catch (Exception ex)
                {
                    Melde("Failed to create client Socket: " + ex.Message);
                    isConnect = false;
                }
            }
            catch (Exception ex)
            {
                Melde(ex.Message + "\nClosing the Connect()...");
                tcpClient.Close();
                isConnect = false;
            }
        }

        private void SyncroStream()
        {
            byte[] buffer = null;
            int iResult;

            ILArray<double> inputs = ILMath.empty();

            Frame vicon;
            Frame arm;
            Frame avatar;

            serverStream = tcpClient.GetStream();
            clientStream = viconClient.GetStream();

            //buffer = new byte[maxSize];
            isStreaming = true;
            while (isStreaming)
            {
                if (isLoad && !isTreeSent)
                {
                    buffer = new byte[tree.byteSize()];
                    buffer = tree.writeToByte(buffer);

                    serverStream.Write(buffer, 0, buffer.Length);
                    serverStream.Read(buffer, 0, 1);
                    isTreeSent = true;
                }

                buffer = new byte[maxSize];
                iResult = clientStream.Read(buffer, 0, maxSize);

                vicon = new Frame();
                vicon.readFromByte(buffer);

                arm = new Frame();
                arm.NUM_ENTRIES = 3;
                arm.ENTRIES = new FrameEntry[3];
                for (int i = 0; i < 3; i++)
                    arm.ENTRIES[i] = vicon.ENTRIES[i];

                if (computingisfinished && isClicked && newY.IsEmpty)
                {
                    computingisfinished = false;
                    c3d = new Frame();
                    c3d.NUM_ENTRIES = 4;
                    c3d.ENTRIES = new FrameEntry[4];
                    int cnt = 0;
                    for (int i = 3; i < 7; i++)
                        c3d.ENTRIES[cnt++] = vicon.ENTRIES[i];

                    simulator = new Thread(Simulate);
                    simulator.Priority = ThreadPriority.Highest;
                    simulator.Start();
                }

                if (computingisfinished && isClicked && !newY.IsEmpty)
                {
                    /*avatar = DataProcess.CreateStreamFrame(newY, listTree, Representation.radian);
                    avatar.PID = 3;

                    buffer = new byte[arm.byteSize()];
                    arm.writeToByte(buffer);
                    serverStream.Write(buffer, 0, buffer.Length);
                    serverStream.Read(buffer, 0, 1);

                    buffer = new byte[avatar.byteSize()];
                    avatar.writeToByte(buffer);
                    serverStream.Write(buffer, 0, buffer.Length);
                    serverStream.Read(buffer, 0, 1);
                    newY = ILMath.empty();*/
                }
                else
                {
                    buffer = new byte[arm.byteSize()];
                    arm.writeToByte(buffer);
                    serverStream.Write(buffer, 0, buffer.Length);
                    serverStream.Read(buffer, 0, 1);
                }
                clientStream.Write(buffer, 0, 1);
            }

            serverStream.Close();
            clientStream.Close();
        }


        private void Simulate()
        {
            //var start = DateTime.Now;

            ILArray<double> inX = ILMath.empty();
            ILArray<double> tmpX = ILMath.zeros(1, c3d.NUM_ENTRIES * 3);
            ILArray<double> Ytmp = ILMath.empty();
            ILArray<double> velocity = ILMath.empty();
            ILArray<double> globalPos = ILMath.empty();
            ILArray<double> root = ILMath.empty();
            //ILArray<double> tmpStyle = ILMath.zeros(styleVariable.S);

            int cnt = 0;

            for (int i = 0; i < c3d.NUM_ENTRIES; i++)
            {
                tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[0];// *0.1;
                tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[1];// *0.1;
                tmpX[cnt++] = c3d.ENTRIES[i].TRANSLATION[2];// *0.1;


            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\..\..\..\Data\ViconStream.txt", true))
            {
                file.WriteLine(tmpX.ToString());
            }
            /*if (lastPosition.IsEmpty)
                velocity = ILMath.zeros(1, 6);
            else
                velocity = tmpX[ILMath.full, ILMath.r(0, 5)] - lastPosition;

            lastPosition = tmpX[ILMath.full, ILMath.r(0, 5)];

            // ballance of RSHF and RSHB setting as root
            root = (tmpX[ILMath.full, ILMath.r(6, 8)] + tmpX[ILMath.full, ILMath.r(9, 11)]) / 2;
            root[ILMath.full, 2] = 0;

            // transormation of RHAA and RHAB from global to lokal coordinates with rotation 0 for root
            inX = tmpX[ILMath.full, ILMath.r(0, 2)] - root; 
            inX[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + 3)] = tmpX[ILMath.full, ILMath.r(3, 5)] - root;

            // normalize possition w.r.t the training data
            inX[ILMath.full, ILMath.r(0, 2)] *= ratio[0];
            inX[ILMath.full, ILMath.r(3, 5)] *= ratio[1];*/

            inX = tmpX[ILMath.full, ILMath.r(0, 5)];
            //inX[ILMath.full, ILMath.r(ILMath.end + 1, ILMath.end + velocity.S[1])] = velocity;
            Ytmp = backInter.PredictData(backSub1.PredictData(inX));
            Ytmp = interaction.PredictData(backInter.PredictData(backSub1.PredictData(inX)));

            Ytmp = Ytmp[ILMath.full, ILMath.r(((IGPLVM)interaction.Nodes[0]).LatentDimension, ILMath.end)];

            newY = subAngle.PredictData(Ytmp);

            // using global position
            //globalPos = newY[0, ILMath.r(0, 2)];
            newY[0, ILMath.r(0, 2)] = initPosition.C;
            //initPosition += globalPos.C;

            //Thread.Sleep(TimeSpan.FromSeconds(bvh.FrameTime - DateTime.Now.Subtract(start).Seconds));

            computingisfinished = true;
        }

        private void Melde(string msg)
        {
            if (txtMeldung.InvokeRequired)
            {
                SetTextDelegate d = new SetTextDelegate(Melde);

                this.Invoke(d, new Object[] { msg });
            }
            else
                txtMeldung.AppendText(msg + "\r\n");
        }

        private void EnableDisableButtonRun(bool value)
        {
            if (button2.InvokeRequired)
            {
                EnableDisableButtonRunDeleg d = new EnableDisableButtonRunDeleg(EnableDisableButtonRun);
                button2.Invoke(d, value);
            }
            else
            {
                button2.Enabled = value;
            }
        }

        private void ipText_TextChanged(object sender, EventArgs e)
        {
            ServerName = ipText.Text;
        }

        private void portText_TextChanged(object sender, EventArgs e)
        {
            Port = Convert.ToInt32(portText.Text);
        }

        // set Ratio
        private void button1_Click(object sender, EventArgs e)
        {
            ILArray<double> tmp = ILMath.zeros(1, c3d.NUM_ENTRIES * 3);

            int cnt = 0;
            for (int i = 0; i < c3d.NUM_ENTRIES; i++)
            {
                tmp[cnt++] = c3d.ENTRIES[i].TRANSLATION[0];
                tmp[cnt++] = c3d.ENTRIES[i].TRANSLATION[1];
                tmp[cnt++] = c3d.ENTRIES[i].TRANSLATION[2];
            }

            ILArray<double> root = (tmp[ILMath.full, ILMath.r(6, 8)] + tmp[ILMath.full, ILMath.r(9, 11)]) / 2;
            ILArray<double> _distance = ILMath.zeros(1, 2);
            _distance[0] = ILMath.sqrt(ILMath.pow(tmp[0] - root[0], 2) + ILMath.pow(tmp[1] - root[1], 2) + ILMath.pow(tmp[2] - root[2], 2));
            _distance[1] = ILMath.sqrt(ILMath.pow(tmp[3] - root[0], 2) + ILMath.pow(tmp[4] - root[10], 2) + ILMath.pow(tmp[5] - root[2], 2));

            ratio = ILMath.divide(distance, _distance);
        }

        private void button2_MouseClick(object sender, MouseEventArgs e)
        {
            if (!computingisfinished && !isClicked)
            {
                button2.Text = "Stop GPDM";
                computingisfinished = true;
                isClicked = true;
            }
            else if (computingisfinished && isClicked)
            {
                button2.Text = "Start GPDM";
                computingisfinished = false;
                isClicked = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isStreaming = false;
            if (this.listenThread.IsAlive) this.listenThread.Abort();
            //if (this.simulator.IsAlive) this.simulator.Abort();
            //if (this.syncroStream.IsAlive) this.syncroStream.Abort();
        }
    }
}
