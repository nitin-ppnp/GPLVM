using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace FactorGraph.Core
{
    [DataContract(IsReference = true)]
    public class NamedEntity : INamedEntity
    {
        [DataMember()]
        protected string sName = "[NOT_SET]";
        public NamedEntity(string sName)
        {
            this.sName = sName;
        }
        public string Name
        {
            get { return sName; }
        }
    }
}
