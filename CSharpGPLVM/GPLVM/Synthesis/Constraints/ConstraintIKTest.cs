using ILNumerics;
using DataFormats;
//using GPLVM.Models;
using GPLVM.Utils.Character;

namespace GPLVM.Synthesis.Constraints
{
    public class ConstraintIKTest : IConstraint
    {
        protected AvatarBVH _model;
        protected ConstraintType _type;     // type of constraint     
        protected int _jointID;             // index of the joint wants to be constraint
        protected int _targetJointID = -1;

        protected ILArray<double> _goalPos = ILMath.localMember<double>();      // desired position
        protected ILArray<double> _position = ILMath.localMember<double>();     // position of the joint
        protected ILArray<double> _YJacobian = ILMath.localMember<double>();
        protected ILArray<double> _jointPositions = ILMath.localMember<double>();
        protected ILArray<double> _jointAngles = ILMath.localMember<double>();

        protected bool isHeight, isHorizontal, isAbsolute;

        private DataPostProcess _postPrc;

        public ConstraintIKTest(AvatarBVH model, int jointID)
        {
            _model = model;
            _type = ConstraintType.IK;
            _postPrc = model.PostProcess;
            isAbsolute = false;
            isHeight = true;
            isHorizontal = true;
        }

        public ConstraintIKTest(DataPostProcess postPrc, int jointID)
        {
            //_model = model;
            _type = ConstraintType.IK;
            _postPrc = postPrc;
            isAbsolute = false;
            isHeight = true;
            isHorizontal = true;

            _jointID = jointID;
        }

        public int JointID
        {
            get
            {
                return _jointID;
            }
        }

        public ConstraintType Type
        {
            get { return _type; }
        }

        public ILArray<double> GoalPosition
        {
            get { return _goalPos; }
            set { _goalPos.a = value; }
        }

        public ILArray<double> Parameter
        {
            get { return _jointAngles; }
            set { _jointAngles.a = value; Jacobian();}
        }

        public bool AbsoluteRoot
        {
            get { return isAbsolute; }
            set { isAbsolute = value; }
        }

        public double Error()
        {
            double error = 0;

            // Compute the Jacobians.
            if (_YJacobian.IsEmpty)
                Jacobian();

            if (isHeight)
                error += 0.5 * (double)ILMath.pow(_position[0, 1] - _goalPos[0, 1], 2);
            if (isHorizontal)
                error += 0.5 * (double)ILMath.pow(_position[0, 0] - _goalPos[0, 0], 2) + 0.5 * (double)ILMath.pow(_position[0, 2] - _goalPos[0, 2], 2);

            return error;
        }

        public ILRetArray<double> Gradient()
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
                    gY += (_position[0, 1] - _goalPos[0, 1]) * _YJacobian[1, ILMath.full];// * LambdaY.array().inverse().sqrt().matrix().asDiagonal();

                if (isHorizontal)
                {
                    gY += (_position[0, 0] - _goalPos[0, 0]) * _YJacobian[0, ILMath.full];// *LambdaY.array().inverse().sqrt().matrix().asDiagonal();
                    gY += (_position[0, 2] - _goalPos[0, 2]) * _YJacobian[2, ILMath.full];// *LambdaY.array().inverse().sqrt().matrix().asDiagonal();
                }

                return gY;
            }
        }

        private void Jacobian()
        {
            if (_targetJointID != -1)
            { 
                // Constraining joint to joint.
                ILArray<double> tempJacobian = ILMath.empty();
                _postPrc.JointGlobalPosition(_targetJointID, _model.Model.Ynew, _position, true, true, _model.RepresentationType, tempJacobian);

                // This is the Jacobian of our joint.
                _postPrc.JointGlobalPosition(_jointID, _model.Model.Ynew, _position, true, true, _model.RepresentationType, _YJacobian);

                // Now take the difference.
                _YJacobian -= tempJacobian;
            }
            else
            { 
                // Constraining joint to point.
                if (!isAbsolute)
                    //_postPrc.JointGlobalPosition(_jointID, _model.Model.Ynew, _position, true, true, _model.RepresentationType, _YJacobian);
                    _postPrc.JointGlobalPosition(_jointID, _jointAngles, _position, true, true, Representation.exponential, _YJacobian);
                    
                else
                    _postPrc.JointGlobalPosition(_jointID, _model.Model.Ynew, _position, false, false, _model.RepresentationType, _YJacobian);
            }
        }

    }
}
