using GPLVM.Embeddings;
using ILNumerics;
using System.Xml;

namespace GPLVM.Backconstraint
{
    public enum BackConstType
    {
        none,
        kbr,    // Kernel-based regression back constraint function.
        mlp,    // Multi-layer perceptron back constraint function.
        ptc,     // periodic topological constraint
        halfPtc,     // half circle periodic topological constraint
        ptcLinear
    };

    public interface IBackconstraint
    {
        BackConstType Type
        {
            get;
        }

        /// <summary>
        /// Gets the number of parameters of the objects wants to be optimized. 
        /// </summary>
        int NumParameter
        {
            get;
        }

        /// <summary>
        /// The log of the parameter in the model wants to be optimized. 
        /// </summary>
        ILArray<double> LogParameter
        {
            get;
            set;
        }

        void Initialize(ILInArray<double> inY, ILInArray<double> inX, ILInArray<double> inSegments, XInit initX = XInit.pca);
        ILRetArray<double> BackConstraintsGradient(ILInArray<double> ingX);
        ILArray<double> GetBackConstraints();

        // Read and Write Data to a XML file
        void Read(ref XmlReader reader);
        void Write(ref XmlWriter writer);
    }
}
