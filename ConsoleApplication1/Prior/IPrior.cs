using System;
using ILNumerics;
using System.Xml;

namespace GPLVM.Prior
{
    public enum PriorType
    {
        PriorTypeNoPrior,
        PriorTypeGauss,
        PriorTypeLocallyLinear,
        PriorTypeDynamics,
        PriorTypeCompound,
        PriorTypeConnectivity
    };

    public interface IPrior
    {
        int NumParameter
        {
            get;
        }

        PriorType Type
        {
            get;
        }

        ILArray<double> LogParameter
        {
            get;
            set;
        }

        ILArray<double> X
        {
            get;
        }

        ILArray<double> Xnew
        {
            get;
            set;
        }

        Guid Key
        {
            get;
        }

        void Initialize(ILInArray<double> _data, ILInArray<double> segments);
        void Initialize(Data _data);

        double LogLikelihood();
        double PostLogLikelihood();
        ILRetArray<double> LogLikGradient();
        ILRetArray<double> PostLogLikGradient();

        void UpdateParameter(Data data);
        void UpdateParameter(ILInArray<double> _data);

        // Read and Write Data to a XML file
        void Read(ref XmlReader reader);
        void Write(ref XmlWriter writer);
    }
}
