using Moq;
using nCredit;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.BusinessEvents.NewCredit;
using nCustomer.Code;
using nCustomer.DbModel;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.Email;
using static NTech.Core.Host.IntegrationTests.Credits;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Utilities
{
    internal static class CreditsUlLegacy
    {
        public static CreditHeader CreateCredit(UlLegacyTestRunner.TestSupport support, int creditNumber, int? mainApplicantCustomerId = null, string? applicationNr = null, decimal creditAmount = 6000m,
            decimal referenceInterestRatePercent = 0.01m, decimal marginInterestRatePercent = 8.25m, decimal annuityAmount = 187.77m, decimal capitalizedInitialFeeAmount = 0m)
        {
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                return context.UsingTransaction(() =>
                {
                    var c = CreateCreditComposable(support, context, creditNumber, mainApplicantCustomerId: mainApplicantCustomerId, applicationNr: applicationNr, creditAmount: creditAmount,
                        referenceInterestRatePercent: referenceInterestRatePercent, marginInterestRatePercent: marginInterestRatePercent, annuityAmount: annuityAmount, capitalizedInitialFeeAmount: capitalizedInitialFeeAmount);
                    context.SaveChanges();
                    return c;
                });
            }
        }

        public static CreditHeader CreateCreditComposable(UlLegacyTestRunner.TestSupport support, CreditContextExtended creditContext, int creditNumber, int? mainApplicantCustomerId = null, string? applicationNr = null,
            decimal creditAmount = 6000m, decimal referenceInterestRatePercent = 0.01m, decimal marginInterestRatePercent = 8.25m, decimal annuityAmount = 187.77m, decimal capitalizedInitialFeeAmount = 0m)
        {
            var contextKey = $"TestCredit{creditNumber}_Header";
            if (support.Context.ContainsKey(contextKey))
            {
                throw new Exception($"Credit {creditNumber} has already been created");
            }

            if (!mainApplicantCustomerId.HasValue)
            {
                int GetPersonSeed(bool isCoApplicant)
                {
                    //Main applicants is odd number so customer 1, 3, 5 and so on
                    //Co applicants use even numbers 2, 4, 6 and so on
                    //This so we can generate as many credits as needed without known the number ahead of time
                    if (isCoApplicant)
                        return creditNumber * 2;
                    else
                        return (creditNumber * 2) - 1;
                }

                mainApplicantCustomerId = TestPersons.EnsureTestPerson(support, GetPersonSeed(false));
            }

            var creditNr = $"C987{creditNumber}"; //The 987 prefix is just to make the length a bit more realistic. It has no significance.

            var payToIban = "FI4840541519568274";
            var creditRequest = new NewCreditRequest
            {
                CapitalizedInitialFeeAmount = capitalizedInitialFeeAmount,
                Iban = payToIban,
                CreditAmount = creditAmount,
                AnnuityAmount = annuityAmount,
                CreditNr = creditNr,
                ProviderName = "self",
                NrOfApplicants = 1,
                NotificationFee = 8m,
                MarginInterestRatePercent = marginInterestRatePercent,
                ProviderApplicationId = null,
                ApplicationNr = applicationNr,
                CampaignCode = "H00000",
                Applicants = new List<NewCreditRequestExceptCapital.Applicant>
                {
                    new NewCreditRequestExceptCapital.Applicant
                    {
                        ApplicantNr = 1,
                        CustomerId = mainApplicantCustomerId.Value,
                        AgreementPdfArchiveKey = "51a1cedb-b957-44d1-84e7-7c595af294e5.pdf"
                    }
                }
            };

            var legalInterestCeilingService = new LegalInterestCeilingService(support.CreditEnvSettings);
            var creditCustomerListServiceComposable = new Mock<ICreditCustomerListServiceComposable>(MockBehavior.Strict);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support);

            var ocrPaymentReferenceGenerator = new OcrPaymentReferenceGenerator("FI", () => new CreditContextExtended(support.CurrentUser, support.Clock));

            var newCreditManager = new NewCreditBusinessEventManager(
                support.CurrentUser,
                legalInterestCeilingService,
                creditCustomerListServiceComposable.Object, support.EncryptionService,
                support.Clock, support.ClientConfiguration, customerClient.Object,
                ocrPaymentReferenceGenerator, support.CreditEnvSettings,
                support.CreatePaymentAccountService(support.CreditEnvSettings),
                support.GetRequiredService<CustomCostTypeService>());

            var createdCredit = newCreditManager.CreateNewCredit(creditContext, creditRequest, new Lazy<decimal>(() => referenceInterestRatePercent));

            //Merge customer relation
            var customerConnectionString = support.CreateCustomerContextFactory().CreateContext().GetConnection().ConnectionString;
            var m = new DatabaseMergeCommand<CustomerRelation>("whatever", customerConnectionString);
            if (!m.TryMergeTable("CustomerRelation",
                createdCredit.CreditCustomers.Select(x => new CustomerRelation
                {
                    CustomerId = x.CustomerId,
                    RelationId = createdCredit.CreditNr,
                    RelationType = $"Credit_{support.CreditType}",
                    StartDate = createdCredit.StartDate.Date
                }).ToList()
                , out var mergeFailedMessage))
                Assert.Fail(mergeFailedMessage);

            support.Context[contextKey] = createdCredit;

            return createdCredit;
        }

        public static CreditHeader GetCreateCredit(UlLegacyTestRunner.TestSupport support, int creditNumber)
        {
            var contextKey = $"TestCredit{creditNumber}_Header";

            if (!support.Context.ContainsKey(contextKey))
            {
                throw new Exception($"Credit {creditNumber} has not been created");
            }

            return (CreditHeader)support.Context[contextKey];
        }

        public static PendingCreditSettlementSuggestionData CreateAndSendSettlementSuggestion(UlLegacyTestRunner.TestSupport support, string creditNr, string? assertThatSuccessWarningMessageContains = null)
        {
            var settlementSuggestionMgr = support.GetRequiredService<CreditSettlementSuggestionBusinessEventManager>();

            var isOk = settlementSuggestionMgr.TryCreateAndSendSettlementSuggestion(
                creditNr, support.Clock.Today.AddDays(1), null, null, out var warningMessage, out var suggestion, "test@example.org");

            if (isOk && assertThatSuccessWarningMessageContains != null)
            {
                var containsText = (warningMessage ?? "").Contains(assertThatSuccessWarningMessageContains);
                Assert.True(containsText,
                    $"Expected warning text to contain '{assertThatSuccessWarningMessageContains}' but instead got '{warningMessage ?? "<no warning text>"}'");
            }

            return suggestion;
        }

        public static CreditTermsChangeHeader? StartChangeTerms(UlLegacyTestRunner.TestSupport support, string creditNr, int newRepaymentTimeInMonths, decimal newMarginInterestRatePercent,
            bool isEmailProviderDown = false,
            string? assertSuccessWarningMessage = null,
            string? assertSystemCommentContains = null)
        {
            var emailServiceFactory = new Mock<INTechEmailServiceFactory>(MockBehavior.Strict);
            emailServiceFactory.Setup(x => x.HasEmailProvider).Returns(true);
            emailServiceFactory.Setup(x => x.CreateEmailService()).Returns(support.EmailServiceMock.Object);
            var signatureClient = new Mock<ICommonSignatureClient>(MockBehavior.Strict);
            signatureClient.Setup(x => x.CreateSession(It.IsAny<SingleDocumentSignatureRequestUnvalidated>())).Returns(new CommonElectronicIdSignatureSession
            {
                Id = "SessionId123",
                SignatureProviderName = "TheSignatureProvider",
                SigningCustomersBySignerNr = new Dictionary<int, CommonElectronicIdSignatureSession.SigningCustomer>
                {
                    [1] = new CommonElectronicIdSignatureSession.SigningCustomer
                    {
                        SignatureUrl = "https://s.example.org"
                    }
                }
            });
            var serviceRegistry = new Mock<INTechServiceRegistry>(MockBehavior.Strict);
            serviceRegistry
                .Setup(x => x.InternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                .Returns(new Uri("https://i.example.org"));
            serviceRegistry
                .Setup(x => x.ExternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                .Returns(new Uri("https://e.example.org"));
            var affiliate = new AffiliateModel
            {
                IsSelf = false,
                DisplayToEnduserName = "A provider"
            };
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var documentRenderer = new Mock<IDocumentRenderer>(MockBehavior.Strict);
            IDictionary<string, object>? termChangePrintContext = null;
            documentRenderer.Setup(x => x.Dispose());
            documentRenderer
                .Setup(x => x.RenderDocumentToArchive("credit-agreement-changeterms", It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                .Returns<string, IDictionary<string, object>, string>((_, context, __) =>
                {
                    termChangePrintContext = context;
                    return "abc123.pdf";
                });
            var mgr = new CreditTermsChangeBusinessEventManager(
                support.CurrentUser,
                new LegalInterestCeilingService(support.CreditEnvSettings), support.Clock, support.ClientConfiguration,
                support.CreateCreditContextFactory(), support.CreditEnvSettings, emailServiceFactory.Object, customerClient.Object,
                support.LoggingService, serviceRegistry.Object, _ => affiliate);

            var (isSuccess, warningMessage, newTerms) = mgr.StartCreditTermsChange(creditNr, newRepaymentTimeInMonths, newMarginInterestRatePercent, () => documentRenderer.Object);

            if (termChangePrintContext != null)
            {
                Assert.That(termChangePrintContext.Opt("repaymentTimeInMonths"), Is.EqualTo(newRepaymentTimeInMonths.ToString()));
                Assert.That(termChangePrintContext.Opt("marginInterestRate"), Is.EqualTo($"{newMarginInterestRatePercent.ToString("N3", support.FormattingCulture)} %"));
            }

            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                if (!isSuccess)
                    TestContext.WriteLine(warningMessage);

                if (isSuccess && assertSuccessWarningMessage != null)
                    Assert.That(warningMessage, Is.EqualTo(assertSuccessWarningMessage));

                if (isSuccess && assertSystemCommentContains != null)
                {
                    var commentText = context.CreditCommentsQueryable.SingleOrDefault(x => x.CreatedByEventId == newTerms.CreatedByEventId)?.CommentText ?? "";
                    var containsText = commentText.Contains(assertSystemCommentContains);
                    if (!containsText)
                        TestContext.WriteLine(commentText);
                    Assert.True(containsText);
                }

                Assert.True(isSuccess);

                return newTerms;
            }
        }

        public static bool TryAcceptCreditTermsChange(UlLegacyTestRunner.TestSupport support, int id)
        {
            var emailServiceFactory = new Mock<INTechEmailServiceFactory>(MockBehavior.Strict);
            emailServiceFactory.Setup(x => x.HasEmailProvider).Returns(true);
            emailServiceFactory.Setup(x => x.CreateEmailService()).Returns(support.EmailServiceMock.Object);
            var signatureClient = new Mock<ICommonSignatureClient>(MockBehavior.Strict);
            signatureClient.Setup(x => x.CreateSession(It.IsAny<SingleDocumentSignatureRequestUnvalidated>())).Returns(new CommonElectronicIdSignatureSession
            {
                Id = "SessionId123",
                SignatureProviderName = "TheSignatureProvider",
                SigningCustomersBySignerNr = new Dictionary<int, CommonElectronicIdSignatureSession.SigningCustomer>
                {
                    [1] = new CommonElectronicIdSignatureSession.SigningCustomer
                    {
                        SignatureUrl = "https://s.example.org"
                    }
                }
            });
            var serviceRegistry = new Mock<INTechServiceRegistry>(MockBehavior.Strict);
            serviceRegistry
                .Setup(x => x.InternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                .Returns(new Uri("https://i.example.org"));
            serviceRegistry
                .Setup(x => x.ExternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                .Returns(new Uri("https://e.example.org"));
            var affiliate = new AffiliateModel
            {
                IsSelf = false,
                DisplayToEnduserName = "A provider"
            };
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var documentRenderer = new Mock<IDocumentRenderer>(MockBehavior.Strict);
            IDictionary<string, object>? termChangePrintContext = null;
            documentRenderer.Setup(x => x.Dispose());
            documentRenderer
                .Setup(x => x.RenderDocumentToArchive("credit-agreement-changeterms", It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                .Returns<string, IDictionary<string, object>, string>((_, context, __) =>
                {
                    termChangePrintContext = context;
                    return "abc123.pdf";
                });

            var mgr = new CreditTermsChangeBusinessEventManager(
               support.CurrentUser,
               new LegalInterestCeilingService(support.CreditEnvSettings), support.Clock, support.ClientConfiguration,
               support.CreateCreditContextFactory(), support.CreditEnvSettings, emailServiceFactory.Object, customerClient.Object,
               support.LoggingService, serviceRegistry.Object, _ => affiliate);

            var isSuccess = mgr.TryAcceptCreditTermsChange(id, out string failedMessage);

            return isSuccess;
        }
    }
}
