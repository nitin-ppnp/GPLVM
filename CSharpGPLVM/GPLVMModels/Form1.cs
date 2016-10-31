using System;
using System.Drawing;
using System.Windows.Forms;
using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;

namespace Models
{
    public partial class Form1 : Form
    {
        ILArray<double> _X;
        ILArray<double> _segments;

        public Form1(ILArray<double> X, ILArray<double> segments)
        {
            _X = X.C;
            _segments = segments;
            InitializeComponent();
        }

        private void ilPanel1_Load(object sender, EventArgs e)
        {
            var scene = new ILScene();
            var plotCube = scene.Add(new ILPlotCube(twoDMode: false)
            {
                // rotate plotcube
                Rotation =
                  Matrix4.Rotation(new Vector3(1, 0, 0), .6155) *
                  Matrix4.Rotation(new Vector3(0, -1, 0), ILMath.pi / 4)
            });
            for (int i = 0; i < _segments.Length; i++)
            {
                if (i == _segments.Length - 1)
                    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], ILMath.end), ILMath.full]).T,
                        lineColor: Color.Blue,
                        lineStyle: DashStyle.Solid,
                        lineWidth: 3)
                        {
                            Marker =
                            {
                                Style = MarkerStyle.Dot,
                                Fill = { Color = Color.Blue },
                                Size = 12
                            }
                        });
                else
                    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
                        lineColor: Color.Blue,
                        lineStyle: DashStyle.Solid,
                        lineWidth: 3)
                        {
                            Marker =
                            {
                                Style = MarkerStyle.Dot,
                                Fill = { Color = Color.Blue },
                                Size = 12
                            }
                        });
                //switch (i)
                //{
                //case 3:
                //    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], ILMath.end), ILMath.full]).T,
                //    lineColor: Color.Black,
                //    lineStyle: DashStyle.Solid,
                //    lineWidth: 3)
                //    {
                //        Marker =
                //        {
                //            Style = MarkerStyle.Dot,
                //            Fill = { Color = Color.Black },
                //            Size = 12
                //        }
                //    });
                //    break;
                //if (i == 0)
                //    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
                //    lineColor: Color.Green,
                //    lineStyle: DashStyle.Solid,
                //    lineWidth: 0)
                //    {
                //        Marker =
                //        {
                //            Style = MarkerStyle.Dot,
                //            Fill = { Color = Color.Green },
                //            Size = 12
                //        }
                //    });
                //if (i == 1)
                //    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], _segments[i + 1] - 1), ILMath.full]).T,
                //    lineColor: Color.Red,
                //    lineStyle: DashStyle.Solid,
                //    lineWidth: 0)
                //    {
                //        Marker =
                //        {
                //            Style = MarkerStyle.Dot,
                //            Fill = { Color = Color.Red },
                //            Size = 12
                //        }
                //    });
                //if (i == 2)
                //    plotCube.Add(new ILLinePlot(ILMath.tosingle(_X[ILMath.r(_segments[i], ILMath.end), ILMath.full]).T,
                //    lineColor: Color.Yellow,
                //    lineStyle: DashStyle.Solid,
                //    lineWidth: 0)
                //    {
                //        Marker =
                //        {
                //            Style = MarkerStyle.Dot,
                //            Fill = { Color = Color.Yellow },
                //            Size = 12
                //        }
                //    });
                //}
            }

            plotCube.Add(new ILLegend("Neutral", "Angry", "Fear"));

            ilPanel1.Scene = scene;

            //var drv = new ILGDIDriver(750, 750, ilPanel1.Scene);
            //drv.Render();
            //drv.BackBuffer.Bitmap.Save("LogSin.png", System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
