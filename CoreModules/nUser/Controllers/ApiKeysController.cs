
using NTech.Core.User.Shared;
using NTech.Core.User.Shared.Services;
using NTech.Services.Infrastructure;
using nUser.DbModel;
using System.Web.Mvc;

namespace nUser.Controllers
{
    [NTechApi]
    [NTechAuthorize]
    public class ApiKeysController : NController
    {
        private ApiKeyService CreateService()
        {
            return new ApiKeyService(
                new UserContextFactory(() =>
                    new UserContextExtended(GetCurrentUserMetadataCore())));
        }

        [Route("Api/User/ApiKeys/Create")]
        [HttpPost]
        public ActionResult Create(CreateApiKeyRequest request) =>
            Json2(CreateService().CreateApiKey(request));

        [Route("Api/User/ApiKeys/GetSingle")]
        [HttpPost]
        public ActionResult GetSingle(ApiKeyIdOnlyRequest request) =>
            Json2(CreateService().GetApiKey(request));

        [Route("Api/User/ApiKeys/GetAll")]
        [HttpPost]
        public ActionResult GetAll() =>
            Json2(CreateService().GetAllApiKeys());

        [Route("Api/User/ApiKeys/Revoke")]
        [HttpPost]
        public ActionResult Revoke(ApiKeyIdOnlyRequest request) =>
            Json2(CreateService().RevokeApiKey(request));

        [Route("Api/User/ApiKeys/Authenticate")]
        [HttpPost]
        public ActionResult Authenticate(ApiKeyAuthenticationRequest request) =>
            Json2(CreateService().Authenticate(request));
    }
}