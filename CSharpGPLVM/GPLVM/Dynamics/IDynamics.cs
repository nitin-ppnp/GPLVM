using ILNumerics;
using GPLVM.Prior;

namespace GPLVM.Dynamics
{
    public enum DynamicType
    {
        DynamicTypeVelocity,        // first order dynamic
        DynamicTypeAcceleration     // second order dynamic
    }

    public interface IDynamics : IPrior
    {
        DynamicType DynamicsType
        {
            get;
        }

        int NumInducing
        {
            get;
            set;
        }

        ILRetArray<double> PredictData(ILInArray<double> inTestInputs, bool xVar = false);
        ILRetArray<double> SimulateDynamics(ILInArray<double> inTestInput, int numTimeSteps);
        ILRetArray<double> KernelGradient();
    }
}
