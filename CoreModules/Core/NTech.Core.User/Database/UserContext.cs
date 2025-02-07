using nUser.DbModel;

namespace NTech.Core.User.Database
{
    public class UserContext : UserContextBase
    {
        public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
        public void RemoveKeyValueItem(KeyValueItem item) => KeyValueItems.Remove(item);
        public void AddKeyValueItem(KeyValueItem item) => KeyValueItems.Add(item);

    }
}
