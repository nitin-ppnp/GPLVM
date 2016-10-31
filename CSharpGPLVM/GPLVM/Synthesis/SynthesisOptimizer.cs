using System;
using System.Collections.Generic;
using GPLVM.GPLVM;
using DataFormats;
using ILNumerics;
using GPLVM.Synthesis.Constraints;
using GPLVM.Optimisation;
using GPLVM.Numerical;

namespace GPLVM.Synthesis
{
    public class SynthesisOptimizer : IFunctionWithGradientConstraint
    {
        public INonlinearConstraint[] lesserThanConstraints { get; set; }
        public INonlinearConstraint[] greaterThanConstraints { get; set; }
        public INonlinearConstraint[] equalityConstraints { get; set; }

        private ILArray<double> _Y = ILMath.localMember<double>();
        private ILArray<double> _Yold = ILMath.localMember<double>();
        private ILArray<double> _Ynz = ILMath.localMember<double>();
        private ILArray<double> _fullY = ILMath.localMember<double>();

        private ILArray<double>  _target = ILMath.localMember<double>();
        private ILArray<double> _rootPos = ILMath.localMember<double>();

        private ILArray<double> _X = ILMath.localMember<double>();
        private ILArray<double> _Ymean = ILMath.localMember<double>();
        private ILArray<double> _Yvar = ILMath.localMember<double>();
        private ILArray<double> differenceY = ILMath.localMember<double>();
        private ILArray<double> varDims = ILMath.localMember<double>();
        private ILArray<double> fixDims = ILMath.localMember<double>();
        private ILArray<double> varDim = ILMath.localMember<double>();
        private ILArray<double> varDimGrad = ILMath.localMember<double>();

        private ILCell _jointPositions = ILMath.cell();
        //private ILArray<double> _Jacobian;
        //private ILArray<double> _positions;

        // footskate constraints indexes
        private int leftFoot = 4, leftToe = 5, rightFoot = 8, rightToe = 9;
        private List<INonlinearConstraint> _constraints;

        private DataPostProcess _postPrc;

        private GP_LVM _gplvm;
        private Representation _repType;
        private bool _useRoot = true;
        private bool _init = false;

        private ILArray<double> _LambdaY = ILMath.localMember<double>();

        public bool SolveLinear { get; set; }

