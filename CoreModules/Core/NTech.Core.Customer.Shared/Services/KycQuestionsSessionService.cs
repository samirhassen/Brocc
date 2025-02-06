using nCustomer;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Customer.Shared.Services.Utilities;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Customer.Shared.Services
{
    public class KycQuestionsSessionService : KycQuestionsSessionArchiveOnlyService
    {
        private readonly INTechCurrentUserMetadata user;
        private readonly KycAnswersUpdateService kycAnswersUpdate;
        private readonly EncryptionService encryptionService;
        private readonly CrossModuleClientFactory crossModuleClientFactory;
        private readonly Lazy<CivicRegNumberParser> civicRegNumberParser;

        public KycQuestionsSessionService(CustomerContextFactory customerContextFactory, ICoreClock clock, INTechCurrentUserMetadata user,
            KycAnswersUpdateService kycAnswersUpdate, IClientConfigurationCore clientConfiguration, EncryptionService encryptionService, 
            CrossModuleClientFactory crossModuleClientFactory) : base(customerContextFactory, clock)
        {
            this.user = user;
            this.kycAnswersUpdate = kycAnswersUpdate;
            this.encryptionService = encryptionService;
            this.crossModuleClientFactory = crossModuleClientFactory;
            civicRegNumberParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(clientConfiguration.Country.BaseCountry));
        }

        public KycQuestionsSession CreateSession(CreateKycQuestionSessionRequest request)
        {
            var session = new KycQuestionsSession
            {
                SessionId = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(),
                IsActive = true,
                RedirectUrl = request.RedirectUrl,
                IsCompleted = false,
                QuestionsRelationType = request.QuestionsRelationType,
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                SourceDescription = request.SourceDescription,
                Language = request.Language,
                SlidingExpirationHours = request.SlidingExpirationHours,
                CustomerIdByCustomerKey = request.CustomerIds.ToDictionary(x => OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(), x => x),
                CompletionCallbackModuleName = request.CompletionCallbackModuleName,
                CustomData = request.CustomData,
                AllowBackToRedirectUrl = request.AllowBackToRedirectUrl
            };

            using (var context = customerContextFactory.CreateContext())
            {
                StoreSession(context, session);
                context.SaveChanges();
            }

            return session;
        }

        public void AddAlternateKey(string sessionId, string alternateKey) => sessionStore.SetAlternateSessionKey(sessionId, alternateKey, user);

        public CustomerPagesHandleKycQuestionAnswersResponse HandleSessionAnswers(CustomerPagesHandleKycQuestionAnswersRequest request)
        {
            try
            {
                using (var context = customerContextFactory.CreateContext())
                {
                    var session = sessionStore.GetSessionComposable(context, request.SessionId);

                    if (session?.IsActive != true)
                    {
                        return new CustomerPagesHandleKycQuestionAnswersResponse
                        {
                            WasCompleted = false
                        };
                    }

                    var answerByCustomerKey = request.CustomerAnswers.ToDictionary(x => x.CustomerKey, x => x.Answers);

                    foreach (var customer in session.CustomerIdByCustomerKey)
                    {
                        var customerKey = customer.Key;
                        var customerId = customer.Value;
                        var questionSet = new CustomerQuestionsSet
                        {
                            AnswerDate = context.CoreClock.Now.DateTime,
                            CustomerId = customerId,
                            Source = session.SourceDescription,
                            Items = answerByCustomerKey[customerKey]
                        };
                        kycAnswersUpdate.AddCustomerQuestionsSetComposable(context, questionSet, session.SourceType, session.SourceId);
                    }

                    session.IsCompleted = true;
                    session.IsActive = false;

                    StoreSession(context, session);

                    context.SaveChanges();

                    if (session.CompletionCallbackModuleName?.ToLowerInvariant() == "nprecredit")
                    {
                        crossModuleClientFactory.PreCreditClient.ReportKycQuestionSessionCompleted(session.SessionId);
                    }

                    return new CustomerPagesHandleKycQuestionAnswersResponse
                    {
                        WasCompleted = true
                    };
                }
            } 
            catch(Exception ex)
            {
                throw new Exception($"HandleSessionAnswers({request?.SessionId}) failed", ex);
            }
        }

        public KycQuestionsSession GetSession(string sessionId) =>        
            sessionStore.GetSession(sessionId);

        public CustomerPagesKycQuestionSessionResponse LoadCustomerPagesSession(string sessionId, KycQuestionsTemplateService templateService)
        {
            var session = sessionStore.GetSession(sessionId);
            if (session == null)
            {
                return new CustomerPagesKycQuestionSessionResponse
                {
                    SessionId = sessionId,
                    IsExisting = false
                };
            }

            if (!session.IsActive)
            {
                return new CustomerPagesKycQuestionSessionResponse
                {
                    SessionId = session.SessionId,
                    Language = session.Language,
                    IsExisting = true,
                    IsActive = session.IsActive,
                    IsCompleted = session.IsCompleted,
                    RedirectUrl = session.RedirectUrl,
                    AllowBackToRedirectUrl = session.AllowBackToRedirectUrl
                };
            }

            var customerIds = session.CustomerIdByCustomerKey.Values.ToHashSetShared();
            Dictionary<int, Dictionary<string, string>> customerData;

            using (var context = customerContextFactory.CreateContext())
            {
                var customerRepository = new CustomerRepositorySimple(context, encryptionService);
                customerData = customerRepository.BulkFetchD(customerIds,
                    propertyNames: new HashSet<string> { "isCompany", "companyName", "firstName", "lastName", "birthDate", "civicRegNr" });
            }

            string GetBirthDateFromCivicRegNr(string civicRegNr)
            {
                if (string.IsNullOrWhiteSpace(civicRegNr))
                    return null;
                return civicRegNumberParser.Value.TryParse(civicRegNr, out var parsedNr)
                    ? parsedNr?.BirthDate?.ToString("yyyy-MM-dd")
                    : null;
            }

            var questionsTemplate = templateService.GetInitialData().ActiveProducts.FirstOrDefault(x => x.RelationType == session.QuestionsRelationType)?.CurrentQuestionsTemplate;

            return new CustomerPagesKycQuestionSessionResponse
            {
                SessionId = session.SessionId,
                Language = session.Language,
                IsExisting = true,
                IsActive = session.IsActive,
                IsCompleted = session.IsCompleted,
                RedirectUrl = session.RedirectUrl,
                AllowBackToRedirectUrl = session.AllowBackToRedirectUrl,
                QuestionsTemplate = questionsTemplate,
                Customers = session.CustomerIdByCustomerKey.Select(x =>
                {
                    var customerId = x.Value;
                    var customerKey = x.Key;
                    var d = customerData.Opt(customerId);
                    var isCompany = d?.Opt("isCompany") == "true";
                    return new CustomerPagesKycQuestionSessionResponseCustomer
                    {
                        FullName = isCompany ? d?.Opt("companyName") : $"{d?.Opt("firstName")} {d?.Opt("lastName")}".Trim(),
                        BirthDate = d?.Opt("birthDate") ?? GetBirthDateFromCivicRegNr(d?.Opt("civicRegNr")),
                        CustomerKey = customerKey
                    };
                }).ToList()
            };
        }

        private void StoreSession(ICustomerContextExtended context, KycQuestionsSession session)
        {
            sessionStore.StoreSessionComposable(context, session, TimeSpan.FromHours(session.SlidingExpirationHours), user);
        }
    }

    public class KycQuestionsSessionArchiveOnlyService
    {
        protected readonly SessionStore<KycQuestionsSession> sessionStore;
        protected readonly CustomerContextFactory customerContextFactory;

        public KycQuestionsSessionArchiveOnlyService(CustomerContextFactory customerContextFactory, ICoreClock clock)
        {
            sessionStore = new SessionStore<KycQuestionsSession>(
                "KycQuestionsSessionServiceV1", "KycQuestionsSessionServiceArchiveDateV1", "KycQuestionsSessionServiceAlternateKeyV1",
                clock,
                x => x.SessionId,
                () => customerContextFactory.CreateContext());
            this.customerContextFactory = customerContextFactory;
        }

        public void ArchiveOldSessions()
        {
            sessionStore.ArchiveOldSessions();
        }
    }

    public class CustomerPagesKycQuestionSessionResponse
    {
        public string SessionId { get; set; }
        public bool IsExisting { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public string RedirectUrl { get; set; }
        public KycQuestionsTemplate QuestionsTemplate { get; set; }
        public List<CustomerPagesKycQuestionSessionResponseCustomer> Customers { get; set; }
        public string Language { get; set; }
        public bool AllowBackToRedirectUrl { get; set; }
    }

    public class CustomerPagesKycQuestionSessionResponseCustomer
    {
        public string FullName { get; set; }
        public string BirthDate { get; set; }
        public string CustomerKey { get; set; }
    }

    public class CustomerPagesHandleKycQuestionAnswersRequest
    {
        [Required]
        public string SessionId { get; set; }

        public List<CustomerPagesHandleKycQuestionAnswersCustomer> CustomerAnswers { get; set; }
    }

    public class CustomerPagesHandleKycQuestionAnswersCustomer
    {
        [Required]
        public string CustomerKey { get; set; }

        [Required]
        public List<CustomerQuestionsSetItem> Answers { get; set; }
    }

    public class CustomerPagesHandleKycQuestionAnswersResponse
    {
        public bool WasCompleted { get; set; }
    }
}
