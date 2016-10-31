using ILNumerics;

namespace GPLVM.Synthesis.Constraints
{
    public enum ConstraintType
    {
        Unknown,
        IK,
        Footskate
    };

    public interface IConstraint
    {
        ILArray<double> GoalPosition
        {
            get;
            set;
        }

        ConstraintType Type
        {
            get;
        }

        int JointID
        {
            get;
        }
    }
}
