namespace GPLVM
{
    public class Radian : RotationType
    {
        private RotType _rotType = RotType.radian;
        private double angle;

        public Radian(double radValue)
        {
            angle = radValue;
        }

        #region Setters and Getters
        public RotType Type
        {
            get { return _rotType; }
        }

        public double Value
        {
            get
            {
                return angle;
            }
            set
            {
                angle = value;
            }
        }

        #endregion
    }
}
