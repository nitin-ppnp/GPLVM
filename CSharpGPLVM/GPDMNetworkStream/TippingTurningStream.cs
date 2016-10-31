using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using ILNumerics;

using DataFormats;
using Models;

using ClientServer;

using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;


namespace GPDMNetworkStream
{
    public partial class TippingTurningStream : Form
    {
        //private GP_LVM subAngle;
        
        private bool computingisfinished = true;

        // Ogre Stream
        //private string ServerName = "192.168.0.4";
        private string ServerName = "127.0.0.1";
        private string Port = "5150";

        Client ogreClient;
        private Thread simulator;
        private Thread syncroStream;

        private bool isLoad = false;
        private bool isSimulate = false;
        private bool isTreeSent = false;
        private bool isStreaming = false;
        private bool isEnabled = false;
        private bool isTurn = false;

        private Tree tree;

        // Vicon Data Stream
        //private TcpClient viconClient;
        //private TcpListener tcpListener;
        private Thread listenThread;
        //NetworkStream clientStream;

        private bool isViconConnect = false;

        private Frame c3dStream;
        private Frame avatarStream;

        private int maxSize = 3000;

        private ILArray<double> ratio;

        private ILArray<double> styleValueIdleE;
        private ILArray<double> styleValueIdleC;
        //private ILArray<double> styleValueTurnE;
        //private ILArray<double> styleValueTurnC;

        delegate void SetTextDelegate(string text);
        delegate void EnableDisableButtonRunDeleg(bool value);

        private TippingTurning model;

        public TippingTurningStream()
        {
            InitializeComponent();
            ipText.Text = ServerName;
            portText.Text = Port.ToString();

            ogreClient = new Client();

            //this.tcpListener = new TcpListener(IPAddress.Any, 3000);
            this.listenThread = new Thread(ListenForClient);
            this.listenThread.Priority = ThreadPriority.Lowest;
            this.listenThread.Start();

            styleValueIdleE = ILMath.zeros(1, 3);
            styleValueIdleC = ILMath.zeros(1, 4);
            //styleValueTurnE = ILMath.zeros(1, 3);
            //styleValueTurnC = ILMath.zeros(1, 4);
            
            ratio = ILMath.ones(1, 2);

            Thread loadModel = new Thread(LoadModels);
            loadModel.Start();
            //Melde("Waiting for Vicon client...");

            trackBar_neutral.Value = 1000;
            trackBar_angry.Value = 0;
            trackBar_fear.Value = 0;

            trackBar_AnCh.Value = 1000;
            trackBar_MaSt.Value = 0;
            trackBar_NiTa.Value = 0;
            trackBar_OlGa.Value = 0;

            NormalizeStyleValue();

            tree = new Tree();
        }

        private void LoadModels()
        {
            //Melde("Loading Model...");

            model = new TippingTurning();
            model.init(true);
            tree.CreateStreamTree(model.skeleton);

            Melde("Model loaded.");
            ilPanel1_Load();
            isLoad = true;
        }

        private void ListenForClient()
        {
            //this.tcpListener.Start();

            //viconClient = this.tcpListener.AcceptTcpClient();

            //Melde("Vicon Client has connected.");
            //isViconConnect = true;

            if (ogreClient.Connect(ServerName, Port))
            {
                syncroStream = new Thread(SyncroStream);
                syncroStream.Priority = ThreadPriority.Lowest;
                syncroStream.Start();
            }

            this.listenThread.Abort();
        }

