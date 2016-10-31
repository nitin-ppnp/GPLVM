using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactorGraph.Core
{
    public class GraphException : Exception
    {
        public GraphException(string sMessage)
            : base(sMessage)
        {
        }

    }
}
