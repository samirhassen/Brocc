using Microsoft.AspNetCore.Mvc;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NTech.Core.PreCredit.Apis
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.ullegacy" })]
    public class UlLegacyPetrusController : Controller
    {
        private readonly PetrusOnlyScoringServiceFactory serviceFactory;

        public UlLegacyPetrusController(PetrusOnlyScoringServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        [HttpPost]
        [Route("Api/PreCredit/Petrus/Audit-Trail")]
        public FileResult AduitTrail(PetrusAuditTrailRequest request)
        {
            var document = serviceFactory.GetService().GetPetrusLog(request?.ApplicationId);
            return File(
                Encoding.UTF8.GetBytes(document.ToString(System.Xml.Linq.SaveOptions.None)),
                "application/xml");
        }
    }

    public class PetrusAuditTrailRequest
    {
        [Required]
        public string ApplicationId { get; set; }
    }
}
