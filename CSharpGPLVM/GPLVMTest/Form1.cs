using System;
using System.Drawing;
using System.Windows.Forms;
using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;

namespace GPLVMTest
{
    public partial class Form1 : Form
    {
        ILArray<double> _X;
        ILArray<double> _segments;

        public Form1(ILArray<double> X, ILArray<double> segments)
        {
            _X = X;
            _segments = segments;
            InitializeComponent();
        }

        private void ilPanel1_Load(object sender, EventArgs e)
        {
            var scene = new ILScene();
            var plotCube = scene.Add(new ILPlotCube(twoDMode: false));
            for (int i = 0; i < _segments.Length; i++)
            {
                if (i == _segments.Length - 1)
                    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], ILMath.end), ILMath.full]).T,
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
                else
                    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
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
                //switch (i)
                //{
                //    case 3:
                //        plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], ILMath.end), ILMath.full]).T,
                //        lineColor: Color.Black,
                //        lineStyle: DashStyle.Solid,
                //        lineWidth: 3)
                //        {
                //            Marker =
                //            {
                //                Style = MarkerStyle.Dot,
                //                Fill = { Color = Color.Black },
                //                Size = 12
                //            }
                //        });
                //        break;
                //    case 0:
                //        plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
                //        lineColor: Color.Blue,
                //        lineStyle: DashStyle.Solid,
                //        lineWidth: 3)
                //        {
                //            Marker =
                //            {
                //                Style = MarkerStyle.Dot,
                //                Fill = { Color = Color.Blue },
                //                Size = 12
                //            }
                //        });
                //        break;
                //    case 1:
                //        plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
                //        lineColor: Color.Red,
                //        lineStyle: DashStyle.Solid,
                //        lineWidth: 3)
                //        {
                //            Marker =
                //            {
                //                Style = MarkerStyle.Dot,
                //                Fill = { Color = Color.Red },
                //                Size = 12
                //            }
                //        });
                //        break;
                //    case 2:
                //        plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
                //        lineColor: Color.Yellow,
                //        lineStyle: DashStyle.Solid,
                //        lineWidth: 3)
                //        {
                //            Marker =
                //            {
                //                Style = MarkerStyle.Dot,
                //                Fill = { Color = Color.Yellow },
                //                Size = 12
                //            }
                //        });
                //        break;
                //}
            }

            ilPanel1.Scene = scene;

            /*var drv = new ILGDIDriver(1000, 750, ilPanel1.Scene);
            drv.Render();
            drv.BackBuffer.Bitmap.Save("LogSin", System.Drawing.Imaging.ImageFormat.Png); */
        }
    }
}
