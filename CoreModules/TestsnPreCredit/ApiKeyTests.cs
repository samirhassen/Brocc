using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.User.Shared;
using NTech.Core.User.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using nUser.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class ApiKeyTests
    {
        [TestMethod]
        public void CanAuthenticate()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...");

            var result = service.Authenticate(key.RawApiKey, scopeName, null);
            Assert.AreEqual(true, result.IsAuthenticated);
        }

        [TestMethod]
        public void WrongScope()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...");

            var result = service.Authenticate(key.RawApiKey, scopeName + "b", null);
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.WrongScope, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void NoSuchHashExists()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var result = service.Authenticate("abc123", "scope1", null);
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.NoSuchHashExists, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void Revoked()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...");

            service.RevokeApiKey(new ApiKeyIdOnlyRequest { ApiKeyId = key.StoredModel.Id });

            var result = service.Authenticate(key.RawApiKey, scopeName, null);
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.Revoked, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void NotExpired()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...", expiresAfterDays: 1);

            testSetup.MoveTimeForward(TimeSpan.FromDays(1).Add(-TimeSpan.FromSeconds(1)));

            var result = service.Authenticate(key.RawApiKey, scopeName, null);
            Assert.AreEqual(true, result.IsAuthenticated);
        }

        [TestMethod]
        public void Expired()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...", expiresAfterDays: 1);

            testSetup.MoveTimeForward(TimeSpan.FromDays(1).Add(TimeSpan.FromSeconds(1)));

            var result = service.Authenticate(key.RawApiKey, scopeName, null);
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.Expired, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void IpFilterAllowed()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...", ipAddressFilter: "192.168.10.12");

            var result = service.Authenticate(key.RawApiKey, scopeName, "192.168.10.12");
            Assert.AreEqual(true, result.IsAuthenticated);
        }

        [TestMethod]
        public void IpFilterBlocked()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...", ipAddressFilter: "192.168.10.12");

            var result = service.Authenticate(key.RawApiKey, scopeName, "192.168.10.13");
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.CallerIpAddressNotAllowed, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void CallerIpMissingWithFilter()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...", ipAddressFilter: "192.168.10.12");

            var result = service.Authenticate(key.RawApiKey, scopeName, null);
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.CallerIpAddressMissing, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void CallerIpInvalidWithFilter()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;

            var scopeName = "scope1";
            var key = service.CreateApiKey(scopeName, "...", ipAddressFilter: "192.168.10.12");

            var result = service.Authenticate(key.RawApiKey, scopeName, "invalidip");
            Assert.AreEqual(false, result.IsAuthenticated);
            Assert.AreEqual(FailedApiKeyAuthentcationReasonCode.CallerIpAddressInvalid, result.FailedAuthenticationReason);
        }

        [TestMethod]
        public void CanGetKey()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;
            var key = service.CreateApiKey("scope1", "...");

            Assert.IsNotNull(service.GetApiKey(new ApiKeyIdOnlyRequest { ApiKeyId = key.StoredModel.Id }));
            Assert.IsNull(service.GetApiKey(new ApiKeyIdOnlyRequest { ApiKeyId = key.StoredModel.Id + "x" }));
        }

        [TestMethod]
        public void CanGetAllKeys()
        {
            var testSetup = CreateTestSetup();
            var service = testSetup.Service;
            var scopeName = "scope1";
            service.CreateApiKey(scopeName, "...");
            service.CreateApiKey(scopeName, "...");

            var allKeys = service.GetAllApiKeys();

            Assert.AreEqual(2, allKeys.Count);
        }

        [TestMethod]
        public void IpAddressRateLimiter_FiveFailedAuthenticationAttemptsCauseRateLimit()
        {
            DateTimeOffset? currentTime = DateTimeOffset.UtcNow;
            var rateLimiter = new IpAddressRateLimiter(() => currentTime.Value);
            var ip = "127.0.0.1";

            Enumerable.Range(1, 5).ToList().ForEach(i => rateLimiter.LogAuthenticationAttempt(ip, false));

            Assert.AreEqual(true, rateLimiter.IsIpRateLimited(ip));
        }

        [TestMethod]
        public void IpAddressRateLimiter_SuccessfulAuthenticationAttemptsResetsRateLimit()
        {
            DateTimeOffset? currentTime = DateTimeOffset.UtcNow;
            var rateLimiter = new IpAddressRateLimiter(() => currentTime.Value);
            var ip = "127.0.0.1";

            Enumerable.Range(1, 4).ToList().ForEach(i => rateLimiter.LogAuthenticationAttempt(ip, false));
            rateLimiter.LogAuthenticationAttempt(ip, true); //Resets the rate limit
            rateLimiter.LogAuthenticationAttempt(ip, false);
            Assert.AreEqual(false, rateLimiter.IsIpRateLimited(ip));
        }

        [TestMethod]
        public void IpAddressRateLimiter_RateLimitIsPerIp()
        {
            DateTimeOffset? currentTime = DateTimeOffset.UtcNow;
            var rateLimiter = new IpAddressRateLimiter(() => currentTime.Value);

            Enumerable.Range(1, 5).ToList().ForEach(i => rateLimiter.LogAuthenticationAttempt("127.0.0.1", false));

            Assert.AreEqual(false, rateLimiter.IsIpRateLimited("127.0.0.2"));
        }

        [TestMethod]
        public void IpAddressRateLimiter_RateLimitResetsAfterFiveMinutes()
        {
            DateTimeOffset? currentTime = DateTimeOffset.UtcNow;
            var rateLimiter = new IpAddressRateLimiter(() => currentTime.Value);
            var ip = "127.0.0.1";

            Enumerable.Range(1, 5).ToList().ForEach(i => rateLimiter.LogAuthenticationAttempt(ip, false));

            currentTime = currentTime.Value.AddMinutes(5).AddSeconds(1);

            Assert.AreEqual(false, rateLimiter.IsIpRateLimited(ip));
        }

        [TestMethod]
        public void IpAddressRateLimiter_RateLimitDoesNotResetAfterTwoMinutes()
        {
            DateTimeOffset? currentTime = DateTimeOffset.UtcNow;
            var rateLimiter = new IpAddressRateLimiter(() => currentTime.Value);
            var ip = "127.0.0.1";

            Enumerable.Range(1, 5).ToList().ForEach(i => rateLimiter.LogAuthenticationAttempt(ip, false));

            currentTime = currentTime.Value.AddMinutes(2).AddSeconds(1);

            Assert.AreEqual(true, rateLimiter.IsIpRateLimited(ip));
        }

        private (Mock<IUserContextExtended> TestContext, Action<TimeSpan> MoveTimeForward, ApiKeyService Service, List<KeyValueItem> Items) CreateTestSetup()
        {
            DateTimeOffset? now = new DateTimeOffset(2022, 5, 5, 08, 30, 10, TimeSpan.FromHours(2));
            var clock = new Mock<ICoreClock>(MockBehavior.Strict);
            clock.Setup(x => x.Now).Returns(() => now.Value);
            clock.Setup(x => x.Today).Returns(() => now.Value.Date);

            var user = new Mock<INTechCurrentUserMetadata>(MockBehavior.Strict);
            user.Setup(x => x.UserId).Returns(1);
            user.Setup(x => x.InformationMetadata).Returns("anything");

            var keyValueItems = new List<KeyValueItem>();
            var context = new Mock<IUserContextExtended>(MockBehavior.Strict);
            context.Setup(x => x.SaveChanges()).Returns(0);
            context.Setup(x => x.CurrentUser).Returns(user.Object);
            context.Setup(x => x.CoreClock).Returns(clock.Object);
            context.Setup(x => x.AddKeyValueItem(It.IsAny<KeyValueItem>()))
                .Callback<KeyValueItem>(item => keyValueItems.Add(item));
            context.Setup(x => x.RemoveKeyValueItem(It.IsAny<KeyValueItem>()))
                .Callback<KeyValueItem>(item => keyValueItems.Remove(item));
            context.Setup(x => x.KeyValueItemsQueryable).Returns(keyValueItems.AsQueryable());
            context.Setup(x => x.Dispose());

            return (TestContext: context,
                MoveTimeForward: duration => now = now.Value.Add(duration),
                Service: new ApiKeyService(new UserContextFactory(() => context.Object)),
                Items: keyValueItems);
        }
    }

    internal static class ApiKeyServiceExtensions
    {
        public static CreateApiKeyResult CreateApiKey(this ApiKeyService source, string scopeName, string description, string providerName = null, int? expiresAfterDays = null, string ipAddressFilter = null) =>
            source.CreateApiKey(new CreateApiKeyRequest
            {
                ScopeName = scopeName,
                Description = description,
                ProviderName = providerName,
                ExpiresAfterDays = expiresAfterDays,
                IpAddressFilter = ipAddressFilter
            });

        public static ApiKeyAuthenticationResult Authenticate(this ApiKeyService source, string rawApiKey, string authenticationScope, string callerIpAddress) => source.Authenticate(new ApiKeyAuthenticationRequest
        {
            RawApiKey = rawApiKey,
            AuthenticationScope = authenticationScope,
            CallerIpAddress = callerIpAddress
        });
    }
}