        private void SyncroStream()
        {
            byte[] buffer = null;
            int iResult;

            ILArray<double> inputs = ILMath.empty();

            Frame vicon;
            Frame human;

            simulator = new Thread(Simulate);
            simulator.Priority = ThreadPriority.Highest;
            simulator.Start();

            //buffer = new byte[maxSize];
            isStreaming = true;
            while (isStreaming)
            {
                if (isLoad && !isTreeSent)
                {
                    buffer = new byte[maxSize];
                    buffer = tree.writeToByte(buffer);

                    ogreClient.Send(buffer);
                    //7buffer = ogreClient.Receive();
                    isTreeSent = true;
                }

                if (ogreClient.Readable())
                {
                    buffer = new byte[maxSize];
                    buffer = ogreClient.Receive();
                    if (buffer[0] == 0)
                    {
                        model.Reset();
                        isEnabled = true;
                        isTurn = false;
                        //ogreClient.Send(buffer);
                    }
                    else if (buffer[0] == 1)
                    {
                        avatarStream = null;
                        computingisfinished = true;
                        isEnabled = false;
                        //ogreClient.Send(buffer);
                    }
                    else if (buffer[0] == 2)
                    {
                        isTurn = true;
                        //ogreClient.Send(buffer);
                    }
                }

                else if (isEnabled)
                {
                    buffer = new byte[maxSize];
                    //iResult = clientStream.Read(buffer, 0, maxSize);

                    //vicon = new Frame();
                    //vicon.readFromByte(buffer);

                    //human = new Frame();
                    //human.NUM_ENTRIES = 4;
                    //human.ENTRIES = new FrameEntry[4];
                    //for (int i = 0; i < 4; i++)
                    //    human.ENTRIES[i] = vicon.ENTRIES[i];


                    if (computingisfinished && avatarStream == null)
                    {
                        //c3dStream = new Frame();
                        //c3dStream.NUM_ENTRIES = 2;
                        //c3dStream.ENTRIES = new FrameEntry[c3dStream.NUM_ENTRIES];
                        //int cnt = 0;
                        //for (int i = 4; i < 6; i++)
                        //    c3dStream.ENTRIES[cnt++] = vicon.ENTRIES[i];

                        //c3dStream.TIMESTAMP = vicon.TIMESTAMP;

                        isSimulate = true;
                        
                    }

                    if (computingisfinished && avatarStream != null)
                    {
                        buffer = new byte[avatarStream.byteSize()];
                        avatarStream.writeToByte(buffer);
                        ogreClient.Send(buffer);
                        //buffer = ogreClient.Receive();
                        avatarStream = null;
                    }
                    //else
                    //{
                    //    buffer = new byte[human.byteSize()];
                    //    human.writeToByte(buffer);
                    //    serverStream.Write(buffer, 0, buffer.Length);
                    //    //serverStream.Read(buffer, 0, 1);
                    //}
                    //clientStream.Write(buffer, 0, 1);
                }
            }
            ogreClient.CloseSockets();
            //clientStream.Close();
        }


