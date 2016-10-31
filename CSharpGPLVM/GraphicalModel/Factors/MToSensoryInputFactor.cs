using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace GraphicalModel.Factors
{
    public class MToSensoryInputFactor : Factor
    {
        public static string CP_M = "Model";
        public static string CP_S = "SensoryInput";

        public MToSensoryInputFactor(FactorDesc desc)
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

    public class MToSensoryInputFactorBuilder : FactorBuilder
    {
        public MToSensoryInputFactorBuilder()
        {
            Type = "MToSensoryInput";
        }

        public override Factor BuildFactor(FactorDesc desc)
        {
            return new MToSensoryInputFactor(desc);
        }

        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(MToSensoryInputFactor.CP_M, desc));
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(MToSensoryInputFactor.CP_S, desc));
            return desc;
        }
    }
}