using nCustomer.Code.Services;
using nCustomer.DbModel;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Module.Shared.Settings.KycUpdateFrequency;
using System;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Customer.Shared.Services.KycQuestionsPeriodicUpdateService;

namespace NTech.Core.Customer.Shared.Services
{
    public class KycAnswersUpdateService
    {
        private readonly CustomerContextFactory customerContextFactory;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ICoreClock clock;
        private readonly KycQuestionsTemplateService templateService;
        private readonly EncryptionService encryptionService;
        private readonly Lazy<(int DefaultMonthCount, Dictionary<string, int> CustomMonthCounts, int NrOfDaysBeforeUpdate, bool AdditionalNotificationIsEnabled, int AdditionalNotificationFrequency)> kycUpdateFrequency;

        public static HashSet<string> RelationTypesWithQuestions = new HashSet<string>
        {
            "Credit_UnsecuredLoan", "Credit_CompanyLoan", "Credit_MortgageLoan", "SavingsAccount_StandardAccount"
        };

        public KycAnswersUpdateService(CustomerContextFactory customerContextFactory, INTechCurrentUserMetadata currentUser,
            ICoreClock clock, KycQuestionsTemplateService templateService, CachedSettingsService settingsService,
            EncryptionService encryptionService)
        {
            this.customerContextFactory = customerContextFactory;
            this.currentUser = currentUser;
            this.clock = clock;
            this.templateService = templateService;
            this.encryptionService = encryptionService;
            this.kycUpdateFrequency = new Lazy<(int DefaultMonthCount, Dictionary<string, int> CustomMonthCounts, int NrOfDaysBeforeUpdate, bool AdditionalNotificationIsEnabled, int AdditionalNotificationFrequency)>(() =>
            {
                var kycUpdateFrequencySettings = KycUpdateFrequencyDataModel.ParseSettingValues(
                    settingsService.LoadSettings("kycUpdateFrequency"));
                var settings = settingsService.LoadSettings("kycUpdateRequiredSecureMessage");
                var nrOfDaysBeforeUpdate = int.Parse(settings.Opt("nrOfDaysBeforeUpdate"));
                var additionalNotificationIsEnabled = settings["additionalNotificationIsEnabled"] == "true";
                var additionalNotificationFrequency = int.Parse(settings.Opt("additionalNotificationFrequency"));

                return (
                DefaultMonthCount: kycUpdateFrequencySettings.DefaultMonthCount,
                CustomMonthCounts: kycUpdateFrequencySettings.CustomMonthCounts,
                NrOfDaysBeforeUpdate: nrOfDaysBeforeUpdate, 
                AdditionalNotificationIsEnabled: additionalNotificationIsEnabled, 
                AdditionalNotificationFrequency: additionalNotificationFrequency);
            });
        }

        public string AddCustomerQuestionsSetFromCustomerPages(int customerId, string relationType, string relationId, List<CustomerQuestionsSetItem> answers)
        {
            DateTime answerDate;
            using (var context = customerContextFactory.CreateContext())
            {
                if (!context.CustomerRelationsQueryable.Any(x => x.CustomerId == customerId && x.RelationId == relationId && x.RelationType == relationType))
                {
                    throw new NTechCoreWebserviceException("Relation does not exist") { ErrorCode = "noSuchRelationOnCustomer" };
                }
                answerDate = context.CoreClock.Now.DateTime;
            }

            var questionSet = new CustomerQuestionsSet
            {
                AnswerDate = answerDate,
                CustomerId = customerId,
                Items = answers,
                Source = $"{relationType} {relationId}"
            };

            return AddCustomerQuestionsSet(questionSet, relationType, relationId);
        }

        public string AddCustomerQuestionsSet(CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId)
        {
            using (var context = customerContextFactory.CreateContext())
            {
                var key = AddCustomerQuestionsSetComposable(context, customerQuestionsSet, sourceType, sourceId);
                context.SaveChanges();
                return key;
            }
        }