        private void Simulate()
        {
            while (isStreaming)
            {

                if (isSimulate)
                {
                    computingisfinished = false;

                    //if (isClickedPlay)
                    //{
                    //var start = DateTime.Now;
                    //avatarStream = model.PlayTrainingDataTurn();
                    //Thread.Sleep(TimeSpan.FromSeconds(model.FrameTime - DateTime.Now.Subtract(start).Seconds));
                    //}
                    //else
                    lock (model)
                    {
                        var start = DateTime.Now;
                        if (!isTurn)
                            avatarStream = model.PredictDataIdle(styleValueIdleE, styleValueIdleC);
                        else
                            avatarStream = model.PredictDataTurn(styleValueIdleE, styleValueIdleC);
                        Thread.Sleep(TimeSpan.FromSeconds(0.02 - DateTime.Now.Subtract(start).Seconds));
                    }
                    computingisfinished = true;
                    isSimulate = false;
                }
            }
            //simulator.Abort();
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
            Port = portText.Text;
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
            int delta = (trackBar_angry.Value + trackBar_fear.Value) - restvalue;
            if (delta > 0)
            {
                bool angry = false;
                bool happy = false;
                if (trackBar_angry.Value != 0 && (!(trackBar_angry.Value - delta < 0) || !(trackBar_angry.Value - delta / 2 < 0))) angry = true;
                if (trackBar_fear.Value != 0 && (!(trackBar_fear.Value - delta < 0) || !(trackBar_fear.Value - delta / 2 < 0))) happy = true;

                if (angry && happy)
                {
                    delta /= 2;
                    trackBar_angry.Value -= delta;
                    trackBar_fear.Value -= delta;
                }
                else if (angry && !happy)
                {
                    trackBar_angry.Value -= delta;
                }
                else if (!angry && happy)
                {
                    trackBar_fear.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_angry_Scroll(object sender, EventArgs e)
        {
            toolTip_angry.SetToolTip(trackBar_angry, (trackBar_angry.Value / 10).ToString());

            int restvalue = 1000 - trackBar_angry.Value;
            int delta = (trackBar_neutral.Value + trackBar_fear.Value) - restvalue;
            if (delta > 0)
            {
                bool neutral = false;
                bool happy = false;
                if (trackBar_neutral.Value != 0 && (!(trackBar_neutral.Value - delta < 0) || !(trackBar_neutral.Value - delta / 2 < 0))) neutral = true;
                if (trackBar_fear.Value != 0 && (!(trackBar_fear.Value - delta < 0) || !(trackBar_fear.Value - delta / 2 < 0))) happy = true;

                if (neutral && happy)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_fear.Value -= delta;
                }
                else if (neutral && !happy)
                {
                    trackBar_neutral.Value -= delta;
                }
                else if (!neutral && happy)
                {
                    trackBar_fear.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_happy_Scroll(object sender, EventArgs e)
        {
            toolTip_Fear.SetToolTip(trackBar_fear, (trackBar_fear.Value / 10).ToString());

            int restvalue = 1000 - trackBar_fear.Value;
            int delta = (trackBar_neutral.Value + trackBar_angry.Value) - restvalue;
            if (delta > 0)
            {
                bool neutral = false;
                bool angry = false;
                if (trackBar_neutral.Value != 0 && (!(trackBar_neutral.Value - delta < 0) || !(trackBar_neutral.Value - delta / 2 < 0))) neutral = true;
                if (trackBar_angry.Value != 0 && (!(trackBar_angry.Value - delta < 0) || !(trackBar_angry.Value - delta / 2 < 0))) angry = true;

                if (neutral && angry)
                {
                    delta /= 2;
                    trackBar_neutral.Value -= delta;
                    trackBar_angry.Value -= delta;
                }
                else if (neutral && !angry)
                {
                    trackBar_neutral.Value -= delta;
                }
                else if (!neutral && angry)
                {
                    trackBar_angry.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_sad_Scroll(object sender, EventArgs e)
        {
            toolTip_AnCh.SetToolTip(trackBar_AnCh, (trackBar_AnCh.Value / 10).ToString());

            int restvalue = 1000 - trackBar_AnCh.Value;
            int delta = (trackBar_MaSt.Value + trackBar_NiTa.Value + trackBar_OlGa.Value) - restvalue;
            if (delta > 0)
            {
                bool MaSt = false;
                bool NiTa = false;
                bool OlGa = false;
                if (trackBar_MaSt.Value != 0 && (!(trackBar_MaSt.Value - delta < 0) || !(trackBar_MaSt.Value - delta / 2 < 0))) MaSt = true;
                if (trackBar_NiTa.Value != 0 && (!(trackBar_NiTa.Value - delta < 0) || !(trackBar_NiTa.Value - delta / 2 < 0))) NiTa = true;
                if (trackBar_OlGa.Value != 0 && (!(trackBar_OlGa.Value - delta < 0) || !(trackBar_OlGa.Value - delta / 2 < 0))) OlGa = true;

                if (MaSt && NiTa && OlGa)
                {
                    delta /= 3;
                    trackBar_MaSt.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                    trackBar_OlGa.Value -= delta;
                }
                else if (MaSt && NiTa && !OlGa)
                {
                    delta /= 2;
                    trackBar_MaSt.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                }
                else if (MaSt && !NiTa && OlGa)
                {
                    delta /= 2;
                    trackBar_MaSt.Value -= delta;
                    trackBar_OlGa.Value -= delta;
                }
                else if (!MaSt && NiTa && OlGa)
                {
                    delta /= 2;
                    trackBar_OlGa.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                }
                else if (MaSt && !NiTa && !OlGa)
                {
                    trackBar_MaSt.Value -= delta;
                }
                else if (!MaSt && NiTa && !OlGa)
                {
                    trackBar_NiTa.Value -= delta;
                }
                else if (!MaSt && !NiTa && OlGa)
                {
                    trackBar_OlGa.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_MaSt_Scroll(object sender, EventArgs e)
        {
            toolTip_AnCh.SetToolTip(trackBar_MaSt, (trackBar_MaSt.Value / 10).ToString());

            int restvalue = 1000 - trackBar_MaSt.Value;
            int delta = (trackBar_AnCh.Value + trackBar_NiTa.Value + trackBar_OlGa.Value) - restvalue;
            if (delta > 0)
            {
                bool AnCh = false;
                bool NiTa = false;
                bool OlGa = false;
                if (trackBar_AnCh.Value != 0 && (!(trackBar_AnCh.Value - delta < 0) || !(trackBar_AnCh.Value - delta / 2 < 0))) AnCh = true;
                if (trackBar_NiTa.Value != 0 && (!(trackBar_NiTa.Value - delta < 0) || !(trackBar_NiTa.Value - delta / 2 < 0))) NiTa = true;
                if (trackBar_OlGa.Value != 0 && (!(trackBar_OlGa.Value - delta < 0) || !(trackBar_OlGa.Value - delta / 2 < 0))) OlGa = true;

                if (AnCh && NiTa && OlGa)
                {
                    delta /= 3;
                    trackBar_AnCh.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                    trackBar_OlGa.Value -= delta;
                }
                else if (AnCh && NiTa && !OlGa)
                {
                    delta /= 2;
                    trackBar_AnCh.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                }
                else if (AnCh && !NiTa && OlGa)
                {
                    delta /= 2;
                    trackBar_AnCh.Value -= delta;
                    trackBar_OlGa.Value -= delta;
                }
                else if (!AnCh && NiTa && OlGa)
                {
                    delta /= 2;
                    trackBar_OlGa.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                }
                else if (AnCh && !NiTa && !OlGa)
                {
                    trackBar_AnCh.Value -= delta;
                }
                else if (!AnCh && NiTa && !OlGa)
                {
                    trackBar_NiTa.Value -= delta;
                }
                else if (!AnCh && !NiTa && OlGa)
                {
                    trackBar_OlGa.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_NiTa_Scroll(object sender, EventArgs e)
        {
            toolTip_AnCh.SetToolTip(trackBar_NiTa, (trackBar_NiTa.Value / 10).ToString());

            int restvalue = 1000 - trackBar_NiTa.Value;
            int delta = (trackBar_AnCh.Value + trackBar_MaSt.Value + trackBar_OlGa.Value) - restvalue;
            if (delta > 0)
            {
                bool AnCh = false;
                bool MaSt = false;
                bool OlGa = false;
                if (trackBar_AnCh.Value != 0 && (!(trackBar_AnCh.Value - delta < 0) || !(trackBar_AnCh.Value - delta / 2 < 0))) AnCh = true;
                if (trackBar_MaSt.Value != 0 && (!(trackBar_MaSt.Value - delta < 0) || !(trackBar_MaSt.Value - delta / 2 < 0))) MaSt = true;
                if (trackBar_OlGa.Value != 0 && (!(trackBar_OlGa.Value - delta < 0) || !(trackBar_OlGa.Value - delta / 2 < 0))) OlGa = true;

                if (AnCh && MaSt && OlGa)
                {
                    delta /= 3;
                    trackBar_AnCh.Value -= delta;
                    trackBar_MaSt.Value -= delta;
                    trackBar_OlGa.Value -= delta;
                }
                else if (AnCh && MaSt && !OlGa)
                {
                    delta /= 2;
                    trackBar_AnCh.Value -= delta;
                    trackBar_MaSt.Value -= delta;
                }
                else if (AnCh && !MaSt && OlGa)
                {
                    delta /= 2;
                    trackBar_AnCh.Value -= delta;
                    trackBar_OlGa.Value -= delta;
                }
                else if (!AnCh && MaSt && OlGa)
                {
                    delta /= 2;
                    trackBar_OlGa.Value -= delta;
                    trackBar_MaSt.Value -= delta;
                }
                else if (AnCh && !MaSt && !OlGa)
                {
                    trackBar_AnCh.Value -= delta;
                }
                else if (!AnCh && MaSt && !OlGa)
                {
                    trackBar_MaSt.Value -= delta;
                }
                else if (!AnCh && !MaSt && OlGa)
                {
                    trackBar_OlGa.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void trackBar_OlGa_Scroll(object sender, EventArgs e)
        {
            toolTip_AnCh.SetToolTip(trackBar_OlGa, (trackBar_OlGa.Value / 10).ToString());

            int restvalue = 1000 - trackBar_OlGa.Value;
            int delta = (trackBar_AnCh.Value + trackBar_NiTa.Value + trackBar_MaSt.Value) - restvalue;
            if (delta > 0)
            {
                bool AnCh = false;
                bool MaSt = false;
                bool NiTa = false;
                if (trackBar_AnCh.Value != 0 && (!(trackBar_AnCh.Value - delta < 0) || !(trackBar_AnCh.Value - delta / 2 < 0))) AnCh = true;
                if (trackBar_MaSt.Value != 0 && (!(trackBar_MaSt.Value - delta < 0) || !(trackBar_MaSt.Value - delta / 2 < 0))) MaSt = true;
                if (trackBar_NiTa.Value != 0 && (!(trackBar_NiTa.Value - delta < 0) || !(trackBar_NiTa.Value - delta / 2 < 0))) NiTa = true;

                if (AnCh && MaSt && NiTa)
                {
                    delta /= 3;
                    trackBar_AnCh.Value -= delta;
                    trackBar_MaSt.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                }
                else if (AnCh && MaSt && !NiTa)
                {
                    delta /= 2;
                    trackBar_AnCh.Value -= delta;
                    trackBar_MaSt.Value -= delta;
                }
                else if (AnCh && !MaSt && NiTa)
                {
                    delta /= 2;
                    trackBar_AnCh.Value -= delta;
                    trackBar_NiTa.Value -= delta;
                }
                else if (!AnCh && MaSt && NiTa)
                {
                    delta /= 2;
                    trackBar_OlGa.Value -= delta;
                    trackBar_MaSt.Value -= delta;
                }
                else if (AnCh && !MaSt && !NiTa)
                {
                    trackBar_AnCh.Value -= delta;
                }
                else if (!AnCh && MaSt && !NiTa)
                {
                    trackBar_MaSt.Value -= delta;
                }
                else if (!AnCh && !MaSt && NiTa)
                {
                    trackBar_NiTa.Value -= delta;
                }
            }
            NormalizeStyleValue();
        }

        private void NormalizeStyleValue()
        {
            if (trackBar_angry.Value != 0)
                styleValueIdleE[1] = trackBar_angry.Value / 1000f;
            if (trackBar_fear.Value != 0)
                styleValueIdleE[2] = trackBar_fear.Value / 1000f;
            if (trackBar_neutral.Value != 0)
                styleValueIdleE[0] = trackBar_neutral.Value / 1000f;

            double normalizer1 = (double)ILMath.sum(styleValueIdleE, 1);
            styleValueIdleE /= normalizer1;

            if (trackBar_AnCh.Value != 0)
                styleValueIdleC[0] = trackBar_AnCh.Value / 1000f;
            if (trackBar_MaSt.Value != 0)
                styleValueIdleC[1] = trackBar_MaSt.Value / 1000f;
            if (trackBar_NiTa.Value != 0)
                styleValueIdleC[2] = trackBar_NiTa.Value / 1000f;
            if (trackBar_OlGa.Value != 0)
                styleValueIdleC[3] = trackBar_OlGa.Value / 1000f;

            double normalizer2 = (double)ILMath.sum(styleValueIdleC, 1);
            styleValueIdleC /= normalizer2;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isStreaming = false;
            if (this.listenThread.IsAlive) this.listenThread.Abort();
            //if (this.simulator.IsAlive) this.simulator.Abort();
            //if (this.syncroStream.IsAlive) this.syncroStream.Abort();
        }

        private void ilPanel1_Load()
        {
            var plotCube1 = ilPanel1.Scene.Add(new ILPlotCube
            {
                ScreenRect = new RectangleF(0, 0, 0.5f, 1f),
            });

            var PosPlot1 = plotCube1.Add(new ILPoints
            {
                Positions = ILMath.tosingle(model.XIdle[ILMath.full, ILMath.r(0, 2)]).T,
                Color = Color.Blue,
                Size = 1
            });

            var PosPlot11 = plotCube1.Add(new ILPoints
            {
                Positions = ILMath.tosingle(model.XIdleDynamics[ILMath.full, ILMath.r(0, 2)]).T,
                Color = Color.Red,
                Size = 5
            });

            ilPanel1.Scene.Add(new ILLabel("Latent Space GPDM Idle")
            {
                Font = new System.Drawing.Font(FontFamily.GenericSansSerif, 12),
                Position = new ILNumerics.Drawing.Vector3(-0.5, 0.7, 0)
            });

            var plotCube2 = ilPanel1.Scene.Add(new ILPlotCube
            {
                ScreenRect = new RectangleF(0.5f, 0, 0.5f, 1f),
            });

            var PosPlot2 = plotCube2.Add(new ILPoints
                  {
                      Positions = ILMath.tosingle(model.XTurn[ILMath.full, ILMath.r(0, 2)]).T,
                      Color = Color.Blue,
                      Size = 1
                  });

            var PosPlot22 = plotCube2.Add(new ILPoints
            {
                Positions = ILMath.tosingle(model.XTurnDynamics[ILMath.full, ILMath.r(0, 2)]).T,
                Color = Color.Red,
                Size = 5
            });

            ilPanel1.Scene.Add(new ILLabel("Latent Space GPDM Turning")
            {
                Font = new System.Drawing.Font(FontFamily.GenericSansSerif, 12),
                Position = new ILNumerics.Drawing.Vector3(0.5, 0.7, 0)
            });

            ilPanel1.BeginRenderFrame += (o, args) =>
            {
                using (ILScope.Enter())
                {
                    PosPlot1.Positions.Update(ILMath.tosingle(model.XIdle[ILMath.full, ILMath.r(0, 2)]).T);
                    PosPlot11.Positions.Update(ILMath.tosingle(model.XIdleDynamics[ILMath.full, ILMath.r(0, 2)]).T);
                    PosPlot2.Positions.Update(ILMath.tosingle(model.XTurn[ILMath.full, ILMath.r(0, 2)]).T);
                    PosPlot22.Positions.Update(ILMath.tosingle(model.XTurnDynamics[ILMath.full, ILMath.r(0, 2)]).T);

                    // fit the line plot inside the plot cube limits
                    plotCube1.Reset();
                    plotCube2.Reset();
                }
            };

            ilPanel1.Clock.Running = true;
        }
    }
}
