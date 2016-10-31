namespace GPDMNetworkStream
{
    partial class AvatarBVHIK
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AvatarBVHIK));
            this.txtMeldung = new System.Windows.Forms.TextBox();
            this.ipText = new System.Windows.Forms.TextBox();
            this.portText = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.trackBar_neutral = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.trackBar_angry = new System.Windows.Forms.TrackBar();
            this.trackBar_happy = new System.Windows.Forms.TrackBar();
            this.trackBar_sad = new System.Windows.Forms.TrackBar();
            this.toolTip_neutral = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_angry = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_happy = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_sad = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_neutral)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_angry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_happy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_sad)).BeginInit();
            this.SuspendLayout();
            // 
            // txtMeldung
            // 
            this.txtMeldung.Location = new System.Drawing.Point(31, 74);
            this.txtMeldung.Multiline = true;
            this.txtMeldung.Name = "txtMeldung";
            this.txtMeldung.Size = new System.Drawing.Size(360, 250);
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
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(319, 47);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(69, 21);
            this.button1.TabIndex = 13;
            this.button1.Text = "Set Ratio";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // trackBar_neutral
            // 
            this.trackBar_neutral.LargeChange = 100;
            this.trackBar_neutral.Location = new System.Drawing.Point(496, 74);
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
            this.label1.Location = new System.Drawing.Point(442, 74);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Neutral:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(450, 135);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 20;
            this.label2.Text = "Angry:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(447, 206);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 13);
            this.label3.TabIndex = 21;
            this.label3.Text = "Happy:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(461, 279);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "Sad:";
            // 
            // trackBar_angry
            // 
            this.trackBar_angry.LargeChange = 100;
            this.trackBar_angry.Location = new System.Drawing.Point(496, 135);
            this.trackBar_angry.Maximum = 1000;
            this.trackBar_angry.Name = "trackBar_angry";
            this.trackBar_angry.Size = new System.Drawing.Size(271, 45);
            this.trackBar_angry.SmallChange = 50;
            this.trackBar_angry.TabIndex = 23;
            this.trackBar_angry.TickFrequency = 50;
            this.trackBar_angry.Scroll += new System.EventHandler(this.trackBar_angry_Scroll);
            // 
            // trackBar_happy
            // 
            this.trackBar_happy.LargeChange = 100;
            this.trackBar_happy.Location = new System.Drawing.Point(496, 206);
            this.trackBar_happy.Maximum = 1000;
            this.trackBar_happy.Name = "trackBar_happy";
            this.trackBar_happy.Size = new System.Drawing.Size(271, 45);
            this.trackBar_happy.SmallChange = 50;
            this.trackBar_happy.TabIndex = 24;
            this.trackBar_happy.TickFrequency = 50;
            this.trackBar_happy.Scroll += new System.EventHandler(this.trackBar_happy_Scroll);
            // 
            // trackBar_sad
            // 
            this.trackBar_sad.LargeChange = 100;
            this.trackBar_sad.Location = new System.Drawing.Point(496, 279);
            this.trackBar_sad.Maximum = 1000;
            this.trackBar_sad.Name = "trackBar_sad";
            this.trackBar_sad.Size = new System.Drawing.Size(271, 45);
            this.trackBar_sad.SmallChange = 50;
            this.trackBar_sad.TabIndex = 25;
            this.trackBar_sad.TickFrequency = 50;
            this.trackBar_sad.Scroll += new System.EventHandler(this.trackBar_sad_Scroll);
            // 
            // AvatarBVHIK
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(793, 358);
            this.Controls.Add(this.trackBar_sad);
            this.Controls.Add(this.trackBar_happy);
            this.Controls.Add(this.trackBar_angry);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackBar_neutral);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.ipText);
            this.Controls.Add(this.txtMeldung);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AvatarBVHIK";
            this.Text = "Hierarchical GPDM - TCP Client / Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_neutral)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_angry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_happy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_sad)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtMeldung;
        private System.Windows.Forms.TextBox ipText;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TrackBar trackBar_neutral;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar trackBar_angry;
        private System.Windows.Forms.TrackBar trackBar_happy;
        private System.Windows.Forms.TrackBar trackBar_sad;
        private System.Windows.Forms.ToolTip toolTip_neutral;
        private System.Windows.Forms.ToolTip toolTip_angry;
        private System.Windows.Forms.ToolTip toolTip_happy;
        private System.Windows.Forms.ToolTip toolTip_sad;
    }
}

