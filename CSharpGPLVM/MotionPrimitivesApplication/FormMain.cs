using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;
using DataFormats;
using GPLVM;
using GPLVM.Kernel;
using GPLVM.Utils.Character;
using FactorGraph.FactorNodes;
using MotionPrimitives.Experiments;
using MotionPrimitives.MotionStream;
using MotionPrimitives.DataSets;

namespace MotionPrimitivesApplication
{
    public partial class FormMain : Form
    {
        private LearnStyledCoupledDynamicsExperiment experiment;
        private string sGraphFileName = "Walk graph.xml";
        private FormILPanel frmPlots;
        private int LatentDimensions = 3;
        private ILPanel[] latentPlots;
        private ILPanel[] fullKernelPlots;
        private PGEKernelUI[,] partialKernelPlots;

        public FormMain()
        {
            experiment = new LearnStyledCoupledDynamicsExperiment("Learning a walk", LatentDimensions);
            experiment.representation = Representation.exponential;
            InitializeComponent();
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            this.Width = 1600;
            this.Height = 800;
        }

        private void buttonLoadBVH_Click(object sender, EventArgs e)
        {
            experiment = new LearnStyledCoupledDynamicsExperiment("Learning a walk", LatentDimensions);
            experiment.representation = Representation.exponential;
            experiment.nIterations = 20;
            experiment.DynamicsApproximation = GPLVM.GPLVM.ApproximationType.ftc;
            experiment.GPLVMApproximation = GPLVM.GPLVM.ApproximationType.ftc;
            experiment.NumberOfInducingPoints = 100;
            experiment.LoadDataSet(EmotionalWalks.Select(EmotionalWalks.Person.Niko, EmotionalWalks.Emotion.Neutral));
            experiment.RunOptimization();
            createPlots();
            updatePlots();
        }

        private void createPlots()
        {
            panelPlots.SuspendLayout();

            int nParts = experiment.BodyPartsPlates.Count;
            panelPlots.Controls.Clear();
            panelPlots.ColumnCount = nParts + 2;
            panelPlots.RowCount = nParts;
            int plotSize = 350;
            panelPlots.Size = new System.Drawing.Size(panelPlots.ColumnCount * plotSize, panelPlots.RowCount * plotSize);

            panelPlots.ColumnStyles.Clear();
            for (int k = 0; k < panelPlots.ColumnCount; k++)
                panelPlots.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, plotSize));

