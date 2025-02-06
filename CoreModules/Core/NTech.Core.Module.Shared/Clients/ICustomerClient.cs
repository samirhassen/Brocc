using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.ElectronicSignatures;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public interface ICustomerClient : ICustomerClientLoadSettingsOnly
    {
        Task<int> GetCustomerIdAsync(IOrganisationNumber orgnr);
        Task<int> GetCustomerIdAsync(ICivicRegNumber civicRegNr);
        Task<Dictionary<int, Dictionary<string, string>>> BulkFetchPropertiesByCustomerIdsDAsync(ISet<int> customerIds, params string[] propertyNames);
        Task<List<int>> FindCustomerIdsOmniAsync(string query);
        int GetCustomerId(IOrganisationNumber orgnr);
        int GetCustomerId(ICivicRegNumber civicRegNr);
        List<int> FindCustomerIdsOmni(string query);
        Dictionary<int, Dictionary<string, string>> BulkFetchPropertiesByCustomerIdsD(ISet<int> customerIds, params string[] propertyNames);
        Task<int?> ArchiveCustomersBasedOnOurCandidatesAsync(ISet<int> candidateCustomerIds, string sourceModuleName, TimeSpan timeout);
        int? ArchiveCustomersBasedOnOurCandidates(ISet<int> candidateCustomerIds, string sourceModuleName, TimeSpan timeout);
        Task MergeCustomerRelationsAsync(List<CustomerClientCustomerRelation> relations);
        void MergeCustomerRelations(List<CustomerClientCustomerRelation> relations);
        Task<Dictionary<int, KycCustomerOnboardingStatusModel>> FetchCustomerOnboardingStatusesAsync(ISet<int> customerIds, string kycQuestionsSourceType, string kycQuestionsSourceId, bool includeLatestQuestionSets);
        Dictionary<int, KycCustomerOnboardingStatusModel> FetchCustomerOnboardingStatuses(ISet<int> customerIds, string kycQuestionsSourceType, string kycQuestionsSourceId, bool includeLatestQuestionSets);
        Task<string> AddCustomerQuestionsSetAsync(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId);
        string AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId);
        Task<List<int>> GetCustomerIdsWithSameAdressAsync(int customerId, bool treatMissingAddressAsNoHits);
        List<int> GetCustomerIdsWithSameAdress(int customerId, bool treatMissingAddressAsNoHits);
        Task<List<int>> GetCustomerIdsWithSameDataAsync(string name, string value);
        List<int> GetCustomerIdsWithSameData(string name, string value);
        Task UpdateCustomerCardAsync(List<CustomerClientCustomerPropertyModel> customerItems, bool force);
        void UpdateCustomerCard(List<CustomerClientCustomerPropertyModel> customerItems, bool force);
        Task<string> UnlockSensitiveItemAsync(int customerId, string itemName);
        string UnlockSensitiveItem(int customerId, string itemName);
        Task SendHtmlSecureMessageWithEmailNotificationAsync(int customerId, string channelId, string channelType, string htmlText);
        void SendHtmlSecureMessageWithEmailNotification(int customerId, string channelId, string channelType, string htmlText);
        Task<Dictionary<int, string>> KycScreenNewAsync(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen);
        Dictionary<int, string> KycScreenNew(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen);
        Task<bool> KycScreenAsync(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen);
        bool KycScreen(ISet<int> customerIds, DateTime screenDate, bool isNonBatchScreen);
        Task<List<string>> AddCustomerQuestionsSetsBatchAsync(List<CustomerQuestionsSet> customerQuestionsSets, string sourceType, string sourceId);
        List<string> AddCustomerQuestionsSetsBatch(List<CustomerQuestionsSet> customerQuestionsSets, string sourceType, string sourceId);
        Task<int> CreateOrUpdatePersonAsync(CreateOrUpdatePersonRequest request);
        int CreateOrUpdatePerson(CreateOrUpdatePersonRequest request);
        Task<CustomerCardPropertyStatusResult> CheckPropertyStatusAsync(int customerId, HashSet<string> propertyNames);
        CustomerCardPropertyStatusResult CheckPropertyStatus(int customerId, HashSet<string> propertyNames);
        Task<int> CreateOrUpdateCompanyAsync(CreateOrUpdateCompanyRequest request);
        int CreateOrUpdateCompany(CreateOrUpdateCompanyRequest request);
        Task<Tuple<bool, DateTime?>> LegacyIsCustomerScreenedAsync(int customerId);
        Tuple<bool, DateTime?> LegacyIsCustomerScreened(int customerId);
        Task<Tuple<byte[], List<TrapetsAmlItem>>> FetchTrapetsAmlDataAsync(byte[] latestSeenTimestamp, IList<int> customerIds);
        Tuple<byte[], List<TrapetsAmlItem>> FetchTrapetsAmlData(byte[] latestSeenTimestamp, IList<int> customerIds);
        Task<int?> SendSecureMessageAsync(int customerId, string creditNr, string channelType, string text, bool notifyCustomerByEmail, string textFormat);
        int? SendSecureMessage(int customerId, string creditNr, string channelType, string text, bool notifyCustomerByEmail, string textFormat);
        Task<FetchCustomerKycStatusChangesResult> FetchCustomerKycStatusChangesAsync(ISet<int> customerIds, DateTime screenDate);
        FetchCustomerKycStatusChangesResult FetchCustomerKycStatusChanges(ISet<int> customerIds, DateTime screenDate);
        Task<CmlExportFileResponse> CreateCm1AmlExportFilesAsync(PerProductCmlExportFileRequest request);
        CmlExportFileResponse CreateCm1AmlExportFiles(PerProductCmlExportFileRequest request);
        Task<Tuple<List<int>, List<int>>> FetchCustomersIdsAsync(List<string> civicRegNumbers, List<string> orgNrs);
        Tuple<List<int>, List<int>> FetchCustomersIds(List<string> civicRegNumbers, List<string> orgNrs);
        Task<CustomerContactInfoModel> FetchCustomerContactInfoAsync(int customerId, bool includeSensitive, bool includeCivicRegNr);
        CustomerContactInfoModel FetchCustomerContactInfo(int customerId, bool includeSensitive, bool includeCivicRegNr);
        Task BulkInsertCheckpointsAsync(BulkInsertCheckpointsRequest request);
        void BulkInsertCheckpoints(BulkInsertCheckpointsRequest request);
        Task<string> FetchCheckpointReasonTextAsync(int checkpointId);
        string FetchCheckpointReasonText(int checkpointId);
        Task<GetActiveCheckPointIdsOnCustomerIdsResult> GetActiveCheckPointIdsOnCustomerIdsAsync(HashSet<int> customerIds, List<string> onlyAmongTheseCodes);
        GetActiveCheckPointIdsOnCustomerIdsResult GetActiveCheckpointIdsOnCustomerIds(HashSet<int> customerIds, List<string> onlyAmongTheseCodes);
        KycQuestionsSession CreateKycQuestionSession(CreateKycQuestionSessionRequest request);
        KycQuestionsSession FetchKycQuestionSession(string sessionId);
        void AddKycQuestionSessionAlternateKey(string sessionId, string alternateKey);
        Dictionary<int, bool> CopyCustomerQuestionsSetIfNotExists(HashSet<int> customerIds, string fromSourceType, string fromSourceId, string toSourceType, string toSourceId, DateTime? ignoreOlderThanDate);
        SetupCustomerKycDefaultsResponse SetupCustomerKycDefaults(SetupCustomerKycDefaultsRequest request);
        List<int> FindCustomerIdsMatchingAllSearchTerms(List<CustomerSearchTermModel> terms);
        CommonElectronicIdSignatureSession CreateElectronicIdSignatureSession(SingleDocumentSignatureRequestUnvalidated request);
        (CommonElectronicIdSignatureSession Session, bool WasClosed)? GetElectronicIdSignatureSession(string sessionId, bool firstCloseItIfOpen);
    }

    public class CustomerSearchTermModel
    {
        public string TermCode { get; set; }
        public string TermValue { get; set; }
    }

    public interface ICustomerClientLoadSettingsOnly
    {
        Dictionary<string, string> LoadSettings(string settingCode);
    }

    public class GetActiveCheckPointIdsOnCustomerIdsResult
    {
        public Dictionary<int, CheckPoint> CheckPointByCustomerId { get; set; }
        public class CheckPoint
        {
            public int CustomerId { get; set; }
            public int CheckPointId { get; set; }
            public string CheckpointUrl { get; set; }
            public List<string> Codes { get; set; }
        }
    }

    public class BulkInsertCheckpointsRequest
    {
        public class HistoricalCheckpoint
        {
            public int CustomerId { get; set; }
            public bool IsCurrentState { get; set; }
            public string ReasonText { get; set; }
            public List<string> Codes { get; set; }
            public DateTime StateDate { get; set; }
            public int StateBy { get; set; }
        }
        public List<HistoricalCheckpoint> Checkpoints { get; set; }
    }

    public class TrapetsAmlItem
    {
        public DateTime CreationDate { get; set; }
        public string CivicRegNr { get; set; }
        public int CustomerId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string AddressStreet { get; set; }
        public string AddressCity { get; set; }
        public string AddressZipcode { get; set; }
        public string AddressCountry { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Taxcountries { get; set; }
        public string ExternalPep { get; set; }
        public string Ispep { get; set; }
    }

    public class CustomerClientSearchTermModel
    {
        public string TermCode { get; set; }
        public string TermValue { get; set; }
    }

    public class CustomerClientCustomerRelation
    {
        public int CustomerId { get; set; }
        public string RelationType { get; set; }
        public string RelationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CustomerClientCustomerPropertyModel
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Value { get; set; }
        public bool IsSensitive { get; set; }
        public bool? ForceUpdate { get; set; }
    }

    public class KycLocalDecisionCurrentModel
    {
        public int CustomerId { get; set; }
        public bool? IsPep { get; set; }
        public bool? IsSanction { get; set; }
    }

    public class KycCustomerOnboardingStatusModel : KycLocalDecisionCurrentModel
    {
        public DateTime? LatestScreeningDate { get; set; }
        public DateTime? LatestPropertyUpdateDate { get; set; }
        public DateTime? LatestKycQuestionsAnswerDate { get; set; }
        public CustomerQuestionsSet LatestKycQuestionsSet { get; set; }
        public string CustomerBirthDate { get; set; }
        public string CustomerShortName { get; set; }
        public bool HasNameAndAddress { get; set; }
    }

    public class CustomerQuestionsSet
    {
        public const string KeyValueStoreKeySpaceName = "CustomerQuestionsSetV1";
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static CustomerQuestionsSet FromString(string s)
        {
            return JsonConvert.DeserializeObject<CustomerQuestionsSet>(s);
        }
        public DateTime? AnswerDate { get; set; }
        public string Source { get; set; }
        public int? CustomerId { get; set; }
        public List<CustomerQuestionsSetItem> Items { get; set; }
    }
    public class CustomerQuestionsSetItem
    {
        public string QuestionCode { get; set; }
        public string AnswerCode { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
    }


    public class CreateOrUpdatePersonRequest
    {
        public string CivicRegNr { get; set; }
        public DateTime? BirthDate { get; set; }
        public List<Property> Properties { get; set; }
        public List<string> AdditionalSensitiveProperties { get; set; }
        public string EventType { get; set; }
        public string EventSourceId { get; set; }
        public int? ExpectedCustomerId { get; set; }

        public class Property
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool ForceUpdate { get; set; }
        }
    }
    public class CustomerCardPropertyStatusResult
    {
        public List<string> MissingPropertyNames { get; set; }

        public bool HasMissingPropertyNamesIssueOnRequestedProperties()
        {
            return (MissingPropertyNames != null && MissingPropertyNames.Any());
        }

        public string GetMissingPropertyNamesIssueDescription()
        {
            return MissingPropertyNames.Any() ? " Missing properties: " + string.Join(", ", MissingPropertyNames) : string.Empty;
        }
    }

    public class CreateOrUpdateCompanyRequest
    {
        public string Orgnr { get; set; }
        public string CompanyName { get; set; }
        public List<Property> Properties { get; set; }
        public List<string> AdditionalSensitiveProperties { get; set; }
        public string EventType { get; set; }
        public string EventSourceId { get; set; }
        public int? ExpectedCustomerId { get; set; }

        public class Property
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool ForceUpdate { get; set; }
        }
    }

    public class TreasuryAmlItem
    {
        public DateTime CreationDate { get; set; }
        public string CivicRegNr { get; set; }
        public int CustomerId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string AddressStreet { get; set; }
        public string AddressCity { get; set; }
        public string AddressZipcode { get; set; }
        public string AddressCountry { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Taxcountries { get; set; }
        public string ExternalPep { get; set; }
        public string Ispep { get; set; }
        public string IsCompany { get; set; }
        public string TransferedStatus { get; set; }
    }

    public class ListScreenBatchResult
    {
        public bool Success { get; set; }
        public List<Item> FailedToGetTrapetsDataItems { get; set; }

        public class Item
        {
            public int CustomerId { get; set; }
            public string Reason { get; set; }
        }
    }

    public class FetchCustomerKycStatusChangesResult
    {
        public int TotalScreenedCount { get; set; }
        public List<int> CustomerIdsWithChanges { get; set; }
    }

    public class CustomerContactInfoModel
    {
        public int customerId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public DateTime? birthDate { get; set; }
        public string civicRegNr { get; set; }
        public string addressStreet { get; set; }
        public string addressZipcode { get; set; }
        public string addressCity { get; set; }
        public string addressCountry { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
    }

    public class Cm1AmlCustomerItem
    {
        public DateTime CreationDate { get; set; }
        public string CivicRegNr { get; set; }
        public int CustomerId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string AddressStreet { get; set; }
        public string AddressCity { get; set; }
        public string AddressZipcode { get; set; }
        public string AddressCountry { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Taxcountries { get; set; }
        public string ExternalPep { get; set; }
        public string Ispep { get; set; }
        public string IsCompany { get; set; }
        public string TransferedStatus { get; set; }
    }

    public class CmlExportFileResponse
    {
        public string CustomerFileArchiveKey { get; set; }
        public List<string> TransactionFileArchiveKeys { get; set; }
    }

    public class PerProductCmlExportFileRequest
    {
        public List<TransactionModel> Transactions { get; set; }

        public bool Credits { get; set; }
        public bool Savings { get; set; }

        public class TransactionModel
        {
            public DateTime TransactionDate { get; set; }
            public long Id { get; set; }
            public bool IsConnectedToOutgoingPayment { get; set; }
            public bool IsConnectedToIncomingPayment { get; set; }
            public decimal Amount { get; set; }
            public int CustomerId { get; set; }
            public string TransactionCustomerName { get; set; }
        }
    }


    public class CreateKycQuestionSessionRequest
    {
        public string Language { get; set; }
        [Required]
        public string QuestionsRelationType { get; set; }
        [Required]
        public List<int> CustomerIds { get; set; }
        [Required]
        public int SlidingExpirationHours { get; set; }
        public string RedirectUrl { get; set; }
        [Required]
        public string SourceType { get; set; }
        [Required]
        public string SourceId { get; set; }
        public string SourceDescription { get; set; }
        public string CompletionCallbackModuleName { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
        public bool AllowBackToRedirectUrl { get; set; }
    }

    public class KycQuestionsSession
    {
        public string SessionId { get; set; }
        public string Language { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public string QuestionsRelationType { get; set; }
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public string SourceDescription { get; set; }
        public string RedirectUrl { get; set; }
        public Dictionary<string, int> CustomerIdByCustomerKey { get; set; }
        public int SlidingExpirationHours { get; set; }
        public string CompletionCallbackModuleName { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
        public bool AllowBackToRedirectUrl { get; set; }
    }

    public class SetupCustomerKycDefaultsRequest
    {
        public List<int> CustomerIds { get; set; }
        public string OnlyThisSourceType { get; set; }
        public string OnlyThisSourceId { get; set; }
    }

    public class SetupCustomerKycDefaultsResponse
    {
        public bool HaveAllCustomersAnsweredQuestions { get; set; }
    }
}