        public List<INonlinearConstraint> Constraints
        {
            get { return _constraints; }
            set { _constraints = value; }
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public bool isConstraint
        {
            get 
            { 
                int conRows = 0;
                for (int i = 0; i < Constraints.Count; i++)
                    conRows += ((Constraint)Constraints[i]).NumDimensions;

                return (IsEnabled && Constraints.Count > 0 && conRows > 0); 
            }
        }

        public SynthesisOptimizer(GP_LVM gplvm, DataPostProcess postPrc, bool fixRoot = true, Representation repType = Representation.exponential)
        {
            _gplvm = gplvm;
            _postPrc = postPrc;
            _constraints = new List<INonlinearConstraint>();

            _repType = repType;

            if (fixRoot)
            {
                varDims.a = 1;
                for (int i = 10; i < _gplvm.Y.S[1]; i++)
                    varDims[ILMath.end + 1] = i;

                fixDims.a = 0;
                for (int i = 2; i <= 9; i++)
                    fixDims[ILMath.end + 1] = i;
            }
            else
            {
                varDims.a = 0;
                for (int i = 1; i < _gplvm.Y.S[1]; i++)
                    varDims[ILMath.end + 1] = i;
            }
            //for (int i = velStrt; i < Vdim; i++)
            //    varDims.push_back(i+Ydim);
            //for (int i = 0; i < velStrt; i++)
            //    fixedDims.push_back(i+Ydim);

            varDim.a = ILMath.zeros(1,varDims.Length);
            varDimGrad.a = ILMath.zeros(1, varDims.Length);

            Constraints.Add(new ConstraintFootSkate(_postPrc, leftFoot, _repType, null, varDims));
            //Constraints.Add(new ConstraintFootSkate(_postPrc, leftToe, _repType, _jointPositions, null));
            //((ConstraintFootSkate)Constraints[Constraints.Count - 1]).ParentJointID = leftFoot;
            Constraints.Add(new ConstraintFootSkate(_postPrc, rightFoot, _repType, null, varDims));
            //Constraints.Add(new ConstraintFootSkate(_postPrc, rightToe, _repType, _jointPositions, null));
            //((ConstraintFootSkate)Constraints[Constraints.Count - 1]).ParentJointID = rightFoot;

            IsEnabled = true;
            SolveLinear = false;

            List<INonlinearConstraint> equality = new List<INonlinearConstraint>();
            List<INonlinearConstraint> lesserThan = new List<INonlinearConstraint>();
            List<INonlinearConstraint> greaterThan = new List<INonlinearConstraint>();

            foreach (var c in _constraints)
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
        }

        public ILRetArray<double> ConstraintPose(ILInArray<double> inYvar, ILInArray<double> inYnz, ILInArray<double> inRootPos, ILInArray<double> inTarget, ILInArray<double> inPoseScale, ILOutArray<double> newYnz = null, bool bMotionFields = false)
        {
            using (ILScope.Enter(inYvar, inYnz, inRootPos, inTarget, inPoseScale))
            {
                ILArray<double> scale = ILMath.check(inPoseScale);

                _Ynz.a = ILMath.check(inYnz);
                _Yvar.a = ILMath.check(inYvar);
                _target.a = ILMath.check(inTarget);
                _rootPos.a = ILMath.check(inRootPos);
    
                if(!_init)
                {
                    _Y.a = _Ynz[1, ILMath.full].C;
                    _Yold.a = _Ynz[0, ILMath.full].C;
                }
                else
                    //_Y.a = _Ynz[1, ILMath.full].C;
                    _Y.a = _Yold.C;

                _fullY.a = _Y.C;

                if (bMotionFields)
                {
                    _LambdaY.a = ILMath.ones(scale.Length);

                    _Ymean.a = _Ynz[1, ILMath.full].C;
                    _Y.a = _Ymean.C;
                }
                else
                {
                    // Compute variances.
                    double Ysigma = (double)(_Yvar[1, 1] - _Yvar[1, 0] * _Yvar[0, 1] / _Yvar[0, 0]);
                    //double Ysigma = (double)(_Yvar[0, 0] - _Yvar[1, 0] * _Yvar[0, 1] / _Yvar[1, 1]);

                    _LambdaY.a = (ILMath.multiplyElem(scale, scale) / Ysigma).T;

                    // Compute conditional mean.
                    _Ymean.a = _Ynz[1, ILMath.full] + ILMath.min(1.0, (_Yvar[1, 0] / _Yvar[0, 0])) * (_Yold - _Ynz[0, ILMath.full]);
                    //_Ymean.a = _Ynz[1, ILMath.full] + ILMath.min(1.0, (_Yvar[0, 1] / _Yvar[1, 1])) * (_Yold - _Ynz[0, ILMath.full]);

                    // Set to the mean for constraint initialization.
                    _Y.a = ILMath.multiply(_Ymean, ILMath.diag(ILMath.sqrt(_LambdaY)));
                    //V_ = Vmean_ * LambdaV.array().sqrt().matrix().asDiagonal();
                }
    
                InitConstraints();
    
                if (SolveLinear)
                {
                    int conRows = 0;
                    if (isConstraint)
                    {
                        // Sum up the number of constraint rows.
                        for (int i = 0; i < Constraints.Count; i++)
                            conRows += ((Constraint)Constraints[i]).NumDimensions;
                    }

                    // Construct the full linear system first.
                    ILArray<double> obj = ILMath.zeros(_Y.S[1] + conRows, _Y.S[1] + conRows);
                    ILArray<double> objb = ILMath.zeros(_Y.S[1] + conRows);
                    //ILArray<double> obj = ILMath.zeros(conRows, _Y.S[1]);
                    //ILArray<double> objb = ILMath.zeros(conRows);

                    // Set the diagonal proportional to the variances.
                    for (int i = 0; i < _Y.S[1]; i++)
                        obj[i, i] = _LambdaY[i];
                    //for (int i = 0; i < V_.cols(); i++)
                    //    obj(i + Y_.cols(),i+Y_.cols()) = LambdaV(i);

                    // Compute the b objective.
                    // Note that for the first three entries in V, which correspond to relative root motion, the objective is a bit different.
                    objb[ILMath.r(0, _Y.S[1] - 1)] = ILMath.multiplyElem(_LambdaY, (_Ymean - _Yold).T);
                    objb[0] = _LambdaY[0] * _Ymean[0];
                    objb[2] = _LambdaY[2] * _Ymean[2];
                    objb[3] = _LambdaY[3] * _Ymean[3];
                    //objb.block(Y_.cols(),0,3,1) = LambdaV.block(0,0,3,1).cwiseProduct(Vmean_.block(0,0,1,3).transpose());
                    //objb.block(Y_.cols()+3,0,V_.cols()-3,1) = LambdaV.block(3,0,V_.cols()-3,1).cwiseProduct((Vmean_.block(0,3,1,V_.cols()-3) - oldV_.block(0,3,1,V_.cols()-3)).transpose());

                    // Now construct the constraint matrix row by row.
                    if (isConstraint)
                    {
                        // Compute forward kinematics.
                        _Y.a = _Yold.C;
                        //V_ = oldV_;
                        if (_useRoot)
                        {
                            _Y[0] = 0;
                            _Y[2] = 0;
                            _Y[3] = 0;
                            //.block(0,0,1,3).setZero();
                        }
                        else
                        {
                            _Y[0] = _Ymean[0];
                            _Y[2] = _Ymean[2];
                            _Y[3] = _Ymean[3];
                        //V_.block(0,0,1,3) = Vmean_.block(0,0,1,3);
                        }

                        _postPrc.ForwardKinematics(_Y, _jointPositions, true, true, _repType);
                        for (int i = 0; i < Constraints.Count; i++)
                            Constraints[i].Parameters = _Y;

                        int r = 0;
                        for (int i = 0; i < Constraints.Count; i++)
                        {
                            ((Constraint)Constraints[i]).LinearSolve(obj, objb, r + _Y.S[1]); //+V_.cols());
                            r += ((Constraint)Constraints[i]).NumDimensions;
                        }

                        // Add transpose of constraint matrix.
                        obj[ILMath.r(0, _Y.S[1] - 1), ILMath.r(_Y.S[1], conRows)] = obj[ILMath.r(_Y.S[1], conRows), ILMath.r(0, _Y.S[1] - 1)].T;

                        // Now construct the reduced size matrix that removes the rows we don't need.
                        //int dim = varDims.size();
                        //MatrixXd smobj(dim + conRows,dim + conRows);
                        //MatrixXd smobjb(dim + conRows,1);
                        //ILArray<double> solution = ILMath.zeros(_Y.S[1]);
                        //smobj.setZero(dim + conRows,dim + conRows);
                        //smobjb.setZero(dim+ conRows,1);

                        //// Write the default solution.
                        //solution.block(0,0,Y_.cols(),1) = Ymean_.transpose() - oldY_.transpose();
                        //solution.block(Y_.cols(),0,3,1) = Vmean_.block(0,0,1,3).transpose();
                        //solution.block(Y_.cols()+3,0,V_.cols()-3,1) = Vmean_.block(0,3,1,V_.cols()-3) - oldV_.block(0,3,1,V_.cols()-3).transpose();

                        //// Copy over all the variable entries.
                        //for (unsigned i = 0; i < varDims.size(); i++)
                        //{
                        //    smobjb(i,0) = objb(varDims[i],0);
                        //    for (unsigned j = 0; j < varDims.size(); j++)
                        //    {
                        //        smobj(i,j) = obj(varDims[i],varDims[j]);
                        //    }
                        //    for (int j = 0; j < conRows; j++)
                        //    {
                        //        smobj(i,dim + j) = obj(varDims[i],Y_.cols() + V_.cols() + j);
                        //        smobj(dim + j,i) = obj(Y_.cols() + V_.cols() + j,varDims[i]);
                        //    }
                        //}
                        //for (int j = 0; j < conRows; j++)
                        //{
                        //    smobjb(dim+j,0) = objb(Y_.cols() + V_.cols() + j,0);
                        //}

                        //// Now add up the contribution from the constant entries.
                        //for (unsigned i = 0; i < fixedDims.size(); i++)
                        //{
                        //    for (unsigned j = 0; j < (unsigned int)conRows; j++)
                        //    {
                        //        smobjb(dim + j,0) -= solution(fixedDims[i],0)*obj(Y_.cols()+V_.cols()+j,fixedDims[i]);
                        //    }
                        //}

                        // Perform the solve.
                        ILArray<double> solution = ILMath.linsolve(obj, objb);
                        //VectorXd smsolution = solver.solve(smobjb);

                        //// Reconstruct full solution.
                        //for (unsigned i = 0; i < varDims.size(); i++)
                        //{
                        //    solution(varDims[i]) = smsolution(i);
                        //}

                        // Write out the result.
                        _Y.a = _Yold + solution[ILMath.r(0, _Y.S[1] - 1)].T;
                        //V_.block(0,0,1,3) = solution.block(Y_.cols(),0,3,1).transpose();
                        //V_.block(0,3,1,V_.cols()-3) = oldV_.block(0,3,1,V_.cols()-3) + solution.block(Y_.cols()+3,0,V_.cols()-3,1).transpose();
                    }
                }
                else
                {
                    // If running a nonlinear solve, run nonlinear optimization now.

                    // Set to previous entry for initial optimization point.
                    if (_init)
                        _Y.a = ILMath.multiplyElem(_Yold, ILMath.sqrt(_LambdaY));

                    for (int i = 0; i < fixDims.Length; i++)
                    {
                        int j = (int)fixDims[i];
                        if (j < _Y.S[1])
                           _Y[0, j] = _Ymean[0, j] * ILMath.sqrt(_LambdaY[j]);
                        //else
                        //    V_(0, j - Ygrad_.cols()) = Vmean_(0, j - Ygrad_.cols()) * sqrt(LambdaV(j - Ygrad_.cols(), 0));
                    }

                    // Initialize varDim.
                    for (int i = 0; i < varDims.Length; i++)
                    {
                        int j = (int)varDims[i];
                        if (j < _Y.S[1])
                            varDim[0, i] = _Y[0, j];
                        //else
                        //    varDim(0, i) = V_(0, j - Ygrad_.cols());
                    }

                    AugLag.Optimize(this, 5, true);

                    for (int i = 0; i < varDims.Length; i++)
                    {
                        int j = (int)varDims[i];
                        if (j < _Y.S[1])
                            _Y[0, j] = varDim[0, i];
                        //else
                        //    V_(0, j - Ygrad_.cols()) = varDim(0, i);
                    }

                    _Y.a = _Y / ILMath.sqrt(_LambdaY);
                    _gplvm.Ynew = _Y.C;
                }

    
                UpdateConstraints();
    
                _Yold.a = _Y.C;
                //oldV_ = V_;
                _init = true;
    
                //model->getSupplementary()->fillFullY(Y_,V_,*constrainedY);

                //// Return the pose too.
                //newYnz = Y_;
                //newVnz = V_;
                return _Y;
            }
        }

        public int NumParameters
        {
            get { return varDims.Length; }
        }

        public double Value()
        {
            // As the SCG minimizes the function, negative log-likelihood is used
            ILArray<double> yY = _Y / ILMath.sqrt(_LambdaY);

            // Previous pose
            differenceY.a = yY - _Ymean;

            //double objective = (double)(-.5 * ILMath.sum(ILMath.log(_LambdaY.T) + ILMath.log(2 * ILMath.pi) + ILMath.divide(ILMath.multiply(differenceY, differenceY.T), _LambdaY.T), 1));
            double objective = -0.5 * (double)(ILMath.multiply(ILMath.multiplyElem(differenceY, _LambdaY), differenceY.T));

            return -objective;
        }

        public ILArray<double> Gradient()
        {
            // As the function value is negated, gradient should be negated too

            //ILArray<double> Ygrad = -(differenceY / _LambdaY.T);
            ILArray<double> Ygrad = -ILMath.multiplyElem(differenceY, _LambdaY.T);
            Ygrad = Ygrad / ILMath.sqrt(_LambdaY.T);

            for (int i = 0; i < varDims.Length; i++)
            {
                int j = (int)varDims[i];
                if (j < _Y.S[1])
                    varDimGrad[0, i] = Ygrad[0, j];
                //else
                //    varDimGrad(0, i) = Vgrad_(0, j - Ygrad_.cols());
            }

            return -varDimGrad;
        }

        public ILArray<double> Parameters
        {
            get { return varDim.C; }
            set
            {
                varDim.a = value;
                for (int i = 0; i < varDims.Length; i++)
                {
                    int j = (int)varDims[i];
                    if (j < _Y.S[1])
                        _Y[0, j] = varDim[0, i];
                    //else
                    //    V_(0, j - Ygrad_.cols()) = varDim(0, i);
                }

                ILArray<double> yY = _Y / ILMath.sqrt(_LambdaY.T);
                _postPrc.ForwardKinematics(yY, _jointPositions, true, true, _repType);

                for (int i = 0; i < equalityConstraints.Length; i++)
                {
                    equalityConstraints[i].Parameters = _Y / ILMath.sqrt(_LambdaY).T;
                    ((Constraint)Constraints[i]).JointPositions = _jointPositions;
                }
                for (int i = 0; i < greaterThanConstraints.Length; i++)
                {
                    greaterThanConstraints[i].Parameters = _Y / ILMath.sqrt(_LambdaY).T;
                    ((Constraint)Constraints[i]).JointPositions = _jointPositions;
                }
                for (int i = 0; i < lesserThanConstraints.Length; i++)
                {
                    lesserThanConstraints[i].Parameters = _Y / ILMath.sqrt(_LambdaY).T;
                    ((Constraint)Constraints[i]).JointPositions = _jointPositions;
                }
            }
        }

        private void UpdateConstraints()
        {
            _postPrc.ForwardKinematics(_Y, _jointPositions, true, true, _repType);
            for (int i = 0; i < Constraints.Count; i++)
            {
                Constraints[i].Parameters = _Y.C;
                ((Constraint)Constraints[i]).JointPositions = _jointPositions;
                ((Constraint)Constraints[i]).Update();
            }
        }

        private void InitConstraints()
        {
            using (ILScope.Enter())
            {
                ILArray<double> yY = _Y / ILMath.sqrt(_LambdaY.T);

                _postPrc.ForwardKinematics(yY, _jointPositions, true, true, _repType);

                for (int i = 0; i < Constraints.Count; i++)
                {
                    ((Constraint)Constraints[i]).YLambda = _LambdaY.C;
                    ((Constraint)Constraints[i]).JointPositions = _jointPositions;
					((Constraint)Constraints[i]).RootPos = _rootPos;
                    Constraints[i].Parameters = yY;
                    ((Constraint)Constraints[i]).Init(this, isConstraint);
                }
            }
        }
    }
}
