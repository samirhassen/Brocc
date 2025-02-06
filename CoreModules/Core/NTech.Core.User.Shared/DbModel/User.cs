using System;
using System.Collections.Generic;

namespace nUser.DbModel
{
    public class User
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedById { get; set; }
        public string DisplayName { get; set; }
        public string ProviderName { get; set; }
        public bool IsSystemUser { get; set; }
        public DateTime? ConsentedDate { get; set; }
        public string ConsentText { get; set; }
        public int? DeletedById { get; set; }
        public DateTime? DeletionDate { get; set; }
        public virtual List<GroupMembership> GroupMemberships { get; set; }
        public virtual List<AuthenticationMechanism> AuthenticationMechanisms { get; set; }
        public virtual List<UserSetting> UserSettings { get; set; }
    }
}