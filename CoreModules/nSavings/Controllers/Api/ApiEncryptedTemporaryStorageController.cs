using System;
using System.Linq;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize(ValidateAccessToken = true)]
    [RoutePrefix("Api/EncryptedTemporaryStorage")]
    public class ApiEncryptedTemporaryStorageController : NController
    {
        private const string CurrentProtocolVersionName = "v1";
        private const string ProtocolVersionNameV1 = "v1";

        public static string StoreStringI(string plaintextMessage, int? expireAfterHours)
        {
            var iv = RijndaelCryptoProvider.GenerateIv();
            var key = RijndaelCryptoProvider.GenerateKey();
            var id = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken();
            var cipherText = RijndaelCryptoProvider.Encrypt(iv, key, plaintextMessage);

            using (var context = new SavingsContext())
            {
                var now = DateTime.Now;
                context.TemporaryExternallyEncryptedItems.Add(new TemporaryExternallyEncryptedItem
                {
                    Id = id,
                    AddedDate = now,
                    DeleteAfterDate = now.AddHours(expireAfterHours ?? 4),
                    ProtocolVersionName = CurrentProtocolVersionName,
                    CipherText = cipherText
                });
                context.SaveChanges();
            }

            return $"{CurrentProtocolVersionName}_{iv}_{key}_{id}";
        }

        private static bool TryGetStringI_V1(string compoundKey, out string plainTextMessage)
        {
            var parts = compoundKey.Split('_');
            if (parts[0] != ProtocolVersionNameV1)
            {
                plainTextMessage = null;
                return false;
            }

            var id = parts[3];

            using (var context = new SavingsContext())
            {
                var cipherText = context
                    .TemporaryExternallyEncryptedItems
                    .Where(x => x.ProtocolVersionName == ProtocolVersionNameV1 && x.Id == id)
                    .Select(x => x.CipherText)
                    .SingleOrDefault();
                if (cipherText == null)
                {
                    plainTextMessage = null;
                    return false;
                }

                var iv = parts[1];
                var key = parts[2];
                var p = new RijndaelCryptoProvider();
                plainTextMessage = RijndaelCryptoProvider.Decrypt(iv, key, cipherText);
                return true;
            }
        }

        public static bool TryGetStringI(string compoundKey, out string plainTextMessage)
        {
            var parts = compoundKey.Split('_');

            var versionName = parts[0];
            if (versionName == ProtocolVersionNameV1)
            {
                return TryGetStringI_V1(compoundKey, out plainTextMessage);
            }

            plainTextMessage = null;
            return false;
        }

        [HttpPost]
        [Route("StoreString")]
        public ActionResult StoreString(string plaintextMessage, int? expireAfterHours = 4)
        {
            return Json2(new
            {
                compoundKey = StoreStringI(plaintextMessage, expireAfterHours)
            });
        }

        [HttpPost]
        [Route("GetString")]
        public ActionResult GetString(string compoundKey)
        {
            if (TryGetStringI(compoundKey, out var m))
                return Json2(new
                {
                    exists = true,
                    plaintextMessage = m
                });
            
            return Json2(new
            {
                exists = false
            });
        }

        public static void DeleteExpiredItems()
        {
            using (var savingsContext = new SavingsContext())
            {
                var now = DateTime.Now;

                var expiredItems = savingsContext.TemporaryExternallyEncryptedItems.Where(x => x.DeleteAfterDate < now)
                    .ToList();
                savingsContext.TemporaryExternallyEncryptedItems.RemoveRange(expiredItems);

                savingsContext.SaveChanges();
            }
        }
    }
}