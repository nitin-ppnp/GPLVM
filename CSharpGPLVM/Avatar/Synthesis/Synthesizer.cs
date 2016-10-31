using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GPLVM.GPLVM;
using ILNumerics;

namespace Avatar.Synthesis
{
    public class Synthesizer
    {
        private IGPLVM _model;
        private Constraint[] _constraints;
        private SynthesisOptimizer _optimizer;

        private bool isConstraint;

        private ILArray<double> _XStar;
        private ILArray<double> _Ynew;

        public Constraint[] Constraints
        {
            get { return _constraints; }
            set { _constraints = value; }
        }

        public Synthesizer(ref IGPLVM model)
        {
            _model = model;
        }

        public ILRetArray<double> synthesize()
        {
            _Ynew = _model.PredictData(_XStar);

            return _Ynew;
        }

        public void reset()
        {
            _XStar = _model.X[2, ILMath.full];
        }
    }
}
