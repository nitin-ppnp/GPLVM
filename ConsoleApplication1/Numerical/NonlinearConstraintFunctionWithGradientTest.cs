using System;
using System.Collections.Generic;
using ILNumerics;

namespace GPLVM.Numerical
{
    /// <code>
    /// // Suppose we would like to minimize the following function:
    /// //
    /// //    f(x,y) = min 100(y-x²)²+(1-x)²
    /// //
    /// // Subject to the constraints
    /// //
    /// //    x >= 0  (x must be positive)
    /// //    y >= 0  (y must be positive)
    /// </code>
    public class NonlinearConstraintFunctionWithGradientTest : IFunctionWithGradientConstraint
    {
        private ILArray<double> X = ILMath.localMember<double>();
        public INonlinearConstraint[] constr;

        public INonlinearConstraint[] lesserThanConstraints { get; set; }
        public INonlinearConstraint[] greaterThanConstraints { get; set; }
        public INonlinearConstraint[] equalityConstraints { get; set; }

        public int NumParameters
        {
            get { return 2; }
        }

        public double Value()
        {
            return (double)(100 * ILMath.pow(X[1] - X[0] * X[0], 2) + ILMath.pow(1 - X[0], 2));
        }

        public ILArray<double> Gradient()
        {
            ILArray<double> g = ILMath.zeros(1, 2);
            g[0] = 2 * (200 * ILMath.pow(X[0], 3) - 200 * X[0] * X[1] + X[0] - 1);
            g[1] = 200 * (X[1] - X[0] * X[0]);
            return g;
        }

        public ILArray<double> Parameters
        {
            get { return X.C; }
            set 
            { 
                X.a = value;
                for (int i = 0; i < equalityConstraints.Length; i++)
                    equalityConstraints[i].Parameters = value;
                for (int i = 0; i < greaterThanConstraints.Length; i++)
                    greaterThanConstraints[i].Parameters = value;
                for (int i = 0; i < lesserThanConstraints.Length; i++)
                    lesserThanConstraints[i].Parameters = value;
            }
        }

        public NonlinearConstraintFunctionWithGradientTest()
        {
            INonlinearConstraint[] constr = new INonlinearConstraint[2];
            constr[0] = new ConstraintTest1();
            constr[1] = new ConstraintTest2();

            List<INonlinearConstraint> equality = new List<INonlinearConstraint>();
            List<INonlinearConstraint> lesserThan = new List<INonlinearConstraint>();
            List<INonlinearConstraint> greaterThan = new List<INonlinearConstraint>();

            foreach (var c in constr)
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

            Parameters = ILMath.zeros(1, 2);
        }
    }

    public class ConstraintTest1 : INonlinearConstraint
    {
        private double _value, _tolerance;
        private OptConstraintType _shouldBe;

        private ILArray<double> X = ILMath.localMember<double>();

        public int NumParameters
        {
            get { return 2; }
        }

        public ILArray<double> Parameters
        {
            get { return X.C; }
            set { X.a = value; }
        }

        public double GetViolation()
        {
            return Function() - Value;
        }

        /// <summary>
        ///   Gets the type of the constraint.
        /// </summary>
        /// 
        public OptConstraintType ShouldBe { get { return _shouldBe; } }

        /// <summary>
        ///   Gets the value in the right hand
        ///   side of the constraint equation.
        /// </summary>
        /// 
        public double Value { get { return _value; } }

        /// <summary>
        ///   Gets the violation tolerance for the constraint. Equality
        ///   constraints should set this to a small positive value.
        /// </summary>
        /// 
        public double Tolerance { get { return _tolerance; } }

        public ConstraintTest1()
        {
            _shouldBe = OptConstraintType.GreaterThanOrEqualTo;
            _value = 0.0;
            _tolerance = 0.0;
        }

        public double Function()
        {
            return (double)X[0];
        }

        public ILRetArray<double> Gradient()
        {
            ILArray<double> g = ILMath.zeros(1, 2);
            g[0] = 1.0;
            return g;
        }
    }

    public class ConstraintTest2 : INonlinearConstraint
    {
        private double _value, _tolerance;
        private OptConstraintType _shouldBe;

        private ILArray<double> X = ILMath.localMember<double>();

        public int NumParameters
        {
            get { return 2; }
        }

        public ILArray<double> Parameters
        {
            get { return X.C; }
            set { X.a = value; }
        }

        public double GetViolation()
        {
            return Function() - Value;
        }

        /// <summary>
        ///   Gets the type of the constraint.
        /// </summary>
        /// 
        public OptConstraintType ShouldBe { get { return _shouldBe; } }

        /// <summary>
        ///   Gets the value in the right hand
        ///   side of the constraint equation.
        /// </summary>
        /// 
        public double Value { get { return _value; } }

        /// <summary>
        ///   Gets the violation tolerance for the constraint. Equality
        ///   constraints should set this to a small positive value.
        /// </summary>
        /// 
        public double Tolerance { get { return _tolerance; } }

        public ConstraintTest2()
        {
            _shouldBe = OptConstraintType.GreaterThanOrEqualTo;
            _value = 0.0;
            _tolerance = 0.0;
        }

        public double Function()
        {
            return (double)X[1];
        }

        public ILRetArray<double> Gradient()
        {
            ILArray<double> g = ILMath.zeros(1, 2);
            g[1] = 1.0;
            return g;
        }
    }
}
