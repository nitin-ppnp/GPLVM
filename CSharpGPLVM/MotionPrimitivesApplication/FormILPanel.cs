using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;

namespace MotionPrimitivesApplication
{
    public partial class FormILPanel : Form
    {
        public class PlotDesc
        {
            private string sName;
            private ILPanel pPanel;

            public PlotDesc(string sName)
            {
                this.sName = sName;
                pPanel = new ILPanel();
            }

            public string Name
            {
                get { return sName; }
            }

            public ILPanel Panel
            {
                get { return pPanel; }
            }
        }

        public class PlotDescList : List<PlotDesc>
        {
        }

        private PlotDescList lPlots;

        public FormILPanel()
        {
            InitializeComponent();
        }

        public PlotDescList Plots
        {
            get { return lPlots; }
        }

        private void FormILPanel_Load(object sender, EventArgs e)
        {
            lPlots = new PlotDescList();
        }

        public ILPanel AddNewPlotPanel(string sName)
        {
            var newPlot = new PlotDesc(sName);
            lPlots.Add(newPlot);
            listBoxPlots.DataSource = null;
            listBoxPlots.DataSource = lPlots;
            listBoxPlots.DisplayMember = "Name";
            return newPlot.Panel;
        }

        private void listBoxPlots_SelectedIndexChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel2.Controls.Clear();
            if (((ListBox)sender).SelectedValue != null)
            {
                ILPanel plotPanel = ((PlotDesc)(((ListBox)sender).SelectedValue)).Panel;
                splitContainer1.Panel2.Controls.Add(plotPanel);
            }
        }
    }
}
