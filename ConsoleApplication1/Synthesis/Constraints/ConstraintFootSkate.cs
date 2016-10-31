using ILNumerics;
using DataFormats;

namespace GPLVM.Synthesis.Constraints
{
    public class ConstraintFootSkate : Constraint
    {
        protected bool _initialized;
        protected bool _footskateCondition;
        protected bool _bFootConstrained;
        protected bool _constraintEnforced;
        protected bool _fixHeight;
        
        protected double _heightThresholdParent;
        protected double _heightThreshold;
        protected double _slidingThreshold;
        protected double _coneFactor;
        protected double _distanceThreshold;
        protected double _pairedHeightThreshold;
        protected double _pairedSlidingThreshold;
        protected double _pairedConeFactor;
        protected double _pairedDistanceThreshold;
        protected double _velocityThreshold;

        protected int _pairedJointID;

        protected ConstraintFootSkate parent;

        protected double CORRECTION_FACTOR = 0.1;

        public override ILArray<double> GoalPosition
        {
            get { return _curGoalPos; }
            set { _curGoalPos.a = value; }
        }

        public override int NumDimensions
        {
            get
            {
                // Number of rows required for this constraint.
                int dim = 0;
                bool parentEnforced = false;
                if (parent != null) parentEnforced = parent._footskateCondition;
                if (_fixHeight) dim += 1;
                if (_footskateCondition && !parentEnforced) dim += 2;
                return dim;
            }
        }

        public int ParentJointID { set; get; }

        public ConstraintFootSkate(DataPostProcess postPrc, int jointID, Representation repType, ILArray<double> rootPos, ILArray<double> inVarDims, double withinTolerance = 0.0)
            : base(postPrc, jointID, repType, rootPos, inVarDims, withinTolerance)
        {
            //normal_.resize(1, 3);
            _initialized = false;
            
            _heightThreshold = 8.0;
            _slidingThreshold = 5.0;
            _velocityThreshold = 0.1;
            _coneFactor = 4.0;
            _distanceThreshold = 10.0;
            _footskateCondition = true;
            _fixHeight = true;
            _bFootConstrained = false;
            ParentJointID = -1;

            parent = null;

            _type = ConstraintType.Footskate;
        }

        public override void LinearSolve(ILOutArray<double> constMatrix, ILOutArray<double> errVec, int dim)
        {
            using (ILScope.Enter())
            {
                _position.a = JointPositions.GetArray<double>(_jointID)[ILMath.r(0, 2), 3].T;

                if (_initialized)
                {
                    bool parentEnforced = false;
                    if (parent != null) parentEnforced = parent._footskateCondition;

                    if ((_footskateCondition && !parentEnforced) || _fixHeight)
                    {
                        // Compute Jacobian.
                        _postPrc.JointGlobalPosition(_jointID, _jointAngles, _position, true, true, _repType, _YJacobian);

                        // Write rows into constraint matrix.
                        ILArray<double> ind;
                        if ((_footskateCondition && !parentEnforced) && _fixHeight) ind = new double[] {0, 1, 2};
                        else if (_fixHeight) ind = new double[] {1};
                        else ind = new double[] {0, 2};

                        for (int i = 0; i < ind.Length; i++)
                        {
                            constMatrix[dim + i, ILMath.r(0, _jointAngles.S[1] - 1)] = _YJacobian[ind[i], ILMath.full];

                            if (ind[i] == 1)
                                errVec[dim + i] = -_position[0, ind[i]];
                            else
                                errVec[dim + i] = -CORRECTION_FACTOR * (_position[0, ind[i]] - _curGoalPos[0, ind[i]]);
                        }
                    }
                }
            }
        }

        public override double Function()
        {
            double error = 0;

            _position.a = JointPositions.GetArray<double>(_jointID)[ILMath.r(0, 2), 3].T;

            if (_initialized)
            {
                bool parentEnforced = false;
                _fixHeight = _position[0, 1] < 0.0;

                if ((_footskateCondition && !parentEnforced) || _fixHeight)
                {

                    _postPrc.JointGlobalPosition(_jointID, _jointAngles, _position, true, true, _repType, _YJacobian);

                    error += (double)(0.5 * ILMath.pow(_position[0, 0] - _curGoalPos[0, 0], 2) + 0.5 * ILMath.pow(_position[0, 2] - _curGoalPos[0, 2], 2));

                    if (_fixHeight)
                        error += (double)(0.5 * ILMath.pow(_position[0, 1], 2));
                }
            }
            return error;
        }

        public override ILRetArray<double> Gradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> gY = ILMath.zeros(1, _jointAngles.S[1]);

