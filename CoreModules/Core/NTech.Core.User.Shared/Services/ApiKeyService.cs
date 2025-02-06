using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace NTech.Core.User.Shared.Services
{
    /*
      Why is this not in user you might ask? 
      We will likely need to swap out the entire user module for at least identity server 4 and maybe just drop it entirely then
      and use an external service like azure users ... for this reason we dont want to add more things there.        
     */
    public class ApiKeyService
    {
        private readonly UserContextFactory userContextFactory;
        public const int CurrentApiKeyModelVersion = 1; //Increment this whenever ApiKeyModel changes and use it to handle migration or handling differences between version

        public ApiKeyService(UserContextFactory userContextFactory)
        {
            this.userContextFactory = userContextFactory;
        }

        private static string ComputeKeyHash(string rawApiKey) => Hashes.Sha256(rawApiKey);

        public CreateApiKeyResult CreateApiKey(CreateApiKeyRequest request)
        {
            request = request ?? new CreateApiKeyRequest();

            if (string.IsNullOrWhiteSpace(request.ScopeName))
                throw new NTechCoreWebserviceException("Missing scope") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new NTechCoreWebserviceException("Missing description") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            //The prefix NT<version>- is just to help with debugging. Like are we sure they are using one of our tokens and could problem be because of version changes. It does not contribute any security.
            var rawApiKey = $"NT{CurrentApiKeyModelVersion}-{OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(length: 40)}";
            //NOTE: Do NOT change this code and store the raw key

            string ipAddressFilterLocal = null;
            if (!string.IsNullOrWhiteSpace(request.IpAddressFilter))
            {
                if (!TryParseIpAddressFilter(request.IpAddressFilter, out var _, out ipAddressFilterLocal))
                    throw new NTechCoreWebserviceException("Invalid ip address filter") { IsUserFacing = true, ErrorCode = "invalidIpAddressFilter", ErrorHttpStatusCode = 400 };
            }

            using (var context = userContextFactory.CreateContext())
            {
                var apiKeyHash = ComputeKeyHash(rawApiKey);
                var now = context.CoreClock.Now;
                var model = new ApiKeyModel
                {
                    Version = CurrentApiKeyModelVersion,
                    IpAddressFilter = ipAddressFilterLocal,
                    CreationDate = now,
                    ExpirationDate = request.ExpiresAfterDays.HasValue ? now.AddDays(request.ExpiresAfterDays.Value) : (DateTimeOffset?)null,
                    Description = request.Description?.Trim(),
                    ProviderName = request.ProviderName?.Trim(),
                    Id = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(length: 20), //Do NOT change this to share with rawApiKey
                    RevokedDate = null,
                    ScopeName = request.ScopeName?.Trim()
                };

                SetValue(context, KeyValueStoreKeySpaceCode.ApiKeyIdByHash, apiKeyHash, model.Id);
                SetValue(context, KeyValueStoreKeySpaceCode.ApiKeyModelById, model.Id, model);

                context.SaveChanges();

                return new CreateApiKeyResult
                {
                    RawApiKey = rawApiKey,
                    StoredModel = model
                };
            }
        }

        public RevokeApiKeyResult RevokeApiKey(ApiKeyIdOnlyRequest request)
        {
            request = request ?? new ApiKeyIdOnlyRequest();
            if (string.IsNullOrWhiteSpace(request.ApiKeyId))
                return new RevokeApiKeyResult();

            using (var context = userContextFactory.CreateContext())
            {
                var model = GetValue<ApiKeyModel>(context, KeyValueStoreKeySpaceCode.ApiKeyModelById, request.ApiKeyId.Trim());
                if (model == null)
                    return new RevokeApiKeyResult();
                if (model.RevokedDate.HasValue)
                    return new RevokeApiKeyResult();
                model.RevokedDate = context.CoreClock.Now;

                SetValue(context, KeyValueStoreKeySpaceCode.ApiKeyModelById, model.Id, model);

                context.SaveChanges();

                return new RevokeApiKeyResult { WasRevoked = true };
            }
        }

        public ApiKeyAuthenticationResult Authenticate(ApiKeyAuthenticationRequest request)
        {
            request = request ?? new ApiKeyAuthenticationRequest();

            using (var context = userContextFactory.CreateContext())
            {
                var hash = ComputeKeyHash(request.RawApiKey);

                var id = GetValue<string>(context, KeyValueStoreKeySpaceCode.ApiKeyIdByHash, hash);

                ApiKeyAuthenticationResult Fail(FailedApiKeyAuthentcationReasonCode reason) =>
                    new ApiKeyAuthenticationResult
                    {
                        IsAuthenticated = false,
                        AuthenticatedKeyModel = null,
                        FailedAuthenticationReason = reason
                    };

                if (id == null)
                    return Fail(FailedApiKeyAuthentcationReasonCode.NoSuchHashExists);

                var model = GetValue<ApiKeyModel>(context, KeyValueStoreKeySpaceCode.ApiKeyModelById, id);

                if (model == null)
                    return Fail(FailedApiKeyAuthentcationReasonCode.NoSuchIdExists);

                if (model.RevokedDate.HasValue)
                    return Fail(FailedApiKeyAuthentcationReasonCode.Revoked);

                if (model.ScopeName != request.AuthenticationScope)
                    return Fail(FailedApiKeyAuthentcationReasonCode.WrongScope);

                if (model.ExpirationDate.HasValue && context.CoreClock.Now > model.ExpirationDate.Value)
                    return Fail(FailedApiKeyAuthentcationReasonCode.Expired);

                if (model.IpAddressFilter != null)
                {
                    if (string.IsNullOrWhiteSpace(request.CallerIpAddress))
                        return Fail(FailedApiKeyAuthentcationReasonCode.CallerIpAddressMissing);

                    if (!IPAddress.TryParse(request.CallerIpAddress, out var parsedCallerIpAddress))
                        return Fail(FailedApiKeyAuthentcationReasonCode.CallerIpAddressInvalid);

                    if (!TryParseIpAddressFilter(model.IpAddressFilter, out var addressList, out var _))
                        throw new Exception($"Invalid stored IpAddressFilter for key with id {model.Id}");

                    if (!addressList.Any(x => x.ToString() == parsedCallerIpAddress.ToString()))
                        return Fail(FailedApiKeyAuthentcationReasonCode.CallerIpAddressNotAllowed);
                }

                return new ApiKeyAuthenticationResult
                {
                    IsAuthenticated = true,
                    FailedAuthenticationReason = null,
                    AuthenticatedKeyModel = model
                };
            }
        }

        public ApiKeyModel GetApiKey(ApiKeyIdOnlyRequest request)
        {
            using (var context = userContextFactory.CreateContext())
            {
                return GetValue<ApiKeyModel>(context, KeyValueStoreKeySpaceCode.ApiKeyModelById, request?.ApiKeyId);
            }
        }

        public List<ApiKeyModel> GetAllApiKeys()
        {
            using (var context = userContextFactory.CreateContext())
            {
                return context
                    .KeyValueItemsQueryable
                    .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.ApiKeyModelById.ToString())
                    .Select(x => x.Value)
                    .ToList()
                    .Select(x => JsonConvert.DeserializeObject<ApiKeyModel>(x))
                    .ToList();
            }
        }

        public static bool TryParseIpAddressFilter(string ipAddressFilter, out List<IPAddress> addressList, out string normalizedIpAddressFilter)
        {
            addressList = null;
            normalizedIpAddressFilter = null;

            if (string.IsNullOrWhiteSpace(ipAddressFilter))
                return false;

            var addressListLocal = new List<IPAddress>();
            foreach (var ipAddressRaw in ipAddressFilter.Split(','))
            {
                if (IPAddress.TryParse(ipAddressRaw?.Trim(), out var ipAddressParsed))
                    addressListLocal.Add(ipAddressParsed);
                else
                    return false;
            }

            addressList = addressListLocal;
            normalizedIpAddressFilter = string.Join(", ", addressList.Select(x => x.ToString()));

            return true;
        }

        private T GetValue<T>(IUserContextExtended context, KeyValueStoreKeySpaceCode keySpace, string key) where T : class
        {
            var rawValue = KeyValueStoreService.GetValueComposable(context, key, keySpace.ToString());
            return rawValue == null ? null : JsonConvert.DeserializeObject<T>(rawValue);
        }

        private void SetValue<T>(IUserContextExtended context, KeyValueStoreKeySpaceCode keySpace, string key, T value) where T : class =>
            KeyValueStoreService.SetValueComposable(context, key, keySpace.ToString(), JsonConvert.SerializeObject(value), context.CurrentUser, context.CoreClock);
    }

    public class CreateApiKeyRequest
    {
        public string ScopeName { get; set; }
        public string Description { get; set; }
        public string ProviderName { get; set; }
        public int? ExpiresAfterDays { get; set; }
        public string IpAddressFilter { get; set; }
    }

    public class ApiKeyIdOnlyRequest
    {
        public string ApiKeyId { get; set; }
    }

    public class CreateApiKeyResult
    {
        public string RawApiKey { get; set; }
        public ApiKeyModel StoredModel { get; set; }
    }

    public class RevokeApiKeyResult
    {
        public bool WasRevoked { get; set; }
    }

    public class ApiKeyModel
    {
        public int Version { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public string ScopeName { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public DateTimeOffset? RevokedDate { get; set; }
        public string IpAddressFilter { get; set; }
        public string ProviderName { get; set; }
    }

    public class ApiKeyAuthenticationRequest
    {
        public string RawApiKey { get; set; }
        public string AuthenticationScope { get; set; }
        public string CallerIpAddress { get; set; }
    }

    public class ApiKeyAuthenticationResult
    {
        public bool IsAuthenticated { get; set; }
        public FailedApiKeyAuthentcationReasonCode? FailedAuthenticationReason { get; set; }
        public ApiKeyModel AuthenticatedKeyModel { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FailedApiKeyAuthentcationReasonCode
    {
        NoSuchHashExists,
        NoSuchIdExists,
        WrongScope,
        Revoked,
        Expired,
        CallerIpAddressMissing,
        CallerIpAddressInvalid,
        CallerIpAddressNotAllowed
    }
}
