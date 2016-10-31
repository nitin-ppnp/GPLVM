using ILNumerics;
using GPLVM.Prior;
using GPLVM.Kernel;
using System.Xml;

namespace GPLVM.GPLVM
{
    public enum GPLVMType
    {
        normal,
        back,
        style,
        dynamic
    }

    public enum ApproximationType
    {
        ftc, // full training conditional
        dtc, // deterministic training conditional
        fitc // fully independent training conditional
    }

    public enum LerningMode
    {
        prior,
        posterior,
        selfposterior
    }

    public enum Mask
    {
        kernel,
        latents,
        full
    }

    public interface IGPLVM
    {
        GPLVMType Type
        {
            get;
        }

        int NumParameter
        {
            get;
        }

        int NumParameterInHierarchy
        {
            get;
        }

        ILArray<double> LogParameter
        {
            get;
            set;
        }

        int LatentDimension
        {
            get;
        }

        ILArray<double> Y
        {
            get;
            set;
        }

        ILArray<double> X
        {
            get;
            set;
        }

        ILArray<double> Xu
        {
            get;
            set;
        }

        ILArray<double> Segments
        {
            get;
        }

        ILArray<double> LatentGradient
        {
            get;
            set;
        }

        ILArray<double> PostLatentGradient
        {
            get;
            set;
        }

        IKernel Kernel
        {
            get;
            set;
        }

        IPrior Prior
        {
            get;
            set;
        }

        ILArray<double> Bias
        {
            get;
        }

        ILArray<double> Scale
        {
            get;
        }

        ApproximationType ApproxType
        {
            get;
            set;
        }

        int NumInducing
        {
            get;
            set;
        }

        ILArray<double> PostMean
        {
            get;
            set;
        }

        ILArray<double> Ynew
        {
            get;
            set;
        }

        ILArray<double> TestInput
        {
            get;
            set;
        }

        ILArray<double> PostVar
        {
            get;
            set;
        }

        LerningMode Mode
        {
            get;
            set;
        }

        Mask Masking
        {
            get;
            set;
        }

        double Noise
        {
            get;
        }

        void AddData(ILInArray<double> data);

        // constructs the model
        void Initialize();

        double LogLikelihood(); // to get the loglikelihood of the of the object and its children
        ILRetArray<double> LogLikGradient(); // to get the gradients of all parameters of the object and its children

        ILRetArray<double> UpdateParameter(); // recursive function; returns latents from the childs

        ILRetArray<double> PredictData(ILInArray<double> testInputs, ILOutArray<double> yVar = null); // reconstructs data given test inputs
        

        // Read and Write Data to a XML file
        void Read(ref XmlReader reader);
        void Write(ref XmlWriter writer);
    }
}
