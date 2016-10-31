namespace GPLVMTest
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.ilPanel1 = new ILNumerics.Drawing.ILPanel();
            this.ilPanel2 = new ILNumerics.Drawing.ILPanel();
            this.SuspendLayout();
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
            this.ilPanel1.Size = new System.Drawing.Size(784, 750);
            this.ilPanel1.TabIndex = 0;
            this.ilPanel1.Load += new System.EventHandler(this.ilPanel1_Load);
            // 
            // ilPanel2
            // 
            this.ilPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ilPanel2.Driver = ILNumerics.Drawing.RendererTypes.OpenGL;
            this.ilPanel2.Editor = null;
            this.ilPanel2.Location = new System.Drawing.Point(0, 0);
            this.ilPanel2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.ilPanel2.Name = "ilPanel2";
            this.ilPanel2.Rectangle = ((System.Drawing.RectangleF)(resources.GetObject("ilPanel2.Rectangle")));
            this.ilPanel2.ShowUIControls = false;
            this.ilPanel2.Size = new System.Drawing.Size(784, 750);
            this.ilPanel2.TabIndex = 1;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 750);
            this.Controls.Add(this.ilPanel2);
            this.Controls.Add(this.ilPanel1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private ILNumerics.Drawing.ILPanel ilPanel1;
        private ILNumerics.Drawing.ILPanel ilPanel2;
    }
}