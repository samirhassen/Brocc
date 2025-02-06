using System;

namespace nUser.DbModel
{
    public class UserSetting
    {
        public int Id { get; set; }
        public byte[] Timestamp { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedById { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}