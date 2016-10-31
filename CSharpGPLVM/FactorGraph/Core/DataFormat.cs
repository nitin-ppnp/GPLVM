using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace FactorGraph.Core
{
    public class DataFormat
    {
        public ILArray<int> Dimensions = ILMath.empty<int>(); // Real dimensionality of the data        
    }
}
