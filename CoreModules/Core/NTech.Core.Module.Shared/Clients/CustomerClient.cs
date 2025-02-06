using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.ElectronicSignatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class CustomerClient : ICustomerClient
    {
        private ServiceClient client;
        public CustomerClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nCustomer");
        }

        public Dictionary<int, Dictionary<string, string>> BulkFetchPropertiesByCustomerIdsD(ISet<int> customerIds, params string[] propertyNames)
            => client.ToSync(() => BulkFetchPropertiesByCustomerIdsDAsync(customerIds, propertyNames));

        public async Task<Dictionary<int, Dictionary<string, string>>> BulkFetchPropertiesByCustomerIdsDAsync(ISet<int> customerIds, params string[] propertyNames)
        {
            var result = await client.Call(x => x.PostJson("Customer/BulkFetchPropertiesByCustomerIds", new
            {
                propertyNames,
                customerIds
            }), x => x.ParseJsonAs<GetPropertyResult>());
            return result
                .Customers
                .ToDictionary(x => x.CustomerId, x => x.Properties.ToDictionary(y => y.Name, y => y.Value));
        }

        public async Task<Dictionary<string, string>> LoadSettingsAsync(string settingCode) =>
            (await client.Call(
                x => x.PostJson("api/Settings/LoadValues", new { settingCode }),
                x => x.ParseJsonAsAnonymousType(new { SettingValues = (Dictionary<string, string>)null })))
                ?.SettingValues;

        public Dictionary<string, string> LoadSettings(string settingCode) =>
            client.ToSync(() => LoadSettingsAsync(settingCode));

        private class GetPropertyCustomer
        {
            public int CustomerId { get; set; }
            public List<Property> Properties { get; set; }

            public class Property
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        private class GetPropertyResult
        {
            public List<GetPropertyCustomer> Customers { get; set; }
        }

        public async Task<List<int>> FindCustomerIdsOmniAsync(string searchQuery) => (await client.Call(
            x => x.PostJson("Customer/FindCustomerIdsOmni", new { searchQuery }),
            x => x.ParseJsonAs<FindCustomerIdsMatchingAllSearchTermsResult>()))?.CustomerIds ?? new List<int>();

        public List<int> FindCustomerIdsOmni(string searchQuery) => client.ToSync(() => FindCustomerIdsOmniAsync(searchQuery));

        public async Task<List<int>> FindCustomerIdsMatchingAllSearchTermsAsync(List<CustomerSearchTermModel> terms) => (await client.Call(
            x => x.PostJson("Customer/FindCustomerIdsMatchingAllSearchTerms", new { terms }),
            x => x.ParseJsonAsAnonymousType(new { customerIds = (List<int>)null })))?.customerIds;

        public List<int> FindCustomerIdsMatchingAllSearchTerms(List<CustomerSearchTermModel> terms) => client.ToSync(() => FindCustomerIdsMatchingAllSearchTermsAsync(terms));

        private class FindCustomerIdsMatchingAllSearchTermsResult
        {
            public List<int> CustomerIds { get; set; }
        }

        public async Task<int> GetCustomerIdAsync(IOrganisationNumber orgnr)
        {
            return (await client.Call(
                x => x.PostJson("api/CustomerIdByOrgnr", new { orgnr = orgnr.NormalizedValue }),
                x => x.ParseJsonAs<GetCustomerIdResult>())).CustomerId;
        }
        public int GetCustomerId(IOrganisationNumber orgnr) => client.ToSync(() => GetCustomerIdAsync(orgnr));

        public async Task<int> GetCustomerIdAsync(ICivicRegNumber civicRegNr)
        {
            return (await client.Call(
                x => x.PostJson("api/CustomerIdByCivicRegNr", new { civicRegNr = civicRegNr.NormalizedValue }),
                x => x.ParseJsonAs<GetCustomerIdResult>())).CustomerId;
        }

        public int GetCustomerId(ICivicRegNumber civicRegNr) => client.ToSync(() => GetCustomerIdAsync(civicRegNr));

        private class GetCustomerIdResult
        {
            public int CustomerId { get; set; }
        }

        public async Task<int?> ArchiveCustomersBasedOnOurCandidatesAsync(ISet<int> candidateCustomerIds, string sourceModuleName, TimeSpan timeout)
        {
            return (await client
                .Call(
                    x => x.PostJson("Api/Archive/Based-On-Module-Candidates", new { candidateCustomerIds, sourceModuleName = sourceModuleName }),
                    x => x.ParseJsonAsAnonymousType(new { ArchivedCount = (int?)null }), timeout: timeout))?.ArchivedCount;
        }

        public int? ArchiveCustomersBasedOnOurCandidates(ISet<int> candidateCustomerIds, string sourceModuleName, TimeSpan timeout) =>
            client.ToSync(() => ArchiveCustomersBasedOnOurCandidatesAsync(candidateCustomerIds, sourceModuleName, timeout));

        public Task MergeCustomerRelationsAsync(List<CustomerClientCustomerRelation> relations) => client.CallVoid(
            x => x.PostJson("/Api/CustomerRelations/Merge", new { Relations = relations }),
            x => x.EnsureSuccessStatusCode());

        public void MergeCustomerRelations(List<CustomerClientCustomerRelation> relations) => client.ToSync(() => MergeCustomerRelationsAsync(relations));

        public Task<Dictionary<int, KycCustomerOnboardingStatusModel>> FetchCustomerOnboardingStatusesAsync(ISet<int> customerIds, string kycQuestionsSourceType, string kycQuestionsSourceId, bool includeLatestQuestionSets) =>
            client.Call(
                x => x.PostJson("Api/KycManagement/FetchCustomerOnboardingStatuses", new { customerIds, kycQuestionsSourceType, kycQuestionsSourceId, includeLatestQuestionSets }),
                x => x.ParseJsonAs<Dictionary<int, KycCustomerOnboardingStatusModel>>());

        public Dictionary<int, KycCustomerOnboardingStatusModel> FetchCustomerOnboardingStatuses(ISet<int> customerIds, string kycQuestionsSourceType, string kycQuestionsSourceId, bool includeLatestQuestionSets) =>
            client.ToSync(() => FetchCustomerOnboardingStatusesAsync(customerIds, kycQuestionsSourceType, kycQuestionsSourceId, includeLatestQuestionSets));

        public async Task<string> AddCustomerQuestionsSetAsync(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId) => (await client.Call(
            x => x.PostJson("Api/KycManagement/AddCustomerQuestionsSet", new
            {
                customerQuestionsSet = customerQuestionsSet,
                sourceType,
                sourceId
            }),
            x => x.ParseJsonAsAnonymousType(new { key = (string)null })))?.key;

        public string AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId) =>
            client.ToSync(() => AddCustomerQuestionsSetAsync(customerQuestionsSet, sourceType, sourceId));

        public Task<List<int>> GetCustomerIdsWithSameAdressAsync(int customerId, bool treatMissingAddressAsNoHits) => client.Call(
            x => x.PostJson("Customer/GetCustomerIdsWithSameAddress", new
            {
                customerId = customerId,
                treatMissingAddressAsNoHits = treatMissingAddressAsNoHits
            }),
            x => x.ParseJsonAs<List<int>>());

        public List<int> GetCustomerIdsWithSameAdress(int customerId, bool treatMissingAddressAsNoHits) => client.ToSync(() => GetCustomerIdsWithSameAdressAsync(customerId, treatMissingAddressAsNoHits));

        public Task<List<int>> GetCustomerIdsWithSameDataAsync(string name, string value) => client.Call(
            x => x.PostJson("Customer/GetCustomerIdsWithSameData", new
            {
                name = name,
                value = value
            }),
            x => x.ParseJsonAs<List<int>>());
        public List<int> GetCustomerIdsWithSameData(string name, string value) =>
            client.ToSync(() => GetCustomerIdsWithSameDataAsync(name, value));

        public Task UpdateCustomerCardAsync(List<CustomerClientCustomerPropertyModel> customerItems, bool force) => client.CallVoid(
            x => x.PostJson("Customer/UpdateCustomer", new
            {
                items = customerItems,
                force = force
            }),
            x => x.EnsureSuccessStatusCode());

        public void UpdateCustomerCard(List<CustomerClientCustomerPropertyModel> customerItems, bool force) => client.ToSync(() => UpdateCustomerCardAsync(customerItems, force));

        public Task<string> UnlockSensitiveItemAsync(int customerId, string itemName) => client.Call(
            x => x.PostJson("Customer/UnlockSensitiveItemByName", new { customerId, itemName }),
            x => x.IsNotFoundStatusCode ? Task.FromResult((string)null) : x.ParseJsonAs<string>());

        public string UnlockSensitiveItem(int customerId, string itemName) => client.ToSync(() => UnlockSensitiveItemAsync(customerId, itemName));

        public Task SendHtmlSecureMessageWithEmailNotificationAsync(int customerId, string channelId, string channelType, string htmlText) => client.CallVoid(
            x => x.PostJson("Api/CustomerMessage/CreateMessage", new
            {
                CustomerId = customerId,
                ChannelType = channelType,
                ChannelId = channelId,
                Text = htmlText,
                TextFormat = "html",
                IsFromCustomer = false,
                FlagPreviousMessageAsHandled = false,
                NotifyCustomerByEmail = true
            }),
            x => x.EnsureSuccessStatusCode());
        public void SendHtmlSecureMessageWithEmailNotification(int customerId, string channelId, string channelType, string htmlText) => client.ToSync(
            () => SendHtmlSecureMessageWithEmailNotificationAsync(customerId, channelId, channelType, htmlText));

        public async Task<Dictionary<int, string>> KycScreenNewAsync(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen) => (await
            client.Call(
                x => x.PostJson("Api/KycScreening/ListScreenBatch", new { customerIds, screenDate, isNonBatchScreen }),
                x => x.ParseJsonAsAnonymousType(new
                {
                    FailedToGetTrapetsDataItems = new[]
                    {
                        new
                        {
                            CustomerId = (int?)null,
                            Reason = (string)null
                        }
                    }
                })))?.FailedToGetTrapetsDataItems?.ToDictionary(x => x.CustomerId.Value, x => x.Reason) ?? new Dictionary<int, string>();

        public Dictionary<int, string> KycScreenNew(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen) => client.ToSync(() =>
            KycScreenNewAsync(customerIds, screenDate, isNonBatchScreen));

        public async Task<bool> KycScreenAsync(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen) => (await client.Call(
            x => x.PostJson("Api/KycScreening/ListScreenBatch", new { customerIds, screenDate, isNonBatchScreen }),
            x => x.ParseJsonAsAnonymousType(new
            {
                Success = (bool?)null
            })))?.Success ?? false;
        public bool KycScreen(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen) => client.ToSync(() => KycScreenAsync(customerIds, screenDate, isNonBatchScreen));

        public async Task<List<string>> AddCustomerQuestionsSetsBatchAsync(List<CustomerQuestionsSet> customerQuestionsSets, string sourceType, string sourceId) => (await client.Call(
            x => x.PostJson("Api/KycManagement/AddCustomerQuestionsSetBatch", new
            {
                customerQuestionsSets = customerQuestionsSets,
                sourceType,
                sourceId
            }),
            x => x.ParseJsonAsAnonymousType(new { keys = (List<string>)null })))?.keys;

        public List<string> AddCustomerQuestionsSetsBatch(List<CustomerQuestionsSet> customerQuestionsSets, string sourceType, string sourceId) =>
            client.ToSync(() => AddCustomerQuestionsSetsBatchAsync(customerQuestionsSets, sourceType, sourceId));

        public async Task<int> CreateOrUpdatePersonAsync(CreateOrUpdatePersonRequest request) => (await client.Call(
            x => x.PostJson("Api/PersonCustomer/CreateOrUpdate", request),
            x => x.ParseJsonAsAnonymousType(new { CustomerId = 0 }))).CustomerId;

        public int CreateOrUpdatePerson(CreateOrUpdatePersonRequest request) => client.ToSync(() => CreateOrUpdatePersonAsync(request));

        public Task<CustomerCardPropertyStatusResult> CheckPropertyStatusAsync(int customerId, HashSet<string> propertyNames) => client.Call(
            x => x.PostJson("Customer/CheckPropertyStatus", new
            {
                customerId = customerId,
                propertyNames = propertyNames
            }),
            x => x.ParseJsonAs<CustomerCardPropertyStatusResult>());

        public CustomerCardPropertyStatusResult CheckPropertyStatus(int customerId, HashSet<string> propertyNames) => client.ToSync(
            () => CheckPropertyStatusAsync(customerId, propertyNames));

        public async Task<int> CreateOrUpdateCompanyAsync(CreateOrUpdateCompanyRequest request) => (await client.Call(
            x => x.PostJson("Api/CompanyCustomer/CreateOrUpdate", request),
            x => x.ParseJsonAsAnonymousType(new { CustomerId = 0 }))).CustomerId;
        public int CreateOrUpdateCompany(CreateOrUpdateCompanyRequest request) => client.ToSync(() => CreateOrUpdateCompanyAsync(request));

        private class LegacyIsCustomerScreenedResult
        {
            public bool IsScreened { get; set; }
            public DateTime? LatestScreenDate { get; set; }
        }

        public async Task<Tuple<bool, DateTime?>> LegacyIsCustomerScreenedAsync(int customerId)
        {
            var r = (await client.Call(
                x => x.PostJson("Kyc/IsCustomerScreened", new
                {
                    customerId = customerId
                }),
                x => x.ParseJsonAs<LegacyIsCustomerScreenedResult>()));
            return r == null ? null : Tuple.Create(r.IsScreened, r.LatestScreenDate);
        }

        public Tuple<bool, DateTime?> LegacyIsCustomerScreened(int customerId) => client.ToSync(() => LegacyIsCustomerScreenedAsync(customerId));

        private class FetchTrapetsAmlDataResult
        {
            public string NewLatestSeenTimestamp { get; set; }
            public List<TrapetsAmlItem> Items { get; set; }
        }

        public async Task<Tuple<byte[], List<TrapetsAmlItem>>> FetchTrapetsAmlDataAsync(byte[] latestSeenTimestamp, IList<int> customerIds)
        {
            var rr = await client.Call(
                x => x.PostJson("Kyc/FetchTrapetsAmlData", new
                {
                    customerIds = customerIds,
                    latestSeenTimestamp = latestSeenTimestamp == null ? null : Convert.ToBase64String(latestSeenTimestamp)
                }),
                x => x.ParseJsonAs<FetchTrapetsAmlDataResult>(), timeout: TimeSpan.FromMinutes(30));
            if (rr.Items != null && rr.Items.Count > 0)
            {
                if (rr.NewLatestSeenTimestamp == null)
                    throw new Exception("Missing new latest timestamp");
                return Tuple.Create(Convert.FromBase64String(rr.NewLatestSeenTimestamp), rr.Items);
            }
            else
            {
                return Tuple.Create((byte[])null, new List<TrapetsAmlItem>());
            }
        }

        public Tuple<byte[], List<TrapetsAmlItem>> FetchTrapetsAmlData(byte[] latestSeenTimestamp, IList<int> customerIds) =>
            client.ToSync(() => FetchTrapetsAmlDataAsync(latestSeenTimestamp, customerIds));

        public async Task<int?> SendSecureMessageAsync(int customerId, string channelId, string channelType, string text, bool notifyCustomerByEmail, string textFormat) => (await client.Call(
            x => x.PostJson("Api/CustomerMessage/CreateMessage", new
            {
                CustomerId = customerId,
                ChannelType = channelType,
                ChannelId = channelId,
                Text = text,
                IsFromCustomer = false,
                FlagPreviousMessageAsHandled = false,
                NotifyCustomerByEmail = notifyCustomerByEmail,
                TextFormat = textFormat
            }),
            x => x.ParseJsonAsAnonymousType(new { CreatedMessage = new { Id = (int?)null } })))?.CreatedMessage?.Id;

        public int? SendSecureMessage(int customerId, string channelId, string channelType, string text, bool notifyCustomerByEmail, string textFormat) => client.ToSync(() =>
            SendSecureMessageAsync(customerId, channelId, channelType, text, notifyCustomerByEmail, textFormat));

        public Task<FetchCustomerKycStatusChangesResult> FetchCustomerKycStatusChangesAsync(ISet<int> customerIds, DateTime screenDate) => client.Call(
            x => x.PostJson("Api/KycScreening/FetchCustomerStatusChanges", new { customerIds = customerIds, screenDate = screenDate }),
            x => x.ParseJsonAs<FetchCustomerKycStatusChangesResult>());

        public FetchCustomerKycStatusChangesResult FetchCustomerKycStatusChanges(ISet<int> customerIds, DateTime screenDate) =>
            client.ToSync(() => FetchCustomerKycStatusChangesAsync(customerIds, screenDate));

        public Task<CmlExportFileResponse> CreateCm1AmlExportFilesAsync(PerProductCmlExportFileRequest request) => client.Call(
            x => x.PostJson("Kyc/CreateCm1AmlExportFiles", request),
            x => x.ParseJsonAs<CmlExportFileResponse>());

        public CmlExportFileResponse CreateCm1AmlExportFiles(PerProductCmlExportFileRequest request) => client.ToSync(() => CreateCm1AmlExportFilesAsync(request));

        public async Task<Tuple<List<int>, List<int>>> FetchCustomersIdsAsync(List<string> civicRegNumbers, List<string> orgNrs)
        {
            var r = await client.Call(
                x => x.PostJson("api/FetchCustomerIds", new
                {
                    CivicRegNrs = civicRegNumbers,
                    OrgNrs = orgNrs
                }), x => x.ParseJsonAsAnonymousType(new
                {
                    CivicRegNrCustomerIds = (List<int>)null,
                    OrgNrCustomerIds = (List<int>)null
                }));
            return Tuple.Create(r?.CivicRegNrCustomerIds, r?.OrgNrCustomerIds);
        }

        public Tuple<List<int>, List<int>> FetchCustomersIds(List<string> civicRegNumbers, List<string> orgNrs) => client.ToSync(() => FetchCustomersIdsAsync(civicRegNumbers, orgNrs));

        public Task<CustomerContactInfoModel> FetchCustomerContactInfoAsync(int customerId, bool includeSensitive, bool includeCivicRegNr) => client.Call(
            x => x.PostJson("/Api/ContactInfo/Fetch", new { customerId, includeSensitive, includeCivicRegNr }),
            x => x.ParseJsonAs<CustomerContactInfoModel>());

        public CustomerContactInfoModel FetchCustomerContactInfo(int customerId, bool includeSensitive, bool includeCivicRegNr) => client.ToSync(() =>
            FetchCustomerContactInfoAsync(customerId, includeSensitive, includeCivicRegNr));

        public Task AttachArchiveDocumentToSecureMessageAsync(int messageId, string archiveKey) => client.CallVoid(
            x => x.PostJson("Api/CustomerMessage/AttachMessageDocument", new
            {
                MessageId = messageId,
                AttachedFileArchiveKey = archiveKey
            }),
            x => x.EnsureSuccessStatusCode());

        public void AttachArchiveDocumentToSecureMessage(int messageId, string archiveKey) =>
            client.ToSync(() => AttachArchiveDocumentToSecureMessageAsync(messageId, archiveKey));

        public Task BulkInsertCheckpointsAsync(BulkInsertCheckpointsRequest request) => client.CallVoid(
            x => x.PostJson("Api/Customer/Checkpoint/Bulk-Insert-Checkpoints", request),
            x => x.EnsureSuccessStatusCode(),
            isCoreHosted: true);

        public void BulkInsertCheckpoints(BulkInsertCheckpointsRequest request) => client.ToSync(() =>
            BulkInsertCheckpointsAsync(request));

        public async Task<string> FetchCheckpointReasonTextAsync(int checkpointId) => (await client.Call(
            x => x.PostJson("Api/Customer/Checkpoint/Fetch-ReasonText", new { checkpointId }),
            x => x.ParseJsonAsAnonymousType(new { ReasonText = "" }),
            isCoreHosted: true))?.ReasonText;

        public string FetchCheckpointReasonText(int checkpointId) => client.ToSync(() =>
            FetchCheckpointReasonTextAsync(checkpointId));

        public Task<GetActiveCheckPointIdsOnCustomerIdsResult> GetActiveCheckPointIdsOnCustomerIdsAsync(HashSet<int> customerIds, List<string> onlyAmongTheseCodes) => client.Call(
            x => x.PostJson("Api/Customer/Checkpoint/Get-Active-On-Customers", new { customerIds, onlyAmongTheseCodes }),
            x => x.ParseJsonAs<GetActiveCheckPointIdsOnCustomerIdsResult>(),
            isCoreHosted: true);

        public GetActiveCheckPointIdsOnCustomerIdsResult GetActiveCheckpointIdsOnCustomerIds(HashSet<int> customerIds, List<string> onlyAmongTheseCodes) => client.ToSync(() =>
            GetActiveCheckPointIdsOnCustomerIdsAsync(customerIds, onlyAmongTheseCodes));

        public Task<KycQuestionsSession> CreateKycQuestionSessionAsync(CreateKycQuestionSessionRequest request) => client.Call(
            x => x.PostJson("Api/Customer/KycQuestionSession/CreateSession", request),
            x => x.ParseJsonAs<KycQuestionsSession>(), isCoreHosted: true);

        public KycQuestionsSession CreateKycQuestionSession(CreateKycQuestionSessionRequest request) => client.ToSync(() => CreateKycQuestionSessionAsync(request));

        public Task<KycQuestionsSession> FetchKycQuestionSessionAsync(string sessionId) => client.Call(
            x => x.PostJson("Api/Customer/KycQuestionSession/Fetch", new { sessionId }),
            x => x.ParseJsonAs<KycQuestionsSession>(),
            isCoreHosted: true);

        public KycQuestionsSession FetchKycQuestionSession(string sessionId) => client.ToSync(() => FetchKycQuestionSessionAsync(sessionId));

        public Task AddKycQuestionSessionAlternateKeyAsync(string sessionId, string alternateKey) =>
            client.CallVoid(
                x => x.PostJson("Api/Customer/KycQuestionSession/AddAlternateKey", new { sessionId, alternateKey }),
                x => x.EnsureSuccessStatusCode());

        public void AddKycQuestionSessionAlternateKey(string sessionId, string alternateKey) =>
            client.ToSync(() => AddKycQuestionSessionAlternateKeyAsync(sessionId, alternateKey));

        public async Task<Dictionary<int, bool>> CopyCustomerQuestionsSetIfNotExistsAsync(HashSet<int> customerIds, string fromSourceType, string fromSourceId, string toSourceType, string toSourceId, DateTime? ignoreOlderThanDate) =>
            (await client.Call(
                x => x.PostJson("Api/KycManagement/CopyCustomerQuestionsSetIfNotExists", new
                {
                    customerIds,
                    fromSourceType,
                    fromSourceId,
                    toSourceType,
                    toSourceId,
                    ignoreOlderThanDate
                }),
                x => x.ParseJsonAsAnonymousType(new { wasCopiedByCustomerId = (Dictionary<int, bool>)null })))?.wasCopiedByCustomerId;

        public Dictionary<int, bool> CopyCustomerQuestionsSetIfNotExists(HashSet<int> customerIds, string fromSourceType, string fromSourceId, string toSourceType, string toSourceId, DateTime? ignoreOlderThanDate) =>
            client.ToSync(() => CopyCustomerQuestionsSetIfNotExistsAsync(customerIds, fromSourceType, fromSourceId, toSourceType, toSourceId, ignoreOlderThanDate));

        public Task<SetupCustomerKycDefaultsResponse> SetupCustomerKycDefaultsAsync(SetupCustomerKycDefaultsRequest request) => client.Call(
            x => x.PostJson("Api/Customer/SetupCustomerKycDefaults", request),
            x => x.ParseJsonAs<SetupCustomerKycDefaultsResponse>(), isCoreHosted: true);

        public SetupCustomerKycDefaultsResponse SetupCustomerKycDefaults(SetupCustomerKycDefaultsRequest request) => client.ToSync(() => SetupCustomerKycDefaultsAsync(request));

        public async Task<CommonElectronicIdSignatureSession> CreateElectronicIdSignatureSessionAsync(SingleDocumentSignatureRequestUnvalidated request) =>
            (await client.Call(
                x => x.PostJson("api/ElectronicSignatures/Create-Session", request),
                x => x.ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null })))?.Session;

        public CommonElectronicIdSignatureSession CreateElectronicIdSignatureSession(SingleDocumentSignatureRequestUnvalidated request) => client.ToSync(() => CreateElectronicIdSignatureSessionAsync(request));

        public async Task<(CommonElectronicIdSignatureSession Session, bool WasClosed)?> GetElectronicIdSignatureSessionAsync(string sessionId, bool firstCloseItIfOpen) =>        
            await client.Call(
                x => x.PostJson("api/ElectronicSignatures/Get-Session", new { sessionId, firstCloseItIfOpen }),
                async x =>
                {
                    if(x.IsApiError && (await x.ParseApiError()).ErrorCode == "noSuchSessionExists")
                        return ((CommonElectronicIdSignatureSession Session, bool WasClosed)?)null;

                    var result = await x
                        .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null, WasClosed = (bool?)null });

                    if (result?.Session == null)
                        return ((CommonElectronicIdSignatureSession Session, bool WasClosed)?)null;

                    return (Session: result.Session, WasClosed: result.WasClosed == true);
                });

        public (CommonElectronicIdSignatureSession Session, bool WasClosed)? GetElectronicIdSignatureSession(string sessionId, bool firstCloseItIfOpen) => client
            .ToSync(() => GetElectronicIdSignatureSessionAsync(sessionId, firstCloseItIfOpen));
    }
}
