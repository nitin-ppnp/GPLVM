using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using GraphicalModel;
using GraphicalModel.Factors.DynamicsFactors;

//QUESTIONS:

namespace GraphicalModel.Factors
{

    public class GPDM
    {
        protected FactorDesc GPLVMFactorDesc;
        protected FactorDesc dynFactorDesc;

        public GPDM(GraphicModelFactory factory)
        {
            this.GPLVMFactorDesc = factory.BuildFactorDesc("GPLVM");
            this.dynFactorDesc = factory.BuildFactorDesc("Dynamics Model");
        }

        public FactorDesc GPLVMDesc
        {
            get
            {
                return this.GPLVMFactorDesc;
            }
        }

        public FactorDesc DynamicsDesc
        {
            get
            {
                return this.dynFactorDesc;
            }
        }


    }


    public class DynamicsModelFactorBuilder : FactorBuilder
    {
        
        static Dictionary<string,string> DynamicsFactorTypes;   //Available Dynamics Types 
                                                                //Key: Dynamics Type; 
                                                                //Value: Name of the Class implementing that type
        private string dynamicsType;

        static DynamicsModelFactorBuilder()
        {
            //Initialize the Dictionary 
            DynamicsFactorTypes = new Dictionary<string, string>();
            DynamicsFactorTypes.Add(FirstOrderMarkovDynamicsFactor.CP_TYPE, "GraphicalModel.Factors.DynamicsFactors.FirstOrderMarkovDynamicsFactor");
            DynamicsFactorTypes.Add("Velocity", "GraphicalModel.Factors.DynamicsFactors.VelocityDynamicsFactor");
            DynamicsFactorTypes.Add("Acceleration", "GraphicalModel.Factors.DynamicsFactors.AccelerationDynamicsFactor");
            //If new type of dynamics defined then it to the dictionary
        }

        public DynamicsModelFactorBuilder(string type)
        {
            if (DynamicsFactorTypes.ContainsKey(type))
            {
                this.Type = "Dynamics Model";
                this.dynamicsType = type;
            }
            else
                throw new GraphicalModelException("Invalid Dynamics Type");

        }

        //Complete
        public override Factor BuildFactor(FactorDesc desc)
        {
            return System.Activator.CreateInstance(System.Reflection
                                                         .Assembly
                                                         .GetExecutingAssembly()
                                                         .GetType(DynamicsFactorTypes[dynamicsType], false, false), desc) 
                                                          as Factor;
        }

        //Complete
        public override FactorDesc BuildDesc()
        {
            var desc = new FactorDesc("[none]", Type);
            desc.AddConnectionPointDesc(new FactorConnectionPointDesc(dynamicsType, desc));
            return desc;
        }
    }

}
