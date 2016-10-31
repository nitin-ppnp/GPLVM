using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM.Numerical;
using GPLVM.Optimisation;
using GraphicalModel.Factors;
using GraphicalModel.Optimisation;

namespace GraphicalModel
{
    public class DataExpander
    {
        protected MatrixDataNode pDataNode;
        protected ILArray<int> aExpandingIndices;
        protected ILArray<int> aUnexpandingIndices;
        public ILArray<double> ExpandedData;
        protected ILArray<double> aGradient;

        public delegate void DataExpandedEventHandler(object source);
        public event DataExpandedEventHandler OnDataExpanded;

        public DataExpander(MatrixDataNode dataNode)
        {
            pDataNode = dataNode;
            aExpandingIndices = ILMath.zeros<int>(0, 1);
            aUnexpandingIndices = ILMath.zeros<int>(0, 1);
            aGradient = ILMath.empty<double>();
        }

        public MatrixDataNode DataNode
        {
            get { return pDataNode; }
        }

        public void OnNewDataExpandIndex(int iLevel)
        {
            if (iLevel == pDataNode.DataDesc.ContainerPlate.Level)
            {
                aExpandingIndices = aExpandingIndices.Concat(pDataNode.Data.S[0] - 1, 0);
                aUnexpandingIndices[aExpandingIndices[ILMath.end]] = ILMath.numel(aExpandingIndices) - 1;
            }
            else if (pDataNode.DataDesc.Mode == EVariableMode.LatentExpandable)
                if (iLevel > pDataNode.DataDesc.ContainerPlate.Level)
                {
                    aExpandingIndices = aExpandingIndices.Concat(pDataNode.Data.S[0] - 1, 0);
                    aUnexpandingIndices[aExpandingIndices[ILMath.end]] = ILMath.numel(aExpandingIndices) - 1;
                }
        }

        public void Expand()
        {
            ExpandedData = pDataNode.Data[aExpandingIndices, ILMath.full];
            if (OnDataExpanded != null)
                OnDataExpanded(this);
        }

        public ILArray<double> Gradient
        {
            get { return aGradient.C; }
            set { aGradient.a = value; }
        }

        public ILArray<int> ExpandignIndices
        {
            get
            {
                return aExpandingIndices.C;
            }
            set
            {
                aExpandingIndices.a = value;
                int dataDim = pDataNode.Data.S[0];
                aUnexpandingIndices = ILMath.zeros<int>(dataDim);
                for (int k = 0; k<aExpandingIndices.S[0]; k++)
                {
                    aUnexpandingIndices[aExpandingIndices[k]] = k;
                }
                //Unexpand();
            }
        }

        public void Unexpand()
        {
            pDataNode.Data.a = ExpandedData[aUnexpandingIndices, ILMath.full];
            foreach (var expander in pDataNode.Expanders.Values)
                if (expander != this) 
                    expander.Expand();            
        }

    }

    public class MatrixDataNode
    {
        protected VariableDesc pDataDesc;
        public ILArray<double> Data;
        protected Dictionary<FactorConnectionPoint, DataExpander> lExpanders;
        protected Dictionary<FactorDesc, DataExpander> lLatestDataExpanders;

        public MatrixDataNode(VariableDesc dataDesc)
        {
            pDataDesc = dataDesc;
            Data = ILMath.zeros(0, dataDesc.Dimensions);
            lExpanders = new Dictionary<FactorConnectionPoint, DataExpander>();
            lLatestDataExpanders = new Dictionary<FactorDesc, DataExpander>();
        }

        public Dictionary<FactorConnectionPoint, DataExpander> Expanders
        {
            get { return lExpanders; }
        }

        public void AddDataExpander(FactorConnectionPoint fcp, DataExpander de)
        {
            lExpanders.Add(fcp, de);
            lLatestDataExpanders.Add(fcp.pOwnerFactor.Desc, de);
        }

        public void AddDataSample(double[] newSample)
        {
            ILArray<double> tmp = newSample;
            Data.a = Data.Concat(tmp.T, 0);
            // Notify the latest connected factors
            foreach (var de in lLatestDataExpanders.Values)
                de.OnNewDataExpandIndex(pDataDesc.ContainerPlate.Level);
        }

        public VariableDesc DataDesc
        {
            get { return pDataDesc; }
        }
    }

    public class MatrixDataNodeList : List<MatrixDataNode>
    {
        public MatrixDataNode FindByName(string name)
        {
            var res = this.Find(delegate(MatrixDataNode mdn) { return mdn.DataDesc.Name == name; });
            if (res == null)
                throw new GraphicalModelException("Matrix data node \"" + name + "\" is not found.");
            return res;
        }
    }

    public class MatrixForm
    {
        private GraphicModelFactory pFactory;
        private PlateData pRootPlateData;
        private List<Factor> lFactors;
        private MatrixDataNodeList lMatrixDataNodes;

