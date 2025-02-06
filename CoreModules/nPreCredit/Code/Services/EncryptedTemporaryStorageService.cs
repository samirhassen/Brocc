using nPreCredit.DbModel;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class EncryptedTemporaryStorageService : IEncryptedTemporaryStorageService
    {
        private readonly IClock clock;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;

        public EncryptedTemporaryStorageService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock)
        {
            this.clock = clock;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
        }

        private string StoreStringI(string plaintextMessage, TimeSpan expireAfter)
        {
            var p = new RijndaelCryptoProvider();

            var compoundKey = new KeyV1
            {
                Version = KeyV1.ProtocolVersionNameV1,
                Id = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(),
                Iv = p.GenerateIv(),
                Key = p.GenerateKey()
            };

            var cipherText = p.Encrypt(compoundKey.Iv, compoundKey.Key, plaintextMessage);

            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var now = clock.Now.DateTime;
                context.TemporaryExternallyEncryptedItems.Add(new TemporaryExternallyEncryptedItem
                {
                    Id = compoundKey.Id,
                    AddedDate = now,
                    DeleteAfterDate = now.Add(expireAfter),
                    ProtocolVersionName = compoundKey.Version,
                    CipherText = cipherText
                });
                context.SaveChanges();
            }

            return compoundKey.Format();
        }

        private bool TryGetStringI(string compoundKey, out string plainTextMessage)
        {
            plainTextMessage = null;

            if (!KeyV1.TryParse(compoundKey, out var key))
                return false;

            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var now = clock.Now;
                var cipherText = ItemQuery(key, context)
                    .Select(x => x.CipherText)
                    .SingleOrDefault();
                if (cipherText == null)
                {
                    plainTextMessage = null;
                    return false;
                }

                var p = new RijndaelCryptoProvider();
                plainTextMessage = p.Decrypt(key.Iv, key.Key, cipherText);
                return true;
            }
        }

        public string StoreString(string plaintextMessage, TimeSpan expireAfter)
        {
            return StoreStringI(plaintextMessage, expireAfter);
        }

        public bool TryGetString(string compoundKey, out string value)
        {
            return TryGetStringI(compoundKey, out value);
        }

        public bool DeleteIfExists(string compoundKey)
        {
            if (!KeyV1.TryParse(compoundKey, out var key))
                return false;

            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var now = clock.Now;
                var item = ItemQuery(key, context)
                    .SingleOrDefault();

                if (item == null)
                    return false;

                context.TemporaryExternallyEncryptedItems.Remove(item);

                context.SaveChanges();

                return true;
            }
        }

        public void DeleteExpiredItems()
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var now = clock.Now;

                var expiredItems = context.TemporaryExternallyEncryptedItems.Where(x => x.DeleteAfterDate < now).ToList();
                context.TemporaryExternallyEncryptedItems.RemoveRange(expiredItems);

                context.SaveChanges();
            }
        }

        private IQueryable<TemporaryExternallyEncryptedItem> ItemQuery(KeyV1 key, PreCreditContextExtended context)
        {
            var now = clock.Now;
            return context
                .TemporaryExternallyEncryptedItems
                .Where(x => x.ProtocolVersionName == key.Version && x.Id == key.Id && x.DeleteAfterDate > now);
        }

        private class KeyV1
        {
            public string Version { get; set; }
            public string Iv { get; set; }
            public string Key { get; set; }
            public string Id { get; set; }

            public static bool TryParse(string compoundKey, out KeyV1 key)
            {
                key = null;

                var parts = compoundKey.Split('_');
                if (parts[0] != ProtocolVersionNameV1)
                    return false;

                key = new KeyV1
                {
                    Version = parts[0],
                    Iv = parts[1],
                    Key = parts[2],
                    Id = parts[3]
                };

                return true;
            }

            public string Format()
            {
                return $"{Version}_{Iv}_{Key}_{Id}";
            }

            public const string ProtocolVersionNameV1 = "v1";
        }
    }

    public interface IEncryptedTemporaryStorageService
    {
        bool TryGetString(string compoundKey, out string value);
        string StoreString(string plaintextMessage, TimeSpan expireAfter);
        bool DeleteIfExists(string compoundKey);
        void DeleteExpiredItems();
    }
}