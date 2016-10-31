using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM.Kernel;
using FactorGraph.Core;

namespace FactorGraph.FactorNodes
{
    [DataContract(IsReference = true)]
    public class GPRegressionNode : GPLVMNode
    {
        public GPRegressionNode(string sName, IKernel iKernel)
            : base(sName, iKernel)
        {
        }

        public override void Initialize()
        {
            dcX.ConnectedDataNode.SetOptimizingMaskNone();
            dcY.ConnectedDataNode.SetOptimizingMaskNone();
            base.Initialize();
        }
    }
}
