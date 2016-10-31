using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace FactorGraph.DataNodes
{
    interface IDataNodeWithSegments
    {
        void AddData(ILArray<double> aTrial);
        ILRetArray<int> Segments
        {
            get;
            set;
        }
    }
}
