using System;

namespace nUser.DbModel
{
    public class AuthenticationMechanism
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedById { get; set; }
        public DateTime? RemovedDate { get; set; }
        public int? RemovedById { get; set; }
        public string UserIdentity { get; set; }
        public string AuthenticationType { get; set; }
        public string AuthenticationProvider { get; set; }
        public string Credentials { get; set; }
        public virtual User User { get; set; }
        public int? UserId { get; set; }
    }
}