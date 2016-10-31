namespace GPDMNetworkStream
{
    partial class Form2
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
            this.txtMeldung = new System.Windows.Forms.TextBox();
            this.ipText = new System.Windows.Forms.TextBox();
            this.portText = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.toolTip_neutral = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_angry = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_happy = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip_sad = new System.Windows.Forms.ToolTip(this.components);
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
            this.button1.Location = new System.Drawing.Point(184, 47);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(69, 21);
            this.button1.TabIndex = 13;
            this.button1.Text = "Set Ratio";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(315, 47);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(76, 21);
            this.button2.TabIndex = 14;
            this.button2.Text = "Start GPDM";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.MouseClick += new System.Windows.Forms.MouseEventHandler(this.button2_MouseClick);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(417, 358);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.ipText);
            this.Controls.Add(this.txtMeldung);
            this.Name = "Form2";
            this.Text = "Hierarchical GPDM - TCP Client / Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
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
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ToolTip toolTip_neutral;
        private System.Windows.Forms.ToolTip toolTip_angry;
        private System.Windows.Forms.ToolTip toolTip_happy;
        private System.Windows.Forms.ToolTip toolTip_sad;
    }
}

