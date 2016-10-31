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

    class GPLVMTest
    {
        public static void Test(string[] args)
        {
            BVHData bvh = new BVHData(Representation.radian);
            ILArray<double> data = bvh.LoadFile(@"..\..\..\..\Data\HandWave\HandWaveSegmented\Handwave_Angry.bvh");

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

            // Root description
            var rootDesc = new PlateDesc("root", null);

            // Factors and variables descriptions
            var factorGPLVMDesc = factory.BuildFactorDesc("GPLVM");
            factorGPLVMDesc.Name = "X-Y GPLVM";
            factorGPLVMDesc.Kernel = factory.BuildKernelDesc("Sum kernel", factorGPLVMDesc);
            // TODO: the factory should be used for combining the kernels
            ((CompoundKernelDesc)(factorGPLVMDesc.Kernel)).ChildKernels.Add(factory.BuildKernelDesc("RBF", factorGPLVMDesc));
            ((CompoundKernelDesc)(factorGPLVMDesc.Kernel)).ChildKernels.Add(factory.BuildKernelDesc("White", factorGPLVMDesc));
            var plateXYDesc = new PlateDesc("X-Y plate", rootDesc);
            var varX = new VariableDesc("X", 3, EVariableMode.LatentExpandable);
            var varY = new VariableDesc("Y", data.S[1], EVariableMode.Observed);
            var varKRBF = new VariableDesc("RBF kernel", 2, EVariableMode.LatentUnique);
            var varKWhite = new VariableDesc("White kernel", 1, EVariableMode.LatentUnique);
            plateXYDesc.AddVariableDesc(varX);
            plateXYDesc.AddVariableDesc(varY);
            rootDesc.AddFactorDesc(factorGPLVMDesc);
            rootDesc.AddVariableDesc(varKRBF);
            rootDesc.AddVariableDesc(varKWhite);
            factorGPLVMDesc.ConnectionPoints.FindByName("Latent").ConnectToVariable(varX);
            factorGPLVMDesc.ConnectionPoints.FindByName("Observed").ConnectToVariable(varY);
            factorGPLVMDesc.ConnectionPoints.FindByName("RBF kernel parameters").ConnectToVariable(varKRBF);
            factorGPLVMDesc.ConnectionPoints.FindByName("White kernel parameters").ConnectToVariable(varKWhite);

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

            /////////////////////////////////////////////
            // Make the matrix representation
            /////////////////////////////////////////////
            var matrixForm = new MatrixForm(factory, rootPlate);
            matrixForm.Optimize(20, true);


        }
    }
}
