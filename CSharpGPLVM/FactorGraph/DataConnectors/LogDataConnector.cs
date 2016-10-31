using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM;
using FactorGraph.Core;

namespace FactorGraph.DataConnectors
{
    /// <summary>
    /// Transforming Data Connector.
    /// Data node contains log of the data
    /// </summary>
    [DataContract(IsReference = true)]
    public class LogDataConnector : DataConnector
    {
        [DataMember()]
        protected ILArray<double> aExpData = ILMath.localMember<double>();

        public LogDataConnector(string sName)
            : base(sName)
        {
        }

        public override void PushParametesToFactorNode()
        {
            // Precalculate the aExpData
            aExpData.a = Util.exp(ConnectedDataNode.GetValues());
        }

        protected override ILRetArray<double> GetValues()
        {
            return aExpData;
        }

        protected override void SetValues(ILInArray<double> inV)
        {
            using (ILScope.Enter(inV))
            {
                ILArray<double> V = ILMath.check(inV);
                ConnectedDataNode.SetValues(ILMath.log(V));
                // Just to be consistent with numerical precision issues
                aExpData.a = Util.exp(ConnectedDataNode.GetValues());
            }
        }
    }
}