        public Dictionary<int, bool> CopyCustomerQuestionsSetIfNotExists(HashSet<int> customerIds, string fromSourceType, string fromSourceId, string toSourceType, string toSourceId, DateTime? ignoreOlderThanDate)
        {
            var wasCopiedByCustomerId = new Dictionary<int, bool>();
            using (var context = customerContextFactory.CreateContext())
            {
                foreach (var customerId in customerIds)
                {
                    wasCopiedByCustomerId[customerId] = CopyCustomerQuestionsSetIfNotExistsComposable(customerId, fromSourceType, fromSourceId, toSourceType, toSourceId, ignoreOlderThanDate, context);
                }
                context.SaveChanges();
                return wasCopiedByCustomerId;
            }
        }

        private bool CopyCustomerQuestionsSetIfNotExistsComposable(int customerId, string fromSourceType, string fromSourceId, string toSourceType, string toSourceId, DateTime? ignoreOlderThanDate, ICustomerContextExtended context)
        {
            var fromQuestionSet = context
                .StoredCustomerQuestionSetsQueryable
                .Where(x => x.CustomerId == customerId && x.SourceType == fromSourceType && x.SourceId == fromSourceId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (fromQuestionSet == null)
                return false;

            var alreadyExistingQuestions = context.StoredCustomerQuestionSetsQueryable.Where(x => x.CustomerId == customerId && x.SourceType == toSourceType && x.SourceId == toSourceId);
            if (ignoreOlderThanDate.HasValue)
            {
                alreadyExistingQuestions = alreadyExistingQuestions.Where(x => x.AnswerDate > ignoreOlderThanDate.Value);
            }
            if (alreadyExistingQuestions.Any())
                return false;

            context.AddStoredCustomerQuestionSets(new StoredCustomerQuestionSet
            {
                AnswerDate = fromQuestionSet.AnswerDate,
                CustomerId = fromQuestionSet.CustomerId,
                ChangedById = currentUser.UserId,
                InformationMetaData = currentUser.InformationMetadata,
                ChangedDate = clock.Now,
                KeyValueStorageKeySpace = fromQuestionSet.KeyValueStorageKeySpace,
                KeyValueStorageKey = fromQuestionSet.KeyValueStorageKey,
                SourceType = toSourceType,
                SourceId = toSourceId
            });

            return true;
        }

        public string AddCustomerQuestionsSetComposable(ICustomerContextExtended context, CustomerQuestionsSet customerQuestionsSet, string sourceType, string sourceId)
        {
            if (customerQuestionsSet == null)
                throw new Exception("Missing customerQuestionsSet");
            if (!customerQuestionsSet.CustomerId.HasValue)
                throw new Exception("Missing customerQuestionsSet.CustomerId");
            if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sourceId))
                throw new Exception("Missing sourceType or sourceId"); ;

            var key = Guid.NewGuid().ToString();
            var keySpaceName = CustomerQuestionsSet.KeyValueStoreKeySpaceName;
            KeyValueStoreService.SetValueComposable(context, key, keySpaceName, customerQuestionsSet.Serialize(), currentUser, clock);
            context.AddStoredCustomerQuestionSets(new StoredCustomerQuestionSet
            {
                AnswerDate = customerQuestionsSet.AnswerDate ?? clock.Today,
                CustomerId = customerQuestionsSet.CustomerId.Value,
                ChangedById = currentUser.UserId,
                InformationMetaData = currentUser.InformationMetadata,
                ChangedDate = clock.Now,
                KeyValueStorageKeySpace = keySpaceName,
                KeyValueStorageKey = key,
                SourceType = sourceType,
                SourceId = sourceId
            });

            return key;
        }

        public CustomerPagesCustomerStatus GetCustomerPagesStatusForCustomer(int customerId)
        {
            var questionsTemplates = templateService.GetInitialData();

            var customerData = GetCustomerData(new HashSet<int> { customerId }).Opt(customerId);
            if (customerData == null)
            {
                return new CustomerPagesCustomerStatus
                {
                    ActiveRelations = new List<CustomerKycQuestionStatusRelation>(),
                    QuestionTemplates = questionsTemplates,
                    LatestAnswers = new List<CustomerPagesCustomerStatus.LatestKycAnswersForRelationModel>(),
                    HistoricalAnswers = customerData.HistoricalQuestions
                };
            }

            var latestAnswers = GetLatestKycAnswers(new HashSet<int> { customerId }, this.customerContextFactory);
            var updateStatus = CreateUpdateFromCustomerData(customerData);
            var activeRelationTypes = updateStatus.ActiveRelations.Select(x => x.RelationType).Distinct();

            return new CustomerPagesCustomerStatus
            {
                ActiveRelations = updateStatus.ActiveRelations,
                IsUpdateRequired = updateStatus.IsUpdateRequired,
                IsReminderRequired = updateStatus.IsReminderRequired,
                QuestionTemplates = questionsTemplates,
                LatestAnswers = latestAnswers.Opt(customerId) ?? new List<CustomerPagesCustomerStatus.LatestKycAnswersForRelationModel>(),
                HistoricalAnswers = customerData.HistoricalQuestions
            };
        }

