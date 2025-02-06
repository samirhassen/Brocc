using Microsoft.AspNetCore.Mvc;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;

namespace NTech.Core.PreCredit.Apis
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.ullegacy" })]
    public class UlLegacyApplicationBasisController : Controller
    {
        private readonly LegacyUlApplicationBasisService basisService;

        public UlLegacyApplicationBasisController(LegacyUlApplicationBasisService basisService)
        {
            this.basisService = basisService;
        }

        [HttpPost]
        [Route("Api/PreCredit/ApplicationBasisData/Fetch")]
        public UlLegacyApplicationBasisResponse ApplicationBasisData(UlLegacyApplicationBasisRequest request)
        {
            return basisService.GetApplicationBasisData(request);
        }
    }
}
