using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using FactorGraph.Core;
using GPLVM;
using GPLVM.GPLVM;
using GPLVM.Kernel;
using FactorGraph.DataConnectors;

namespace FactorGraph.FactorNodes
{
    [DataContract(IsReference = true)]
    public class FactorDesc
    {
        [DataMember()]
        public DataConnector ConnectionPoint;
        [DataMember()]
        public DataConnector ConnectionPointInducing;
        [DataMember()]
        public ILArray<double> IndexesInFullData = ILMath.localMember<double>();

        public FactorDesc(DataConnector cp)
        {
            ConnectionPoint = cp;
            ConnectionPointInducing = null;
        }

        public FactorDesc(DataConnector cp, DataConnector cpInducing)
        {
            ConnectionPoint = cp;
            ConnectionPointInducing = cpInducing;
        }
    }

    public class FactorDescList : List<FactorDesc>
    {
    }

    [DataContract(IsReference = true)]
    public abstract class FactorNodeWithStyles : FactorNodeWithKernel
    {
        public static string CP_STYLE_PREFIX = "Style";

        [DataMember()]
        protected FactorDescList lFactorDescs;
        //[DataMember()]
        protected ILArray<double> aFullLatent = ILMath.localMember<double>(); // Lazy initialized full latent data matrix
        //[DataMember()]
        protected ILArray<double> aFullLatentInducing = ILMath.localMember<double>(); // Lazy initialized full latent inducing data matrix

        // Callbacks for overriding kernel indexes initialization
        public delegate void InitializeKernelIndexesEventHandler();
        public event InitializeKernelIndexesEventHandler OnInitializeKernelIndexes;
        public delegate void InitializeTensorKernelIndexesEventHandler();
        public event InitializeTensorKernelIndexesEventHandler OnInitializeTensorKernelIndexes;
        public delegate void InitializeCompoundKernelIndexesEventHandler();
        public event InitializeCompoundKernelIndexesEventHandler OnInitializeCompoundKernelIndexes;

        public FactorNodeWithStyles(string sName, IKernel iKernel)
            : base(sName, iKernel)
        {
            lFactorDescs = new FactorDescList();
            
        }

        public FactorDescList FactorDescs
        {
            get { return lFactorDescs; }
        }

        protected ILArray<double> FullLatent
        {
            get
            {
                using (ILScope.Enter())
                {
                    if (aFullLatent.IsEmpty)
                    {
                        foreach (FactorDesc desc in lFactorDescs)
                            aFullLatent.a = Util.Concatenate<double>(aFullLatent, desc.ConnectionPoint.Values, 1);
                    }
                    return aFullLatent;
                }
            }
        }

        protected ILArray<double> FullLatentInducing
        {
            get
            {
                using (ILScope.Enter())
                {
                    if (aFullLatentInducing.IsEmpty)
                    {
                        foreach (FactorDesc desc in lFactorDescs)
                            aFullLatentInducing.a = Util.Concatenate<double>(aFullLatentInducing, desc.ConnectionPointInducing.Values, 1);
                    }
                    return aFullLatentInducing;
                }
            }
        }

        public abstract bool UseInducing();

        public FactorDesc CreateFactorDataConnector(string sStyleName)
        {
            DataConnector cp = new DataConnector(CP_STYLE_PREFIX + " " + sStyleName);
            DataConnector inducing = null;
            DataConnectors.Add(cp);
            if (UseInducing())
            {
                inducing = new DataConnector(CP_STYLE_PREFIX + " " + sStyleName + " inducing");
                DataConnectors.Add(inducing);
            }
            lFactorDescs.Add(new FactorDesc(cp, inducing));
            return lFactorDescs.Last();
        }

        protected IKernel FindKernel(IKernel kernel, Type type)
        {
            if (kernel.GetType() == type)
                return kernel;
            if ((kernel is CompoundKern) || (kernel is TensorKern))
            {
                List<IKernel> lKernels = kernel is CompoundKern ? ((CompoundKern)kernel).Kernels : ((TensorKern)kernel).Kernels;
                foreach (IKernel k in lKernels)
                {
                    var r = FindKernel(k, type);
                    if ((r != null) && (r.GetType() == type))
                        return r;
                }
            }
            if (kernel is ModulationKern)
            {
                IKernel k = ((ModulationKern)kernel).GetInnerKern();
                var r = FindKernel(k, type);
                if ((r != null) && (r.GetType() == type))
                    return r;
            }
            return null;
        }

        protected void InitializeKernelIndexes()
        {
            if (OnInitializeKernelIndexes != null)
            {
                OnInitializeKernelIndexes();
                return;
            }
            InitializeTensorKernelIndexes();
            InitializeCompoundKernelIndexes();
        }

        protected void InitializeTensorKernelIndexes()
        {
            if (OnInitializeTensorKernelIndexes != null)
            {
                OnInitializeTensorKernelIndexes();
                return;
            }
            using (ILScope.Enter())
            {
                int k = 0;
                List<ILArray<double>> aKernelIndexes = new List<ILArray<double>>();
                foreach (FactorDesc desc in lFactorDescs)
                {
                    int nDimensions = desc.ConnectionPoint.ConnectedDataNode.GetValuesSize()[1];
                    desc.IndexesInFullData.a = k + ILMath.counter<double>(0, 1, nDimensions);
                    ILArray<double> w = ILMath.localMember<double>();
                    w.a = desc.IndexesInFullData.C;
                    aKernelIndexes.Add(w);
                    k = k + nDimensions;
                }
                TensorKern kTensor = (TensorKern)FindKernel(KernelObject, typeof(TensorKern));
                if (kTensor != null)
                    kTensor.Indexes = aKernelIndexes;
            }
        }

        protected void InitializeCompoundKernelIndexes()
        {
            if (OnInitializeCompoundKernelIndexes != null)
            {
                OnInitializeCompoundKernelIndexes();
                return;
            }
            using (ILScope.Enter())
            {
                // Consists of tensor + noise kernels
                int k = 0;
                List<ILArray<double>> aKernelIndexes = new List<ILArray<double>>();
                foreach (FactorDesc desc in lFactorDescs)
                {
                    int nDimensions = desc.ConnectionPoint.ConnectedDataNode.GetValuesSize()[1];
                    k = k + nDimensions;
                }
                ILArray<double> w;
                w = ILMath.localMember<double>();
                w.a = (ILMath.counter(k) - 1);
                aKernelIndexes.Add(w); // Full latent data
                w = ILMath.localMember<double>();
                w.a = lFactorDescs[0].IndexesInFullData;
                aKernelIndexes.Add(w); // Only indexes of X
                CompoundKern kCompound = (CompoundKern)FindKernel(KernelObject, typeof(CompoundKern));
                if (kCompound != null)
                    kCompound.Indexes = aKernelIndexes;
            }
        }

        public override void OnDeserializedMethod()
        {
            using (ILScope.Enter())
            {
                base.OnDeserializedMethod();
                aFullLatent = ILMath.localMember<double>();
                aFullLatentInducing = ILMath.localMember<double>();
            }
        }
    }
}
