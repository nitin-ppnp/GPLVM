using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILNumerics;
using DataFormats;
using GraphicalModel;
using GraphicalModel.Factors;
using GraphicalModel.Kernels;
using GPLVM;

namespace GraphicalModelTest
{

    class GPDMTest
    {
        public static void Test(string[] args)
        {

            BVHData bvh = new BVHData();
            ILArray<double> data = bvh.LoadFile(@"..\..\..\..\Data\HandWave\HandWaveSegmented\Handwave_Angry.bvh");
            //ILArray<double> data = DataProcess.ProcessBVH(ref bvh, Representation.radian);

            /////////////////////////////////////////////
            // Create graph description
            /////////////////////////////////////////////
            // Factory
            var factory = new GraphicModelFactory();
            factory.RegisterFactor(new GPLVMFactorBuilder());
            factory.RegisterKernel(new LinearKernelBuilder());
            factory.RegisterKernel(new SumKernelBuilder());
            factory.RegisterKernel(new RBFKernelBuilder());
            factory.RegisterKernel(new WhiteKernelBuilder());
            factory.RegisterFactor(new DynamicsModelFactorBuilder("Markov"));

            // gpdm object
            GPDM gpdm = new GPDM(factory);



            // Factors and variables descriptions
            gpdm.GPLVMDesc.Name = "X-Y GPLVM";
            gpdm.GPLVMDesc.Kernel = factory.BuildKernelDesc("Sum kernel", gpdm.GPLVMDesc);
            // TODO: the factory should be used for combining the kernels
            ((CompoundKernelDesc)(gpdm.GPLVMDesc.Kernel)).ChildKernels.Add(factory.BuildKernelDesc("RBF", gpdm.GPLVMDesc));
            ((CompoundKernelDesc)(gpdm.GPLVMDesc.Kernel)).ChildKernels.Add(factory.BuildKernelDesc("White", gpdm.GPLVMDesc));
            gpdm.DynamicsDesc.Name = "latent dynamics";
            gpdm.DynamicsDesc.Kernel = factory.BuildKernelDesc("Sum kernel", gpdm.DynamicsDesc);
            ((CompoundKernelDesc)(gpdm.DynamicsDesc.Kernel)).ChildKernels.Add(factory.BuildKernelDesc("RBF", gpdm.DynamicsDesc));
            ((CompoundKernelDesc)(gpdm.DynamicsDesc.Kernel)).ChildKernels.Add(factory.BuildKernelDesc("White", gpdm.DynamicsDesc));


            // Root description
            var rootDesc = new PlateDesc("root", null);

            var plateXYDesc = new PlateDesc("X-Y plate", rootDesc);
            var varX = new VariableDesc("X", 3, EVariableMode.LatentExpandable);
            var varY = new VariableDesc("Y", data.S[1], EVariableMode.Observed);
            var varKRBF = new VariableDesc("RBF kernel", 2, EVariableMode.LatentUnique);
            var varKWhite = new VariableDesc("White kernel", 1, EVariableMode.LatentUnique);
            var varKRBF2 = new VariableDesc("dyn RBF kernel", 2, EVariableMode.LatentUnique);
            var varklinear = new VariableDesc("dyn White kernel", 1, EVariableMode.LatentUnique);

            plateXYDesc.AddVariableDesc(varX);
            plateXYDesc.AddVariableDesc(varY);

            rootDesc.AddFactorDesc(gpdm.GPLVMDesc);
            rootDesc.AddFactorDesc(gpdm.DynamicsDesc);

            rootDesc.AddVariableDesc(varKRBF);
            rootDesc.AddVariableDesc(varKWhite);
            rootDesc.AddVariableDesc(varKRBF2);
            rootDesc.AddVariableDesc(varklinear);

            gpdm.GPLVMDesc.ConnectionPoints.FindByName("Latent").ConnectToVariable(varX);
            gpdm.GPLVMDesc.ConnectionPoints.FindByName("Observed").ConnectToVariable(varY);
            gpdm.GPLVMDesc.ConnectionPoints.FindByName("RBF kernel parameters").ConnectToVariable(varKRBF);
            gpdm.GPLVMDesc.ConnectionPoints.FindByName("White kernel parameters").ConnectToVariable(varKWhite);

            gpdm.DynamicsDesc.ConnectionPoints.FindByName("Markov").ConnectToVariable(varX);
            gpdm.DynamicsDesc.ConnectionPoints.FindByName("RBF kernel parameters").ConnectToVariable(varKRBF2);
            gpdm.DynamicsDesc.ConnectionPoints.FindByName("White kernel parameters").ConnectToVariable(varklinear);

            // Emotions variable description
            //var varEmotion = new StyleVariableDesc("Emotion", EVariableMode.Observed);
            //varEmotion.SubStyles.Add("angry");
            //varEmotion.SubStyles.Add("neutral");
            //varEmotion.SubStyles.Add("sad");
            //varEmotion.SubStyles.Add("happy");
            //plateXYDesc.AddVariableDesc(varEmotion);

            /////////////////////////////////////////////
            // Build the actual graph with data
            // (factors connection points stay not connected yet)
            /////////////////////////////////////////////
            var rootPlate = factory.AddPlate(rootDesc, null);
            for (int i = 0; i < data.S[0]; i++)
            {
                var XYPlate = factory.AddPlate(plateXYDesc, rootPlate);
                XYPlate.Variables.FindByName("X").Data = new double[3] { i, 2, 3 }; // latent initialization
                double[] observed = null;
                data[i, ILMath.full].ExportValues(ref observed);
                Array.Resize(ref observed, data.S[1]);
                XYPlate.Variables.FindByName("Y").Data = observed; // observed data
                //string sEmotion = (i < 5 ? "sad" : "happy");
                //((StyleVariableData)XYPlate.Variables.FindByName("Emotion")).SetSubStyleData(sEmotion);
            }
            rootPlate.Variables.FindByName("RBF kernel").Data = new double[2] { 0, 0 };
            rootPlate.Variables.FindByName("White kernel").Data = new double[1] { 0 };
            rootPlate.Variables.FindByName("dyn RBF kernel").Data = new double[2] { 0, 0 };
            rootPlate.Variables.FindByName("dyn White kernel").Data = new double[1] { 0 };

            /////////////////////////////////////////////
            // Make the matrix representation
            /////////////////////////////////////////////
            var matrixForm = new MatrixForm(factory, rootPlate);
            matrixForm.Optimize(20, true);


        }
    }
}
