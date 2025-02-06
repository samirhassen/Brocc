using nCredit.Excel;
using nCustomer.Code.Services;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Customer.Shared.Services.KycAnswersUpdateService.CustomerData;

namespace NTech.Core.Customer.Shared.Services
{
    public class KycQuestionsPeriodicUpdateService
    {
        private readonly CustomerContextFactory customerContextFactory;
        private readonly ICoreClock clock;
        private readonly CustomerMessageSendingService customerMessageService;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly ISharedEnvSettings envSettings;
        private readonly CachedSettingsService settingsService;
        private readonly EncryptionService encryptionService;
        private readonly KycAnswersUpdateService kycAnswersUpdateService;

        public KycQuestionsPeriodicUpdateService(CustomerContextFactory customerContextFactory, ICoreClock clock, CustomerMessageSendingService customerMessageService,
            INTechEmailServiceFactory emailServiceFactory, ISharedEnvSettings envSettings, CachedSettingsService settingsService,
            EncryptionService encryptionService, KycAnswersUpdateService kycAnswersUpdateService)
        {
            this.customerContextFactory = customerContextFactory;
            this.clock = clock;
            this.customerMessageService = customerMessageService;
            this.emailServiceFactory = emailServiceFactory;
            this.envSettings = envSettings;
            this.settingsService = settingsService;
            this.encryptionService = encryptionService;
            this.kycAnswersUpdateService = kycAnswersUpdateService;
        }