        public CustomerKycQuestionStatus CreateUpdateFromCustomerData(CustomerData customerData)
        {
            var today = clock.Today;
            var isUpdateRequiredForCustomer = false;
            var isReminderRequiredForCustomer = false;
            var relations = customerData.ActiveRelations.Select(activeRelation =>
            {
                var mostRecentQuestionsDate = customerData
                    .HistoricalQuestions
                    .Where(x => x.RelationType == activeRelation.RelationType && x.RelationId == activeRelation.RelationId)
                    .Select(x => x.AnswerDate.Date)
                    .Concat(Enumerables.Singleton(activeRelation.StartDate.Date))
                    .Max();

                var (UpdateRequiredMonthCount, RemindDaysBeforeCount) = GetQuestionMonthCountInterval(customerData);
                var updateRequiredDate = mostRecentQuestionsDate.AddMonths(UpdateRequiredMonthCount);
                var reminderRequiredDate = updateRequiredDate.AddDays(-RemindDaysBeforeCount);
                var nrOfDaysSinceAnswerForUpdate = Dates.GetAbsoluteNrOfDaysBetweenDates(mostRecentQuestionsDate, today);
                var nrOfMonthsSinceAnswerForUpdate = Dates.GetAbsoluteNrOfMonthsBetweenDates(mostRecentQuestionsDate, today);

                var isUpdateRequiredForRelation = updateRequiredDate <= today;
                var isReminderRequiredForRelation = reminderRequiredDate <= today;

                isUpdateRequiredForCustomer = isUpdateRequiredForCustomer || isUpdateRequiredForRelation;
                isReminderRequiredForCustomer = isReminderRequiredForCustomer || isReminderRequiredForRelation;

                return new CustomerKycQuestionStatusRelation
                {
                    RelationId = activeRelation.RelationId,
                    RelationType = activeRelation.RelationType,
                    IsUpdateRequired = isUpdateRequiredForRelation,
                    IsReminderRequired = isReminderRequiredForRelation,
                    UpdateFrequencyMonthCount = UpdateRequiredMonthCount,
                    NrOfDaysSinceAnswer = nrOfDaysSinceAnswerForUpdate,
                    NrOfMonthsSinceAnswer = nrOfMonthsSinceAnswerForUpdate,
                    UpdateRequiredDate = updateRequiredDate,
                    ReminderRequiredDate = reminderRequiredDate
                };
            }).ToList();

            return new CustomerKycQuestionStatus
            {
                CustomerId = customerData.CustomerId,
                IsUpdateRequired = isUpdateRequiredForCustomer,
                IsReminderRequired = isReminderRequiredForCustomer,
                ActiveRelations = relations
            };
        }

        private (int UpdateRequiredMonthCount, int RemindDaysBeforeCount) GetQuestionMonthCountInterval(CustomerData customerData)
        {
            var (DefaultMonthCount, CustomMonthCounts, NrOfDaysBeforeUpdate, _, _) = kycUpdateFrequency.Value;
            var amlRisk = customerData.AmlRiskClass;
            if (string.IsNullOrWhiteSpace(amlRisk) || !CustomMonthCounts.ContainsKey(amlRisk))
            {
                return (UpdateRequiredMonthCount: DefaultMonthCount, RemindDaysBeforeCount: NrOfDaysBeforeUpdate);
            }
            return (UpdateRequiredMonthCount: CustomMonthCounts[amlRisk], RemindDaysBeforeCount: NrOfDaysBeforeUpdate);
        }

