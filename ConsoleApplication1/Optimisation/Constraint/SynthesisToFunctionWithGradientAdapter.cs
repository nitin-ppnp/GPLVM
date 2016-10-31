using System;
using System.Collections.Generic;
using GPLVM.Numerical;
using ILNumerics;
using GPLVM.GPLVM;

namespace GPLVM.Optimisation
{
    public class SynthesisToFunctionWithGradientAdapter : IFunctionWithGradientConstraint
    {
        protected IGPLVM _model;

        public INonlinearConstraint[] lesserThanConstraints { get; set; }
        public INonlinearConstraint[] greaterThanConstraints { get; set; }
        public INonlinearConstraint[] equalityConstraints { get; set; }

        public SynthesisToFunctionWithGradientAdapter(GP_LVM model, IEnumerable<INonlinearConstraint> constraints)
        {
            _model = model;

            if (_model.Mode != GPLVM.LerningMode.selfposterior)
                _model.Mode = GPLVM.LerningMode.selfposterior;

            List<INonlinearConstraint> equality = new List<INonlinearConstraint>();
            List<INonlinearConstraint> lesserThan = new List<INonlinearConstraint>();
            List<INonlinearConstraint> greaterThan = new List<INonlinearConstraint>();

            foreach (var c in constraints)
            {
                switch (c.ShouldBe)
                {
                    case OptConstraintType.EqualTo:
                        equality.Add(c); break;

                    case OptConstraintType.GreaterThanOrEqualTo:
                        greaterThan.Add(c); break;

                    case OptConstraintType.LesserThanOrEqualTo:
                        lesserThan.Add(c); break;

                    default:
                        throw new ArgumentException("Unknown constraint type.", "constraints");
                }
            }

            this.lesserThanConstraints = lesserThan.ToArray();
            this.greaterThanConstraints = greaterThan.ToArray();
            this.equalityConstraints = equality.ToArray();
        }

        public int NumParameters
        {
            get { return _model.NumParameterInHierarchy; }
        }

        public double Value()
        {
            // As the SCG minimizes the function, negative log-likelihood is used
            return -_model.LogLikelihood();
        }

        public ILArray<double> Gradient()
        {
            // As the function value is negated, gradient should be negated too
            return -_model.LogLikGradient();
        }

        public ILArray<double> Parameters
        {
            get { return _model.LogParameter; }
            set 
            { 
                _model.LogParameter = value;
                for (int i = 0; i < equalityConstraints.Length; i++)
                    equalityConstraints[i].Parameters = _model.Ynew;
                for (int i = 0; i < greaterThanConstraints.Length; i++)
                    greaterThanConstraints[i].Parameters = _model.Ynew;
                for (int i = 0; i < lesserThanConstraints.Length; i++)
                    lesserThanConstraints[i].Parameters = _model.Ynew;
            }
        }
    }
}
