using ILNumerics;
using System.Collections.Generic;
using System.Xml;

namespace GPLVM.Styles
{
    public interface IStyle
    {
        int NumParameter
        {
            get;
        }

        ILArray<double> LogParameter
        {
            get;
            set;
        }

        string StyleName
        {
            get;
        }

        List<string> SubStyles
        {
            get;
        }

        ILArray<double> StyleVariable
        {
            get;
            set;
        }

        ILArray<double> StyleInducingVariable
        {
            get;
            set;
        }

        ILArray<double> FactorIndex
        {
            get;
        }

        ILArray<double> FactorIndexInducing
        {
            get;
            set;
        }

        ILRetArray<double> StyleGradient(ILInArray<double> gSSequence);
        ILRetArray<double> StyleInducingGradient(ILInArray<double> gSSequence);
        //ILRetArray<double> ComputeStarMatrix(ILInArray<double> inStyleStar, int numSteps);

        // Adds a sub style; updates the factorIndex; updates the data object with the new latents and factorIndexes
        void AddSubStyle(string subStyleName, int seqenceLength);

        void InterpolateSubStyles(ILInArray<double> interpolationRatio);

        // Read and Write Data to a XML file
        void Read(ref XmlReader reader);
        void Write(ref XmlWriter writer);
        bool ReadMat(string filename);
    }
}
