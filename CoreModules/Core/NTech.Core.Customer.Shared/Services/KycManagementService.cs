using nCustomer.DbModel;
using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services.Kyc
{
    public class KycManagementService : IKycManagementService
    {
        private readonly CustomerContextFactory customerContextFactory;
        private readonly Func<ICustomerContext, CustomerWriteRepositoryBase> createCustomerRepository;
        private readonly IUrlService urlService;
        private readonly KycAnswersUpdateService answersUpdateService;
        private readonly Lazy<CivicRegNumberParser> civicRegNumberParser;

        public KycManagementService(CustomerContextFactory customerContextFactory,
            IUrlService urlService,
            INTechCurrentUserMetadata user,
            IClientConfigurationCore clientConfiguration,
            EncryptionService encryptionService,
            ICoreClock clock,
            KycAnswersUpdateService answersUpdateService) : this(customerContextFactory, 
                x => new CustomerWriteRepository(x, user, clock, encryptionService, clientConfiguration), 
                urlService, clientConfiguration, answersUpdateService)
        {

        }

        public KycManagementService(
            CustomerContextFactory customerContextFactory,
            Func<ICustomerContext, CustomerWriteRepositoryBase> createCustomerRepository,
            IUrlService urlService,
            IClientConfigurationCore clientConfiguration,
            KycAnswersUpdateService answersUpdateService)
        {
            this.customerContextFactory = customerContextFactory;
            this.createCustomerRepository = createCustomerRepository;
            this.urlService = urlService;
            this.answersUpdateService = answersUpdateService;
            civicRegNumberParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(clientConfiguration.Country.BaseCountry));
        }

        public KycLocalDecisionCurrentModel FetchLocalDecisionCurrentData(int customerId)
        {
            using (var db = customerContextFactory.CreateContext())
            {
                var repo = createCustomerRepository(db);
                var props = repo.GetProperties(customerId, onlyTheseNames: new List<string>
                {
                    CustomerProperty.Codes.localIsPep.ToString(),
                    CustomerProperty.Codes.localIsSanction.ToString(),
                    CustomerProperty.Codes.amlRiskClass.ToString(),
                }, skipDecryptingEncryptedItems: false).ToDictionary(x => x.Name, x => x.Value);

                Func<string, bool?> optBool = name => props.ContainsKey(name) ? new bool?(props[name].ToLowerInvariant() == "true") : new bool?();

                return new KycLocalDecisionCurrentModel
                {
                    IsPep = optBool(CustomerProperty.Codes.localIsPep.ToString()),
                    IsSanction = optBool(CustomerProperty.Codes.localIsSanction.ToString()),
                    AmlRiskClass = props?.Opt(CustomerProperty.Codes.amlRiskClass.ToString())
                };
            }
        }

        public Dictionary<int, KycCustomerOnboardingStatusModel> FetchKycCustomerOnboardingStatuses(
            ISet<int> customerIds,
            (string SourceType, string SourceId)? kycQuestionsSource,
            bool includeLatestQuestionSets)
        {
            using (var db = customerContextFactory.CreateContext())
            {
                var repo = createCustomerRepository(db);
                var props = repo.BulkFetch(customerIds, propertyNames: new HashSet<string>
                {
                    CustomerProperty.Codes.localIsPep.ToString(),
                    CustomerProperty.Codes.localIsSanction.ToString(),
                    CustomerProperty.Codes.isCompany.ToString(),
                    CustomerProperty.Codes.firstName.ToString(),
                    CustomerProperty.Codes.companyName.ToString(),
                    CustomerProperty.Codes.birthDate.ToString(),
                    CustomerProperty.Codes.civicRegNr.ToString(),
                    CustomerProperty.Codes.email.ToString(),
                    CustomerProperty.Codes.lastName.ToString(),
                    CustomerProperty.Codes.addressCity.ToString(),
                    CustomerProperty.Codes.addressZipcode.ToString(),
                }, skipDecryptingEncryptedItems: false).ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Name, y => y.Value));

                Func<int, string, bool?> optBool = (customerId, name) =>
                {
                    var v = props.Opt(customerId).Opt(name)?.ToLowerInvariant();
                    if (v == null)
                        return null;
                    return v?.ToLowerInvariant() == "true";
                };

                var latestScreenDateByCustomerId = db
                    .TrapetsQueryResultsQueryable
                    .Where(x => customerIds.Contains(x.CustomerId))
                    .GroupBy(x => x.CustomerId)
                    .Select(x => new
                    {
                        CustomerId = x.Key,
                        QueryDate = x
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.QueryDate)
                            .FirstOrDefault()
                    })
                    .ToList()
                    .ToDictionary(x => x.CustomerId, x => x.QueryDate);

                var q = db
                    .StoredCustomerQuestionSetsQueryable
                    .Where(x => customerIds.Contains(x.CustomerId));
                if (kycQuestionsSource.HasValue)
                {
                    var sourceType = kycQuestionsSource.Value.SourceType;
                    var sourceId = kycQuestionsSource.Value.SourceId;
                    q = q.Where(x => x.SourceType == sourceType && x.SourceId == sourceId);
                }
                var latestKycQuestionsAnswerDateByCustomerId = q
                    .Select(x => new
                    {
                        x.CustomerId,
                        x.Id,
                        x.AnswerDate,
                        x.KeyValueStorageKeySpace,
                        x.KeyValueStorageKey
                    })
                    .ToList()
                    .GroupBy(x => x.CustomerId)
                    .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Id).Select(y => new { y.KeyValueStorageKeySpace, y.KeyValueStorageKey, y.AnswerDate }).First());

                var customerQuestionSetByCustomerId = new Dictionary<int, CustomerQuestionsSet>();
                if (includeLatestQuestionSets)
                {
                    var keysAndSpaces = latestKycQuestionsAnswerDateByCustomerId.Values.Where(x => x.KeyValueStorageKey != null).ToList();
                    var keysOnly = keysAndSpaces.Select(x => x.KeyValueStorageKey).ToList();
                    var questionSetsByKey = db
                        .KeyValueItemsQueryable
                        .Where(x => keysOnly.Contains(x.Key))
                        .ToList()
                        .Where(x => keysAndSpaces.Any(y => y.KeyValueStorageKey == x.Key && y.KeyValueStorageKeySpace == x.KeySpace))
                        .ToDictionary(x => x.Key);

                    foreach (var customer in latestKycQuestionsAnswerDateByCustomerId)
                    {
                        var questionData = questionSetsByKey.Opt(customer.Value.KeyValueStorageKey);
                        if (questionData?.Value != null)
                        {
                            customerQuestionSetByCustomerId[customer.Key] = CustomerQuestionsSet.FromString(questionData.Value);
                        }
                    }
                }

                /*
                 These are used as a proxy for how up to date the customers kyc is for clients
                 that do this manually outside the system. These are the properties that will be set after
                 having done a manual check outside the system.
                 */
                var propertyUpdateFields = new string[] { CustomerProperty.Codes.localIsPep.ToString(), CustomerProperty.Codes.localIsSanction.ToString(),
                    CustomerProperty.Codes.localIsSanction.ToString(), CustomerProperty.Codes.taxcountries.ToString(), CustomerProperty.Codes.citizencountries.ToString() };
                var latestPropertyUpdateDateByCustomerId = db
                    .CustomerPropertiesQueryable.Where(x => x.IsCurrentData && customerIds.Contains(x.CustomerId) && propertyUpdateFields.Contains(x.Name))
                    .Select(x => new { x.CustomerId, x.ChangedDate })
                    .ToList()
                    .GroupBy(x => x.CustomerId)
                    .ToDictionary(x => x.Key, x => x.Max(y => y.ChangedDate));

                var result = new Dictionary<int, KycCustomerOnboardingStatusModel>();
                foreach (var customerId in customerIds)
                {
                    var nameAndDate = GetCustomerShortNameAndBirthDate(props.Opt(customerId));
                    var latestData = latestKycQuestionsAnswerDateByCustomerId.Opt(customerId);
                    var latestKycQuestionsAnswerDate = latestData?.AnswerDate;
                    result[customerId] = new KycCustomerOnboardingStatusModel
                    {
                        CustomerId = customerId,
                        IsPep = optBool(customerId, CustomerProperty.Codes.localIsPep.ToString()),
                        IsSanction = optBool(customerId, CustomerProperty.Codes.localIsSanction.ToString()),
                        LatestScreeningDate = latestScreenDateByCustomerId.ContainsKey(customerId) ? new DateTime?(latestScreenDateByCustomerId[customerId]) : null,
                        LatestKycQuestionsAnswerDate = latestKycQuestionsAnswerDate,
                        LatestPropertyUpdateDate = latestPropertyUpdateDateByCustomerId.ContainsKey(customerId) ? new DateTime?(latestPropertyUpdateDateByCustomerId[customerId].Date.Date) : null,
                        LatestKycQuestionsSet = customerQuestionSetByCustomerId.Opt(customerId),
                        CustomerBirthDate = nameAndDate.CustomerBirthDate,
                        CustomerShortName = nameAndDate.CustomerShortName,
                        HasNameAndAddress = HasNameAndAddress(props.Opt(customerId))
                    };
                }

                return result;
            }
        }

        private static bool HasNameAndAddress(Dictionary<string, string> props)
        {
            string P(CustomerProperty.Codes name) => props.Opt(name.ToString());
            bool IsDefined(CustomerProperty.Codes name) => !string.IsNullOrWhiteSpace(P(name));

            var isCompany = P(CustomerProperty.Codes.isCompany) == "true";
            var isNameDefined = isCompany ? IsDefined(CustomerProperty.Codes.companyName) : (IsDefined(CustomerProperty.Codes.firstName) && IsDefined(CustomerProperty.Codes.lastName));

            return isNameDefined
                && IsDefined(CustomerProperty.Codes.email)
                && IsDefined(CustomerProperty.Codes.addressCity)
                && IsDefined(CustomerProperty.Codes.addressZipcode);
        }

        private (string CustomerShortName, string CustomerBirthDate) GetCustomerShortNameAndBirthDate(Dictionary<string, string> properties)
        {
            if (properties.Opt(CustomerProperty.Codes.isCompany.ToString()) == "true")
            {
                return (CustomerShortName: properties.Opt(CustomerProperty.Codes.companyName.ToString()), CustomerBirthDate: null);
            }
            else
            {
                var birthDate = properties.Opt(CustomerProperty.Codes.birthDate.ToString());
                if (!string.IsNullOrWhiteSpace(birthDate))
                {
                    var civicRegNrRaw = properties.Opt(CustomerProperty.Codes.civicRegNr.ToString());
                    if (civicRegNrRaw != null)
                    {
                        birthDate = civicRegNumberParser.Value.Parse(civicRegNrRaw)?.BirthDate?.ToString("yyyy-MM-dd");
                    }
                }
                return (CustomerShortName: properties.Opt(CustomerProperty.Codes.firstName.ToString()), CustomerBirthDate: birthDate);
            }
        }

        public KycLocalDecisionHistoryModel FetchLocalDecisionHistoryData(int customerId, bool isModellingPep, Func<string, string> getDisplayNameFromUserId)
        {
            using (var db = customerContextFactory.CreateContext())
            {
                var repo = createCustomerRepository(db);

                var currentAndHistoricalValues = repo.GetCurrentAndHistoricalValuesForProperty(
                    customerId,
                    isModellingPep ? CustomerProperty.Codes.localIsPep.ToString() : CustomerProperty.Codes.localIsSanction.ToString(),
                    getDisplayNameFromUserId);

                Func<string, bool?> optBool = x => x != null ? new bool?(x == "true") : new bool?();
                var result = new List<KycLocalDecisionHistoryModel>();
                Func<CustomerPropertyModelExtended, KycLocalDecisionHistoryItem> convert = x =>
                {
                    if (x == null) return null;
                    return new KycLocalDecisionHistoryItem
                    {
                        ChangeDate = x.ChangeDate.DateTime,
                        ChangedByUserDisplayName = x.ChangedByDisplayName,
                        ChangedByUserId = x.ChangedById,
                        IsModellingPep = isModellingPep,
                        Value = optBool(x.Value)
                    };
                };

                return new KycLocalDecisionHistoryModel
                {
                    CustomerId = customerId,
                    IsModellingPep = isModellingPep,
                    CurrentValue = convert(currentAndHistoricalValues.Item1),
                    HistoricalValues = currentAndHistoricalValues.Item2?.Select(convert)?.ToList() ?? new List<KycLocalDecisionHistoryItem>()
                };
            }
        }

        public FetchPropertiesWithGroupedEditHistoryResult FetchPropertiesWithGroupedEditHistory(int customerId, List<string> propertyNames, Func<string, string> getDisplayNameFromUserId)
        {
            var r = new FetchPropertiesWithGroupedEditHistoryResult
            {
                CurrentValues = new Dictionary<string, string>()
            };

            using (var db = customerContextFactory.CreateContext())
            {
                var repo = createCustomerRepository(db);

                var allHistoricalValues = new List<CustomerPropertyModelExtended>();
                foreach (var propertyName in propertyNames)
                {
                    var currentAndHistoricalValues = repo.GetCurrentAndHistoricalValuesForProperty(
                        customerId,
                        propertyName,
                        getDisplayNameFromUserId);
                    r.CurrentValues[propertyName] = currentAndHistoricalValues?.Item1?.Value;
                    if (currentAndHistoricalValues?.Item2 != null)
                        allHistoricalValues.AddRange(currentAndHistoricalValues.Item2);
                }

                var eventsGroups = allHistoricalValues
                                    .Where(x => x.CreatedByBusinessEventId.HasValue)
                                    .GroupBy(x => x.CreatedByBusinessEventId.Value)
                                    .OrderByDescending(x => x.Key)
                                    .Select(x =>
                                    {
                                        return new FetchPropertiesWithGroupedEditHistoryResult.HistoryItem
                                        {
                                            EditDate = x.First().ChangeDate,
                                            UserDisplayName = x.First().ChangedByDisplayName,
                                            UserId = x.First().ChangedById,
                                            Values = x.ToDictionary(
                                            y => y.Name,
                                            y => y.Value)
                                        };
                                    })
                                    .ToList();

                //These are older edits from before events were added. These are considered a single group per item and are always from before events were created. Note that time changes in test can cause wierd things here.
                var nonEventGroups = allHistoricalValues
                                    .Where(x => !x.CreatedByBusinessEventId.HasValue)
                                    .OrderByDescending(x => x.Id)
                                    .Select(x => new FetchPropertiesWithGroupedEditHistoryResult.HistoryItem
                                    {
                                        EditDate = x.ChangeDate,
                                        UserDisplayName = x.ChangedByDisplayName,
                                        UserId = x.ChangedById,
                                        Values = new Dictionary<string, string> { { x.Name, x.Value } }
                                    })
                                    .ToList();

                r.HistoryItems = eventsGroups.Concat(nonEventGroups).ToList();

                return r;
            }
        }

        public void SetCurrentLocalDecision(int customerId, bool isModellingPep, bool currentValue)
        {
            using (var db = customerContextFactory.CreateContext())
            {
                var repo = createCustomerRepository(db);
                db.BeginTransaction();
                try
                {
                    repo.UpdateProperties(new List<CustomerPropertyModel> { new CustomerPropertyModel
                    {
                        CustomerId = customerId,
                        Group = isModellingPep ? CustomerProperty.Groups.pepKyc.ToString() : CustomerProperty.Groups.sanction.ToString(),
                        IsSensitive = true,
                        Name = isModellingPep ? CustomerProperty.Codes.localIsPep.ToString() : CustomerProperty.Codes.localIsSanction.ToString(),
                        Value = currentValue ? "true" : "false"
                    } }, true);

                    db.SaveChanges();
                    db.CommitTransaction();
                }
                catch
                {
                    db.RollbackTransaction();
                    throw;
                }
            }
        }

        public string AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId)
        {
            return answersUpdateService.AddCustomerQuestionsSet(customerQuestionsSet, sourceType, sourceId);
        }

        public CustomerQuestionsSet FetchLatestCustomerQuestionsSet(int customerId)
        {
            if (customerId <= 0)
                throw new Exception("Missing customerId");

            using (var db = customerContextFactory.CreateContext())
            {
                var latestStored = db
                    .StoredCustomerQuestionSetsQueryable
                    .Where(x => x.CustomerId == customerId)
                    .Select(x => new
                    {
                        x.Id,
                        x.KeyValueStorageKeySpace,
                        x.KeyValueStorageKey
                    })
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                if (latestStored != null)
                {
                    var value = KeyValueStoreService.GetValueComposable(db, latestStored.KeyValueStorageKey, latestStored.KeyValueStorageKeySpace);
                    if (value == null)
                        return null;
                    return CustomerQuestionsSet.FromString(value);
                }
                else
                {
                    //NOTE: Data source used by ul legacy. Can probably be removed but need to check the production dbs to see if customers exist where these could cause issues.
                    var repo = createCustomerRepository(db);
                    var latestCustomerQuestionsSetKey = repo
                        .GetProperties(customerId, onlyTheseNames: new List<string> { "latestCustomerQuestionsSetKey" })
                        ?.SingleOrDefault()
                        ?.Value;

                    if (latestCustomerQuestionsSetKey == null)
                        return null;
                    var value = KeyValueStoreService.GetValueComposable(db, latestCustomerQuestionsSetKey, CustomerQuestionsSet.KeyValueStoreKeySpaceName);
                    if (value == null)
                        return null;

                    return CustomerQuestionsSet.FromString(value);
                }
            }
        }

        public List<CustomerRelationModel> FetchCustomerRelations(int customerId)
        {
            using (var db = customerContextFactory.CreateContext())
            {
                return db
                    .CustomerRelationsQueryable.Where(x => x.CustomerId == customerId)
                    .Select(x => new
                    {
                        x.CustomerId,
                        x.StartDate,
                        x.EndDate,
                        x.RelationId,
                        x.RelationType
                    })
                    .ToList()
                    .OrderByDescending(x => x.StartDate)
                    .Select(x => new CustomerRelationModel
                    {
                        CustomerId = x.CustomerId,
                        StartDate = DateOnly.Create(x.StartDate),
                        EndDate = DateOnly.Create(x.EndDate),
                        RelationId = x.RelationId,
                        RelationType = x.RelationType,
                        RelationNavigationUrl = urlService.GetCustomerRelationUrlOrNull(x.RelationType, x.RelationId)?.ToString()
                    })
                    .ToList();
            }
        }
    }

    public interface IKycManagementService
    {
        KycLocalDecisionCurrentModel FetchLocalDecisionCurrentData(int customerId);
        KycLocalDecisionHistoryModel FetchLocalDecisionHistoryData(int customerId, bool isModellingPep, Func<string, string> getDisplayNameFromUserId);
        Dictionary<int, KycCustomerOnboardingStatusModel> FetchKycCustomerOnboardingStatuses(ISet<int> customerIds, (string SourceType, string SourceId)? kycQuestionsSource, bool includeLatestQuestionSets);
        void SetCurrentLocalDecision(int customerId, bool isModellingPep, bool currentValue);
        string AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId);
        CustomerQuestionsSet FetchLatestCustomerQuestionsSet(int customerId);
        FetchPropertiesWithGroupedEditHistoryResult FetchPropertiesWithGroupedEditHistory(int customerId, List<string> propertyNames, Func<string, string> getDisplayNameFromUserId);
        List<CustomerRelationModel> FetchCustomerRelations(int customerId);
    }

    public class CustomerRelationModel
    {
        public int CustomerId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string RelationId { get; set; }
        public string RelationType { get; set; }
        public string RelationNavigationUrl { get; set; }
    }

    public class KycLocalDecisionCurrentModel
    {
        public int CustomerId { get; set; }
        public bool? IsPep { get; set; }
        public bool? IsSanction { get; set; }
        public string AmlRiskClass { get; set; }
    }
    public class FetchPropertiesWithGroupedEditHistoryResult
    {
        public Dictionary<string, string> CurrentValues { get; set; }
        public List<HistoryItem> HistoryItems { get; set; }
        public class HistoryItem
        {
            public int UserId { get; set; }
            public string UserDisplayName { get; set; }
            public DateTimeOffset EditDate { get; set; }
            public Dictionary<string, string> Values { get; set; }
        }
    }
    public class KycCustomerOnboardingStatusModel : KycLocalDecisionCurrentModel
    {
        public DateTime? LatestScreeningDate { get; set; }
        public DateTime? LatestKycQuestionsAnswerDate { get; set; }
        public CustomerQuestionsSet LatestKycQuestionsSet { get; set; }
        public string CustomerShortName { get; set; }
        public string CustomerBirthDate { get; set; }
        public DateTime? LatestPropertyUpdateDate { get; set; }
        public bool HasNameAndAddress { get; set; }
    }

    public class KycLocalDecisionHistoryModel
    {
        public int CustomerId { get; set; }
        public bool IsModellingPep { get; set; }

        public KycLocalDecisionHistoryItem CurrentValue { get; set; }
        public List<KycLocalDecisionHistoryItem> HistoricalValues { get; set; }
    }

    public class KycLocalDecisionHistoryItem
    {
        public int CustomerId { get; set; }
        public bool IsModellingPep { get; set; }
        public DateTime ChangeDate { get; set; }
        public int ChangedByUserId { get; set; }
        public string ChangedByUserDisplayName { get; set; }
        public bool? Value { get; set; }
    }
}