using ILNumerics;

namespace GPLVM.Numerical
{
    public enum OptConstraintType
    {
        EqualTo = 0,
        GreaterThanOrEqualTo,
        LesserThanOrEqualTo
    }

    public interface INonlinearConstraint
    {
        /// <summary>
        ///   Gest the number of variables in the constraint.
        /// </summary>
        /// 
        int NumParameters { get; }

        /// <summary>
        ///   Gets the left hand side of 
        ///   the constraint equation.
        /// </summary>
        /// 
        double Function();

        /// <summary>
        ///   Gets the gradient of the left hand
        ///   side of the constraint equation.
        /// </summary>
        /// 
        ILRetArray<double> Gradient();
        ILArray<double> Parameters
        {
            get;
            set;
        }

        /// <summary>
        ///   Gets how much the constraint is being violated.
        /// </summary>
        /// 
        /// <param name="x">The function point.</param>
        /// 
        /// <returns>How much the constraint is being violated at the given point.</returns>
        /// 
        double GetViolation();

        /// <summary>
        ///   Gets the type of the constraint.
        /// </summary>
        /// 
        OptConstraintType ShouldBe { get; }

        /// <summary>
        ///   Gets the value in the right hand
        ///   side of the constraint equation.
        /// </summary>
        /// 
        double Value { get; }

        /// <summary>
        ///   Gets the violation tolerance for the constraint. Equality
        ///   constraints should set this to a small positive value.
        /// </summary>
        /// 
        double Tolerance { get; }
    }
}
