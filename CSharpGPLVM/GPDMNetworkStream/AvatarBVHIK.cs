using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ILNumerics;

using DataFormats;
using Models;


namespace GPDMNetworkStream
{
    public partial class AvatarBVHIK : Form
    {
        //private GP_LVM subAngle;
        
        private bool computingisfinished = true;

        // Ogre Stream
        //private string ServerName = "192.168.0.4";
        private string ServerName = "127.0.0.1";
        private int Port = 5150;

        TcpClient tcpClient;
        NetworkStream serverStream;
        private IPEndPoint ServerEndPoint;

        private Thread simulator;
        private Thread syncroStream;

        private bool isConnect = false;
        private bool isLoad = false;
        private bool isSimulate = false;
        private bool isTreeSent = false;
        private bool isStreaming = false;
        private bool isEnabled = false;

        private Tree tree;

        // Vicon Data Stream
        private TcpClient viconClient;
        private TcpListener tcpListener;
        private Thread listenThread;
        NetworkStream clientStream;

        private bool isViconConnect = false;

        private Frame c3dStream;
        private Frame avatarStream;

        private int maxSize = 2048;

        private ILArray<double> ratio;
        
        private ILArray<double> styleValue;

        delegate void SetTextDelegate(string text);
        delegate void EnableDisableButtonRunDeleg(bool value);

        private IModel model;

        public AvatarBVHIK()
        {
            InitializeComponent();
            ipText.Text = ServerName;
            portText.Text = Port.ToString();

            this.tcpListener = new TcpListener(IPAddress.Any, 3000);
            this.listenThread = new Thread(ListenForClient);
            this.listenThread.Priority = ThreadPriority.Lowest;
            this.listenThread.Start();

            styleValue = ILMath.zeros(1, 4);

            ratio = ILMath.ones(1, 2);

            Thread loadModel = new Thread(LoadModels);
            loadModel.Start();
            //Melde("Waiting for Vicon client...");

            trackBar_neutral.Value = 1000;
            trackBar_angry.Value = 0;
            trackBar_sad.Value = 0;
            trackBar_happy.Value = 0;

            NormalizeStyleValue();

            tree = new Tree();
        }

        private void LoadModels()
        {
            //Melde("Loading Model...");

            model = new HighFiveStyleIK();
            model.init(true);
            tree.CreateStreamTree(model.skeleton);

            Melde("Model loaded.");
            isLoad = true;
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
                    //Melde("Der Client started on " + ServerName.ToString() + ":" + Port + "\r\n");
                    tcpClient = new TcpClient();
                    isConnect = true;
                }

                catch (Exception ex)
                {
                    //Melde("Failed to create client Socket: " + ex.Message);
                }
                ServerEndPoint = new IPEndPoint(IPAddress.Parse(ServerName), Convert.ToInt16(Port));

                try
                {
                    tcpClient.Connect(ServerEndPoint);
                    //Melde("Connected to the Ogre server");
                    isConnect = true;
                }

                catch (Exception ex)
                {
                    //Melde("Failed to create client Socket: " + ex.Message);
                    isConnect = false;
                }
            }
            catch (Exception ex)
            {
                //Melde(ex.Message + "\nClosing the Connect()...");
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
            Frame human;

            serverStream = tcpClient.GetStream();
            clientStream = viconClient.GetStream();

            //buffer = new byte[maxSize];
            isStreaming = true;
            while (isStreaming)
            {
                if (isLoad && !isTreeSent)
                {
                    buffer = new byte[maxSize];
                    buffer = tree.writeToByte(buffer);

                    serverStream.Write(buffer, 0, buffer.Length);
                    //serverStream.Read(buffer, 0, 1);
                    isTreeSent = true;
                }

                if (serverStream.DataAvailable)
                {
                    buffer = new byte[maxSize];
                    serverStream.Read(buffer, 0, 1);
                    if (buffer[0] == 0)
                    {
                        model.Reset();
                        isEnabled = true;
                        //serverStream.Write(buffer, 0, 1);
                    }
                    else if (buffer[0] == 1)
                    {
                        if (simulator != null)
                            simulator.Abort();
                        avatarStream = null;
                        computingisfinished = true;
                        isEnabled = false;
                        //serverStream.Write(buffer, 0, 1);
                    }
                }

                if (isEnabled)
                {
                    buffer = new byte[maxSize];
                    iResult = clientStream.Read(buffer, 0, maxSize);

                    vicon = new Frame();
                    vicon.readFromByte(buffer);

                    human = new Frame();
                    human.NUM_ENTRIES = 4;
                    human.ENTRIES = new FrameEntry[4];
                    for (int i = 0; i < 4; i++)
                        human.ENTRIES[i] = vicon.ENTRIES[i];


                    if (computingisfinished && avatarStream == null)
                    {
                        c3dStream = new Frame();
                        c3dStream.NUM_ENTRIES = 2;
                        c3dStream.ENTRIES = new FrameEntry[c3dStream.NUM_ENTRIES];
                        int cnt = 0;
                        for (int i = 4; i < 6; i++)
                            c3dStream.ENTRIES[cnt++] = vicon.ENTRIES[i];

                        c3dStream.TIMESTAMP = vicon.TIMESTAMP;

                        simulator = new Thread(Simulate);
                        simulator.Priority = ThreadPriority.Highest;
                        simulator.Start();
                    }

                    if (computingisfinished && avatarStream != null)
                    {
                        //buffer = new byte[human.byteSize()];
                        //human.writeToByte(buffer);
                        //serverStream.Write(buffer, 0, buffer.Length);
                        //serverStream.Read(buffer, 0, 1);

                        buffer = new byte[avatarStream.byteSize()];
                        avatarStream.writeToByte(buffer);
                        serverStream.Write(buffer, 0, buffer.Length);
                        //serverStream.Read(buffer, 0, 1);
                        avatarStream = null;
                    }
                    else
                    {
                        buffer = new byte[human.byteSize()];
                        human.writeToByte(buffer);
                        serverStream.Write(buffer, 0, buffer.Length);
                        //serverStream.Read(buffer, 0, 1);
                    }
                    clientStream.Write(buffer, 0, 1);
                }
            }
            serverStream.Close();
            clientStream.Close();
        }


