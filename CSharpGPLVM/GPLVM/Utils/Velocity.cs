using ILNumerics;

namespace GPLVM
{
    public static partial class Util
    {

        /// <summary>
        /// Computes the velocity of a NxD data matrix.
        /// </summary>
        /// <param name="inData">NxD data matrix, where N is the number of data points and D is the dimension.</param>
        /// <returns>Velocity of the data matrix.</returns>
        public static ILRetArray<T> Velocity<T>(ILInArray<T> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<T> data = ILMath.check(inData);
                ILArray<T> vel = ILMath.zeros<T>(data.S);

                ILArray<T> prevPoint = ILMath.empty<T>();
                ILArray<T> currPoint = ILMath.empty<T>();

                for (int t = 0; t < data.S[0]; t++)
                {
                    // Get indices of current and previous frame.
                    int curridx = t;
                    int previdx = t - 1;
                    if (previdx < 0)
                    { // Don't have a previous frame, so use the next one.
                        curridx = t + 1;
                        previdx = t;
                    }

                    prevPoint = data[previdx, ILMath.full].C;
                    currPoint = data[curridx, ILMath.full].C;

                    vel[t, ILMath.full] = (currPoint - prevPoint);
                }

                return vel;
            }
        }
    }
}
