using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GraphicalModel.Kernels;

namespace GraphicalModel.Factors
{
    public abstract class FactorBuilder
    {
        public string Type;
        public abstract Factor BuildFactor(FactorDesc desc);
        public abstract FactorDesc BuildDesc();
    }

    public enum EFactorConnectionPointDataType
    {
        Unique,
        Expandable
    }

    public class FactorConnectionPointDesc
    {
        protected string sName;
        public VariableDesc ConnectedVariable;
        protected EFactorConnectionPointDataType eDataType;
        protected FactorDesc pOwnerFactorDesc;

        public FactorConnectionPointDesc(string name, FactorDesc ownweFactorDesc)
        {
            sName = name;
            pOwnerFactorDesc = ownweFactorDesc;
        }

        public FactorDesc OwnerFactorDesc
        {
            get { return pOwnerFactorDesc; }
        }

        public string Name
        {
            get { return sName; }
        }

        public void Disconnect()
        {
            if (ConnectedVariable != null)
            {
                ConnectedVariable.ConnectedFactors.Remove(this);
                ConnectedVariable = null;
            }
        }

        public void ConnectToVariable(VariableDesc variableDesc)
        {
            Disconnect();
            variableDesc.ConnectedFactors.Add(this);
            ConnectedVariable = variableDesc;
        }
    }

    public class FactorConnectionPointDescList : List<FactorConnectionPointDesc>
    {
        public FactorConnectionPointDesc FindByName(string name)
        {
            var cpt = this.Find(delegate(FactorConnectionPointDesc cp) { return cp.Name == name; });
            if (cpt == null)
                throw new GraphicalModelException("Factor connection point description \"" + name + "\" is not found.");
            return cpt;
        }
    }

    public class FactorConnectionPoint
    {
        public Factor pOwnerFactor;
        public FactorConnectionPointDesc Desc;
        public Object ConnectedDataObject;
        //public ILArray<double> FunctionGradient;

        public FactorConnectionPoint(FactorConnectionPointDesc desc, Factor ownerFactor)
        {
            Desc = desc;
            pOwnerFactor = ownerFactor;
            //FunctionGradient = ILMath.empty();
        }

        public Factor OwnerFactor
        {
            get { return pOwnerFactor; }
        }
    }

    public class FactorConnectionPointList : List<FactorConnectionPoint>
    {
        public FactorConnectionPoint FindByName(string name)
        {
            var cpt = this.Find(delegate(FactorConnectionPoint cp) { return cp.Desc.Name == name; });
            if (cpt == null)
                throw new GraphicalModelException("Factor connection point \"" + name + "\" is not found.");
            return cpt;
        }
    }

    public class FactorDesc : InPlateGraphicModelElementDesc
    {
        protected string sFactorType;
        protected FactorConnectionPointDescList lConnectionPoints;
        public KernelDesc Kernel;

        public FactorDesc(string name, string factorType)
            : base(name)
        {
            sFactorType = factorType;
            lConnectionPoints = new FactorConnectionPointDescList();
            Kernel = null;
        }

        public string FactorType
        {
            get { return sFactorType; }
        }

        public void AddConnectionPointDesc(FactorConnectionPointDesc newConnectionPoint)
        {
            lConnectionPoints.Add(newConnectionPoint);
        }

        public FactorConnectionPointDescList ConnectionPoints
        {
            get
            {
                FactorConnectionPointDescList res = new FactorConnectionPointDescList();
                res.AddRange(lConnectionPoints);
                if (Kernel != null)
                    res.AddRange(Kernel.ConnectionPoints);
                return res;
            }
        }
    }

    public class Factor //: PlateData
    {
        protected FactorDesc pFactorDescription;
        protected FactorConnectionPointList lConnectionPoints;
        public PlateData Plate;
        public Kernel KernelObject;

        public Factor(FactorDesc desc)
        {
            pFactorDescription = desc;
            lConnectionPoints = new FactorConnectionPointList();
            foreach (var connectionPointDesc in pFactorDescription.ConnectionPoints)
                lConnectionPoints.Add(new FactorConnectionPoint(connectionPointDesc, this));
        }

        public FactorDesc Desc
        {
            get { return pFactorDescription; }
        }

        public FactorConnectionPointList ConnectionPoints
        {
            get { return lConnectionPoints; }
        }

        // TODO: make abstract
        public virtual void Initialize()
        {
            KernelObject.Initialise();
        }

        // TODO: make abstract
        public virtual void ComputeAllGradients()
        {
        }

        // TODO: make abstract
        public virtual double FunctionValue()
        {
            return 0;
        }
    }
}