        private void Simulate()
        {
            computingisfinished = false;

            //if (isClickedPlay)
            //{
            //var start = DateTime.Now;
            //avatarStream = model.PlayTrainingData();
            //Thread.Sleep(TimeSpan.FromSeconds(model.FrameTime - DateTime.Now.Subtract(start).Seconds));
            //}
            //else
            {
                //var start = DateTime.Now;
                using (ILScope.Enter())
                {
                    ILArray<double> tmpX = ILMath.zeros(1, c3dStream.NUM_ENTRIES * 3);
                    int cnt = 0;
                    for (int i = 0; i < c3dStream.NUM_ENTRIES; i++)
                    {
                        tmpX[cnt++] = c3dStream.ENTRIES[i].TRANSLATION[0];
                        tmpX[cnt++] = c3dStream.ENTRIES[i].TRANSLATION[1];
                        tmpX[cnt++] = c3dStream.ENTRIES[i].TRANSLATION[2];
                    }
                    avatarStream = model.PredictData(tmpX, styleValue);
                    //Thread.Sleep(TimeSpan.FromSeconds(avatar.FrameTime - DateTime.Now.Subtract(start).Seconds));
                }
            }
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
            /*ILArray<double> tmp = ILMath.zeros(1, c3dStream.NUM_ENTRIES * 3);

            int cnt = 0;
            for (int i = 0; i < c3dStream.NUM_ENTRIES; i++)
            {
                tmp[cnt++] = c3dStream.ENTRIES[i].TRANSLATION[0];
                tmp[cnt++] = c3dStream.ENTRIES[i].TRANSLATION[1];
                tmp[cnt++] = c3dStream.ENTRIES[i].TRANSLATION[2];
            }

            ILArray<double> root = (tmp[ILMath.full, ILMath.r(6, 8)] + tmp[ILMath.full, ILMath.r(9, 11)]) / 2;
            ILArray<double> _distance = ILMath.zeros(1, 2);
            _distance[0] = ILMath.sqrt(ILMath.pow(tmp[0] - root[0], 2) + ILMath.pow(tmp[1] - root[1], 2) + ILMath.pow(tmp[2] - root[2], 2));
            _distance[1] = ILMath.sqrt(ILMath.pow(tmp[3] - root[0], 2) + ILMath.pow(tmp[4] - root[10], 2) + ILMath.pow(tmp[5] - root[2], 2));

            ratio = ILMath.divide(distance, _distance);*/
        }

