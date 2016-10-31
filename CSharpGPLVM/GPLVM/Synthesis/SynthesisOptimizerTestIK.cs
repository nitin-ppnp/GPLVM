using GPLVM.Synthesis.Constraints;
using GPLVM.Utils.Character;
using ILNumerics;
//using GPLVM.Models;

namespace GPLVM.Synthesis
{
    public class SynthesisOptimizerTestIK
    {
        private ILArray<double> _Y;
        private ILArray<double> _Yold;
        private ILArray<double> _X;
        private ILArray<double> _Ymean;
        private ILArray<double> _Yvar;
        private ILArray<double> _Jacobian;
        private ILArray<double> _positions;

        private ConstraintIKTest _ik;

        AvatarBVH _avatar;

        double _D;

        public SynthesisOptimizerTestIK(AvatarBVH avatar)
        {
            _avatar = avatar;
            if (_avatar.Model.Mode != GPLVM.LerningMode.selfposterior) 
                _avatar.Model.Mode = GPLVM.LerningMode.selfposterior;
        }

        public SynthesisOptimizerTestIK(ConstraintIKTest ik)
        {
            _ik = ik;
        }

        public int NumParameter
        {
            get { return _ik.Parameter.Length; }
        }

        public ILArray<double> LogPostParameter
        {
            get
            {
                ILArray<double> param;

                param = _ik.Parameter;
                return param.C;
            }
            set
            {
                _ik.Parameter = value;
            }
        }

        /// <summary>
        /// Computes the posterior log likelihood of the model. 
        /// </summary>
        /// <returns>Posterior log likelihood of the model.</returns>
        public double LogLikelihood()
        {
            double L = 0;

            L += _ik.Error();

            return -L;
        }

        /// <summary>
        /// Computes the gradients of the latents and kernel parameters of the model. 
        /// </summary>
        /// <remarks>
        /// Rekursive function going through the hierarchy to get the
        /// gradients of the latents and kernel parameters of the model.
        /// </remarks>
        /// <returns>Gradients of the latents and kernel parameters of the model.</returns>
        public ILRetArray<double> LogLikGradient()
        {
            using (ILScope.Enter())
            {
                ILArray<double> g = _ik.Gradient();

                return -g.C;
            }
        }
    }
}
