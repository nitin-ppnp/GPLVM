using System;
using ILNumerics;


namespace GPLVM
{
    public static class DataProcess
    {
        private static double scaleFactor = 0.1;

        public static ILRetArray<double> ProcessC3D(string path)
        {
            using (ILScope.Enter())
            {
                C3DSERVERLib.C3D m_c3d = new C3DSERVERLib.C3D();
                ILArray<double> data = ILMath.empty();
                ILArray<double> position = ILMath.zeros(1,3);

                Console.WriteLine("Loading " + path + "...");

                if (m_c3d.Open(path, 3) == 0)
                {
                    int tmp = m_c3d.GetVideoFrame(0);
                    data = ILMath.zeros(m_c3d.GetVideoFrame(1) - m_c3d.GetVideoFrame(0) + 1, m_c3d.GetNumber3DPoints() * 3);

                    for (int i = m_c3d.GetVideoFrame(0); i <= m_c3d.GetVideoFrame(2); i++)
                    {
                        //int cnt = 0;
                        for (int j = 0; j < m_c3d.GetNumber3DPoints(); j++)
                        {
                            position[0] = m_c3d.GetPointData(j, 0, i, 1) * (-scaleFactor);
                            position[2] = m_c3d.GetPointData(j, 1, i, 1) * scaleFactor;
                            position[1] = m_c3d.GetPointData(j, 2, i, 1) * scaleFactor;

                            data[i - m_c3d.GetVideoFrame(0), ILMath.r(j * 3, j * 3 + 2)] = position.C;

                            //data[i - m_c3d.GetVideoFrame(0), cnt++] = m_c3d.GetPointData(j, 0, i, 1);
                            //data[i - m_c3d.GetVideoFrame(0), cnt++] = m_c3d.GetPointData(j, 1, i, 1);
                            //data[i - m_c3d.GetVideoFrame(0), cnt++] = m_c3d.GetPointData(j, 2, i, 1);
                        }
                    }
                }
                m_c3d.Close();

                return data;
            }
        }
    }
}
