using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace GraphicalModel.Factors
{
    public class MToDynamicsIndexFactor : Factor
    {
        public static string CP_M = "Model";
        public static string CP_Z = "DynamicsIndex";

        public MToDynamicsIndexFactor(FactorDesc desc)
            : base(desc)
        {
        }

        public override void Initialize()
        {

        }

        public double LogLikelihood()
        {
            double L = 0;
            return L;
        }



        public override void ComputeAllGradients()
        {

        }

        public override double FunctionValue()
        {
            return LogLikelihood();
        }
    }

    public class MToDynamicsIndexFactorBuilder : FactorBuilder
    {
        public MToDynamicsIndexFactorBuilder()
        {
            Type = "MToDynamicsIndex";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new MToDynamicsIndexFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(MToDynamicsIndexFactor.CP_M, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(MToDynamicsIndexFactor.CP_Z, desc));
            return desc;
        }
    }
}