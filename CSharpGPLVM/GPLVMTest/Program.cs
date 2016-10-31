namespace GPLVMTest
{
    class Program
	{
		static void Main(string[] args)
		{
            loadTest(7);
		}

        private static void loadTest(int mode)
        {
            switch (mode)
            {
                case 1:
                    IKTest test = new IKTest();
                    test.go();
                    break;
                case 2:
                    PredictTest test2 = new PredictTest();
                    test2.go();
                    break;
                case 3:
                    MinimizationTest test3 = new MinimizationTest();
                    test3.go();
                    break;
                case 4:
                    FootSkateTest test4 = new FootSkateTest();
                    test4.go();
                    break;
                case 5:
                    TrainingDataTest test5 = new TrainingDataTest();
                    test5.go();
                    break;
                case 6:
                    EmbeddingPlots test6 = new EmbeddingPlots();
                    test6.go();
                    break;
                case 7:
                    TrajectoryPlots test7 = new TrajectoryPlots();
                    test7.go();
                    break;
            }
        }
	}
}
