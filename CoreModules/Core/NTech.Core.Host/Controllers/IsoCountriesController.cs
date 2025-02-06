using Microsoft.AspNetCore.Mvc;
using NTech.Core.Module.Shared;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class IsoCountriesController : Controller
    {
        [HttpPost]
        [Route("Api/IsoCountries")]
        public List<IsoCountry> IsoCountries()
        {
            return IsoCountry.LoadEmbedded();
        }
    }
}
