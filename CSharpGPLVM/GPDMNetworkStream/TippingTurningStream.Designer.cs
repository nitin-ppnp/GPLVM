namespace GPDMNetworkStream
{
    partial class TippingTurningStream
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TippingTurningStream));
            this.txtMeldung = new System.Windows.Forms.TextBox();
            this.ipText = new System.Windows.Forms.TextBox();
            this.portText = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.trackBar_neutral = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.trackBar_angry = new System.Windows.Forms.TrackBar();
            this.trackBar_fear = new System.Windows.Forms.TrackBar();
            this.trackBar_AnCh = new System.Windows.Forms.TrackBar();
            this.toolTip_neutral = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_angry = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_Fear = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_AnCh = new System.Windows.Forms.ToolTip(this.components);
            this.sep1 = new System.Windows.Forms.Label();
            this.sep2 = new System.Windows.Forms.Label();
            this.trackBar_MaSt = new System.Windows.Forms.TrackBar();
            this.label7 = new System.Windows.Forms.Label();
            this.trackBar_OlGa = new System.Windows.Forms.TrackBar();
            this.label8 = new System.Windows.Forms.Label();
            this.trackBar_NiTa = new System.Windows.Forms.TrackBar();
            this.label9 = new System.Windows.Forms.Label();
            this.toolTip_MaSt = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_NiTa = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_OlGa = new System.Windows.Forms.ToolTip(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.ilPanel1 = new ILNumerics.Drawing.ILPanel();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_neutral)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_angry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_fear)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AnCh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_MaSt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_OlGa)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_NiTa)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtMeldung
            // 
            this.txtMeldung.Location = new System.Drawing.Point(31, 74);
            this.txtMeldung.Multiline = true;
            this.txtMeldung.Name = "txtMeldung";
            this.txtMeldung.Size = new System.Drawing.Size(360, 362);
            this.txtMeldung.TabIndex = 3;
            // 
            // ipText
            // 
            this.ipText.Location = new System.Drawing.Point(52, 10);
            this.ipText.Name = "ipText";
            this.ipText.Size = new System.Drawing.Size(201, 20);
            this.ipText.TabIndex = 9;
            this.ipText.TextChanged += new System.EventHandler(this.ipText_TextChanged);
            // 
            // portText
            // 
            this.portText.Location = new System.Drawing.Point(322, 10);
            this.portText.Name = "portText";
            this.portText.Size = new System.Drawing.Size(70, 20);
            this.portText.TabIndex = 10;
            this.portText.TextChanged += new System.EventHandler(this.portText_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(291, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(29, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Port:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(30, 13);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(20, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "IP:";
            // 
            // trackBar_neutral
            // 
            this.trackBar_neutral.LargeChange = 100;
            this.trackBar_neutral.Location = new System.Drawing.Point(496, 13);
            this.trackBar_neutral.Maximum = 1000;
            this.trackBar_neutral.Name = "trackBar_neutral";
            this.trackBar_neutral.Size = new System.Drawing.Size(271, 45);
            this.trackBar_neutral.SmallChange = 50;
            this.trackBar_neutral.TabIndex = 15;
            this.trackBar_neutral.TickFrequency = 50;
            this.trackBar_neutral.Scroll += new System.EventHandler(this.trackBar_neutral_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(442, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Neutral:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(450, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 20;
            this.label2.Text = "Angry:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(455, 145);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 21;
            this.label3.Text = "Fear:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(454, 233);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "AnCh:";
            // 
            // trackBar_angry
            // 
            this.trackBar_angry.LargeChange = 100;
            this.trackBar_angry.Location = new System.Drawing.Point(496, 74);
            this.trackBar_angry.Maximum = 1000;
            this.trackBar_angry.Name = "trackBar_angry";
            this.trackBar_angry.Size = new System.Drawing.Size(271, 45);
            this.trackBar_angry.SmallChange = 50;
            this.trackBar_angry.TabIndex = 23;
            this.trackBar_angry.TickFrequency = 50;
            this.trackBar_angry.Scroll += new System.EventHandler(this.trackBar_angry_Scroll);
            // 
            // trackBar_fear
            // 
            this.trackBar_fear.LargeChange = 100;
            this.trackBar_fear.Location = new System.Drawing.Point(496, 145);
            this.trackBar_fear.Maximum = 1000;
            this.trackBar_fear.Name = "trackBar_fear";
            this.trackBar_fear.Size = new System.Drawing.Size(271, 45);
            this.trackBar_fear.SmallChange = 50;
            this.trackBar_fear.TabIndex = 24;
            this.trackBar_fear.TickFrequency = 50;
            this.trackBar_fear.Scroll += new System.EventHandler(this.trackBar_happy_Scroll);
            // 
            // trackBar_AnCh
            // 
            this.trackBar_AnCh.LargeChange = 100;
            this.trackBar_AnCh.Location = new System.Drawing.Point(496, 233);
            this.trackBar_AnCh.Maximum = 1000;
            this.trackBar_AnCh.Name = "trackBar_AnCh";
            this.trackBar_AnCh.Size = new System.Drawing.Size(271, 45);
            this.trackBar_AnCh.SmallChange = 50;
            this.trackBar_AnCh.TabIndex = 25;
            this.trackBar_AnCh.TickFrequency = 50;
            this.trackBar_AnCh.Scroll += new System.EventHandler(this.trackBar_sad_Scroll);
            // 
            // sep1
            // 
            this.sep1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.sep1.Location = new System.Drawing.Point(420, 23);
            this.sep1.Name = "sep1";
            this.sep1.Size = new System.Drawing.Size(2, 392);
            this.sep1.TabIndex = 26;
            // 
            // sep2
            // 
            this.sep2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.sep2.Location = new System.Drawing.Point(530, 203);
            this.sep2.Name = "sep2";
            this.sep2.Size = new System.Drawing.Size(200, 2);
            this.sep2.TabIndex = 27;
            // 
            // trackBar_MaSt
            // 
            this.trackBar_MaSt.LargeChange = 100;
            this.trackBar_MaSt.Location = new System.Drawing.Point(496, 292);
            this.trackBar_MaSt.Maximum = 1000;
            this.trackBar_MaSt.Name = "trackBar_MaSt";
            this.trackBar_MaSt.Size = new System.Drawing.Size(271, 45);
            this.trackBar_MaSt.SmallChange = 50;
            this.trackBar_MaSt.TabIndex = 29;
            this.trackBar_MaSt.TickFrequency = 50;
            this.trackBar_MaSt.Scroll += new System.EventHandler(this.trackBar_MaSt_Scroll);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(452, 292);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(35, 13);
            this.label7.TabIndex = 28;
            this.label7.Text = "MaSt:";
            // 
            // trackBar_OlGa
            // 
            this.trackBar_OlGa.LargeChange = 100;
            this.trackBar_OlGa.Location = new System.Drawing.Point(496, 409);
            this.trackBar_OlGa.Maximum = 1000;
            this.trackBar_OlGa.Name = "trackBar_OlGa";
            this.trackBar_OlGa.Size = new System.Drawing.Size(271, 45);
            this.trackBar_OlGa.SmallChange = 50;
            this.trackBar_OlGa.TabIndex = 33;
            this.trackBar_OlGa.TickFrequency = 50;
            this.trackBar_OlGa.Scroll += new System.EventHandler(this.trackBar_OlGa_Scroll);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(452, 409);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(34, 13);
            this.label8.TabIndex = 32;
            this.label8.Text = "OlGa:";
            // 
            // trackBar_NiTa
            // 
            this.trackBar_NiTa.LargeChange = 100;
            this.trackBar_NiTa.Location = new System.Drawing.Point(496, 350);
            this.trackBar_NiTa.Maximum = 1000;
            this.trackBar_NiTa.Name = "trackBar_NiTa";
            this.trackBar_NiTa.Size = new System.Drawing.Size(271, 45);
            this.trackBar_NiTa.SmallChange = 50;
            this.trackBar_NiTa.TabIndex = 31;
            this.trackBar_NiTa.TickFrequency = 50;
            this.trackBar_NiTa.Scroll += new System.EventHandler(this.trackBar_NiTa_Scroll);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(453, 350);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(33, 13);
            this.label9.TabIndex = 30;
            this.label9.Text = "NiTa:";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ilPanel1);
            this.panel1.Location = new System.Drawing.Point(33, 460);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(734, 367);
            this.panel1.TabIndex = 34;
            // 
            // ilPanel1
            // 
            this.ilPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ilPanel1.Driver = ILNumerics.Drawing.RendererTypes.OpenGL;
            this.ilPanel1.Editor = null;
            this.ilPanel1.Location = new System.Drawing.Point(0, 0);
            this.ilPanel1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.ilPanel1.Name = "ilPanel1";
            this.ilPanel1.Rectangle = ((System.Drawing.RectangleF)(resources.GetObject("ilPanel1.Rectangle")));
            this.ilPanel1.ShowUIControls = false;
            this.ilPanel1.Size = new System.Drawing.Size(734, 367);
            this.ilPanel1.TabIndex = 0;
            // 
            // TippingTurningStream
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(799, 852);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.trackBar_OlGa);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.trackBar_NiTa);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.trackBar_MaSt);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.sep2);
            this.Controls.Add(this.sep1);
            this.Controls.Add(this.trackBar_AnCh);
            this.Controls.Add(this.trackBar_fear);
            this.Controls.Add(this.trackBar_angry);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackBar_neutral);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.ipText);
            this.Controls.Add(this.txtMeldung);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TippingTurningStream";
            this.Text = "Tipping GPDM - TCP Client / Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_neutral)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_angry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_fear)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AnCh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_MaSt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_OlGa)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_NiTa)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtMeldung;
        private System.Windows.Forms.TextBox ipText;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TrackBar trackBar_neutral;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar trackBar_angry;
        private System.Windows.Forms.TrackBar trackBar_fear;
        private System.Windows.Forms.TrackBar trackBar_AnCh;
        private System.Windows.Forms.ToolTip toolTip_neutral;
        private System.Windows.Forms.ToolTip toolTip_angry;
        private System.Windows.Forms.ToolTip toolTip_Fear;
        private System.Windows.Forms.ToolTip toolTip_AnCh;
        private System.Windows.Forms.Label sep1;
        private System.Windows.Forms.Label sep2;
        private System.Windows.Forms.TrackBar trackBar_MaSt;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TrackBar trackBar_OlGa;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TrackBar trackBar_NiTa;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ToolTip toolTip_MaSt;
        private System.Windows.Forms.ToolTip toolTip_NiTa;
        private System.Windows.Forms.ToolTip toolTip_OlGa;
        private System.Windows.Forms.Panel panel1;
        private ILNumerics.Drawing.ILPanel ilPanel1;
    }
}

