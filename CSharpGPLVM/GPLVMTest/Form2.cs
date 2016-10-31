using System;
using System.Drawing;
using System.Windows.Forms;
using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;

namespace GPLVMTest
{
    public partial class Form2 : Form
    {
        ILArray<double> _X;

        public Form2(ILArray<double> X)
        {
            _X = X;
            InitializeComponent();
        }

        private void ilPanel1_Load(object sender, EventArgs e)
        {
            var scene = new ILScene();
            var plotCube = scene.Add(new ILPlotCube(twoDMode: false));
            for (int i = 0; i < _X.Size[1]; i++)
            {

                plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[i]).T,
                        lineColor: Color.Blue,
                        lineStyle: DashStyle.Solid,
                        lineWidth: 2)
                        {
                            Marker =
                            {
                                Style = MarkerStyle.Dot,
                                Fill = { Color = Color.Blue },
                                Size = 5
                            }
                        });
                
            }

            ilPanel1.Scene = scene;

            /*var drv = new ILGDIDriver(1000, 750, ilPanel1.Scene);
            drv.Render();
            drv.BackBuffer.Bitmap.Save("LogSin", System.Drawing.Imaging.ImageFormat.Png); */
        }
    }
}
