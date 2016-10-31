using System;
using ILNumerics;
using System.Xml;

namespace GPLVM.Kernel
{
    // possible types of kernels
    public enum KernelType
    {
        KernelTypeLinear,               // linear kernel
        KernelTypeLinearAcceleration,   // linear kernel for velocity (1-st order dynamics)
        KernelTypeRBF,                  // radial basis function kernel
        KernelTypeRBFBack,
        KernelTypeRBFAcceleration,      // radial basis function velocity kernel
        KernelTypeBias,            // white noise kernel
        KernelTypeWhite,            // white noise kernel
        KernelTypeCompound,         // collection of kernels
        KernelTypeTensor,           // collection of kernels for tensor product
        KernelTypeStyle,             // linear style kernel
        KernelTypeDirection,          // vectors directions similarity kernel
        KernelTypeModulation,         // constant modulation kernel
        KernelTypeStyleAcceleration,  // style kernel for acceleration dynamics
        KernelTypePGE,                // product of Gaussian experts 
    };

    public enum Flag
    {
        learning,
        reconstruction,
        postlearning
    }

    public interface IKernel
    {
        ///<summary>
        ///Gets the kernel type.
        ///</summary>
        KernelType Type
        {
            get;
        }

        ///<summary>
        ///Gets the number of parameters.
        ///</summary>
        int NumParameter
        {
            get;
        }

        ///<summary>
        ///Gets the kernel parameters of type ILArray<double>.
        ///</summary>
        ILArray<double> Parameter
        {
            get;
            set;
        }

        ///<summary>
        ///The kernel matrix of type ILArray<double>.
        ///</summary>
        ILArray<double> K
        {
            get;
        }

        ///<summary>
        ///The kernel matrix of type ILArray<double>.
        ///</summary>
        ILArray<double> Kuf
        {
            get;
        }

        ///<summary>
        ///The kernel matrix of type ILArray<double>.
        ///</summary>
        ILArray<double> Krec
        {
            get;
        }

        ILArray<double> LogParameter
        {
            get;
            set;
        }

        double Noise
        {
            get;
        }

        ///<summary>
        ///Gets the unique ID of the object.
        ///</summary>
        Guid Key
        {
            get;
        }

        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<double>.
        /// </returns>
        ILRetArray<double> ComputeKernelMatrix(ILInArray<double> inX1, ILInArray<double> inX2, Flag flag = Flag.learning); // example in rbf kernel

        /// <summary>
        /// Computes the kernel matrix of the kernel.
        /// </summary>
        /// <param name="X1">First n times q matrix of latent points.</param>
        /// <param name="X2">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the kernel matrix of type ILArray<double>.
        /// </returns>
        ILRetArray<double> ComputeKernelMatrix(Data _data); // example in rbf and linear kernel

        /// <summary>
        /// Computes the diagonal of the kernel given a design matrix ofinputs.
        /// </summary>
        /// <param name="X1">Second m times q matrix of latent points.</param>
        /// <returns>
        /// The method returns the diagonal matrix of type ILArray<double>.
        /// </returns>
        ILRetArray<double> ComputeDiagonal(ILInArray<double> inX);

        /// <summary>
        /// Computes the gradient w.r.t the latents.
        /// </summary>
        /// <param name="data">The data dictionary.</param>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradients of the latent points of type ILArray<double>.
        /// </returns>
        ILRetArray<double> LogLikGradientX(ILInArray<double> inX, ILInArray<double> indL_dK); // example in rbf kernel
        ILRetArray<double> LogLikGradientX(Data data, ILInArray<double> indL_dK); // example in rbf kernel
        ILRetCell LogLikGradientX(ILInArray<double> inXu, ILInArray<double> indL_dKuu, ILInArray<double> inX, ILInArray<double> indL_dKuf); // for sparse approximations

        /// <summary>
        /// Computes the gradient of the kernel parameters.
        /// </summary>
        /// <param name="dL_dK">Derivative of the loglikelihood w.r.t the kernel matrix K.</param>
        /// <returns>
        /// The method returns the gradients of the kernel parameters of type ILArray<double>.
        /// </returns>
        ILRetArray<double> LogLikGradientParam(ILInArray<double> indL_dK); // example in rbf kernel
        ILRetArray<double> LogLikGradientParamTensor(ILInArray<double> inX, ILInArray<double> indL_dK);

        /// <summary>
        /// Computes the gradient of the latents and kernel parameters.
        /// </summary>
        /// <param name="X">The N times q matrix of latent points.</param>
        /// <param name="dL_dK">Derivative of the likelihood w.r.t the kernel matrix K.</param>
        /// <param name="style">The style object.</param>
        /// <returns>
        /// The method returns the gradients of the latent points and the kernel parameters of type ILArray<double>.
        /// </returns>
        // ILRetArray<double> LogLikGradient(ILInArray<double> X, double dL_dK, IStyle style);

        ILRetArray<double> GradX(ILInArray<double> inX1, ILInArray<double> inX2, int q, Flag flag = Flag.learning);
        ILRetArray<double> DiagGradX(ILInArray<double> inX);
        ILRetArray<double> DiagGradParam(ILInArray<double> inX, ILInArray<double> inCovDiag);

        // Read and Write Data to a XML file
        void Read(ref XmlReader reader);
        void Write(ref XmlWriter writer);
    }
}
