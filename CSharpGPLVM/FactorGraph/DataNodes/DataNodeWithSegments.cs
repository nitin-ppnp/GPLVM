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
    public class DataNodeWithSegments : MatrixDataNode, IDataNodeWithSegments
    {
        [DataMember()]
        protected ILArray<int> aSegments = ILMath.localMember<int>(); // Start indexes of the segments

        public ILRetArray<int> Segments
        {
            get { return aSegments.C; }
            set { aSegments.a = value.C; }
        }

        public DataNodeWithSegments(string sName)
            : base(sName)
        {
        }

        public void AddData(ILArray<double> aTrial)
        {
            using (ILScope.Enter())
            {
                aSegments.a = Util.Concatenate<int>(aSegments, new int[] { GetValues().S[0] });
                SetValues(Util.Concatenate<double>(GetValues(), aTrial));
                OnValuesSizeChanged();
            }
        }
    }
}
