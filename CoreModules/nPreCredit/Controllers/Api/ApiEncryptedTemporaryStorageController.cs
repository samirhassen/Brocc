using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize(ValidateAccessToken = true)]
    [RoutePrefix("Api/EncryptedTemporaryStorage")]
    public class ApiEncryptedTemporaryStorageController : NController
    {
        [HttpPost]
        [Route("StoreString")]
        public ActionResult StoreString(string plaintextMessage, int? expireAfterHours = 4)
        {
            return Json2(new
            {
                compoundKey = this.Service.Resolve<IEncryptedTemporaryStorageService>().StoreString(plaintextMessage, TimeSpan.FromHours(expireAfterHours ?? 4))
            });
        }

        [HttpPost]
        [Route("GetString")]
        public ActionResult GetString(string compoundKey, bool? removeAfter)
        {
            string m;
            var s = this.Service.Resolve<IEncryptedTemporaryStorageService>();
            if (s.TryGetString(compoundKey, out m))
            {
                if (removeAfter ?? false)
                {
                    s.DeleteIfExists(compoundKey);
                }
                return Json2(new
                {
                    exists = true,
                    plaintextMessage = m
                });
            }
            else
                return Json2(new
                {
                    exists = false
                });
        }
    }
}