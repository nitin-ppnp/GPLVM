using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicalModel.DataStructures;
using GraphicalModel.Kernels;
using GraphicalModel.Factors;

namespace GraphicalModel
{
    public class GraphicalModelException : Exception
    {
        public GraphicalModelException()
        {
        }

        public GraphicalModelException(string message)
            : base(message)
        {
        }

    }

    public enum EVariableMode
    {
        LatentUnique,
        LatentExpandable,
        Observed,
    }

    public class GraphicModelElement
    {
        protected string sName;

        public GraphicModelElement(string name)
        {
            sName = name;
        }

        public string Name
        {
            get { return sName; }
            set { sName = value; }
        }
    }

    public class InPlateGraphicModelElementDesc : GraphicModelElement
    {
        public PlateDesc ContainerPlate;

        public InPlateGraphicModelElementDesc(string name)
            : base(name)
        {
        }
    }

    public class VariableDesc : InPlateGraphicModelElementDesc
    {
        protected int nDimensions;
        protected EVariableMode eVariableMode;
        protected List<FactorConnectionPointDesc> lConnectedFactors;

        public VariableDesc(string name, int dimensions, EVariableMode variableMode)
            : base(name)
        {
            nDimensions = dimensions;
            eVariableMode = variableMode;
            lConnectedFactors = new List<FactorConnectionPointDesc>();
        }

        public int Dimensions
        {
            get { return nDimensions; }
        }

        public EVariableMode Mode
        {
            get { return eVariableMode; }
        }

        public List<FactorConnectionPointDesc> ConnectedFactors
        {
            get { return lConnectedFactors; }
        }

        public virtual VariableData BuildVariableData()
        {
            return new VariableData(this);
        }
    }

    public class StyleVariableDesc : VariableDesc
    {
        private ObservableList<string> lSubStyles;

        public StyleVariableDesc(string name, EVariableMode variableMode)
            : base(name, 0, variableMode)
        {
            lSubStyles = new ObservableList<string>();
            lSubStyles.ListChanged += OnSubStylesChanged;
        }

        void OnSubStylesChanged(object source, ObservableList<string>.ListChangedEventArgs e)
        {
            nDimensions = lSubStyles.Count;
        }

        public ObservableList<string> SubStyles
        {
            get { return lSubStyles; }
        }

        public override VariableData BuildVariableData()
        {
            return new StyleVariableData(this);
        }
    }

    public class VariableData
    {
        protected VariableDesc pVariableDesc;
        public double[] Data;

        public VariableData(VariableDesc desc)
        {
            pVariableDesc = desc;
            Data = new double[pVariableDesc.Dimensions];
        }

        public VariableDesc Desc
        {
            get { return pVariableDesc; }
        }
    }

    public class StyleVariableData : VariableData
    {
        public StyleVariableData(StyleVariableDesc desc)
            : base(desc)
        {
        }

        public StyleVariableDesc StyleDesc
        {
            get { return (StyleVariableDesc)pVariableDesc; }
        }

        public void SetSubStyleData(string subStyle)
        {
            Data[this.StyleDesc.SubStyles.IndexOf(subStyle)] = 1;
        }
    }

    public class PlateDesc : InPlateGraphicModelElementDesc
    {
        protected List<VariableDesc> lVariablesDesc;
        protected List<FactorDesc> lFactorsDesc;
        protected List<PlateDesc> lChildPlatesDesc;
        // Links to nodes outside the plate. Contrsucted automatically
        protected List<FactorConnectionPointDesc> lExternalFactorConnectionPoints;
        protected List<FactorConnectionPointDesc> lExternalVariables;
        protected int iLevel;

        public PlateDesc(string name, PlateDesc parentPlateDesc)
            : base(name)
        {
            lVariablesDesc = new List<VariableDesc>();
            lFactorsDesc = new List<FactorDesc>();
            lChildPlatesDesc = new List<PlateDesc>();
            lExternalFactorConnectionPoints = new List<FactorConnectionPointDesc>();
            lExternalVariables = new List<FactorConnectionPointDesc>();
            ContainerPlate = parentPlateDesc;
            iLevel = 0;
            if (ContainerPlate != null)
            {
                ContainerPlate.ChildPlatesDesc.Add(this);
                iLevel = ContainerPlate.iLevel + 1; ;
            }
        }

        public int Level
        {
            get { return iLevel; }
        }

        public void AddVariableDesc(VariableDesc varDesc)
        {
            lVariablesDesc.Add(varDesc);
            varDesc.ContainerPlate = this;
        }

        public List<VariableDesc> VariablesDesc
        {
            get { return lVariablesDesc; }
        }

        public void AddFactorDesc(FactorDesc factorDesc)
        {
            lFactorsDesc.Add(factorDesc);
            factorDesc.ContainerPlate = this;
        }

        public List<FactorDesc> FactorsDesc
        {
            get { return lFactorsDesc; }
        }

        public void AddPlateDesc(PlateDesc plateDesc)
        {
            lChildPlatesDesc.Add(plateDesc);
            plateDesc.ContainerPlate = this;
        }

        public List<PlateDesc> ChildPlatesDesc
        {
            get { return lChildPlatesDesc; }
        }

        // Not sure we need this at all
        void FindExternalLinks()
        {
            lExternalFactorConnectionPoints.Clear();
            // For each variable in the plate: check the connection point whether it is outside
            foreach (var varDesc in lVariablesDesc)
                foreach (var connPoint in varDesc.ConnectedFactors)
                    if (connPoint.OwnerFactorDesc.ContainerPlate != this)
                        lExternalFactorConnectionPoints.Add(connPoint);

            lExternalVariables.Clear();
            // The same for the factors
            foreach (var factorDesc in lFactorsDesc)
                foreach (var connPoint in factorDesc.ConnectionPoints)
                    if (connPoint.ConnectedVariable.ContainerPlate != this)
                        lExternalFactorConnectionPoints.Add(connPoint);
        }
    }

