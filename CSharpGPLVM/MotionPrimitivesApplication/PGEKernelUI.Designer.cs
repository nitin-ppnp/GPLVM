namespace MotionPrimitivesApplication
{
    partial class PGEKernelUI
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PGEKernelUI));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.plotPanel = new ILNumerics.Drawing.ILPanel();
            this.sigmaSqr = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.plotPanel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.sigmaSqr);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Size = new System.Drawing.Size(355, 416);
            this.splitContainer1.SplitterDistance = 381;
            this.splitContainer1.TabIndex = 0;
            // 
            // plotPanel
            // 
            this.plotPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotPanel.Driver = ILNumerics.Drawing.RendererTypes.OpenGL;
            this.plotPanel.Editor = null;
            this.plotPanel.Location = new System.Drawing.Point(0, 0);
            this.plotPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.plotPanel.Name = "plotPanel";
            this.plotPanel.Rectangle = ((System.Drawing.RectangleF)(resources.GetObject("plotPanel.Rectangle")));
            this.plotPanel.ShowUIControls = false;
            this.plotPanel.Size = new System.Drawing.Size(355, 381);
            this.plotPanel.TabIndex = 0;
            // 
            // sigmaSqr
            // 
            this.sigmaSqr.Location = new System.Drawing.Point(55, 5);
            this.sigmaSqr.Name = "sigmaSqr";
            this.sigmaSqr.Size = new System.Drawing.Size(68, 20);
            this.sigmaSqr.TabIndex = 10;
            this.sigmaSqr.Text = "1.0";
            this.sigmaSqr.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Sigma^2:";
            // 
            // PGEKernelUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "PGEKernelUI";
            this.Size = new System.Drawing.Size(355, 416);
            this.Load += new System.EventHandler(this.PGEKernelUI_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private ILNumerics.Drawing.ILPanel plotPanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox sigmaSqr;
    }
}
