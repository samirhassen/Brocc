using nUser.DbModel;
using System;

namespace nUser
{
    public class GroupMembershipModel
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public string GroupName { get; set; }
        public string ForProduct { get; set; }
        public int CreatedById { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsApproved { get; set; }

        internal static GroupMembershipModel FromGroupMembership(GroupMembership m)
        {
            return new GroupMembershipModel
            {
                GroupName = m.GroupName,
                ForProduct = m.ForProduct,
                CreationDate = m.CreationDate,
                CreatedById = m.CreatedById,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                IsApproved = m.ApprovedDate.HasValue,
                Id = m.Id
            };
        }
    }
}