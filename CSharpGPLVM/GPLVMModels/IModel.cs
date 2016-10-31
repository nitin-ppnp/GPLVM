using DataFormats;
using GPLVM.Utils.Character;
using ILNumerics;

namespace Models
{
    public interface IModel
    {
        DataPostProcess PostProcess
        {
            get;
        }

        Skeleton skeleton
        {
            get;
        }

        double FrameTime
        {
            get;
        }

        void learnModel();
        void SaveModel();
        bool init(bool isLoad);
        void showPlots();

        Frame PredictData(ILInArray<double> inTestInputs, ILInArray<double> inStyleValue);
        Frame PlayTrainingData();

        void Reset();
    }
}
