namespace MotionPrimitivesApplication
{
    partial class FormMain
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxStartPhaseLower = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxStartPhaseUpper = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxLowerToLower = new System.Windows.Forms.TextBox();
            this.textBoxLowerToUpper = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonSetKernelModulation = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonPlotDynamics = new System.Windows.Forms.Button();
            this.buttonPlotLatentTrajectory = new System.Windows.Forms.Button();
            this.buttonPlayBVH = new System.Windows.Forms.Button();
            this.buttonPlotLatentData = new System.Windows.Forms.Button();
            this.buttonRunGP = new System.Windows.Forms.Button();
            this.buttonLoadGraph = new System.Windows.Forms.Button();
            this.buttonSaveGraph = new System.Windows.Forms.Button();
            this.buttonRunOptimization = new System.Windows.Forms.Button();
            this.buttonLoadBVH = new System.Windows.Forms.Button();
            this.textBoxUpper = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBoxLower = new System.Windows.Forms.TextBox();
            this.textBoxUpperToLower = new System.Windows.Forms.TextBox();
            this.textBoxUpperToUpper = new System.Windows.Forms.TextBox();
            this.panelPlots = new System.Windows.Forms.TableLayoutPanel();
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
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.label5);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxStartPhaseLower);
            this.splitContainer1.Panel1.Controls.Add(this.label6);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxStartPhaseUpper);
            this.splitContainer1.Panel1.Controls.Add(this.label4);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxLowerToLower);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxLowerToUpper);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.buttonSetKernelModulation);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxUpperToLower);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.textBoxUpperToUpper);
            this.splitContainer1.Panel1.Controls.Add(this.buttonPlotDynamics);
            this.splitContainer1.Panel1.Controls.Add(this.buttonPlotLatentTrajectory);
            this.splitContainer1.Panel1.Controls.Add(this.buttonPlayBVH);
            this.splitContainer1.Panel1.Controls.Add(this.buttonPlotLatentData);
            this.splitContainer1.Panel1.Controls.Add(this.buttonRunGP);
            this.splitContainer1.Panel1.Controls.Add(this.buttonLoadGraph);
            this.splitContainer1.Panel1.Controls.Add(this.buttonSaveGraph);
            this.splitContainer1.Panel1.Controls.Add(this.buttonRunOptimization);
            this.splitContainer1.Panel1.Controls.Add(this.buttonLoadBVH);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.AutoScroll = true;
            this.splitContainer1.Panel2.Controls.Add(this.panelPlots);
            this.splitContainer1.Size = new System.Drawing.Size(708, 608);
            this.splitContainer1.SplitterDistance = 180;
            this.splitContainer1.TabIndex = 0;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 402);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(114, 13);
            this.label5.TabIndex = 21;
            this.label5.Text = "Start phase lower, 2Pi*";
            // 
            // textBoxStartPhaseLower
            // 
            this.textBoxStartPhaseLower.Enabled = false;
            this.textBoxStartPhaseLower.Location = new System.Drawing.Point(127, 396);
            this.textBoxStartPhaseLower.Name = "textBoxStartPhaseLower";
            this.textBoxStartPhaseLower.Size = new System.Drawing.Size(37, 20);
            this.textBoxStartPhaseLower.TabIndex = 20;
            this.textBoxStartPhaseLower.Text = "0.0";
            this.textBoxStartPhaseLower.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 376);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(116, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Start phase upper, 2Pi*";
            // 
            // textBoxStartPhaseUpper
            // 
            this.textBoxStartPhaseUpper.Enabled = false;
            this.textBoxStartPhaseUpper.Location = new System.Drawing.Point(127, 370);
            this.textBoxStartPhaseUpper.Name = "textBoxStartPhaseUpper";
            this.textBoxStartPhaseUpper.Size = new System.Drawing.Size(37, 20);
            this.textBoxStartPhaseUpper.TabIndex = 18;
            this.textBoxStartPhaseUpper.Text = "0.0";
            this.textBoxStartPhaseUpper.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(46, 302);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 13);
            this.label4.TabIndex = 17;
            this.label4.Text = "From upper     lower";
            // 
            // textBoxLowerToLower
            // 
            this.textBoxLowerToLower.Enabled = false;
            this.textBoxLowerToLower.Location = new System.Drawing.Point(114, 344);
            this.textBoxLowerToLower.Name = "textBoxLowerToLower";
            this.textBoxLowerToLower.Size = new System.Drawing.Size(37, 20);
            this.textBoxLowerToLower.TabIndex = 16;
            this.textBoxLowerToLower.Text = "1.0";
            this.textBoxLowerToLower.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBoxLowerToUpper
            // 
            this.textBoxLowerToUpper.Enabled = false;
            this.textBoxLowerToUpper.Location = new System.Drawing.Point(114, 318);
            this.textBoxLowerToUpper.Name = "textBoxLowerToUpper";
            this.textBoxLowerToUpper.Size = new System.Drawing.Size(37, 20);
            this.textBoxLowerToUpper.TabIndex = 15;
            this.textBoxLowerToUpper.Text = "1.0";
            this.textBoxLowerToUpper.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(38, 281);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Kernel modulation:";
            // 
            // buttonSetKernelModulation
            // 
            this.buttonSetKernelModulation.Location = new System.Drawing.Point(25, 423);
            this.buttonSetKernelModulation.Name = "buttonSetKernelModulation";
            this.buttonSetKernelModulation.Size = new System.Drawing.Size(132, 23);
            this.buttonSetKernelModulation.TabIndex = 13;
            this.buttonSetKernelModulation.Text = "Set";
            this.buttonSetKernelModulation.UseVisualStyleBackColor = true;
            this.buttonSetKernelModulation.Click += new System.EventHandler(this.buttonSetKernelModulation_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 350);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "To lower";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 324);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "To upper";
            // 
            // buttonPlotDynamics
            // 
            this.buttonPlotDynamics.Location = new System.Drawing.Point(25, 245);
            this.buttonPlotDynamics.Name = "buttonPlotDynamics";
            this.buttonPlotDynamics.Size = new System.Drawing.Size(132, 23);
            this.buttonPlotDynamics.TabIndex = 8;
            this.buttonPlotDynamics.Text = "Plot dynamics";
            this.buttonPlotDynamics.UseVisualStyleBackColor = true;
            this.buttonPlotDynamics.Click += new System.EventHandler(this.buttonPlotDynamics_Click);
            // 
            // buttonPlotLatentTrajectory
            // 
            this.buttonPlotLatentTrajectory.Location = new System.Drawing.Point(25, 216);
            this.buttonPlotLatentTrajectory.Name = "buttonPlotLatentTrajectory";
            this.buttonPlotLatentTrajectory.Size = new System.Drawing.Size(132, 23);
            this.buttonPlotLatentTrajectory.TabIndex = 7;
            this.buttonPlotLatentTrajectory.Text = "Plot new latent trajectory";
            this.buttonPlotLatentTrajectory.UseVisualStyleBackColor = true;
            this.buttonPlotLatentTrajectory.Click += new System.EventHandler(this.buttonPlotLatentTrajectory_Click);
            // 
            // buttonPlayBVH
            // 
            this.buttonPlayBVH.Location = new System.Drawing.Point(25, 160);
            this.buttonPlayBVH.Name = "buttonPlayBVH";
            this.buttonPlayBVH.Size = new System.Drawing.Size(132, 23);
            this.buttonPlayBVH.TabIndex = 6;
            this.buttonPlayBVH.Text = "Play raw BVH";
            this.buttonPlayBVH.UseVisualStyleBackColor = true;
            this.buttonPlayBVH.Click += new System.EventHandler(this.buttonPlayBVH_Click);
            // 
            // buttonPlotLatentData
            // 
            this.buttonPlotLatentData.Location = new System.Drawing.Point(25, 189);
            this.buttonPlotLatentData.Name = "buttonPlotLatentData";
            this.buttonPlotLatentData.Size = new System.Drawing.Size(132, 23);
            this.buttonPlotLatentData.TabIndex = 5;
            this.buttonPlotLatentData.Text = "Plot latent data";
            this.buttonPlotLatentData.UseVisualStyleBackColor = true;
            this.buttonPlotLatentData.Click += new System.EventHandler(this.buttonPlotLatentData_Click);
            // 
            // buttonRunGP
            // 
            this.buttonRunGP.Location = new System.Drawing.Point(25, 131);
            this.buttonRunGP.Name = "buttonRunGP";
            this.buttonRunGP.Size = new System.Drawing.Size(132, 23);
            this.buttonRunGP.TabIndex = 4;
            this.buttonRunGP.Text = "Play GP";
            this.buttonRunGP.UseVisualStyleBackColor = true;
            this.buttonRunGP.Click += new System.EventHandler(this.buttonPlayGP_Click);
            // 
            // buttonLoadGraph
            // 
            this.buttonLoadGraph.Location = new System.Drawing.Point(25, 101);
            this.buttonLoadGraph.Name = "buttonLoadGraph";
            this.buttonLoadGraph.Size = new System.Drawing.Size(132, 23);
            this.buttonLoadGraph.TabIndex = 3;
            this.buttonLoadGraph.Text = "Load graph";
            this.buttonLoadGraph.UseVisualStyleBackColor = true;
            this.buttonLoadGraph.Click += new System.EventHandler(this.buttonLoadGraph_Click);
            // 
            // buttonSaveGraph
            // 
            this.buttonSaveGraph.Location = new System.Drawing.Point(25, 71);
            this.buttonSaveGraph.Name = "buttonSaveGraph";
            this.buttonSaveGraph.Size = new System.Drawing.Size(132, 23);
            this.buttonSaveGraph.TabIndex = 2;
            this.buttonSaveGraph.Text = "Save graph";
            this.buttonSaveGraph.UseVisualStyleBackColor = true;
            this.buttonSaveGraph.Click += new System.EventHandler(this.buttonSaveGraph_Click);
            // 
            // buttonRunOptimization
            // 
            this.buttonRunOptimization.Location = new System.Drawing.Point(25, 41);
            this.buttonRunOptimization.Name = "buttonRunOptimization";
            this.buttonRunOptimization.Size = new System.Drawing.Size(132, 23);
            this.buttonRunOptimization.TabIndex = 1;
            this.buttonRunOptimization.Text = "Run optimization";
            this.buttonRunOptimization.UseVisualStyleBackColor = true;
            this.buttonRunOptimization.Click += new System.EventHandler(this.buttonRunOptimization_Click);
            // 
            // buttonLoadBVH
            // 
            this.buttonLoadBVH.Location = new System.Drawing.Point(24, 11);
            this.buttonLoadBVH.Name = "buttonLoadBVH";
            this.buttonLoadBVH.Size = new System.Drawing.Size(133, 23);
            this.buttonLoadBVH.TabIndex = 0;
            this.buttonLoadBVH.Text = "Load BVH data";
            this.buttonLoadBVH.UseVisualStyleBackColor = true;
            this.buttonLoadBVH.Click += new System.EventHandler(this.buttonLoadBVH_Click);
            // 
            // textBoxUpper
            // 
            this.textBoxUpper.Location = new System.Drawing.Point(59, 318);
            this.textBoxUpper.Name = "textBoxUpper";
            this.textBoxUpper.Size = new System.Drawing.Size(37, 20);
            this.textBoxUpper.TabIndex = 9;
            this.textBoxUpper.Text = "1.0";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(102, 318);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(37, 20);
            this.textBox2.TabIndex = 15;
            this.textBox2.Text = "1.0";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(102, 344);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(37, 20);
            this.textBox1.TabIndex = 16;
            this.textBox1.Text = "1.0";
            // 
            // textBoxLower
            // 
            this.textBoxLower.Location = new System.Drawing.Point(59, 344);
            this.textBoxLower.Name = "textBoxLower";
            this.textBoxLower.Size = new System.Drawing.Size(37, 20);
            this.textBoxLower.TabIndex = 11;
            this.textBoxLower.Text = "1.0";
            // 
            // textBoxUpperToLower
            // 
            this.textBoxUpperToLower.Enabled = false;
            this.textBoxUpperToLower.Location = new System.Drawing.Point(71, 344);
            this.textBoxUpperToLower.Name = "textBoxUpperToLower";
            this.textBoxUpperToLower.Size = new System.Drawing.Size(37, 20);
            this.textBoxUpperToLower.TabIndex = 11;
            this.textBoxUpperToLower.Text = "1.0";
            this.textBoxUpperToLower.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBoxUpperToUpper
            // 
            this.textBoxUpperToUpper.Enabled = false;
            this.textBoxUpperToUpper.Location = new System.Drawing.Point(71, 318);
            this.textBoxUpperToUpper.Name = "textBoxUpperToUpper";
            this.textBoxUpperToUpper.Size = new System.Drawing.Size(37, 20);
            this.textBoxUpperToUpper.TabIndex = 9;
            this.textBoxUpperToUpper.Text = "1.0";
            this.textBoxUpperToUpper.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // panelPlots
            // 
            this.panelPlots.ColumnCount = 4;
            this.panelPlots.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.panelPlots.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.panelPlots.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.panelPlots.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.panelPlots.Location = new System.Drawing.Point(0, 0);
            this.panelPlots.Name = "panelPlots";
            this.panelPlots.RowCount = 2;
            this.panelPlots.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelPlots.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelPlots.Size = new System.Drawing.Size(500, 568);
            this.panelPlots.TabIndex = 0;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(708, 608);
            this.Controls.Add(this.splitContainer1);
            this.Name = "FormMain";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button buttonRunGP;
        private System.Windows.Forms.Button buttonLoadGraph;
        private System.Windows.Forms.Button buttonSaveGraph;
        private System.Windows.Forms.Button buttonRunOptimization;
        private System.Windows.Forms.Button buttonLoadBVH;
        private System.Windows.Forms.Button buttonPlotLatentData;
        private System.Windows.Forms.Button buttonPlayBVH;
        private System.Windows.Forms.Button buttonPlotLatentTrajectory;
        private System.Windows.Forms.Button buttonPlotDynamics;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonSetKernelModulation;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxLowerToLower;
        private System.Windows.Forms.TextBox textBoxLowerToUpper;
        private System.Windows.Forms.TextBox textBoxUpper;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBoxLower;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxStartPhaseLower;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxStartPhaseUpper;
        private System.Windows.Forms.TextBox textBoxUpperToLower;
        private System.Windows.Forms.TextBox textBoxUpperToUpper;
        private System.Windows.Forms.TableLayoutPanel panelPlots;
    }
}