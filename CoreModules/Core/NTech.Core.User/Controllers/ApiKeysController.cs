using Microsoft.AspNetCore.Mvc;
using NTech.Core.User.Shared.Services;

namespace NTech.Core.User.Controllers;

[ApiController]
[Route("Api/User/ApiKeys")]
public class ApiKeysController : Controller
{
    private readonly ApiKeyService apiKeyService;

    public ApiKeysController(ApiKeyService apiKeyService)
    {
        this.apiKeyService = apiKeyService;
    }

    [Route("Create")]
    [HttpPost]
    public CreateApiKeyResult Create(CreateApiKeyRequest request) =>
        apiKeyService.CreateApiKey(request);

    [Route("GetSingle")]
    [HttpPost]
    public ApiKeyModel GetSingle(ApiKeyIdOnlyRequest request) =>
        apiKeyService.GetApiKey(request);

    [Route("GetAll")]
    [HttpPost]
    public List<ApiKeyModel> GetAll() =>
        apiKeyService.GetAllApiKeys();

    [Route("Revoke")]
    [HttpPost]
    public RevokeApiKeyResult Revoke(ApiKeyIdOnlyRequest request) =>
        apiKeyService.RevokeApiKey(request);

    [Route("Authenticate")]
    [HttpPost]
    public ApiKeyAuthenticationResult Authenticate(ApiKeyAuthenticationRequest request) =>
        apiKeyService.Authenticate(request);
}