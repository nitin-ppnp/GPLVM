using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;
using DataFormats;

namespace Avatar.Constraints
{
    public class Constraint
    {
        public enum ConstraintType
        {
            Base,
            IK,
            Footskate
        };

        protected ConstraintType _type;     // type of constraint     
        protected int _jointID;             // index of the joint wants to be constraint

        protected ILArray<double> _goalPos; // desired position

        public ConstraintType Type
        {
            get { return _type; }
        }

        public ILArray<double> GoalPosition
        {
            get { return _goalPos; }
            set { _goalPos = value; }
        }

        public Constraint(int jointID)
        {
            _type = ConstraintType.Base;
            _jointID = jointID;
        }

        protected virtual void Jacobian()
        {

        }
    }
}