        public MatrixForm(GraphicModelFactory factory, PlateData rootPlateData)
        {
            pFactory = factory;
            pRootPlateData = rootPlateData;
            lMatrixDataNodes = new MatrixDataNodeList();
            
            ///////////////////////////////////
            // Build data nodes
            ///////////////////////////////////
            
            // Make the list of data nodes descriptions
            var lVarDesc = MakeVariablesDescriptionList(pRootPlateData.Description);
            // Create the matrix data nodes
            foreach (var varDesc in lVarDesc)
                lMatrixDataNodes.Add(new MatrixDataNode(varDesc));
            
            ///////////////////////////////////
            // Build factor nodes
            // Fill the data into the matrix data nodes
            ///////////////////////////////////
            lFactors = MakeFactorsInstancesList(pRootPlateData);

            ///////////////////////////////////
            // Expand all data expanders
            ///////////////////////////////////
            ExpandAllDataExpanders();
            
        }

        private void ExpandAllDataExpanders()
        {
            foreach (var mdn in lMatrixDataNodes)
                foreach (var de in mdn.Expanders)
                    de.Value.Expand();
        }

        private List<Factor> MakeFactorsInstancesList(PlateData rootPlateData)
        {
            var res = new List<Factor>();
            foreach (var factorDesc in rootPlateData.Description.FactorsDesc)
            {
                var factor = pFactory.BuildFactor(factorDesc);
                factor.Plate = rootPlateData;
                res.Add(factor);
                // Fill the connected matrix data nodes
                foreach (var cp in factor.ConnectionPoints)
                {
                    var mdn = lMatrixDataNodes.FindByName(cp.Desc.ConnectedVariable.Name);
                    var de = new DataExpander(mdn);
                    mdn.AddDataExpander(cp, de);
                    cp.ConnectedDataObject = de;
                }
            }
            
            // Feed the data
            foreach (var varData in rootPlateData.Variables)
            {
                lMatrixDataNodes.FindByName(varData.Desc.Name).AddDataSample(varData.Data);
            }

            foreach (var childPlateCollection in rootPlateData.ChildPlatesCollection)
                foreach(var childPlate in childPlateCollection)
                    res.AddRange(MakeFactorsInstancesList(childPlate));
            return res;
        }

        private List<VariableDesc> MakeVariablesDescriptionList(PlateDesc plateDesc)
        {
            var lVarDesc = new List<VariableDesc>();
            lVarDesc.AddRange(plateDesc.VariablesDesc);
            foreach (var childPlateDesc in plateDesc.ChildPlatesDesc)
                lVarDesc.AddRange(MakeVariablesDescriptionList(childPlateDesc));
            return lVarDesc;
        }

        public void Optimize(int maxIterations, bool display)
        {
            // Init the factors
            foreach (var factor in lFactors)
                factor.Initialize();

            // Run the optimisation
            var modelAdapter = new MatrixFormToFunctionWithGradientAdapter(this);
            var optimiser = new SCGOptimizer();
            optimiser.Optimize(modelAdapter, maxIterations, display);
        }

        public int NumParameters()
        {
            int res = 0;
            // Count the number of variables which are subjuect to optimisation in the data nodes
            foreach (var mdn in lMatrixDataNodes)
                if (mdn.DataDesc.Mode != EVariableMode.Observed)
                    res += ILMath.numel(mdn.Data);
            return res;
        }

        public ILArray<double> Parameters
        {
            get
            {
                // Collect the parameters from the optimised data nodes
                ILArray<double> aParams = ILMath.zeros<double>(NumParameters());
                int done = 0;
                foreach (var mdn in lMatrixDataNodes)
                    if (mdn.DataDesc.Mode != EVariableMode.Observed)
                    {
                        aParams[ILMath.r(done, done + ILMath.numel(mdn.Data) - 1)] = mdn.Data[ILMath.full];
                        done += ILMath.numel(mdn.Data);
                    }
                return aParams.T;
            }
            set
            {
                // Push data the the data expanders
                int done = 0;
                foreach (var mdn in lMatrixDataNodes)
                    if (mdn.DataDesc.Mode != EVariableMode.Observed)
                    {
                        mdn.Data.a = ILMath.reshape(value[ILMath.r(done, done + ILMath.numel(mdn.Data) - 1)], mdn.Data.S);
                        done += ILMath.numel(mdn.Data);
                    }
                ExpandAllDataExpanders();
                
                // Recompute the gradients, factors values, and other internal variables
                foreach (var factor in lFactors)
                {
                    factor.ComputeAllGradients();
                }
            }
        }

        public double FunctionValue()
        {
            // Sum the values of the factors as they are supposed to be in log scale
            double res = 0;
            foreach (var factor in lFactors)
                res += factor.FunctionValue();
            return res;
        }

        public ILRetArray<double> FunctionGradient()
        {
            // Traverse all data nodes and collect the partial gradient values
            ILArray<double> aGradient = ILMath.zeros<double>(NumParameters());
            int done = 0;
            foreach (var mdn in lMatrixDataNodes)
                if (mdn.DataDesc.Mode != EVariableMode.Observed)
                {
                    // Sum all the gradients form the data expanders
                     foreach (var de in mdn.Expanders)
                         aGradient[ILMath.r(done, done + ILMath.numel(mdn.Data) - 1)] =
                            aGradient[ILMath.r(done, done + ILMath.numel(mdn.Data) - 1)] + de.Value.Gradient[ILMath.full];
                    done = done + ILMath.numel(mdn.Data);
                }
            return aGradient;
        }


    }
}
