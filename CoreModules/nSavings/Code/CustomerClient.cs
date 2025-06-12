using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nSavings.Code
{
    public class CustomerClient : AbstractServiceClient
    {
        protected override string ServiceName => "nCustomer";

        private class GetCustomerIdResult
        {
            public int CustomerId { get; set; }
        }

        private class FetchTrapetsAmlDataResult
        {
            public string NewLatestSeenTimestamp { get; set; }
            public List<TrapetsAmlItem> Items { get; set; }
        }

        public Tuple<byte[], List<TreasuryAmlItem>> FetchTreasuryAmlData(byte[] latestSeenTimestamp,
            IList<int> customerIds)
        {
            var rr = Begin(timeout: TimeSpan.FromMinutes(30))
                .PostJson("Kyc/FetchTreasuryAmlData", new
                {
                    customerIds = customerIds,
                    latestSeenTimestamp =
                        latestSeenTimestamp == null ? null : Convert.ToBase64String(latestSeenTimestamp)
                })
                .ParseJsonAs<FetchTreasuryAmlDataResult>();

            if (rr.Items == null || rr.Items.Count <= 0) return Tuple.Create((byte[])null, new List<TreasuryAmlItem>());

            if (rr.NewLatestSeenTimestamp == null)
                throw new Exception("Missing new latest timestamp");
            return Tuple.Create(Convert.FromBase64String(rr.NewLatestSeenTimestamp), rr.Items);
        }

        public Tuple<byte[], List<TrapetsAmlItem>> FetchTrapetsAmlData(byte[] latestSeenTimestamp,
            IList<int> customerIds)
        {
            var rr = Begin()
                .PostJson("Kyc/FetchTrapetsAmlData", new
                {
                    customerIds = customerIds,
                    latestSeenTimestamp =
                        latestSeenTimestamp == null ? null : Convert.ToBase64String(latestSeenTimestamp)
                })
                .ParseJsonAs<FetchTrapetsAmlDataResult>();

            if (rr.Items == null || rr.Items.Count <= 0) return Tuple.Create((byte[])null, new List<TrapetsAmlItem>());

            if (rr.NewLatestSeenTimestamp == null)
                throw new Exception("Missing new latest timestamp");
            return Tuple.Create(Convert.FromBase64String(rr.NewLatestSeenTimestamp), rr.Items);
        }

        public int GetCustomerId(ICivicRegNumber civicRegNr)
        {
            //These can be heavily cached since the ids never change
            return NTechCache.WithCacheS(
                $"CustomerIdByCivicRegNr({civicRegNr.NormalizedValue})",
                TimeSpan.FromHours(8),
                () => GetCustomerIdI(civicRegNr));
        }

        public int GetCustomerIdI(ICivicRegNumber civicRegNr)
        {
            return Begin().PostJson("api/CustomerIdByCivicRegNr", new
            {
                civicRegNr = civicRegNr.NormalizedValue,
            }).ParseJsonAs<GetCustomerIdResult>().CustomerId;
        }

        private class GetPropertyResult
        {
            public List<GetPropertyCustomer> Customers { get; set; }
        }

        public IDictionary<int, GetPropertyCustomer> BulkFetchPropertiesByCustomerIds(ISet<int> customerIds,
            params string[] propertyNames)
        {
            return Begin()
                .PostJson("Customer/BulkFetchPropertiesByCustomerIds", new
                {
                    propertyNames = propertyNames,
                    customerIds = customerIds
                })
                .ParseJsonAs<GetPropertyResult>()
                .Customers
                .ToDictionary(x => x.CustomerId, x => x);
        }

        public Dictionary<int, Dictionary<string, string>> BulkFetchPropertiesByCustomerIdsSimple(ISet<int> customerIds,
            params string[] propertyNames)
        {
            return BulkFetchPropertiesByCustomerIds(customerIds, propertyNames)
                ?.ToDictionary(x => x.Key, x => x.Value.Properties.ToDictionary(y => y.Name, y => y.Value));
        }

        public ListScreenBatchResult ListScreenBatch(ISet<int> customerIds, DateTime screenDate)
        {
            return Begin().PostJson("Api/KycScreening/ListScreenBatch", new
            {
                customerIds = customerIds,
                screenDate = screenDate
            }).ParseJsonAs<ListScreenBatchResult>();
        }

        public static Uri GetCustomerCardUri(int customerId, bool forceLegacyUi = false,
            NTechNavigationTarget backTarget = null)
        {
            return NEnv.ServiceRegistry.External.ServiceUrl("nCustomer", "Customer/CustomerCard",
                Tuple.Create("customerId", customerId.ToString()),
                Tuple.Create("forceLegacyUi", forceLegacyUi ? "true" : null),
                backTarget == null ? null : Tuple.Create("backTarget", backTarget.GetBackTargetOrNull()));
        }

        public static Uri GetCustomerFatcaCrsUri(int customerId, NTechNavigationTarget backTarget = null)
        {
            var args = new List<Tuple<string, string>>
            {
                Tuple.Create("customerId", customerId.ToString())
            };
            if (backTarget != null)
                args.Add(Tuple.Create("backTarget", backTarget.GetBackTargetOrNull()));

            return NEnv.ServiceRegistry.External.ServiceUrl("nCustomer", "Ui/KycManagement/FatcaCrs", args.ToArray());
        }

        public static string GetCustomerPepKycUrl(int customerId, NTechNavigationTarget back)
        {
            var backTarget = back?.GetBackTargetOrNull();
            return NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer", "Ui/KycManagement/Manage",
                Tuple.Create("customerId", customerId.ToString()),
                backTarget == null ? null : Tuple.Create("backTarget", backTarget)).ToString();
        }

        public static string GetCustomerKycQuestionsUrl(int customerId, NTechNavigationTarget back)
        {
            var backTarget = back?.GetBackTargetOrNull();
            return NEnv.ServiceRegistry.Internal.ServiceUrl("nBackOffice", $"/s/customer-kyc/questions/{customerId}",
                backTarget == null ? null : Tuple.Create("backTarget", backTarget)).ToString();
        }

        private class ExistAllPropertiesResult
        {
            public bool AllExist { get; set; }
            public List<string> MissingPropertyNames { get; set; }
        }

        public class CreateOrUpdateCustomerInput
        {
            public int? ExpectedCustomerId { get; set; }
            public bool KycScreenIfNeeded { get; set; }
            public DateTime? Today { get; set; }
            public Dictionary<string, string> CustomerItems { get; set; }
            public Dictionary<string, string> CustomerItemSources { get; set; }
            public Dictionary<string, string> KycQuestionItems { get; set; }
            public List<string> OtherCustomersWithSameDataCheckNames { get; set; }
        }

        public class CreateOrUpdateCustomerResult
        {
            public class KycScreenResultModel
            {
                public bool Success { get; set; }
                public bool Skipped { get; set; }
                public bool IsManualAttentionNeeded { get; set; }
                public string FailureCode { get; set; }
            }

            public KycScreenResultModel KycScreenResult { get; set; }
            public Dictionary<string, List<int>> OtherCustomerIdsWithSameDataResult { get; set; }
        }

        public CreateOrUpdateCustomerResult CreateOrUpdateCustomer(CreateOrUpdateCustomerInput input)
        {
            return Begin()
                .PostJson("Customer/CreateOrUpdateCustomer", input)
                .ParseJsonAs<CreateOrUpdateCustomerResult>();
        }

        public class CustomerPropertyModel
        {
            public int CustomerId { get; set; }
            public string Name { get; set; }
            public string Group { get; set; }
            public string Value { get; set; }
            public bool IsSensitive { get; set; }
        }

        //TODO: Refactor out all use of this since it encourages looping instead of batching
        public IDictionary<string, string> GetCustomerCardItems(int customerId, params string[] names)
        {
            return Begin()
                .PostJson("Customer/GetDecryptedProperties", new
                {
                    customerId = customerId,
                    names = new HashSet<string>(names).ToList()
                })
                .ParseJsonAs<List<CustomerPropertyModel>>()
                .ToDictionary(x => x.Name, x => x.Value);
        }

        private class FindCustomerIdsMatchingAllSearchTermsResult
        {
            public List<int> CustomerIds { get; set; }
        }

        public List<int> FindCustomerIdsMatchingAllSearchTerms(List<CustomerSearchTermModel> terms)
        {
            var rr = Begin()
                .PostJson("Customer/FindCustomerIdsMatchingAllSearchTerms", new
                {
                    terms = terms
                })
                .ParseJsonAs<FindCustomerIdsMatchingAllSearchTermsResult>();
            return rr?.CustomerIds ?? new List<int>();
        }

        public List<int> FindCustomerIdsByFullName(string name)
        {
            var rr = Begin()
                .PostJson("Customer/FindCustomerIdsMatchingName", new
                {
                    name = name
                })
                .ParseJsonAs<FindCustomerIdsMatchingAllSearchTermsResult>();
            return rr?.CustomerIds ?? new List<int>();
        }


        public List<int> FindCustomerIdsByExactName(string name)
        {
            var rr = Begin().PostJson("Customer/FindCustomerIdsExactName", new
            {
                name = name
            }).ParseJsonAs<FindCustomerIdsMatchingAllSearchTermsResult>();
            return rr?.CustomerIds ?? new List<int>();
        }

        private class FetchTreasuryAmlDataResult
        {
            public string NewLatestSeenTimestamp { get; set; }
            public List<TreasuryAmlItem> Items { get; set; }
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

        public class GetPropertyCustomer
        {
            public int CustomerId { get; set; }
            public List<Property> Properties { get; set; }

            public class Property
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public class CustomerSearchTermModel
        {
            public string TermCode { get; set; }
            public string TermValue { get; set; }
        }

        private class IsCustomerScreenedResult
        {
            public bool IsScreened { get; set; }
        }

        public bool IsCustomerScreened(int customerId)
        {
            return Begin()
                .PostJson("Kyc/IsCustomerScreened", new
                {
                    customerId = customerId
                })
                .ParseJsonAs<IsCustomerScreenedResult>()
                .IsScreened;
        }

        public List<int> GetCustomerIdsWithSameData(string name, string value)
        {
            return Begin()
                .PostJson("Customer/GetCustomerIdsWithSameData", new
                {
                    name = name,
                    value = value
                })
                .ParseJsonAs<List<int>>();
        }

        public LatestKycScreenResult FetchLatestKycScreenResult(int customerId)
        {
            return Begin()
                .PostJson("Kyc/FetchLatestKycScreenResult", new
                {
                    customerId = customerId
                })
                .ParseJsonAsAnonymousType(new { latestResult = (LatestKycScreenResult)null })
                ?.latestResult;
        }

        public class LatestKycScreenResult
        {
            public DateTime QueryDate { get; set; }
            public bool IsPepHit { get; set; }
            public bool IsSanctionHit { get; set; }
        }

        public CustomerContactInfoModel FetchCustomerContactInfo(int customerId, bool includeSensitive,
            bool includeCivicRegNr)
        {
            return Begin()
                .PostJson("/Api/ContactInfo/Fetch", new { customerId, includeSensitive, includeCivicRegNr })
                .ParseJsonAs<CustomerContactInfoModel>();
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

        public List<int> FetchFatcaCustomerIds()
        {
            return Begin()
                .PostJson("/Api/Fatca/FetchCustomerIds", new { })
                .ParseJsonAsAnonymousType(new { CustomerIds = (List<int>)null })
                ?.CustomerIds;
        }

        public void CreateFatcaExportFile(CreateFatcaExportFileRequest request, Stream exportTarget)
        {
            Begin()
                .PostJson("/Api/Fatca/CreateExportFile", request)
                .CopyToStream(exportTarget);
        }

        public class CreateFatcaExportFileRequest
        {
            public DateTime? ExportDate { get; set; }
            public DateTime? ReportingDate { get; set; }
            public List<FatcaExportFileRequestAccount> Accounts { get; set; }
        }

        public class FatcaExportFileRequestAccount
        {
            public string AccountNumber { get; set; }
            public bool IsClosed { get; set; }
            public int CustomerId { get; set; }
            public decimal AccountBalance { get; set; }
            public decimal AccountInterest { get; set; }
        }

        public void MergeCustomerRelations(List<CustomerRelation> relations)
        {
            Begin()
                .PostJson("/Api/CustomerRelations/Merge", new { Relations = relations })
                .EnsureSuccessStatusCode();
        }

        public class CustomerRelation
        {
            public int CustomerId { get; set; }
            public string RelationType { get; set; }
            public string RelationId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public string AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType,
            string sourceId)
        {
            return Begin()
                .PostJson("Api/KycManagement/AddCustomerQuestionsSet", new
                {
                    customerQuestionsSet = customerQuestionsSet,
                    sourceType,
                    sourceId
                }).ParseJsonAsAnonymousType(new { key = (string)null })
                ?.key;
        }

        public class CustomerQuestionsSet
        {
            public DateTime? AnswerDate { get; set; }
            public string Source { get; set; }
            public int? CustomerId { get; set; }
            public List<Item> Items { get; set; }

            public class Item
            {
                public string QuestionCode { get; set; }
                public string AnswerCode { get; set; }
                public string QuestionText { get; set; }
                public string AnswerText { get; set; }
            }
        }

        public FetchCustomerKycStatusChangesResult FetchCustomerKycStatusChanges(ISet<int> customerIds,
            DateTime screenDate)
        {
            return Begin()
                .PostJson("Api/KycScreening/FetchCustomerStatusChanges",
                    new { customerIds = customerIds, screenDate = screenDate })
                .ParseJsonAs<FetchCustomerKycStatusChangesResult>();
        }

        public class FetchCustomerKycStatusChangesResult
        {
            public int TotalScreenedCount { get; set; }
            public List<int> CustomerIdsWithChanges { get; set; }
        }

        public LoadSettingValuesResponse LoadSettings(string key)
        {
            return Begin()
                .PostJson("api/Settings/LoadValues", new { SettingCode = key })
                .ParseJsonAs<LoadSettingValuesResponse>();
        }

        public class LoadSettingValuesResponse
        {
            public Dictionary<string, string> SettingValues { get; set; }
        }
    }
}