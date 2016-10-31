using ILNumerics;
using DataFormats;

namespace GPLVM.Synthesis.Constraints
{
    public class ConstraintIK : Constraint
    {
        protected int _targetJointID = -1;

        protected bool isHeight, isHorizontal;

        public override int NumDimensions
        {
            get
            {
                // Number of rows required for this constraint.
                int dim = 0;
                if (isHeight) dim += 1;
                if (isHorizontal) dim += 2;
                return dim;
            }
        }

        public ConstraintIK(DataPostProcess postPrc, int jointID, Representation repType, ILArray<double> rootPos, ILArray<double> inVarDims, double withinTolerance = 0.0)
            : base(postPrc, jointID, repType, rootPos, inVarDims, withinTolerance)
        {
            _type = ConstraintType.IK;
            
            isHeight = true;
            isHorizontal = true;
        }

        public override void LinearSolve(ILOutArray<double> constMatrix, ILOutArray<double> errVec, int dim)
        {
        }

        public override double Function()
        {
            double error = 0;

            // Compute the Jacobians.
            if (_YJacobian.IsEmpty)
                Jacobian();

            if (isHeight)
                error += 0.5 * (double)ILMath.pow(_position[0, 1] - _curGoalPos[0, 1], 2);
            if (isHorizontal)
                error += 0.5 * (double)ILMath.pow(_position[0, 0] - _curGoalPos[0, 0], 2) + 0.5 * (double)ILMath.pow(_position[0, 2] - _curGoalPos[0, 2], 2);

            return -error;
        }

        public override ILRetArray<double> Gradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> gY = ILMath.zeros(1, _jointAngles.S[1]);

                // Compute the Jacobians.
                if (_YJacobian.IsEmpty)
                    Jacobian();

                // Set target.
                /*if (_targetJointID != -1)
                    _goalPos = _jointPositions[_targetJointID].block(0, 3, 3, 1).transpose() + offset_;*/

                // Add to gradients.
                if (isHeight)
                    gY += (_position[0, 1] - _curGoalPos[0, 1]) * _YJacobian[1, ILMath.full];// * LambdaY.array().inverse().sqrt().matrix().asDiagonal();

                if (isHorizontal)
                {
                    gY += (_position[0, 0] - _curGoalPos[0, 0]) * _YJacobian[0, ILMath.full];// *LambdaY.array().inverse().sqrt().matrix().asDiagonal();
                    gY += (_position[0, 2] - _curGoalPos[0, 2]) * _YJacobian[2, ILMath.full];// *LambdaY.array().inverse().sqrt().matrix().asDiagonal();
                }

                return -gY.C;
            }
        }

        private void Jacobian()
        {
            if (_targetJointID != -1)
            { 
                // Constraining joint to joint.
                ILArray<double> tempJacobian = ILMath.empty();
                _postPrc.JointGlobalPosition(_targetJointID, _jointAngles, _position, true, true, _repType, tempJacobian);

                // This is the Jacobian of our joint.
                _postPrc.JointGlobalPosition(_jointID, _jointAngles, _position, true, true, _repType, _YJacobian);

                // Now take the difference.
                _YJacobian -= tempJacobian;
            }
            else
            { 
                // Constraining joint to point.
                if (!_isAbsolute)
                    _postPrc.JointGlobalPosition(_jointID, _jointAngles, _position, true, true, _repType, _YJacobian);
                    
                else
                    _postPrc.JointGlobalPosition(_jointID, _jointAngles, _position, false, false, _repType, _YJacobian);
            }
        }

        public override void Init(SynthesisOptimizer opt, bool isInUse)
        {
            if (_isAbsolute)
            {
                // Compute absolute target in the coordinate frame of the previous pose.
                _curGoalPos.a = _goalPos.C;

                // Subtract position and undo rotation of previous pose.
                _curGoalPos[0, 0] = (_goalPos[0, 0] - _rootPos[0, 0]) * ILMath.cos(-_rootPos[0, 3]) + (_goalPos[0, 2] - _rootPos[0, 2]) * ILMath.sin(-_rootPos[0, 3]);
                _curGoalPos[0, 2] = (_goalPos[0, 2] - _rootPos[0, 2]) * ILMath.cos(-_rootPos[0, 3]) - (_goalPos[0, 0] - _rootPos[0, 0]) * ILMath.sin(-_rootPos[0, 3]);
            }
            else
            {
                _curGoalPos.a = _goalPos.C;
            }
    
        }
    }
}
