using GPLVM.GPLVM;
using GPLVM;

namespace Models
{
    class Program
    {
        static void Main(string[] args)
        {
            loadModel(7);
        }

        private static void loadModel(int mode)
        {
            switch (mode)
            {
                case 1:
                    HighFiveModel model1 = new HighFiveModel();

                    model1.init(ApproximationType.fitc, OptimizerType.CERCGOptimizer, Representation.exponential);

                    model1.learnModel();
                    //model1.SaveModel("radianDemo2");

                    //model1.showPlots();

                    /*HighFiveModel model3 = new HighFiveModel();
                    model3.init(ApproximationType.dtc, OptimizerType.BFGSOptimizer);
                    model3.learnModel();
                    model3.SaveModel();
                    model3.showPlots();*/
                    break;

                case 2:
                    HighFiveModelStyle model3 = new HighFiveModelStyle();
                    model3.init(ApproximationType.fitc, OptimizerType.SCGOptimizer, Representation.exponential);
                    model3.learnModel();
                    model3.SaveModel("radianDemo_newSCG_.20It");

                    //model3.showPlots();
                    break;

                case 3:
                    HighFiveModelStyleTopPos model4 = new HighFiveModelStyleTopPos();
                    model4.init(ApproximationType.dtc, OptimizerType.SCGOptimizer, Representation.radian);
                    model4.learnModel();
                    model4.SaveModel("radian");

                    model4.showPlots();
                    break;

                case 4:
                    TestGPLVM model2 = new TestGPLVM();
                    model2.init();
                    //model2.learnModel();
                    //model2.SaveModel();
                    //model2.LoadModel();
                    //model2.showPlots();
                    break;

                case 5:
                    HighFiveStyleIK model5 = new HighFiveStyleIK();
                    model5.init(false);
                    model5.learnModel();
                    model5.SaveModel();
                    //model2.LoadModel();
                    model5.showPlots();
                    break;

                case 6:
                    HighFiveStyleIKCoupledDynamics model6 = new HighFiveStyleIKCoupledDynamics();
                    model6.init(false);
                    model6.learnModel();
                    //model6.SaveModel();
                    //model6.LoadModel();
                    model6.showPlots();
                    break;

                case 7:
                    TippingTurning model7 = new TippingTurning();
                    model7.init(false);
                    //model7.learnModel();
                    //model7.SaveModel();
                    //model6.LoadModel();
                    model7.showPlots();
                    break;
                    
            }
        }
    }
}