                if (_initialized)
                {
                    bool parentEnforced = false;

                    // Footskate condition
                    if ((_footskateCondition && !parentEnforced) || _fixHeight)
                    {
                        gY += ILMath.multiplyElem(ILMath.multiply(_position[0] - _curGoalPos[0], _YJacobian[0, ILMath.full]), _YLambda.T);
                        gY += ILMath.multiplyElem(ILMath.multiply(_position[2] - _curGoalPos[2], _YJacobian[2, ILMath.full]), _YLambda.T);

                        if (_fixHeight)
                            gY += ILMath.multiplyElem(ILMath.multiply(_position[1] - _curGoalPos[1], _YJacobian[1, ILMath.full]), _YLambda.T);
                    }

                    for (int i = 0; i < varDims.Length; i++)
                    {
                        int j = (int)varDims[i];
                        if (j < gY.S[1])
                            varDimGrad[0, i] = gY[0, j];
                        //else
                        //    varDimGrad(0, i) = Vgrad_(0, j - Ygrad_.cols());
                    }
                }
                return varDimGrad;
            }
        }

        public override void Init(SynthesisOptimizer opt, bool isInUse)
        {
            // Determine parent (if relevant).
            if (ParentJointID != -1 && parent == null)
                for (int i = 0; i < opt.Constraints.Count; i++)
                    if (((IConstraint)opt.Constraints[i]).Type == ConstraintType.Footskate)
                    {
                        ConstraintFootSkate c = (ConstraintFootSkate)opt.Constraints[i];
                        if (c.JointID == this.ParentJointID)
                            parent = c;
                    }

            _position.a = JointPositions.GetArray<double>(_jointID)[ILMath.r(0, 2), 3].T;
            if (_initialized && isInUse)
            {
                double jointAbsoluteHeight = (double)_position[0, 1];
                double normalMotion = (double)(_position[0, 1] - _curGoalPos[0, 1]);
                double tangentMotion = (double)ILMath.sqrt(ILMath.pow(_position[0, 0] - _curGoalPos[0, 0], 2) + ILMath.pow(_position[0, 2] - _curGoalPos[0, 2], 2));
                double distance = Util.SquareEuclidean(_position - _curGoalPos);

                // Choose the height threshold.
                double height_thresh = _heightThreshold;
                if (ParentJointID != -1 && JointPositions.GetArray<double>(ParentJointID)[ILMath.r(0, 2), 3].T > _heightThreshold)
                    height_thresh = parent._heightThreshold;

                // If the paired joint is constrained, make this joint harder to constrain and easier to release.
                double sliding_threshold = _slidingThreshold;
                double cone_factor = _coneFactor;
                double distance_threshold = _distanceThreshold;

                // First, check if we should enter constrained mode. This is the case if we are not
                // already constrained, and the foot is moving slowly and is below the specified height.
                if (!_bFootConstrained)
                    _bFootConstrained = (jointAbsoluteHeight <= height_thresh) && (normalMotion < _velocityThreshold) && Util.Euclidean(_position - _curGoalPos) < sliding_threshold;

                // Don't update foot positions if we're constraining already.
                if (_footskateCondition)
                    _constraintEnforced = true;

                // If the foot is constrained, check if it is remaining constrained.
                if (_bFootConstrained)
                {
                    // The constraint is broken if the desired position is too high or moving within the "friction cone".
                    _footskateCondition = ((normalMotion < _velocityThreshold ||         // Moving upwards fast enough.
                                           tangentMotion > normalMotion * cone_factor)) &&  // This is the cone condition.
                                          (jointAbsoluteHeight <= height_thresh) &&       // This is the height condition.
                                          (distance < ILMath.pow(distance_threshold, 2));         // This is the distance condition.

                    // Break the constraint if necessary.
                    if (!_footskateCondition)
                        _bFootConstrained = false;
                }

                if (_position[0, 1] < 0.0)
                    _fixHeight = true;
                else
                    _fixHeight = false;

                // Update foot position if we have no footskate condition.
                if (!_footskateCondition)
                    _constraintEnforced = false;
            }
            else
            {
                _constraintEnforced = false;
            }
        }

        public override void Update()
        {
            using (ILScope.Enter())
            {
                // Update the position only if we did not constraint this frame.
                if (!_constraintEnforced)
                {
                    _curGoalPos.a = _position.C;
                    _constraintEnforced = false;
                }

                // Transform the previous position into the coordinate frame of the current frame (which will be the previous frame next this constraint is called).
                // Get dt.
                double dt = _postPrc.FrameTime;
                double x = (double)_position[0, 0];
                double z = (double)_position[0, 2];

                // Undo the rotation caused by this pose.
                _curGoalPos[0, 0] = x * ILMath.cos(-Util.deg2rad(_jointAngles[0, 3]) * dt) + z * ILMath.sin(-Util.deg2rad(_jointAngles[0, 3]) * dt);
                _curGoalPos[0, 2] = z * ILMath.cos(-Util.deg2rad(_jointAngles[0, 3]) * dt) - x * ILMath.sin(-Util.deg2rad(_jointAngles[0, 3]) * dt);

                // Subtract the root position.
                _curGoalPos[0, 0] -= _jointAngles[0, 0] * dt;
                _curGoalPos[0, 2] -= _jointAngles[0, 2] * dt;

                // Constraint is now ready.
                _initialized = true;
            }
        }
    }
}
