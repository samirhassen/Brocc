using NTech.Core.Module.Shared.Infrastructure;
using nUser.DbModel;
using System;
using System.Linq;

namespace NTech.Core.User.Shared.Services
{
    public class KeyValueStoreService : IKeyValueStoreService
    {
        private readonly Func<IUserContext> createContext;
        private readonly ICoreClock clock;

        public KeyValueStoreService(Func<IUserContext> createContext, ICoreClock clock)
        {
            this.createContext = createContext;
            this.clock = clock;
        }

        public void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null)
        {
            using (var context = createContext())
            {
                var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
                var wasRemoved = false;
                if (c != null)
                {
                    context.RemoveKeyValueItem(c);
                    context.SaveChanges();
                }

                observeWasRemoved?.Invoke(wasRemoved);
            }
        }

        string IKeyValueStoreService.GetValue(string key, string keySpace)
        {
            using (var context = createContext())
            {
                return GetValueComposable(context, key, keySpace);
            }
        }

        public void SetValue(string key, string keySpace, string value, INTechCurrentUserMetadata user, Action<bool> observeWasUpdated = null)
        {
            using (var context = createContext())
            {
                var wasUpdated = SetValueComposable(context, key, keySpace, value, user, clock);

                context.SaveChanges();

                observeWasUpdated?.Invoke(wasUpdated);
            }
        }


        public static string GetValueComposable(IUserContext context, string key, string keySpace) =>
            context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace)?.Value;

        public static bool SetValueComposable(IUserContext context, string key, string keySpace, string value, INTechCurrentUserMetadata user, ICoreClock clock)
        {
            var c = context.KeyValueItemsQueryable.SingleOrDefault(x => x.Key == key && x.KeySpace == keySpace);
            var wasUpdated = true;
            if (c == null)
            {
                wasUpdated = false;
                c = new KeyValueItem
                {
                    Key = key,
                    KeySpace = keySpace,
                };
                context.AddKeyValueItem(c);
            }
            c.ChangedById = user.UserId;
            c.ChangedDate = clock.Now;
            c.InformationMetaData = user.InformationMetadata;
            c.Value = value;
            return wasUpdated;
        }
    }

    public interface IKeyValueStoreService
    {
        void RemoveValue(string key, string keySpace, Action<bool> observeWasRemoved = null);
        string GetValue(string key, string keySpace);
        void SetValue(string key, string keySpace, string value, INTechCurrentUserMetadata user, Action<bool> observeWasUpdated = null);
    }

    public enum KeyValueStoreKeySpaceCode
    {
        ApiKeyModelById,
        ApiKeyIdByHash
    }
}