using System;
using System.Collections.Generic;

namespace nUser.DbModel
{
    public class GroupMembership
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedById { get; set; }
        public string ForProduct { get; set; }
        public string GroupName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int User_Id { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? DisapprovedDate { get; set; }
        public int? ApprovedById { get; set; }
        public virtual User User { get; set; }
        public virtual List<GroupMembershipCancellation> GroupMembershipCancellation { get; set; }
    }
}