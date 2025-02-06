using System;

namespace nUser.DbModel
{
    public class GroupMembershipCancellation
    {
        public int Id { get; set; }
        public DateTime CancellationBeginDate { get; set; }
        public DateTime? CancellationEndDate { get; set; }
        public int BegunById { get; set; }
        public int? CommittedById { get; set; }
        public int? UndoneById { get; set; }
        public int GroupMembership_Id { get; set; }
        public virtual GroupMembership GroupMembership { get; set; }
    }
}