using Newtonsoft.Json;
using nPreCredit;
using nPreCredit.Code;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts.Fi;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Core.PreCredit.Shared.Services.UlLegacy
{
    public class UlLegacyAgreementSignatureService
    {
        private readonly ICoreClock clock;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly EncryptionService encryptionService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICreditClient creditClient;
        private readonly ICustomerClient customerClient;
        private readonly INTechCurrentUserMetadata nTechCurrentUser;

        public UlLegacyAgreementSignatureService(ICoreClock clock, IPreCreditContextFactoryService preCreditContextFactory, EncryptionService encryptionService,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, IClientConfigurationCore clientConfiguration,
            ICreditClient creditClient, ICustomerClient customerClient, INTechCurrentUserMetadata nTechCurrentUser)
        {
            this.clock = clock;
            this.preCreditContextFactory = preCreditContextFactory;
            this.encryptionService = encryptionService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.clientConfiguration = clientConfiguration;
            this.creditClient = creditClient;
            this.customerClient = customerClient;
            this.nTechCurrentUser = nTechCurrentUser;
        }

        public bool TryHandleAnswersToAdditionalQuestions(string applicationNr, string tokenIfAny, UlLegacyKycAnswersModel answers, out string failedMessage, string userLanguage = null)
        {
            List<string> errors = new List<string>();
            var now = this.clock.Now;

            bool isAdditionalLoanOffer;
            int currentUserId;
            string informationMetadata;
            using (var context = preCreditContextFactory.CreateExtended())
            {
                currentUserId = context.CurrentUserId;
                informationMetadata = context.InformationMetadata;
                var tmp = AdditionalLoanSupport.HasAdditionalLoanOffer(applicationNr, context, out failedMessage);
                if (!tmp.HasValue)
                {
                    return false;
                }
                isAdditionalLoanOffer = tmp.Value;
            }

            var repo = new UpdateCreditApplicationRepository(clock, preCreditContextFactory, encryptionService);
            var req = new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
            {
                InformationMetadata = informationMetadata,
                StepName = "AdditionalQuestions",
                UpdatedByUserId = currentUserId,
                Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>()
            };
            void AddItem(string groupName, string name, string value)
            {
                var i = new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                {
                    GroupName = groupName,
                    Name = name,
                    Value = value,
                    IsSensitive = false
                };
                req.Items.Add(i);
            };

            var appModel = partialCreditApplicationModelRepository.Get(
                applicationNr,
                applicationFields: new List<string> { "creditnr" },
                applicantFields: new List<string> { "customerId" });

            var customerIdsByApplicantNr = new Dictionary<int, int>();
            appModel.DoForEachApplicant(applicantNr =>
            {
                customerIdsByApplicantNr[applicantNr] = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required;

                var applicant = applicantNr == 1 ? answers.Applicant1 : answers.Applicant2;
                if (!string.IsNullOrWhiteSpace(applicant?.ConsentRawJson))
                {
                    AddItem($"question{applicantNr}", "consentRawJson", applicant.ConsentRawJson);
                }
            });

            if (clientConfiguration.Country.BaseCountry == "FI")
            {
                if (string.IsNullOrWhiteSpace(answers.Iban))
                {
                    failedMessage = "Missing iban";
                    return false;
                }
                IBANFi i;
                if (!IBANFi.TryParse(answers.Iban, out i))
                {
                    failedMessage = "Invalid iban";
                    return false;
                }
                AddItem("application", "iban", i.NormalizedValue);
            }
            else
                throw new NotImplementedException();

            if (!isAdditionalLoanOffer && appModel.Application.Get("creditnr").StringValue.Optional == null)
            {
                var creditNr = creditClient.NewCreditNumber();
                AddItem("application", "creditnr", creditNr);
            }

            AddItem("application", "isPendingExternalKycQuestions", "true");
            if (userLanguage != null)
            {
                AddItem("application", "userLanguage", userLanguage);
            }

            repo.UpdateApplication(applicationNr, req, also: c =>
            {
                if (!string.IsNullOrWhiteSpace(tokenIfAny))
                {
                    var token = c.CreditApplicationOneTimeTokensQueryable.Single(x => x.Token == tokenIfAny);
                    token.TokenExtraData = JsonConvert.SerializeObject(new { hasAnswered = true, answeredDate = this.clock.Now });
                }
                else
                {
                    //Create a token to make tracking the state easier to reason about
                    var token = CreateAdditionalQuestionsToken(applicationNr, nTechCurrentUser, now, isAdditionalLoanOffer, true);
                    c.AddCreditApplicationOneTimeTokens(token);
                }

                c.AddCreditApplicationComments(new CreditApplicationComment
                {
                    ApplicationNr = applicationNr,
                    ChangedById = c.CurrentUserId,
                    CommentById = c.CurrentUserId,
                    CommentDate = now,
                    ChangedDate = now,
                    CommentText = "Additional questions answered" + (isAdditionalLoanOffer ? " for additional loan" : ""),
                    EventType = "AdditionalQuestionsAnswered"
                });
            });

            UpdateCustomerCheckStatus(applicationNr, partialCreditApplicationModelRepository, preCreditContextFactory, customerClient);

            return true;
        }

        public static bool UpdateCustomerCheckStatus(string applicationNr, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, IPreCreditContextFactoryService preCreditContextFactory,
            ICustomerClient customerClient)
        {
            using (var context = preCreditContextFactory.CreateExtended())
            {
                var d = context.CreditApplicationHeadersQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => new
                {
                    IsMortgageLoanApplicaton = x.MortgageLoanExtension != null,
                    Data = new CustomerCheckStatusHandler.ApplicationData
                    {
                        ApplicationNr = x.ApplicationNr,
                        IsActive = x.IsActive,
                        IsFinalDecisionMade = x.IsFinalDecisionMade,
                        IsPartiallyApproved = x.IsPartiallyApproved,
                        CreditCheckStatus = x.CreditCheckStatus,
                        CustomerCheckStatus = x.CustomerCheckStatus,
                        AgreementStatus = x.AgreementStatus
                    },
                    Header = x
                }).Single();

                if (d.IsMortgageLoanApplicaton)
                    return false;

                var handler = new CustomerCheckStatusHandler(customerClient, partialCreditApplicationModelRepository);
                var changeStatusTo = handler.GetUpdateCustomerCheckStatusUpdateOrNull(d.Data);
                var shouldChange = changeStatusTo != null;

                if (shouldChange)
                {
                    var h = d.Header;
                    h.ChangedDate = context.CoreClock.Now;
                    h.ChangedById = context.CurrentUserId;
                    h.CustomerCheckStatus = changeStatusTo;
                }

                context.SaveChanges();

                return shouldChange;
            }
        }

        public static CreditApplicationOneTimeToken CreateAdditionalQuestionsToken(string applicationNr, INTechCurrentUserMetadata user, DateTimeOffset now, bool isAdditionalLoanOffer, bool hasAnswered)
        {
            return new CreditApplicationOneTimeToken
            {
                CreationDate = now,
                Token = CreditApplicationOneTimeToken.GenerateUniqueToken(),
                TokenType = "AdditionalQuestions",
                TokenExtraData = JsonConvert.SerializeObject(new { hasAnswered = hasAnswered, isAdditionalLoanOffer = isAdditionalLoanOffer }),
                ApplicationNr = applicationNr,
                ChangedById = user.UserId,
                InformationMetaData = user.InformationMetadata,
                ChangedDate = now,
                ValidUntilDate = now.AddDays(4)
            };
        }

        private static List<CustomerClientCustomerPropertyModel> GetCustomerProperties(int customerId, List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem> applicantItems, string propertyGroup, List<string> propertyNames)
        {
            var candidateItems = applicantItems
              .Where(x => propertyNames.Contains(x.Name))
              .ToList();

            if (candidateItems.Any(x => !x.Name.StartsWith("customer_")))
            {
                throw new Exception("Names must start with customer_");
            }

            return candidateItems
              .Select(x => new CustomerClientCustomerPropertyModel
              {
                  CustomerId = customerId,
                  Name = x.Name.Substring("customer_".Length),
                  Group = propertyGroup,
                  Value = x.Value,
                  IsSensitive = x.IsSensitive
              }).ToList();
        }
    }

    public class UlLegacyKycAnswersModel
    {
        public Applicant Applicant1 { get; set; }
        public Applicant Applicant2 { get; set; }


        public IEnumerable<Applicant> GetApplicants()
        {
            yield return Applicant1;
            if (Applicant2 != null)
                yield return Applicant2;
        }

        public class Applicant
        {
            public string ConsentRawJson { get; set; }
        }
        public string Iban { get; set; }
    }
}
