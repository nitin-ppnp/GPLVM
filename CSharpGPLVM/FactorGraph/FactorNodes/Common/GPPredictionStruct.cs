using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM.Kernel;

namespace FactorGraph.FactorNodes.Common
{
    [DataContract(IsReference = true)]
    [KnownType(typeof(BiasKern))]
    [KnownType(typeof(CompoundKern))]
    [KnownType(typeof(LinearAccelerationKern))]
    [KnownType(typeof(LinearKern))]
    [KnownType(typeof(RBFAccelerationKern))]
    [KnownType(typeof(RBFKern))]
    [KnownType(typeof(RBFKernBack))]
    [KnownType(typeof(StyleKern))]
    [KnownType(typeof(StyleAccelerationKern))]
    [KnownType(typeof(TensorKern))]
    [KnownType(typeof(WhiteKern))]
    [KnownType(typeof(ModulationKern))]
    [KnownType(typeof(DirectionKern))]
    [KnownType(typeof(PGEKern))]
    public class GPPredictionStruct
    {
        [DataMember()]
        public IKernel Kernel;
        [DataMember()]
        public ILArray<double> aAlpha = ILMath.localMember<double>();
    }
}