        private void trackBar_neutral_Scroll(object sender, EventArgs e)
        {
            toolTip_neutral.SetToolTip(trackBar_neutral, (trackBar_neutral.Value / 10).ToString());

            int restvalue = 1000 - trackBar_neutral.Value;
            int delta = (trackBar_angry.Value + trackBar_happy.Value + trackBar_sad.Value) - restvalue;
            if (delta > 0)
            {
                bool angry = false;
                bool happy = false;
                bool sad = false;
                if (trackBar_angry.Value != 0) angry = true;
                if (trackBar_happy.Value != 0) happy = true;
                if (trackBar_sad.Value != 0) sad = true;

                if (angry && happy && sad)
                {
                    delta /= 3;
                    trackBar_angry.Value -= delta;
                    trackBar_happy.Value -= delta;
                    trackBar_sad.Value -= delta;
                }
                else if (angry && happy && !sad)
                {
                    delta /= 2;
                    trackBar_angry.Value -= delta;
                    trackBar_happy.Value -= delta;
                }
                else if (angry && !happy && sad)
                {
                    delta /= 2;
                    trackBar_angry.Value -= delta;
                    trackBar_sad.Value -= delta;
                }
                else if (!angry && happy && sad)
                {
                    delta /= 2;
                    trackBar_sad.Value -= delta;
                    trackBar_happy.Value -= delta;
                }
                else if (angry && !happy && !sad)
                {
                    trackBar_angry.Value -= delta;
                }
                else if (!angry && happy && !sad)
                {
                    trackBar_happy.Value -= delta;
                }
                else if (!angry && !happy && sad)
                {
                    trackBar_sad.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_angry_Scroll(object sender, EventArgs e)
        {
            toolTip_angry.SetToolTip(trackBar_angry, (trackBar_angry.Value / 10).ToString());

            int restvalue = 1000 - trackBar_angry.Value;
            int delta = (trackBar_neutral.Value + trackBar_happy.Value + trackBar_sad.Value) - restvalue;
            if (delta > 0)
            {
                bool neutral = false;
                bool happy = false;
                bool sad = false;
                if (trackBar_neutral.Value != 0) neutral = true;
                if (trackBar_happy.Value != 0) happy = true;
                if (trackBar_sad.Value != 0) sad = true;

                if (neutral && happy && sad)
                {
                    delta /= 3;
                    trackBar_neutral.Value -= delta;
                    trackBar_happy.Value -= delta;
                    trackBar_sad.Value -= delta;
                }
                else if (neutral && happy && !sad)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_happy.Value -= delta;
                }
                else if (neutral && !happy && sad)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_sad.Value -= delta;
                }
                else if (!neutral && happy && sad)
                {
                    delta /= 2;
                    trackBar_sad.Value -= delta;
                    trackBar_happy.Value -= delta;
                }
                else if (neutral && !happy && !sad)
                {
                    trackBar_neutral.Value -= delta;
                }
                else if (!neutral && happy && !sad)
                {
                    trackBar_happy.Value -= delta;
                }
                else if (!neutral && !happy && sad)
                {
                    trackBar_sad.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_happy_Scroll(object sender, EventArgs e)
        {
            toolTip_happy.SetToolTip(trackBar_happy, (trackBar_happy.Value / 10).ToString());

            int restvalue = 1000 - trackBar_happy.Value;
            int delta = (trackBar_neutral.Value + trackBar_angry.Value + trackBar_sad.Value) - restvalue;
            if (delta > 0)
            {
                bool neutral = false;
                bool angry = false;
                bool sad = false;
                if (trackBar_neutral.Value != 0) neutral = true;
                if (trackBar_angry.Value != 0) angry = true;
                if (trackBar_sad.Value != 0) sad = true;

                if (neutral && angry && sad)
                {
                    delta /= 3;
                    trackBar_neutral.Value -= delta;
                    trackBar_angry.Value -= delta;
                    trackBar_sad.Value -= delta;
                }
                else if (neutral && angry && !sad)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_angry.Value -= delta;
                }
                else if (neutral && !angry && sad)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_sad.Value -= delta;
                }
                else if (!neutral && angry && sad)
                {
                    delta /= 2;
                    trackBar_sad.Value -= delta;
                    trackBar_angry.Value -= delta;
                }
                else if (neutral && !angry && !sad)
                {
                    trackBar_neutral.Value -= delta;
                }
                else if (!neutral && angry && !sad)
                {
                    trackBar_angry.Value -= delta;
                }
                else if (!neutral && !angry && sad)
                {
                    trackBar_sad.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_sad_Scroll(object sender, EventArgs e)
        {
            toolTip_sad.SetToolTip(trackBar_sad, (trackBar_sad.Value / 10).ToString());

            int restvalue = 1000 - trackBar_sad.Value;
            int delta = (trackBar_neutral.Value + trackBar_angry.Value + trackBar_happy.Value) - restvalue;
            if (delta > 0)
            {
                bool neutral = false;
                bool angry = false;
                bool happy = false;
                if (trackBar_neutral.Value != 0) neutral = true;
                if (trackBar_angry.Value != 0) angry = true;
                if (trackBar_happy.Value != 0) happy = true;

                if (neutral && angry && happy)
                {
                    delta /= 3;
                    trackBar_neutral.Value -= delta;
                    trackBar_angry.Value -= delta;
                    trackBar_happy.Value -= delta;
                }
                else if (neutral && angry && !happy)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_angry.Value -= delta;
                }
                else if (neutral && !angry && happy)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_happy.Value -= delta;
                }
                else if (!neutral && angry && happy)
                {
                    delta /= 2;
                    trackBar_happy.Value -= delta;
                    trackBar_angry.Value -= delta;
                }
                else if (neutral && !angry && !happy)
                {
                    trackBar_neutral.Value -= delta;
                }
                else if (!neutral && angry && !happy)
                {
                    trackBar_angry.Value -= delta;
                }
                else if (!neutral && !angry && happy)
                {
                    trackBar_happy.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void NormalizeStyleValue()
        {
            if (trackBar_angry.Value != 0)
                styleValue[0] = trackBar_angry.Value / 1000f;
            if (trackBar_happy.Value != 0)
                styleValue[1] = trackBar_happy.Value / 1000f;
            if (trackBar_neutral.Value != 0)
                styleValue[2] = trackBar_neutral.Value / 1000f;
            if (trackBar_sad.Value != 0)
                styleValue[3] = trackBar_sad.Value / 1000f;

            double normalizer = (double)ILMath.sum(styleValue, 1);

            styleValue /= normalizer;
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
