using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using FactorGraph.Core;

namespace FactorGraph.DataNodes
{
    [DataContract(IsReference = true)]
    public class CompoundMatrixDataNodeWithSegments : CompoundMatrixDataNode, IDataNodeWithSegments
    {
        public CompoundMatrixDataNodeWithSegments(string sName)
            : base(sName)
        {
        }

        public ILRetArray<int> Segments
        {
            get 
            {
                if ((DCDNs.Count > 0) && (DCDNs.First().DataNode is IDataNodeWithSegments))
                    return (DCDNs.First().DataNode as IDataNodeWithSegments).Segments;
                else
                    return new int[] { 0 };
            }
            set { throw new NotImplementedException("Can't set segments to CompoundMatrixDataNodeWithSegments"); }
        }

        public void AddData(ILArray<double> aTrial)
        {
            throw new NotImplementedException("Can't add data to CompoundMatrixDataNodeWithSegments");
        }
    }
}
