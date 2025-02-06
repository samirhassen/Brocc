using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Services;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class EncryptionController : Controller
    {
        private readonly EncryptionService encryptionService;
        private readonly CreditContextFactory contextFactory;

        public EncryptionController(EncryptionService encryptionService, CreditContextFactory contextFactory)
        {
            this.encryptionService = encryptionService;
            this.contextFactory = contextFactory;
        }

        [HttpPost]
        [Route("Api/Credit/Encryption/Decrypt")]
        public CreditDecryptionResponse DecryptedValue(CreditDecryptionRequest request)
        {
            using(var context = contextFactory.CreateContext())
            {
                return new CreditDecryptionResponse
                {
                    DecryptedValue = encryptionService.DecryptEncryptedValues(context, new[] { request.Id })[request.Id]
                };
            }
        }
    }

    public class CreditDecryptionRequest
    {
        [Required]
        public long Id { get; set; }
    }

    public class CreditDecryptionResponse
    {
        public string DecryptedValue { get; set; }
    }
}
