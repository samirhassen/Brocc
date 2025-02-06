using nUser.DbModel;
using System;

namespace nUser
{
    public class UserModel
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedById { get; set; }
        public string Name { get; set; }
        public DateTime? DeletionDate { get; set; }
        public string DeletedBy { get; set; }

        internal static UserModel FromDbUser(User u)
        {
            return new UserModel
            {
                Id = u.Id,
                CreatedById = u.CreatedById,
                CreationDate = u.CreationDate,
                Name = u.DisplayName
            };
        }
    }
}