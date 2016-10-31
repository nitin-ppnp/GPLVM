using System;
using System.Windows.Forms;

namespace GPDMNetworkStream
{
    static class Program
    {
        private static int selection = 5;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            switch (selection)
            {
                case 1:
                    //Application.Run(new Form1());
                    break;
                case 2:
                    //Application.Run(new Form2());
                    break;
                case 3:
                    //Application.Run(new Form3());
                    break;
                case 4:
                    Application.Run(new AvatarBVHIK());
                    break;
                case 5:
                    Application.Run(new TippingTurningStream());
                    break;
            }
            
        }
    }
}
