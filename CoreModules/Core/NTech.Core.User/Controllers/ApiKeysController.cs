using Microsoft.AspNetCore.Mvc;
using NTech.Core.User.Shared.Services;

namespace NTech.Core.User.Controllers
{
    [ApiController]
    public class ApiKeysController : Controller
    {
        private readonly ApiKeyService apiKeyService;

        public ApiKeysController(ApiKeyService apiKeyService)
        {
            this.apiKeyService = apiKeyService;
        }

        [Route("Api/User/ApiKeys/Create")]
        [HttpPost]
        public CreateApiKeyResult Create(CreateApiKeyRequest request) =>
            apiKeyService.CreateApiKey(request);

        [Route("Api/User/ApiKeys/GetSingle")]
        [HttpPost]
        public ApiKeyModel GetSingle(ApiKeyIdOnlyRequest request) =>
            apiKeyService.GetApiKey(request);

        [Route("Api/User/ApiKeys/GetAll")]
        [HttpPost]
        public List<ApiKeyModel> GetAll() =>
            apiKeyService.GetAllApiKeys();

        [Route("Api/User/ApiKeys/Revoke")]
        [HttpPost]
        public RevokeApiKeyResult Revoke(ApiKeyIdOnlyRequest request) =>
            apiKeyService.RevokeApiKey(request);

        [Route("Api/User/ApiKeys/Authenticate")]
        [HttpPost]
        public ApiKeyAuthenticationResult Authenticate(ApiKeyAuthenticationRequest request) =>
            apiKeyService.Authenticate(request);
    }
}
