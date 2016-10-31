namespace GPLVM.Numerical
{
    public interface IFunctionWithGradientConstraint : IFunctionWithGradient
    {
        INonlinearConstraint[] lesserThanConstraints
        {
            get;
            set;
        }

        INonlinearConstraint[] greaterThanConstraints
        {
            get;
            set;
        }

        INonlinearConstraint[] equalityConstraints
        {
            get;
            set;
        }
    }
}
