namespace GPLVM
{
    public enum Representation
    {
        radian,         // data in radians
        exponential,     // data as Exponential Map
        quaternion      // data in quaternions
    }

    public enum RotType
    {
        quaternion,
        dualquaternion,
        radian,
        matrix3,
        matrix4,
        degree
    }

    public interface RotationType
    {
        RotType Type
        {
            get;
        }
    }
}
