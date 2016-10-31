using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ILNumerics;
using GPLVM.Utils.Character;

namespace MotionPrimitives.SkeletonMap
{
    [DataContract()]
    public class SkeletonMap
    {
        [DataMember()]
        private Skeleton skeleton;
        [DataMember()]
        private List<JointsGroup> map = new List<JointsGroup>();

        public SkeletonMap(Skeleton skeleton)
        {
            this.skeleton = skeleton;
        }

        public List<JointsGroup> ChannelsGroups
        {
            get { return map; }
        }

        public void AddChannelsGroup(JointsGroup group)
        {
            if (GetChannelsGroupByName(group.Name) == null)
            {
                map.Add(group);
            }
        }

        public JointsGroup GetChannelsGroupByName(string name)
        {
            var found = map.Where(x => x.Name == name);
            return (found.Count() == 0 ? null : found.First());
        }

        public List<JointsGroupData> SplitDataIntoGroups(ILInArray<double> inFullData)
        {
            using (ILScope.Enter(inFullData))
            {
                ILArray<double> fullData = ILMath.check(inFullData);

                List<JointsGroupData> res = new List<JointsGroupData>();
                foreach (JointsGroup group in map)
                {
                    res.Add(new JointsGroupData(group, fullData));
                }
                return res;
            }
        }

        public ILOutArray<double> MergeDataFromGroups(List<JointsGroupData> groupsData)
        {
            ILArray<double> fullData = ILMath.zeros(groupsData.First().Data.S[0], 1);
            foreach (JointsGroupData groupData in groupsData)
            {
                fullData[ILMath.full, groupData.Group.Channels] = groupData.Data;
            }
            return fullData;
        }

        private int GetSkeletonChannelsCount()
        {
            int res = 0;
            foreach (Joint j in skeleton.Joints)
                res += j.posInd.Length + j.rotInd.Length;
            return res;
        }
    }
}
