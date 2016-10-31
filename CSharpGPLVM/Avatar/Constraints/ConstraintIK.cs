using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avatar.Constraints
{
    public class ConstraintIK : Constraint
    {
        public ConstraintIK(int jointID)
            : base(jointID)
        {
            _type = ConstraintType.IK;
        }

        protected override void Jacobian()
        {

        }
    }
}
