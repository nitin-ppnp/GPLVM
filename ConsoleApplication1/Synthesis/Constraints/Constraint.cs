using ILNumerics;
using DataFormats;
using GPLVM.Numerical;

namespace GPLVM.Synthesis.Constraints
{
    public abstract class Constraint : INonlinearConstraint, IConstraint
    {
        protected int _jointID;             // index of the joint wants to be constraint

        protected ILArray<double> _position = ILMath.localMember<double>();     // position of the joint
        protected ILArray<double> _YJacobian = ILMath.localMember<double>();
        //protected ILArray<double> _jointPositions = ILMath.localMember<double>();
        protected ILArray<double> _jointAngles = ILMath.localMember<double>();
        protected ILArray<double> _curGoalPos = ILMath.localMember<double>();      // desired position
        protected ILArray<double> _goalPos = ILMath.localMember<double>();
        protected ILArray<double> _YLambda = ILMath.localMember<double>();

        protected ILArray<double> varDims = ILMath.localMember<double>();
        //private ILArray<double> fixDims = ILMath.localMember<double>();
        //protected ILArray<double> varDim = ILMath.localMember<double>();
        protected ILArray<double> varDimGrad = ILMath.localMember<double>();

        protected ILArray<double> _rootPos = ILMath.localMember<double>();

        protected ConstraintType _type;     // type of constraint
        protected DataPostProcess _postPrc;
        protected Representation _repType;

        protected double _value, _tolerance;
        protected OptConstraintType _shouldBe;

        protected ILCell _jointPositions = ILMath.cell();

        protected bool _isAbsolute;

        public int NumParameters
        {
            get { return varDims.Length; }
        }

        public ILArray<double> Parameters
        {
            get { return _jointAngles.C; }
            set { _jointAngles.a = value; }
        }

        public ConstraintType Type
        {
            get { return _type; }
        }

        public double GetViolation()
        {
            return Function() - Value;
        }

        public virtual ILArray<double> GoalPosition
        {
            get { return _goalPos; }
            set { _goalPos.a = value; }
        }

        public bool AbsoluteRoot
        {
            get { return _isAbsolute; }
            set { _isAbsolute = value; }
        }

        public int JointID
        {
            get
            {
                return _jointID;
            }
        }

        public virtual int NumDimensions
        {
            set;
            get;
        }

        public ILCell JointPositions 
        {
            get { return _jointPositions.C; }
            set { _jointPositions.a = value; }
        }

        public virtual ILArray<double> YLambda
        {
            get { return _YLambda; }
            set { _YLambda.a = value; }
        }
		
		public virtual ILArray<double> RootPos
        {
            get { return _rootPos; }
            set { _rootPos.a = value; }
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

        public Constraint(DataPostProcess postPrc, int jointID, Representation repType, ILArray<double> rootPos, ILArray<double> inVarDims, double withinTolerance = 0.0)
        {
            _shouldBe = OptConstraintType.EqualTo;
            _value = 0.0;
            _tolerance = withinTolerance;

            _postPrc = postPrc;
            _jointID = jointID;

            _repType = repType;

            _isAbsolute = false;

            //_rootPos = rootPos;

            _type = ConstraintType.Unknown;

            varDims = inVarDims;
            varDimGrad.a = ILMath.zeros(1, varDims.Length);
        }

        public abstract void LinearSolve(ILOutArray<double> constMatrix, ILOutArray<double> errVec, int dim);
        public abstract double Function();
        public abstract ILRetArray<double> Gradient();
        public abstract void Init(SynthesisOptimizer opt, bool isInUse);
        public virtual void Update() { }
        //public abstract void Reset();

        public ILRetArray<double> MakeRelative(ILInArray<double> inRootPos, ILInArray<double> inP)
        {
            using (ILScope.Enter(inRootPos, inP))
            {
                ILArray<double> rootPos = ILMath.check(inRootPos);
                ILArray<double> p = ILMath.check(inP);

                ILArray<double> relativePos = ILMath.zeros(1,3);
                ILArray<double> theta = ILMath.zeros(1,2);

                ILArray<double> difference = new double[2] {(double)(p[0, 0] - rootPos[0, 0]), (double)(p[0, 2] - rootPos[0, 2])};
                theta[0, 0] = Util.Euclidean(difference);
    
                double angle = (double)ILMath.atan2(difference[0], difference[1]);
                theta[0, 1] = Util.modAngle(angle - (double)rootPos[0, 3]);
    
                relativePos[0, 0] = theta[0, 0] * theta[0, 0] * ILMath.sin(theta[0, 1]);
                relativePos[0, 1] = p[0, 1] - rootPos[0, 1];
                relativePos[0, 2] = theta[0, 0] * theta[0, 0] * ILMath.cos(theta[0, 1]);

                return relativePos;
            }
        }

        public ILRetArray<double> MakeAbsolute(ILInArray<double> inRootPos, ILInArray<double> inP)
        {
            using (ILScope.Enter(inRootPos, inP))
            {
                ILArray<double> rootPos = ILMath.check(inRootPos);
                ILArray<double> p = ILMath.check(inP);

                ILArray<double> absolutePos = ILMath.zeros(1,3);
                ILArray<double> theta = ILMath.zeros(1,2);

                ILArray<double> difference = new double[2] { (double)p[0, 0], (double)p[0, 2] };
                theta[0, 0] = Util.Euclidean(difference);

                double angle = (double)ILMath.atan2(difference[0], difference[1]);
                theta[0, 1] = Util.modAngle(angle + (double)rootPos[0, 3]);

                absolutePos[0, 0] = theta[0, 0] * theta[0, 0] * ILMath.sin(theta[0, 1]) + rootPos[0, 0];
                absolutePos[0, 1] = p[0, 1] + rootPos[0, 1];
                absolutePos[0, 2] = theta[0, 0] * theta[0, 0] * ILMath.cos(theta[0, 1]) + rootPos[0, 2];

                return absolutePos;
            }
        }
    }
}