        public Dictionary<int, CustomerData> GetCustomerData(HashSet<int> customerIds)
        {
            var today = clock.Today;
            using (var context = customerContextFactory.CreateContext())
            {
                var activeRelationsByCustomerId = context
                    .CustomerRelationsQueryable
                    .Where(x => customerIds.Contains(x.CustomerId) && RelationTypesWithQuestions.Contains(x.RelationType) && x.EndDate == null)
                    .Select(x => new
                    {
                        x.CustomerId,
                        x.StartDate,
                        x.RelationType,
                        x.RelationId
                    })
                    .ToList()
                    .GroupBy(x => x.CustomerId)
                    .ToDictionary(x => x.Key, x => x.Select(y => new CustomerData.ActiveRelationModel
                    {
                        RelationType = y.RelationType,
                        RelationId = y.RelationId,
                        StartDate = y.StartDate ?? today
                    }).ToList());

                var usedSourceTypes = RelationTypesWithQuestions;
                var historicalQuestionsPerCustomerId = context
                    .StoredCustomerQuestionSetsQueryable
                    .Where(x => customerIds.Contains(x.CustomerId) && usedSourceTypes.Contains(x.SourceType))
                    .OrderByDescending(x => x.AnswerDate).ThenByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.CustomerId,
                        x.AnswerDate,
                        x.SourceId,
                        x.SourceType,
                        AnswerData = context.KeyValueItemsQueryable
                              .Where(keyValueItem => keyValueItem.KeySpace == x.KeyValueStorageKeySpace && keyValueItem.Key == x.KeyValueStorageKey)
                              .Select(keyValueItem => keyValueItem.Value)
                              .FirstOrDefault()
                    })
                    .ToList()
                    .GroupBy(x => x.CustomerId)
                    .ToDictionary(x => x.Key, x => x.Select(y => new CustomerData.StoredCustomerQuestionSetModel
                    {
                        AnswerDate = y.AnswerDate,
                        Answers = y.AnswerData.Any() ? JsonConvert.DeserializeObject<CustomerQuestionsSet>(y.AnswerData)?.Items : null,
                        RelationId = y.SourceId,
                        RelationType = y.SourceType
                    }).ToList());

                var repo = new nCustomer.CustomerRepositorySimple(context, encryptionService);
                var customerPropertiesByCustomerId = repo.BulkFetchD(customerIds, propertyNames: new HashSet<string> { "amlRiskClass" });

                var result = new Dictionary<int, CustomerData>(customerIds.Count);
                List<T> DefaultEmptyList<T>(List<T> items) => items ?? new List<T>();
                foreach (var customerId in customerIds)
                {
                    result[customerId] = new CustomerData
                    {
                        CustomerId = customerId,
                        AmlRiskClass = customerPropertiesByCustomerId.Opt(customerId)?.Opt("amlRiskClass"),
                        ActiveRelations = DefaultEmptyList(activeRelationsByCustomerId.Opt(customerId)),
                        HistoricalQuestions = DefaultEmptyList(historicalQuestionsPerCustomerId.Opt(customerId))
                    };
                }

                return result;
            }
        }

        public (int nrOfDaysBeforeUpdate, bool additionalNotificationIsEnabled, int additionalNotificationFrequency) GetAdditionalNotificationsFrequency()
        {
            return (kycUpdateFrequency.Value.NrOfDaysBeforeUpdate, kycUpdateFrequency.Value.AdditionalNotificationIsEnabled, kycUpdateFrequency.Value.AdditionalNotificationFrequency);
        }

        public class CustomerData
        {
            public int CustomerId { get; set; }
            public string AmlRiskClass { get; set; }
            public List<StoredCustomerQuestionSetModel> HistoricalQuestions { get; set; }
            public List<ActiveRelationModel> ActiveRelations { get; set; }
            public class ActiveRelationModel
            {
                public DateTime StartDate { get; set; }
                public string RelationType { get; set; }
                public string RelationId { get; set; }
            }
            public class StoredCustomerQuestionSetModel
            {
                public string RelationType { get; set; }
                public string RelationId { get; set; }
                public DateTime AnswerDate { get; set; }
                public List<CustomerQuestionsSetItem> Answers { get; set; }
            }
        }

    }
}