            panelPlots.RowStyles.Clear();
            for (int k = 0; k < panelPlots.RowCount; k++)
                this.panelPlots.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, plotSize));
            
            latentPlots = new ILPanel[nParts];
            fullKernelPlots = new ILPanel[nParts];
            partialKernelPlots = new PGEKernelUI[nParts, nParts];

            int i = 0;
            foreach (var bodyPart in experiment.BodyPartsPlates)
            {
                int j = 0;
                var latentPointsPlot = new ILPanel();

                panelPlots.Controls.Add(latentPointsPlot, j, i);
                latentPlots[i] = latentPointsPlot;
                j++;

                var fullKernelPlot = new ILPanel();
                panelPlots.Controls.Add(fullKernelPlot, j, i);
                fullKernelPlots[i] = fullKernelPlot;
                j++;

                if (bodyPart.fnDynamics.KernelObject is PGEKern)
                    foreach (var kern in (bodyPart.fnDynamics.KernelObject as PGEKern).Kernels)
                    {
                        var partialKernelPlot = new PGEKernelUI();
                        panelPlots.Controls.Add(partialKernelPlot, j, i);
                        partialKernelPlots[i, j - 2] = partialKernelPlot;
                        j++;
                    }
                i++;
            }
            panelPlots.ResumeLayout();
        }

        private void updatePlots()
        {
            int nParts = experiment.BodyPartsPlates.Count;

            int i = 0;
            foreach (var bodyPart in experiment.BodyPartsPlates)
            {
                plotLatentPoints(latentPlots[i], bodyPart.dnX.GetValues(), bodyPart.dnX.Segments);
                if (bodyPart.fnDynamics.PredictionStructs[0].Kernel is PGEKern)
                {
                    var kPGE = (bodyPart.fnDynamics.PredictionStructs[0].Kernel as PGEKern);
                    plotKernel(fullKernelPlots[i], kPGE.K);

                    int j = 0;
                    foreach (var kern in kPGE.Kernels)
                    {
                        plotKernel(partialKernelPlots[i, j].PlotPanel, kern.K);
                        partialKernelPlots[i, j].SigmaSqr = (double)kPGE.Parameter[j];
                        j++;
                    }
                }
                latentPlots[i].Scene.Screen.Add(new ILLabel(bodyPart.Name)
                {
                    Anchor = new PointF(0, 0),
                    Color = Color.FromArgb(0, 0, 0),
                    Font = new Font("Verdana", 8f),
                    Position = new ILNumerics.Drawing.Vector3(0.01, 0.01, 0),
                    Fringe = { Width = 1 }
                });
                latentPlots[i].Refresh();
                
                i++;
            }
        }

        private void plotLatentPoints(ILPanel panel, ILArray<double> points, ILArray<int> segments)
        {
            var scene = new ILScene();
            var plotCube = scene.Camera.Add(new ILPlotCube(twoDMode: false));
            for (int i = 0; i < segments.Length; i++)
            {
                int sStart = (int)segments[i];
                int sEnd = (i == segments.Length - 1 ? points.S[0] : (int)segments[i + 1]) - 1;

                var linePlot = plotCube.Add(new ILLinePlot(
                    ILMath.tosingle(points[ILMath.r(sStart, sEnd), ILMath.full].T)));
                linePlot.Marker.Style = MarkerStyle.Dot;
            }
            panel.Scene = scene;
            panel.Refresh();
        }

        private void plotKernel(ILPanel panel, ILArray<double> data)
        {
            ILArray<float> kernelMatrix = ILMath.empty<float>();
            kernelMatrix.a = ILMath.tosingle(data);
            var scene = new ILScene();
            var plotCube = scene.Camera.Add(new ILPlotCube(twoDMode: false));
            var surface = plotCube.Add(new ILSurface(kernelMatrix));
            surface.Colormap = Colormaps.Jet;
            surface.Wireframe.Color = Color.FromArgb(50, Color.LightGray);
            surface.UpdateColormapped();
            surface.Fill.Markable = false;
            surface.Wireframe.Markable = false;
            panel.Scene = scene;
            panel.Refresh();
        }

        private void plotLatentTrajectory(ILPanel panel, ILArray<double> trajectory)
        {
            var scene = panel.Scene;
            var cube = scene.First<ILPlotCube>();
            var linePlot = cube.Add(new ILLinePlot(ILMath.tosingle(trajectory.T)));
            linePlot.ColorOverride = Color.Orange;
            linePlot.Marker.Style = MarkerStyle.Diamond;
            linePlot.Marker.ColorOverride = Color.Orange;
            panel.Refresh();
        }

        private void buttonRunOptimization_Click(object sender, EventArgs e)
        {
            using (ILScope.Enter())
            {
                experiment.RunOptimization();
                updatePlots();
            }
        }

        private void buttonSaveGraph_Click(object sender, EventArgs e)
        {
            experiment.SaveToFile(@"..\", sGraphFileName);
        }

        private void buttonLoadGraph_Click(object sender, EventArgs e)
        {
            experiment.LoadFromFile(@"..\", sGraphFileName);
            updatePlots();
        }

        private void buttonPlayGP_Click(object sender, EventArgs e)
        {
            var generator = new ExperimentGenerator(experiment.skeleton, experiment);
            generator.MaxFrameCounter = 30 * 4;
            var visualizer = new OgreVisualizer();
            var player = new Player(generator, visualizer);
            player.PlayAll();
        }

        private void buttonPlotLatentData_Click(object sender, EventArgs e)
        {
            updatePlots();
        }

        private void ilPanelX_Load(object sender, EventArgs e)
        {
            //var scene = new ILScene();            
            //ilPanelX.Scene = scene;
        }

        private void buttonPlayBVH_Click(object sender, EventArgs e)
        {
            //Representation representation = Representation.radian;
            //Representation representation = Representation.exponential;
            //BVHData bvh = new BVHData(representation);

            //ILArray<double> motionData = bvh.LoadFile(sBVHFileName);
            //Skeleton skeleton = bvh.skeleton;

            //var generator = new RelativeRootGenerator(skeleton, motionData, bvh.FrameTime, representation);
            //var visualiser = new OgreVisualizer();
            //var player = new Player(generator, visualiser);
            //player.PlayAll();
        }

        private void buttonPlotLatentTrajectory_Click(object sender, EventArgs e)
        {
            int nParts = experiment.BodyPartsPlates.Count;

            int kSizeFactor = 2; // # of cycles
            ILArray<double> fullX = experiment.PredictFullX(kSizeFactor);
            int i = 0;
            foreach (var bodyPart in experiment.BodyPartsPlates)
            {
                plotLatentTrajectory(latentPlots[i], fullX[ILMath.full, bodyPart.fnDynamics.DynamicsIndexes]);
                i++;
            }
        }

        private void buttonPlotDynamics_Click(object sender, EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            frmPlots = new FormILPanel();
            frmPlots.Visible = false;
        }

        private void buttonSetKernelModulation_Click(object sender, EventArgs e)
        {
            int nParts = experiment.BodyPartsPlates.Count;

            int i = 0;
            foreach (var bodyPart in experiment.BodyPartsPlates)
            {
                bodyPart.fnDynamics.UpdatePredictors(true); // pull the parameters
                int j = 0;
                if (bodyPart.fnDynamics.PredictionStructs[0].Kernel is PGEKern)
                {
                    var kPGE = (bodyPart.fnDynamics.PredictionStructs[0].Kernel as PGEKern);
                    ILArray<double> parameters = kPGE.Parameter;
                    foreach (var kern in kPGE.Kernels)
                    {
                        parameters[j] = partialKernelPlots[i, j].SigmaSqr;
                        j++;
                    }
                    kPGE.Parameter = parameters;
                }
                bodyPart.fnDynamics.UpdatePredictors(false); // do not pull the parameters
                i++;
            }

            updatePlots();
        }

    }
}
