using System;
using System.Collections.Generic;
using System.Xml;
using System.Runtime.Serialization;
using ILNumerics;

namespace MotionPrimitives.DataFormats
{
    [DataContract(IsReference = true)]
    public class CharacterJoint
    {
        [DataMember()]
        public string Name;
        [DataMember()]
        public int ParentID;
        [DataMember()]
        public int ID;
        [DataMember()]
        public ILArray<double> RawOffset = ILMath.empty();
        [DataMember()]
        public ILArray<int> RawRotInd = ILMath.empty<int>();
        [DataMember()]
        public ILArray<int> RawPosInd = ILMath.empty<int>();
        [DataMember()]
        public string RawRotOrder = null;
        [DataMember()]
        public ILArray<int> RawRotOrderInt = ILMath.empty<int>();

        public ILArray<double> RawRotDataChannels = ILMath.empty();
        public ILArray<double> RawPosDataChannels = ILMath.empty();

        public void SetRawDataChannels(ILInArray<double> inFullData)
        {
            using (ILScope.Enter(inFullData))
            { 
                ILArray<double> fullData = ILMath.check(inFullData);
                RawRotDataChannels.a = fullData[ILMath.full, RawRotInd];
                RawPosDataChannels.a = fullData[ILMath.full, RawPosInd];
            }
        }

    }
}
