using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MotionPrimitivesApplication
{
    public partial class PGEKernelUI : UserControl
    {
        public PGEKernelUI()
        {
            InitializeComponent();
        }

        public ILNumerics.Drawing.ILPanel PlotPanel 
        {
            get { return plotPanel; }
        }
        public double SigmaSqr
        {
            get
            {
                double level;
                double.TryParse(sigmaSqr.Text.ToString(), out level);
                return level;
            }
            set
            {
                sigmaSqr.Text = value.ToString();
            }
        }

        private void PGEKernelUI_Load(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;
        }
    }
}
