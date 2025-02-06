using nCustomer.Code.Services.CustomerMessages;
using nCustomer.Code.Services.Settings;
using nCustomer.DbModel;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Customer.Shared.Settings;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class NonSearchCustomerMessageService : INonSearchCustomerMessageService
    {
        private readonly Func<ICustomerContext> createContext;
        private readonly Func<ICustomerContext, CustomerRepositorySimple> createCustomerRepository;
        private readonly Lazy<INTechEmailService> nTechEmailService;
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly ICoreClock clock;
        private readonly ICrossServiceEventService crossServiceEventService;
        private readonly Lazy<CustomerMessageCommentDispatcher> customerMessageCommentDispatcher;
        private readonly Lazy<CustomerMessageClientNotificationDispatcher> customerMessageClientNotificationDispatcher;

        public NonSearchCustomerMessageService(
            Func<ICustomerContext> createContext,
            EncryptionService encryptionService,
            INTechEmailServiceFactory emailServiceFactory,
            IKeyValueStoreService keyValueStoreService,
            ReadonlySettingsService settingsService,
            IClientConfigurationCore clientConfiguration,
            ILoggingService loggingService,
            INTechServiceRegistry serviceRegistry,
            bool isStandardUnsecuredLoansEnabled,
            CrossModuleClientFactory crossModuleClientFactory,
            ICoreClock clock,
            ICrossServiceEventService crossServiceEventService)
        {
            this.createContext = createContext;
            createCustomerRepository = c => new CustomerRepositorySimple(c, encryptionService);
            nTechEmailService = new Lazy<INTechEmailService>(emailServiceFactory.CreateEmailService);
            this.keyValueStoreService = keyValueStoreService;
            this.clientConfiguration = clientConfiguration;
            this.serviceRegistry = serviceRegistry;
            this.clock = clock;
            this.crossServiceEventService = crossServiceEventService;
            customerMessageClientNotificationDispatcher = new Lazy<CustomerMessageClientNotificationDispatcher>(() =>
                new CustomerMessageClientNotificationDispatcher(settingsService, emailServiceFactory, loggingService, serviceRegistry));
            customerMessageCommentDispatcher = new Lazy<CustomerMessageCommentDispatcher>(() =>
                new CustomerMessageCommentDispatcher(crossModuleClientFactory));
            ChannelMappedRelationTypes = new Lazy<ISet<string>>(() =>
            {
                var channelTypes = new string[]
                            {
                "Credit_UnsecuredLoan", "Credit_MortgageLoan", "Credit_CompanyLoan", "SavingsAccount_StandardAccount"
                            }.ToHashSetShared();

                if (isStandardUnsecuredLoansEnabled)
                    channelTypes.Add("Application_UnsecuredLoan");

                return channelTypes;
            });
        }

        public CustomerMessageModel ToCustomerMessage(CustomerMessage c, bool includeMessageTexts, IEnumerable<CustomerMessageAttachedDocument> documents)
        {
            return new CustomerMessageModel
            {
                Id = c.Id,
                Text = !includeMessageTexts ? null : c.Text,
                TextFormat = !includeMessageTexts ? null : c.TextFormat,
                CustomerId = c.CustomerId,
                ChannelType = c.ChannelType,
                ChannelId = c.ChannelId,
                IsFromCustomer = c.IsFromCustomer,
                CreationDate = c.CreatedDate,
                CreatedByUserId = c.CreatedByUserId,
                HandledDate = c.HandledDate,
                HandledByUserId = c.HandledByUserId,
                AttachedDocuments = documents.Select(ToCustomerMessageAttachedDocument).ToList()
            };
        }

        public CustomerMessageAttachedDocumentModel ToCustomerMessageAttachedDocument(CustomerMessageAttachedDocument c)
        {
            return new CustomerMessageAttachedDocumentModel
            {
                Id = c.Id,
                ArchiveKey = c.ArchiveKey,
                ContentTypeMimetype = c.ContentTypeMimetype,
                FileName = c.FileName
            };
        }

        public void FlagMessagesBeforeInChannelAsHandled(int messageId, int customerId, string channelType, string channelId, bool? isFromCustomer, int userid)
        {
            var now = clock.Now.DateTime;

            using (var context = createContext())
            {
                var q = context.CustomerMessagesQueryable.Where(x => x.Id < messageId
                    && x.CustomerId == customerId
                    && x.ChannelType == channelType
                    && x.ChannelId == channelId
                    && !x.HandledByUserId.HasValue);
                if (isFromCustomer.HasValue)
                {
                    q = q.Where(x => x.IsFromCustomer == isFromCustomer.Value);
                }
                foreach (var m in q.ToList())
                {
                    m.HandledByUserId = userid;
                    m.HandledDate = now;
                }
                context.SaveChanges();
            }
        }

        public CustomerMessageModel SaveCustomerMessage(int customerId, string channelType, string channelId, string text, string textFormat, bool isFromCustomer, int userid)
        {
            var now = clock.Now.DateTime;
            CustomerMessageModel m;
            using (var context = createContext())
            {
                var c = new CustomerMessage
                {
                    CustomerId = customerId,
                    ChannelType = channelType,
                    ChannelId = channelId,
                    Text = text,
                    TextFormat = textFormat,
                    IsFromCustomer = isFromCustomer,
                    CreatedDate = now,
                    CreatedByUserId = customerId,
                    HandledByUserId = isFromCustomer ? null : (int?)userid,
                    HandledDate = isFromCustomer ? null : new DateTime?(now),
                };
                context.AddCustomerMessages(c);
                context.SaveChanges();
                m = ToCustomerMessage(c, true, new CustomerMessageAttachedDocument[] { });
            }

            var wasNotificationSent = customerMessageClientNotificationDispatcher.Value.Notify(m, out bool isFailedToSend);
            customerMessageCommentDispatcher.Value.CreateComment(m, wasNotificationSent, isFailedToSend);


            return m;
        }

        public CustomerMessageAttachedDocumentModel SaveCustomerMessageAttachedDocument(int messageId, string fileName, string contentTypeMimetype, string archiveKey)
        {
            CustomerMessageAttachedDocumentModel m;
            using (var context = createContext())
            {
                var c = new CustomerMessageAttachedDocument
                {
                    ArchiveKey = archiveKey,
                    ContentTypeMimetype = contentTypeMimetype,
                    CustomerMessageId = messageId,
                    FileName = fileName,
                };
                context.AddCustomerMessageAttachedDocuments(c);
                context.SaveChanges();
                m = ToCustomerMessageAttachedDocument(c);
            }

            return m;
        }

        public string HandleMessages(List<int> messageIds, int userid)
        {
            var now = clock.Now.DateTime;

            using (var context = createContext())
            {
                var messages = context.CustomerMessagesQueryable.Where(f => messageIds.Contains(f.Id)).ToList();
                messages.ForEach(a => { a.HandledByUserId = userid; a.HandledDate = new DateTime?(now); });
                context.SaveChanges();

                return "true";
            }
        }

        public Dictionary<int, string> GetCustomerMessageTexts(List<int> messageIds, out Dictionary<int, string> messageTextFormat, out Dictionary<int, bool> isFromCustomerByMessageId, out Dictionary<int, string> attachedDocumentsByMessageId)
        {
            using (var context = createContext())
            {
                var result = context.CustomerMessagesQueryable
                    .Where(x => messageIds.Contains(x.Id))
                    .Select(y => new { y.Id, y.Text, y.TextFormat, y.IsFromCustomer, AttachmentArchiveKey = y.CustomerMessageAttachedDocuments.OrderByDescending(z => z.Id).Select(z => z.ArchiveKey).FirstOrDefault() })
                    .ToDictionary(y => y.Id, y => new { y.Text, y.TextFormat, y.IsFromCustomer, AttachmentArchiveKey = y.AttachmentArchiveKey });

                messageTextFormat = result.ToDictionary(x => x.Key, x => x.Value.TextFormat);
                isFromCustomerByMessageId = result.ToDictionary(x => x.Key, x => x.Value.IsFromCustomer);
                attachedDocumentsByMessageId = result.Where(x => x.Value.AttachmentArchiveKey != null).ToDictionary(x => x.Key, x => x.Value.AttachmentArchiveKey);

                return result.ToDictionary(x => x.Key, x => x.Value.Text);
            }
        }

        public FetchCustomerMessageModels GetCustomerMessages(int? customerId, string channelType, string channelId, bool includeMessageTexts, int? skipCount, int? takeCount, bool? isHandled, bool? isFromCustomer, List<string> onlyTheseChannelTypes)
        {
            using (var context = createContext())
            {
                var q = context
                    .CustomerMessagesQueryable
                    .Select(x => new
                    {
                        M = x,
                        Attachments = x.CustomerMessageAttachedDocuments
                    });

                if (customerId.HasValue)
                {
                    q = q.Where(x => x.M.CustomerId == customerId.Value);
                }

                if (!string.IsNullOrWhiteSpace(channelType))
                {
                    q = q.Where(x => x.M.ChannelType == channelType);
                }

                if (!string.IsNullOrWhiteSpace(channelId))
                {
                    q = q.Where(x => x.M.ChannelId == channelId);
                }

                if (isHandled.HasValue)
                {
                    q = q.Where(x => x.M.HandledByUserId.HasValue == isHandled.Value);
                }

                if (isFromCustomer.HasValue)
                {
                    q = q.Where(x => x.M.IsFromCustomer == isFromCustomer.Value);
                }

                if (onlyTheseChannelTypes != null && onlyTheseChannelTypes.Count > 0)
                {
                    q = q.Where(x => onlyTheseChannelTypes.Contains(x.M.ChannelType));
                }

                var totalCount = q.Count();

                q = q.OrderByDescending(x => x.M.Id);

                if (skipCount.HasValue)
                {
                    q = q.Skip((int)skipCount);
                }
                if (takeCount.HasValue)
                {
                    q = q.Take((int)takeCount);
                }
                return new FetchCustomerMessageModels
                {
                    AreMessageTextsIncluded = includeMessageTexts,
                    CustomerMessageModels = q
                    .ToList()
                    .Select(x => ToCustomerMessage(x.M, includeMessageTexts, x.Attachments))
                    .ToList(),
                    TotalMessageCount = totalCount
                };
            }
        }

        private readonly Lazy<ISet<string>> ChannelMappedRelationTypes;
        public const string GeneralChannelType = "General";
        public const string GeneralChannelId = "General";

        protected IQueryable<MessageChannelModel> GetCustomerRelationChannelsQueryable(ICustomerContext context)
        {
            var channelMappedRelationTypes = ChannelMappedRelationTypes.Value;
            return context
                    .CustomerRelationsQueryable
                    .Where(x => ChannelMappedRelationTypes.Value.Contains(x.RelationType))
                    .Select(x => new MessageChannelModel
                    {
                        ChannelId = x.RelationId,
                        ChannelType = x.RelationType,
                        IsRelation = true,
                        CustomerId = x.CustomerId,
                        RelationStartDate = x.StartDate,
                        RelationEndDate = x.EndDate
                    });
        }

        public List<MessageChannelModel> SortChannels(List<MessageChannelModel> channels)
        {
            if (channels == null)
                return null;
            return channels.OrderBy(x => x.CustomerId).ThenBy(x => x.ChannelType).ThenBy(x => x.ChannelId).ToList();
        }

        public List<MessageChannelModel> GetCustomerChannels(int customerId, bool includeGeneralChannel, List<string> onlyTheseChannelTypes)
        {
            var channels = new List<MessageChannelModel>();

            using (var context = createContext())
            {
                var channelsQuery = GetCustomerRelationChannelsQueryable(context).Where(x => x.CustomerId == customerId);
                var hasChannelTypeFilter = onlyTheseChannelTypes != null && onlyTheseChannelTypes.Count > 0;
                if (hasChannelTypeFilter)
                {
                    channelsQuery = channelsQuery.Where(x => onlyTheseChannelTypes.Contains(x.ChannelType));
                }
                channels.AddRange(channelsQuery.ToList());

                if (includeGeneralChannel && (!hasChannelTypeFilter || onlyTheseChannelTypes.Contains(GeneralChannelType)))
                    channels.Add(new MessageChannelModel
                    {
                        ChannelId = GeneralChannelId,
                        ChannelType = GeneralChannelType,
                        CustomerId = customerId,
                        IsRelation = false,
                        RelationStartDate = null,
                        RelationEndDate = null
                    });
            }

            return channels;
        }

        public void SendNewMessageNotification(CustomerMessageModel message, INTechCurrentUserMetadata currentUser)
        {
            using (var context = createContext())
            {
                var repo = this.createCustomerRepository(context);
                var email = repo.BulkFetchD(repo.AsSet(message.CustomerId), propertyNames: repo.AsSet("email"))
                    .Opt(message.CustomerId)
                    .Opt("email");
                if (string.IsNullOrWhiteSpace(email))
                    throw new NTechCoreWebserviceException("Customer is missing email")
                    {
                        IsUserFacing = true,
                        ErrorCode = "missingEmail"
                    };

                var isStandard = clientConfiguration.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");
                var customerPagesLinkTargetName = "SecureMessages";
                if (isStandard && message.ChannelType == "Application_UnsecuredLoan")
                {
                    customerPagesLinkTargetName = "ApplicationsOverview";
                }
                var emailTemplates = nTechEmailService.Value.LoadClientResourceTemplate("customer-securemessage-notification", false);
                var settingsService = new Lazy<SettingsService>(() => new SettingsService(
                    SettingsModelSource.GetSharedSettingsModelSource(clientConfiguration),
                    keyValueStoreService, currentUser, clientConfiguration, crossServiceEventService.BroadcastCrossServiceEvent));
                if (!emailTemplates.HasValue)
                {
                    //No client specific template so we fall back to settings
                    var templateSetting = settingsService.Value.LoadSettingsValues("customerSecureMessageEmailNotification");
                    emailTemplates = (
                        SubjectTemplateText: templateSetting["messageSubjectTemplate"],
                        BodyTemplateText: templateSetting["messageBodyTemplate"],
                        IsEnabled: true);
                }

                var emailDataContext = new Dictionary<string, object>
                {
                    { "customerPagesLink",  serviceRegistry.ExternalServiceUrl("nCustomerPages", "login/eid", Tuple.Create("targetName", customerPagesLinkTargetName)).ToString() },
                };

                if (isStandard)
                {
                    var documentClientData = settingsService.Value.LoadSettingsValues("documentClientData"); ;
                    emailDataContext["clientDisplayName"] = documentClientData?.Opt("name") ?? "lender";
                }

                nTechEmailService.Value.SendRawEmail(
                    new List<string> { email },
                    emailTemplates.Value.SubjectTemplateText,
                    emailTemplates.Value.BodyTemplateText,
                    emailDataContext,
                    $"nCustomer.SendNewMessageNotification: m = {message.Id}, c = {message.CustomerId}");
            }
        }
    }
}