using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using GPLVM;
using FactorGraph.Core;

namespace FactorGraph.Core
{
    public delegate void ValuesSizeChangedEventHandler();

    public interface IMatrixDataNode : INamedEntity
    {
        ILSize GetValuesSize();
        void SetValuesSize(ILSize newSize);
        event ValuesSizeChangedEventHandler ValuesSizeChanged;
        ILRetArray<int> GetOptimizingMask();
        void SetOptimizingMask(ILInArray<int> inMask);
        void SetOptimizingMaskAll();
        void SetOptimizingMaskNone();
        int GetOptimizingSize();
        
        ILRetArray<double> GetValues(); // Accessed by factor nodes
        void SetValues(ILInArray<double> newValues); // Accessed by factor nodes
        ILRetArray<double> GetValuesGradient(); // Accessed by factor nodes

        ILRetArray<double> GetParametersVector(); // Accessed by the graph minimizing routine
        void SetParametersVector(ILInArray<double> inValue); // Accessed by the graph minimizing routine
        ILRetArray<double> GetParametersVectorGradient(); // Accessed by the graph minimizing routine

        void ConnectDataConnector(DataConnector dataConnector);
        void PushParametersToFactorNodes();
        void PullGradientsFormFactorNodes();
        void CreateInducing(IMatrixDataNode matrixDataNode, ILArray<int> indexes);
        void OnDeserializedMethod();
    }
}