    public class VariableDataList : List<VariableData>
    {
        public VariableData FindByName(string name)
        {
            var vData = this.Find(delegate(VariableData vd) { return vd.Desc.Name == name; });
            if (vData == null)
                throw new GraphicalModelException("Variable data node \"" + name + "\" is not found.");
            return vData;
        }
    }

    public class PlateDataList : List<PlateData>
    {
        public PlateDesc CollectionPlateDesc; // PlateDataListDesc 
        public PlateDataList(PlateDesc collectionPlateDesc)
        {
            CollectionPlateDesc = collectionPlateDesc;
        }
    }

    public class PlatesDataCollections : List<PlateDataList>
    {
        public PlatesDataCollections(PlateDesc plateDesc)
        {
            foreach (var childPlateDesc in plateDesc.ChildPlatesDesc)
            {
                this.Add(new PlateDataList(childPlateDesc));
            }
        }

        public PlateDataList FindByPlateDesc(PlateDesc plateDesc)
        {
            var plateDataList = this.Find(delegate(PlateDataList pdl) { return pdl.CollectionPlateDesc == plateDesc; });
            if (plateDataList == null)
                throw new GraphicalModelException("Plate description \"" + plateDesc.Name + "\" is not found.");
            return plateDataList;
        }

        public void AddPlateDataByPlateDesc(PlateData plateData)
        {
            FindByPlateDesc(plateData.Description).Add(plateData);
        }
    }

    public class PlateData
    {
        protected PlateDesc pDescription;
        protected PlateData pParentPlate;
        protected VariableDataList lVariables;
        protected PlatesDataCollections lChildPlatesCollection;

        public PlateData(GraphicModelFactory factory, PlateDesc desc, PlateData parentPlate)
        {
            pDescription = desc;
            pParentPlate = parentPlate;
            lVariables = new VariableDataList();
            lChildPlatesCollection = new PlatesDataCollections(desc);

            foreach (var varDesc in pDescription.VariablesDesc)
            {
                lVariables.Add(varDesc.BuildVariableData());
            }

            if (pParentPlate != null)
            {
                pParentPlate.ChildPlatesCollection.AddPlateDataByPlateDesc(this);
            }
        }

        public PlateDesc Description
        {
            get { return pDescription; }
        }

        public VariableDataList Variables
        {
            get { return lVariables; }
        }

        public PlatesDataCollections ChildPlatesCollection
        {
            get { return lChildPlatesCollection; }
        }
    }

    public class GraphicModelFactory
    {
        protected Dictionary<string, FactorBuilder> lFactorBuilders;
        protected Dictionary<string, KernelBuilder> lKernelBuilders;

        public GraphicModelFactory()
        {
            lFactorBuilders = new Dictionary<string, FactorBuilder>();
            lKernelBuilders = new Dictionary<string, KernelBuilder>();
        }

        public void RegisterFactor(FactorBuilder factorBuilder)
        {
            lFactorBuilders.Add(factorBuilder.Type, factorBuilder);
        }

        public void RegisterKernel(KernelBuilder kernelBuilder)
        {
            lKernelBuilders.Add(kernelBuilder.Type, kernelBuilder);
        }

        public FactorDesc BuildFactorDesc(string factorType)
        {
            if (lFactorBuilders.ContainsKey(factorType))
                return lFactorBuilders[factorType].BuildDesc();
            else
                throw new GraphicalModelException("Factor type \"" + factorType + "\" is not found.");
        }

        public Factor BuildFactor(FactorDesc factorDesc)
        {
            if (lFactorBuilders.ContainsKey(factorDesc.FactorType))
            {
                var res = lFactorBuilders[factorDesc.FactorType].BuildFactor(factorDesc);
                res.KernelObject = BuildKernel(factorDesc.Kernel, res);
                return res;
            }
            else
                throw new GraphicalModelException("Factor type \"" + factorDesc.FactorType + "\" is not found.");
        }

        public KernelDesc BuildKernelDesc(string kernelType, FactorDesc ownerFactorDesc)
        {
            if (lKernelBuilders.ContainsKey(kernelType))
                return lKernelBuilders[kernelType].BuildDesc(ownerFactorDesc);
            else
                throw new GraphicalModelException("Kernel type \"" + kernelType + "\" is not found.");
        }

        public Kernel BuildKernel(KernelDesc kernelDesc, Factor containerFactor)
        {
            if (lKernelBuilders.ContainsKey(kernelDesc.KernelType))
            {
                var res = lKernelBuilders[kernelDesc.KernelType].BuildKernel(kernelDesc, containerFactor);
                if (kernelDesc is CompoundKernelDesc)
                {
                    foreach (var childKernelDesc in ((CompoundKernelDesc)kernelDesc).ChildKernels)
                        ((CompoundKernel)res).AddChildKernel(BuildKernel(childKernelDesc, containerFactor));
                }
                return res;
            }
            else
                throw new GraphicalModelException("Kernel type \"" + kernelDesc.KernelType + "\" is not found.");
        }

        public PlateData AddPlate(PlateDesc newPlateDesc, PlateData pParentPlate)
        {
            PlateData newPlate = new PlateData(this, newPlateDesc, pParentPlate);
            return newPlate;
        }
    }



}