        public void SendReminderMessages(HashSet<int> onlyConsiderCustomerIds = null)
        {
            var today = clock.Today;
            var customersIdsThatNeedToUpdateQuestions = GetCustomerIdsThatNeedQuestionReminders()
                .ToArray();

            if (onlyConsiderCustomerIds != null && onlyConsiderCustomerIds.Count > 0)
            {
                customersIdsThatNeedToUpdateQuestions = customersIdsThatNeedToUpdateQuestions.Intersect(onlyConsiderCustomerIds).ToArray();
            }

            int countSent = 0;

            var reminderMessageTemplate = settingsService.LoadSettings("kycUpdateRequiredSecureMessage")?.Opt("templateText");
            foreach (var customerIdGroup in customersIdsThatNeedToUpdateQuestions.ToArray().SplitIntoGroupsOfN(200))
            {
                using (var context = customerContextFactory.CreateContext())
                {
                    var customerIdsStr = customerIdGroup.Select(x => x.ToString()).ToList();
                    var lastReminderDateItemByCustomerId = context
                        .KeyValueItemsQueryable
                        .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.LatestKycReminderDate.ToString() && customerIdsStr.Contains(x.Key))
                        .ToDictionary(x => int.Parse(x.Key), x => x);

                    var lastReminderDateByCustomerId = context
                        .KeyValueItemsQueryable
                        .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.LatestKycReminderDate.ToString() && customerIdsStr.Contains(x.Key))
                        .ToDictionary(x => int.Parse(x.Key), x => Dates.ParseDateOnlyExactOrNull(x.Value, "yyyy-MM-dd").ToDate());

                    var customerKycStatusByCustomerId = kycAnswersUpdateService
                        .GetCustomerData(customerIdGroup.ToHashSetShared())
                        .ToDictionary(x => x.Key, x => kycAnswersUpdateService.CreateUpdateFromCustomerData(x.Value));

                    foreach (var customerId in customerIdGroup)
                    {
                        var lastReminderDateItem = lastReminderDateItemByCustomerId.Opt(customerId);
                        var lastReminderDate = lastReminderDateItem == null
                            ? new DateTime?()
                            : Dates.ParseDateOnlyExactOrNull(lastReminderDateItem.Value, "yyyy-MM-dd").ToDate();

                        var (nrOfDaysBeforeUpdate, additionalNotificationIsEnabled, additionalNotificationFrequency) = kycAnswersUpdateService.GetAdditionalNotificationsFrequency();

                        if (additionalNotificationIsEnabled && additionalNotificationFrequency != 0)
                        {
                            if (lastReminderDate.HasValue && today.AddDays(-additionalNotificationFrequency) < lastReminderDate.Value)
                            {
                                continue;
                            }
                        }

                        else if (lastReminderDate.HasValue && today.AddDays(-nrOfDaysBeforeUpdate) < lastReminderDate.Value)
                        {
                            continue;
                        }

                        var customerStatus = customerKycStatusByCustomerId[customerId];
                        var customerRelations = customerStatus
                            .ActiveRelations
                            .Where(x => x.IsReminderRequired)
                            .ToList();

                        var last = customerRelations.Last(); //Only notify by email on last message
                        foreach (var relation in customerRelations)
                        {
                            customerMessageService.SendMessage(new CustomerMessageSendingService.Request
                            {
                                FlagPreviousMessagesAsHandled = false,
                                ChannelType = relation.RelationType,
                                ChannelId = relation.RelationId,
                                CustomerId = customerId,
                                IsFromCustomer = false,
                                //In test we only send one notification email so we can test what the text looks like
                                NotifyCustomerByEmail = relation == last && emailServiceFactory.HasEmailProvider && (envSettings.IsProduction || countSent < 1),
                                Text = reminderMessageTemplate,
                                TextFormat = "markdown"
                            });
                        }

                        if (lastReminderDateItem != null)
                        {
                            context.FillInfrastructureFields(lastReminderDateItem);
                            lastReminderDateItem.Value = today.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            context.AddKeyValueItem(context.FillInfrastructureFields(new nCustomer.DbModel.KeyValueItem
                            {
                                KeySpace = KeyValueStoreKeySpaceCode.LatestKycReminderDate.ToString(),
                                Key = customerId.ToString(),
                                Value = today.ToString("yyyy-MM-dd")
                            }));
                        }

                        context.SaveChanges();

                        countSent++;
                    }
                }
            }
        }

        private HashSet<int> GetCustomerIdsWithActiveRelations()
        {
            using (var context = customerContextFactory.CreateContext())
            {
                return context
                    .CustomerRelationsQueryable
                    .Where(x => x.EndDate == null && KycAnswersUpdateService.RelationTypesWithQuestions.Contains(x.RelationType))
                    .Select(x => x.CustomerId)
                    .ToHashSetShared();
            }
        }

        private HashSet<int> GetCustomerIdsThatNeedQuestionReminders()
        {
            var allCustomerIdsToCheck = GetCustomerIdsWithActiveRelations();
            var customerIdsThatNeedToUpdate = new HashSet<int>();
            foreach (var customerIdsGroup in allCustomerIdsToCheck.ToArray().SplitIntoGroupsOfN(200))
            {
                var customerIds = customerIdsGroup.ToHashSetShared();
                var customerDataByCustomerId = kycAnswersUpdateService.GetCustomerData(customerIds);
                foreach (var customerId in customerIds)
                {
                    var customerStatus = kycAnswersUpdateService.CreateUpdateFromCustomerData(customerDataByCustomerId[customerId]);
                    if (customerStatus.IsReminderRequired)
                    {
                        customerIdsThatNeedToUpdate.Add(customerId);
                    }
                }
            }

            return customerIdsThatNeedToUpdate;
        }

        private List<KycReportRow> GetCustomerKycStatusReportRows()
        {
            var allCustomerIdsWithActiveRelations = GetCustomerIdsWithActiveRelations();
            var rows = new List<KycReportRow>();
            var customerPropertiesToFetch = new HashSet<string>
            {
                "email", "phone", "firstName", "lastName", "amlRiskClass"
            };
            foreach (var customerIdsGroup in allCustomerIdsWithActiveRelations.ToArray().SplitIntoGroupsOfN(200))
            {
                using (var context = customerContextFactory.CreateContext())
                {
                    var repo = new nCustomer.CustomerRepositorySimple(context, encryptionService);
                    var customerPropertiesByCustomerId = repo.BulkFetchD(customerIdsGroup.ToHashSetShared(), customerPropertiesToFetch);
                    var customerIds = customerIdsGroup.ToHashSetShared();
                    var customerDataByCustomerId = kycAnswersUpdateService.GetCustomerData(customerIds);

                    var customerIdsStr = customerIdsGroup.Select(x => x.ToString());
                    var lastReminderDateByCustomerId = context
                        .KeyValueItemsQueryable
                        .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.LatestKycReminderDate.ToString() && customerIdsStr.Contains(x.Key))
                        .ToDictionary(x => int.Parse(x.Key), x => Dates.ParseDateOnlyExactOrNull(x.Value, "yyyy-MM-dd").ToDate());

                    foreach (var customerId in customerIds)
                    {
                        var customerData = customerDataByCustomerId[customerId];
                        var customerProperties = customerPropertiesByCustomerId.Opt(customerId);
                        var customerStatus = kycAnswersUpdateService.CreateUpdateFromCustomerData(customerDataByCustomerId[customerId]);
                        foreach (var activeRelation in customerStatus.ActiveRelations)
                        {
                            rows.Add(new KycReportRow
                            {
                                CustomerId = customerId,
                                IsUpdateRequired = activeRelation.IsUpdateRequired,
                                IsReminderRequired = activeRelation.IsReminderRequired,
                                NrOfMonthsSinceAnswer = activeRelation.NrOfMonthsSinceAnswer,
                                ActiveRelation = activeRelation.RelationId,
                                Email = customerProperties?.Opt("email"),
                                Phone = customerProperties?.Opt("phone"),
                                FirstName = customerProperties?.Opt("firstName"),
                                LastName = customerProperties?.Opt("lastName"),
                                UpdateFrequencyMonthCount = activeRelation.UpdateFrequencyMonthCount,
                                RiskClass = customerProperties?.Opt("amlRiskClass"),
                                LastReminderDate = lastReminderDateByCustomerId.OptS(customerId),
                                UpdateRequiredDate = activeRelation.UpdateRequiredDate,
                                ReminderRequiredDate = activeRelation.ReminderRequiredDate
                            });
                        }
                    }
                }
            }

            return rows
                .OrderBy(x => x.IsUpdateRequired ? 0 : 1)
                .ThenBy(x => x.IsReminderRequired ? 0 : 1)
                .ThenByDescending(x => x.NrOfMonthsSinceAnswer)
                .ThenBy(x => x.CustomerId)
                .ToList();
        }

        private class KycReportRow
        {
            public int CustomerId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string RiskClass { get; set; }
            public string ActiveRelation { get; set; }
            public bool IsUpdateRequired { get; set; }
            public int NrOfMonthsSinceAnswer { get; set; }
            public int UpdateFrequencyMonthCount { get; set; }
            public DateTime? LastReminderDate { get; set; }
            public bool IsReminderRequired { get; set; }
            public DateTime UpdateRequiredDate { get; set; }
            public DateTime ReminderRequiredDate { get; set; }
        }

        public DocumentClientExcelRequest CreateCustomerKycStatusReport()
        {
            var rows = GetCustomerKycStatusReportRows();

            var request = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = "Kyc status"
                    }
                }
            };

            request.Sheets[0].SetColumnsAndData(rows,
                rows.Col(x => x.ActiveRelation, ExcelType.Text, "Relation"),
                rows.Col(x => x.IsUpdateRequired ? 1 : 0, ExcelType.Number, "Needs update", includeSum: true, nrOfDecimals: 0),
                rows.Col(x => x.IsReminderRequired ? 1 : 0, ExcelType.Number, "Needs reminder", includeSum: true, nrOfDecimals: 0),
                rows.Col(x => x.NrOfMonthsSinceAnswer, ExcelType.Number, "# months since update", nrOfDecimals: 0),
                rows.Col(x => x.LastReminderDate, ExcelType.Date, "Last reminder date"),
                rows.Col(x => $"{x.FirstName} {x.LastName}", ExcelType.Text, "Name"),
                rows.Col(x => x.Email, ExcelType.Text, "Email"),
                rows.Col(x => x.Phone, ExcelType.Text, "Phone"),
                rows.Col(x => x.UpdateFrequencyMonthCount, ExcelType.Number, "Update frequency (months)", nrOfDecimals: 0),
                rows.Col(x => x.RiskClass, ExcelType.Text, "Risk class"),
                rows.Col(x => x.ReminderRequiredDate, ExcelType.Date, "Reminder required date"),
                rows.Col(x => x.UpdateRequiredDate, ExcelType.Date, "Update required date"));

            return request;
        }

        public static Dictionary<int, List<CustomerPagesCustomerStatus.LatestKycAnswersForRelationModel>> GetLatestKycAnswers(HashSet<int> customerIds, CustomerContextFactory customerContextFactory)
        {
            var result = new Dictionary<int, List<CustomerPagesCustomerStatus.LatestKycAnswersForRelationModel>>();
            using (var context = customerContextFactory.CreateContext())
            {
                return context
                    .StoredCustomerQuestionSetsQueryable
                    .Where(x => customerIds.Contains(x.CustomerId) && KycAnswersUpdateService.RelationTypesWithQuestions.Contains(x.SourceType))
                    .GroupBy(x => new { x.CustomerId, x.SourceType, x.SourceId })
                    .Select(x => new
                    {
                        x.Key,
                        Latest = x.Select(questionSet => new
                        {
                            questionSet.AnswerDate,
                            questionSet.Id,
                            AnswerData = context.KeyValueItemsQueryable
                                .Where(keyValueItem => keyValueItem.KeySpace == questionSet.KeyValueStorageKeySpace && keyValueItem.Key == questionSet.KeyValueStorageKey)
                                .Select(keyValueItem => keyValueItem.Value)
                                .FirstOrDefault()
                        }).OrderByDescending(y => y.AnswerDate).ThenByDescending(y => y.Id).FirstOrDefault()
                    })
                    .ToList()
                    .GroupBy(x => x.Key.CustomerId)
                    .ToDictionary(x => x.Key, x => x.Select(y => new CustomerPagesCustomerStatus.LatestKycAnswersForRelationModel
                    {
                        RelationId = y.Key.SourceId,
                        RelationType = y.Key.SourceType,
                        AnswerDate = y.Latest?.AnswerDate,
                        Answers = (y.Latest?.AnswerData == null
                            ? null
                            : JsonConvert.DeserializeObject<CustomerQuestionsSet>(y.Latest.AnswerData))?.Items
                    }).ToList());
            }
        }

        public class CustomerKycQuestionStatus
        {
            public int CustomerId { get; set; }
            public List<CustomerKycQuestionStatusRelation> ActiveRelations { get; set; }
            public bool IsUpdateRequired { get; set; }
            public bool IsReminderRequired { get; set; }
        }

        public class CustomerKycQuestionStatusRelation
        {
            public string RelationType { get; set; }
            public string RelationId { get; set; }
            public bool IsUpdateRequired { get; set; }
            public bool IsReminderRequired { get; set; }
            public int NrOfDaysSinceAnswer { get; set; }
            public int NrOfMonthsSinceAnswer { get; set; }
            public int UpdateFrequencyMonthCount { get; set; }
            public DateTime UpdateRequiredDate { get; set; }
            public DateTime ReminderRequiredDate { get; set; }
        }

        public class CustomerPagesCustomerStatus
        {
            public List<CustomerKycQuestionStatusRelation> ActiveRelations { get; set; }
            public KycQuestionsTemplateInitialDataResponse QuestionTemplates { get; set; }
            public List<LatestKycAnswersForRelationModel> LatestAnswers { get; set; }
            public List<StoredCustomerQuestionSetModel> HistoricalAnswers { get; set; }
            public bool IsUpdateRequired { get; set; }
            public bool IsReminderRequired { get; set; }

            public class LatestKycAnswersForRelationModel
            {
                public string RelationType { get; set; }
                public string RelationId { get; set; }
                public DateTime? AnswerDate { get; set; }
                public List<CustomerQuestionsSetItem> Answers { get; set; }
            }

        }
    }
}
