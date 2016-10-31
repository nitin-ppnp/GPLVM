using System.IO;
using ILNumerics;

namespace GPLVM.DebugTools
{
    public class FileLogger
    {
        protected StreamWriter file;
        public bool Enabled = true;

        public FileLogger(string sFileName)
        {
            file = new StreamWriter(sFileName);
        }

        public StreamWriter Stream
        {
            get { return file; }
        }

        public void Close()
        {
            file.Close();
        }

        public void WriteDelimiter(char ch)
        {
            if (!Enabled)
                return;
            for (int i = 0; i<60; i++)
                file.Write(ch);
            file.WriteLine("");
        }

        public void Write(string sMessage, ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                if (!Enabled)
                    return;
                ILArray<double> data = ILMath.check(inData);
                WriteDelimiter('-');
                file.WriteLine("{0} ({1}x{2})", sMessage, data.S[0], data.S[1]);
                for (int k1 = 0; k1 < data.S[0]; k1++)
                {
                    for (int k2 = 0; k2 < data.S[1]; k2++)
                    {
                        double val = (double)data[k1, k2];
                        file.Write(val.ToString() + " ");
                        //file.Write(val.ToString("  0.000000e+00; -0.000000e+00;  0.000000e+00", CultureInfo.InvariantCulture));
                    }
                    file.WriteLine();
                }
            }
        }

       
    }
}
