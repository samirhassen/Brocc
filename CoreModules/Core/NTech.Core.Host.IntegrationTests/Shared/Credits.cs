using Microsoft.EntityFrameworkCore;
using Moq;
using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Banking.LoanModel;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System.Text;

namespace NTech.Core.Host.IntegrationTests
{
    public static class Credits
    {
        public static PendingCreditSettlementSuggestionData CreateSettlementOffer(CreditSupportShared support, string creditNr, DateTime settlementDate, decimal? swedishRseEstimatedAmount, decimal? swedishRseInterestRatePercent)
        {
            var service = support.GetRequiredService<CreditSettlementSuggestionBusinessEventManager>();
            var isOk = service.TryCreateAndSendSettlementSuggestion(creditNr, settlementDate, swedishRseEstimatedAmount, swedishRseInterestRatePercent, out var warningMessage, out var offer, null);
            Assert.IsTrue(isOk, warningMessage);
            return offer;
        }

        public static void OverrideOutgoingPaymentAccount(IBankAccountNumber bankAccountNumber, SupportShared support)
        {
            var settingsService = support.CreateSettingsService();
            settingsService.SaveSettingsValues("outgoingPaymentSourceBankAccount", new Dictionary<string, string>
            {
                ["isEnabled"] = "true",
                ["twoLetterIsoCountryCode"] = bankAccountNumber.TwoLetterCountryIsoCode,
                ["bankAccountNr"] = bankAccountNumber.FormatFor(null),
                ["bankAccountNrTypeCode"] = bankAccountNumber.AccountType.ToString(),
            }, (IsSystemUser: true, GroupMemberships: new HashSet<string>()));
        }

        public static void ClearOutgoingPaymentAccountOverride(SupportShared support)
        {
            var settingsService = support.CreateSettingsService();
            settingsService.ClearUserValues("outgoingPaymentSourceBankAccount");
        }

        public static void OverrideIncomingPaymentAccount(IBankAccountNumber bankAccountNumber, SupportShared support)
        {
            var settingsService = support.CreateSettingsService();
            settingsService.SaveSettingsValues("incomingPaymentBankAccount", new Dictionary<string, string>
            {
                ["isEnabled"] = "true",
                ["twoLetterIsoCountryCode"] = bankAccountNumber.TwoLetterCountryIsoCode,
                ["bankAccountNr"] = bankAccountNumber.FormatFor(null),
                ["bankAccountNrTypeCode"] = bankAccountNumber.AccountType.ToString(),
            }, (IsSystemUser: true, GroupMemberships: new HashSet<string>()));
        }

        public static void ClearIncomingPaymentAccountOverride(SupportShared support)
        {
            var settingsService = support.CreateSettingsService();
            settingsService.ClearUserValues("incomingPaymentBankAccount");
        }

        public static decimal InterestAmountForNDays(decimal loanAmount, decimal intrestRatePercent, int nrOfDays) => Math.Round(loanAmount * intrestRatePercent / 365.25m / 100m * (decimal)nrOfDays, 2);

        public static OutgoingPaymentHeader LoadOutgoingPaymentForTesting(CreditContextExtended creditContext, int? id = null, int? businessEventId = null)
        {
            var q = creditContext
                    .OutgoingPaymentHeaders
                    .Include(x => x.Transactions)
                    .Include(x => x.Items);
            return id.HasValue
                ? q.Single(x => x.Id == id.Value)
                : businessEventId.HasValue
                ? q.Single(x => x.CreatedByBusinessEventId == businessEventId.Value)
                : throw new Exception("Must supply one of id and businessEventId");
        }


        public static CreditHeader LoadCreditForTesting(string creditNr, CreditContextExtended creditContext) =>
            creditContext
                .CreditHeaders
                .Include(x => x.Transactions)
                .Single(x => x.CreditNr == creditNr);

        public static decimal GetBalanceForTesting(this CreditHeader source, TransactionAccountType accountCode) => source
            .Transactions
            .Where(x => x.AccountCode == accountCode.ToString())
            .Sum(x => x.Amount);

        public static BusinessEvent AddUnplacedPayments(SupportShared support, ICreditEnvSettings envSettings, params (decimal Amount, string NoteText)[] payments)
        {
            BusinessEvent evt;
            var mgr = new NewManualIncomingPaymentBatchBusinessEventManager(support.CurrentUser, support.Clock, support.ClientConfiguration);
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                evt = mgr.CreateBatch(context, payments.Select(x => new NewManualIncomingPaymentBatchBusinessEventManager.ManualPayment
                {
                    Amount = x.Amount,
                    BookkeepingDate = support.Clock.Today,
                    InitiatedByUserId = support.CurrentUser.UserId,
                    NoteText = x.NoteText
                }).ToArray());

                context.SaveChanges();
            }

            return evt;
        }

        internal static CreateNotificationsResult NotifyCreditsSimple<TSupport>(TSupport support, params (string CreditNr, int CustomerId)[] credits) where TSupport : SupportShared, ISupportSharedCredit =>
            NotifyCredits(support, credits.Select(x => (x.CreditNr, x.CustomerId, new NotificationExpectedResultCode?(NotificationExpectedResultCode.NotificationCreated))).ToArray());

        internal static CreateNotificationsResult NotifyCredits<TSupport>(TSupport support, params (string CreditNr, int CustomerId, NotificationExpectedResultCode? ExpectedResult)[] credits) where TSupport : SupportShared, ISupportSharedCredit
        {
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var postalInfo = new CustomerPostalInfoRepository(false, customerClient.Object, support.ClientConfiguration);
            var renderer = new NotificationRenderer();

            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);
            var notificationProcessFactory = support.GetNotificationProcessSettingsFactory();

            var notificationService = new NotificationService(support.Clock, support.GetRequiredService<ZippedPdfsLoanDeliveryService>(),
                paymentAccountService, support.CreateCreditContextFactory(), support.LoggingService, support.CreditEnvSettings,
                support.ClientConfiguration, notificationProcessFactory, customerClient.Object, renderer, support.GetRequiredService<AlternatePaymentPlanService>(),
                support.CurrentUser, support.GetRequiredService<PaymentOrderService>());

            var result = notificationService.CreateNotifications(skipDeliveryExport: false, skipNotify: false, onlyTheseCreditNrs: credits == null || credits.Length == 0 ? null : credits?.Select(x => x.CreditNr).ToList());

            if (credits != null)
            {
                foreach (var (CreditNr, CustomerId, ExpectedResult) in credits)
                {
                    if (ExpectedResult.HasValue)
                    {
                        var actualResult = result.ResultByCreditNr.OptS(CreditNr)?.Result;
                        if (ExpectedResult.Value == NotificationExpectedResultCode.NotNotified)
                        {
                            Assert.That(result.ResultByCreditNr.ContainsKey(CreditNr), Is.False, "Notification sent");
                        }
                        else if (ExpectedResult.Value == NotificationExpectedResultCode.NotificationCreated)
                        {
                            Assert.That(result.ResultByCreditNr.OptS(CreditNr)?.Result, Is.EqualTo(NotificationResultCode.NotificationCreated), "Notification");
                        }
                        else if (ExpectedResult.Value == NotificationExpectedResultCode.PaymentFreeMonth)
                        {
                            Assert.That(result.ResultByCreditNr.OptS(CreditNr)?.Result, Is.EqualTo(NotificationResultCode.PaymentFreeMonth), "Notification");
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            return result;
        }

        public static void RemindCredits<TSupport>(TSupport support, params (string CreditNr, int? ReminderCountExpected)[] credits) where TSupport : SupportShared, ISupportSharedCredit
        {
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            var notificationSettings = support.GetNotificationProcessSettingsFactory();

            var mgr = new NewCreditRemindersBusinessEventManager(support.CurrentUser, support.CreatePaymentAccountService(support.CreditEnvSettings), support.Clock, support.ClientConfiguration,
                 notificationSettings, support.CreditEnvSettings, support.CreateCreditContextFactory(), support.LoggingService, () => new DocumentRenderer(),
                 support.GetRequiredService<PaymentOrderService>());

            var s = new ReminderService(documentClient.Object, mgr, support.LoggingService, notificationSettings, TestPersons.CreateRealisticCustomerClient(support).Object, support.CreditEnvSettings);

            var result = s.CreateReminders(true, false, support.CreditType, onlyTheseCreditNrs: credits.Length == 0 ? null : credits.Select(x => x.CreditNr).ToHashSetShared());

            if (result.Errors != null)
            {
                if (result.Errors.Count > 0)
                {
                    TestContext.WriteLine(JsonConvert.SerializeObject(result.Errors, Formatting.Indented));
                }

                Assert.That(result.Errors.Count, Is.EqualTo(0));
            }

            if (credits != null)
            {
                foreach (var (CreditNr, ReminderCountExpected) in credits)
                {
                    if (ReminderCountExpected.HasValue)
                    {
                        Assert.That(result.GetReminderCount(CreditNr), Is.EqualTo(ReminderCountExpected.Value), "Reminder count");
                    }
                }
            }
        }

        public static (NewCreditTerminationLettersBusinessEventManager Manager, TerminationLetterService Service, TerminationLetterCandidateService CandidateService) CreateTerminationLetterServices<TSupport>(TSupport support,
            Action<IDictionary<string, object>>? observeTerminationLetterPrintContext = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            var notificationSettings = support.GetNotificationProcessSettingsFactory();
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support).Object;
            var debtCollectionCandidateService = new DebtCollectionCandidateService(support.Clock, support.CreateCreditContextFactory(), customerClient, support.ClientConfiguration);
            var terminationLetterCandidateService = new TerminationLetterCandidateService(support.Clock, debtCollectionCandidateService, notificationSettings, support.CreditEnvSettings,
                support.CreateCreditContextFactory(), customerClient, support.ClientConfiguration);
            var mgr = new NewCreditTerminationLettersBusinessEventManager(support.CurrentUser, support.CreatePaymentAccountService(support.CreditEnvSettings), support.Clock,
                support.ClientConfiguration, notificationSettings, support.CreateCreditContextFactory(), support.CreditEnvSettings,
                terminationLetterCandidateService, support.LoggingService, support.GetRequiredService<PaymentOrderService>());
            var service = new TerminationLetterService(() => new DocumentRenderer(observeTerminationLetterPrintContext), mgr, notificationSettings, customerClient,
                support.ClientConfiguration, support.LoggingService, documentClient.Object);

            return (Manager: mgr, Service: service, CandidateService: terminationLetterCandidateService);
        }
        public static void CreateTerminationLetters<TSupport>(TSupport support, params (string CreditNr, bool? TerminationLetterCreatedExpected)[] credits) where TSupport : SupportShared, ISupportSharedCredit =>
            CreateTerminationLetters(support, null, credits);

        public static void CreateTerminationLetters<TSupport>(TSupport support, Action<IDictionary<string, object>>? observeTerminationLetterPrintContext, params (string CreditNr, bool? TerminationLetterCreatedExpected)[] credits) where TSupport : SupportShared, ISupportSharedCredit
        {
            var (Manager, Service, CandidateService) = CreateTerminationLetterServices(support, observeTerminationLetterPrintContext: observeTerminationLetterPrintContext);

            string[] eligableCreditNrs;
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                eligableCreditNrs = CandidateService.GetEligibleForTerminationLetterCreditNrs(context);
            }

            var result = Service.CreateTerminationLetters(true, false,
                credits == null || credits.Length == 0
                ? eligableCreditNrs.ToList()
                : eligableCreditNrs.Intersect(credits.Select(x => x.CreditNr)).ToList(),
                support.CreditType);

            if (credits != null)
            {
                foreach (var (CreditNr, TerminationLetterCreatedExpected) in credits)
                {
                    if (TerminationLetterCreatedExpected.HasValue)
                    {
                        Assert.That(result.CreditNrsWithLettersCreated.Contains(CreditNr), Is.EqualTo(TerminationLetterCreatedExpected.Value));
                    }
                }
            }
        }

        private static (CreditDebtCollectionBusinessEventManager Manager, DebtCollectionCandidateService CandidateService) CreateDebtCollectionServices<TSupport>(TSupport support) where TSupport : SupportShared, ISupportSharedCredit
        {
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            documentClient.Setup(x => x.CreateXlsxToArchive(It.IsAny<DocumentClientExcelRequest>(), It.IsAny<string>())).Returns("abc123.xls");
            var customerClient = TestPersons.CreateRealisticCustomerClient(support).Object;
            var debtCollectionCandidateService = new DebtCollectionCandidateService(support.Clock, support.CreateCreditContextFactory(), customerClient, support.ClientConfiguration);
            var signatureClient = new Mock<nCredit.Code.ICommonSignatureClient>(MockBehavior.Strict);

            var termsChangeManager = new CreditTermsChangeCancelOnlyBusinessEventManager(support.CurrentUser, support.Clock, support.ClientConfiguration,
                support.CreateCreditContextFactory(), customerClient);
            var mgr = new CreditDebtCollectionBusinessEventManager(support.CurrentUser, documentClient.Object, support.Clock, support.ClientConfiguration,
                support.CreditEnvSettings, termsChangeManager, customerClient, debtCollectionCandidateService,
                support.GetRequiredService<PaymentOrderService>());
            return (Manager: mgr, CandidateService: debtCollectionCandidateService);
        }

        public static void SendCreditsToDebtCollection<TSupport>(TSupport support, params (string CreditNr, bool? IsSentToDebtCollectionExpected)[] credits) where TSupport : SupportShared, ISupportSharedCredit =>
            SendCreditsToDebtCollectionExtended(support, credits.Select(x => (CreditNr: x.CreditNr, IsSentToDebtCollectionExpected: x.IsSentToDebtCollectionExpected, ExpectedNotNotifiedInterestAmount: new decimal?())).ToArray());

        public static void SendCreditsToDebtCollectionExtended<TSupport>(TSupport support, params (string CreditNr, bool? IsSentToDebtCollectionExpected, decimal? ExpectedNotNotifiedInterestAmount)[] credits) where TSupport : SupportShared, ISupportSharedCredit
        {
            var (Manager, CandidateService) = CreateDebtCollectionServices(support);
            using var context = support.CreateCreditContextFactory().CreateContext();
            var dcBefore = context.CreditHeadersQueryable.Where(x => x.Status == CreditStatus.SentToDebtCollection.ToString()).Select(x => x.CreditNr).ToHashSetShared();

            var eligableCreditNrs = CandidateService.GetEligibleForDebtCollectionCreditNrs(context);
            Dictionary<string, DebtCollectionNotNotifiedInterest>? notNotifiedInterestPerCreditNr = null;
            var creditsNrsToSend = credits == null || credits.Length == 0
                ? eligableCreditNrs
                : eligableCreditNrs.Intersect(credits.Select(x => x.CreditNr)).ToHashSetShared();
            Manager.SendCreditsToDebtCollection(creditsNrsToSend, context, out var skippedCreditNrsWithReasons,
                observeNotNotifiedInterestPerCreditNr: x => notNotifiedInterestPerCreditNr = x);
            context.SaveChanges();

            if (credits != null)
            {
                var dcAfter = context.CreditHeadersQueryable.Where(x => x.Status == CreditStatus.SentToDebtCollection.ToString()).Select(x => x.CreditNr).ToHashSetShared();

                var newToDc = dcAfter.Except(dcBefore).ToHashSetShared();
                foreach (var (CreditNr, IsSentToDebtCollectionExpected, ExpectedNotNotifiedInterestAmount) in credits)
                {
                    if (IsSentToDebtCollectionExpected.HasValue)
                    {
                        Assert.That(newToDc.Contains(CreditNr), Is.EqualTo(IsSentToDebtCollectionExpected.Value), $"Sent to debt collection");
                    }
                    if (ExpectedNotNotifiedInterestAmount.HasValue)
                    {
                        Assert.That(notNotifiedInterestPerCreditNr.Opt(CreditNr)?.Amount, Is.EqualTo(ExpectedNotNotifiedInterestAmount), "Expected not notified interest");
                    }
                }
            }
        }

        public static void PostponeOrResumeDebtCollection<TSupport>(TSupport support, string creditNr, DateTime? postponedUntilDate) where TSupport : SupportShared, ISupportSharedCredit
        {
            var (Manager, CandidateService) = CreateDebtCollectionServices(support);
            using var context = support.CreateCreditContextFactory().CreateContext();
            var isOk = Manager.TryPostponeOrResumeDebtCollection(creditNr, context, postponedUntilDate, out var failedMessage);
            context.SaveChanges();
            Assert.That(isOk, Is.EqualTo(true), failedMessage);
        }

        public static void PostponeTerminationLetter<TSupport>(TSupport support, string creditNr, DateTime postponedUntilDate) where TSupport : SupportShared, ISupportSharedCredit
        {
            support.GetRequiredService<TerminationLetterInactivationService>().PostponeTerminationLetters(new PostponeTerminationLettersRequest { CreditNr = creditNr, PostponeUntilDate = postponedUntilDate });
        }

        public static void InactivateTerminationLetter<TSupport>(TSupport support, string creditNr) where TSupport : SupportShared, ISupportSharedCredit
        {
            using var context = support.CreateCreditContextFactory().CreateContext();
            var letter = context.CreditTerminationLetterHeadersQueryable.Single(x => x.CreditNr == creditNr && x.InactivatedByBusinessEventId == null);
            var mgr = support.GetRequiredService<CreditTerminationLettersInactivationBusinessEventManager>();

            var isOk = mgr.TryInactivateTerminationLetter(letter.Id, context, out var failedMessage);

            Assert.That(isOk, Is.EqualTo(true), failedMessage);
            context.SaveChanges();
        }

        public static IncomingPaymentFileWithOriginal CreateIncomingPaymentFile<TSupport>(TSupport support,
            List<(string Ocr, string CustomerName, decimal Amount, string NoteText, string ExternalId)> payments) where TSupport : SupportShared, ISupportSharedCredit
        {
            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);
            IncomingPaymentFile.BankAccountNr bankAccountNr;
            if (support.ClientConfiguration.Country.BaseCountry == "SE")
            {
                bankAccountNr = new IncomingPaymentFile.BankAccountNr(paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro());
            }
            else if (support.ClientConfiguration.Country.BaseCountry == "FI")
            {
                bankAccountNr = new IncomingPaymentFile.BankAccountNr(paymentAccountService.GetIncomingPaymentBankAccountNrRequireIbanFi());
            }
            else
            {
                throw new NotImplementedException();
            }
            return new IncomingPaymentFileWithOriginal
            {
                OriginalFileName = "test.txt",
                OriginalFileData = Encoding.UTF8.GetBytes("test test test"),
                ExternalCreationDate = support.Clock.Today,
                ExternalId = "f207feee-057c-48d5-b48b-975d9014a8b8", //Value has no particular meaning
                Format = "some-testfile",
                Accounts = new List<IncomingPaymentFile.Account>
                {
                    new IncomingPaymentFile.Account
                    {
                        AccountNr = bankAccountNr,
                        Currency = support.ClientConfiguration.Country.BaseCurrency,
                        DateBatches = new List<IncomingPaymentFile.AccountDateBatch>
                        {
                            new IncomingPaymentFile.AccountDateBatch
                            {
                                BookKeepingDate = support.Clock.Today,
                                Payments = payments.Select(x => new IncomingPaymentFile.AccountDateBatchPayment
                                {
                                    Amount = x.Amount,
                                    OcrReference = x.Ocr,
                                    ExternalId = x.ExternalId ?? x.Ocr,
                                    CustomerName = x.CustomerName,
                                    InformationText = x.NoteText
                                }).ToList()
                            }
                        }
                    }
                }
            };
        }

        public static void CreateAndImportPaymentFile<TSupport>(TSupport support, Dictionary<string, decimal> amountPerCredit, params string[] creditNrsToSettle) where TSupport : SupportShared, ISupportSharedCredit =>
            CreateAndImportPaymentFileComplex(support, amountPerCredit, creditNrsToSettle ?? new string[] { });

        public static void CreateAndImportPaymentFileComplex<TSupport>(TSupport support, Dictionary<string, decimal> amountPerCredit, string[]? creditNrsToSettle = null,
            Dictionary<string, string>? payerNameByCreditNr = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);

            IncomingPaymentFile.BankAccountNr bankAccountNr;
            if (support.ClientConfiguration.Country.BaseCountry == "SE")
            {
                bankAccountNr = new IncomingPaymentFile.BankAccountNr(paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro());
            }
            else if (support.ClientConfiguration.Country.BaseCountry == "FI")
            {
                bankAccountNr = new IncomingPaymentFile.BankAccountNr(paymentAccountService.GetIncomingPaymentBankAccountNrRequireIbanFi());
            }
            else
            {
                throw new NotImplementedException();
            }

            var creditNrs = amountPerCredit.Keys.ToList();

            Dictionary<string, string?> ocrByCreditNr;
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                ocrByCreditNr = context.CreditHeadersQueryable.Where(x => creditNrs.Contains(x.CreditNr)).Select(x => new
                {
                    x.CreditNr,
                    OcrPaymentReference = x
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => y.Value)
                            .FirstOrDefault()
                })
                .ToDictionary(x => x.CreditNr, x => x.OcrPaymentReference);
            }

            var file = new IncomingPaymentFileWithOriginal
            {
                OriginalFileName = "test.txt",
                OriginalFileData = Encoding.UTF8.GetBytes("test test test"),
                ExternalCreationDate = support.Clock.Today,
                ExternalId = "f207feee-057c-48d5-b48b-975d9014a8b8", //Value has no particular meaning
                Format = "some-testfile",
                Accounts = new List<IncomingPaymentFile.Account>
                {
                    new IncomingPaymentFile.Account
                    {
                        AccountNr = bankAccountNr,
                        Currency = support.ClientConfiguration.Country.BaseCurrency,
                        DateBatches = new List<IncomingPaymentFile.AccountDateBatch>
                        {
                            new IncomingPaymentFile.AccountDateBatch
                            {
                                BookKeepingDate = support.Clock.Today,
                                Payments = creditNrs.Select(creditNr => new IncomingPaymentFile.AccountDateBatchPayment
                                {
                                    Amount = amountPerCredit[creditNr],
                                    OcrReference = ocrByCreditNr[creditNr]!,
                                    ExternalId = creditNr,
                                    CustomerName = payerNameByCreditNr?.Opt(creditNr)
                                }).ToList()
                            }
                        }
                    }
                }
            };

            var mgr = support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();
            var isOk = mgr.TryImportFile(file, overrideDuplicateCheck: true, overrideIbanCheck: true, out var failedMessage, out var placementMessage);
            Assert.That(isOk, Is.True, failedMessage);

            if (creditNrsToSettle != null && creditNrsToSettle.Length > 0)
            {
                var settledCreditNrs = support.WithCreditDb(x => x.CreditHeaders.Where(x => creditNrs.Contains(x.CreditNr) && x.Status == CreditStatus.Settled.ToString()).Select(x => x.CreditNr).ToHashSet());
                foreach (var creditNr in creditNrs)
                {
                    var isSettledExpected = creditNrsToSettle?.Contains(creditNr) == true;
                    var isSettledActually = settledCreditNrs.Contains(creditNr);
                    Assert.That(isSettledExpected, Is.EqualTo(isSettledActually), $"Settled {creditNr}");
                }
            }
        }

        public static void CreateOutgoingPaymentFile<TSupport>(TSupport support) where TSupport : SupportShared, ISupportSharedCredit
        {
            var mgr = support.GetRequiredService<NewOutgoingPaymentFileBusinessEventManager>();
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                mgr.Create(context, skipWhenNoPaymentsExist: true);
                context.SaveChanges();
            }
        }

        public static void CreateAndImportPaymentFileWithOcr<TSupport>(TSupport support, Dictionary<string, decimal> amountPerOcr, Dictionary<string, string>? payerNameByOcr = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);

            IncomingPaymentFile.BankAccountNr bankAccountNr;
            if (support.ClientConfiguration.Country.BaseCountry == "SE")
            {
                bankAccountNr = new IncomingPaymentFile.BankAccountNr(paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro());
            }
            else if (support.ClientConfiguration.Country.BaseCountry == "FI")
            {
                bankAccountNr = new IncomingPaymentFile.BankAccountNr(paymentAccountService.GetIncomingPaymentBankAccountNrRequireIbanFi());
            }
            else
            {
                throw new NotImplementedException();
            }

            var ocrs = amountPerOcr.Keys;

            var payments = ocrs.Select(ocr => new IncomingPaymentFile.AccountDateBatchPayment
            {
                Amount = amountPerOcr[ocr],
                OcrReference = ocr,
                ExternalId = ocr,
                CustomerName = payerNameByOcr?.Opt(ocr)
            }).ToList();

            var file = new IncomingPaymentFileWithOriginal
            {
                OriginalFileName = "test.txt",
                OriginalFileData = Encoding.UTF8.GetBytes("test test test"),
                ExternalCreationDate = support.Clock.Today,
                ExternalId = "f207feee-057c-48d5-b48b-975d9014a8b8", //Value has no particular meaning
                Format = "some-testfile",
                Accounts = new List<IncomingPaymentFile.Account>
                {
                    new IncomingPaymentFile.Account
                    {
                        AccountNr = bankAccountNr,
                        Currency = support.ClientConfiguration.Country.BaseCurrency,
                        DateBatches = new List<IncomingPaymentFile.AccountDateBatch>
                        {
                            new IncomingPaymentFile.AccountDateBatch
                            {
                                BookKeepingDate = support.Clock.Today,
                                Payments = payments
                            }
                        }
                    }
                }
            };

            var mgr = support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();
            var isOk = mgr.TryImportFile(file, true, true, out var failedMessage, out var placementMessage);
            Assert.That(isOk, Is.True, failedMessage);
        }

        public static void CreateAndPlaceUnplacedPayment<TSupport>(TSupport support, string creditNr, decimal amount,
            Action<MultiCreditPaymentPlacementInstruction>? modifyPlacementInstruction = null) where TSupport : SupportShared, ISupportSharedCredit =>
            CreateAndPlaceUnplacedPayments(support, new Dictionary<string, decimal> { [creditNr] = amount },
                modifyPlacementInstruction: modifyPlacementInstruction == null ? null : x =>
                {
                    if (x.CreditNr == creditNr)
                        modifyPlacementInstruction(x.Instruction);
                });

        public static void CreateAndPlaceUnplacedPayments<TSupport>(TSupport support, Dictionary<string, decimal> amountPerCredit,
            Action<(string CreditNr, MultiCreditPaymentPlacementInstruction Instruction)>? modifyPlacementInstruction = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            var evt = AddUnplacedPayments(support, support.CreditEnvSettings,
                            amountPerCredit.Select(x => (Amount: x.Value, NoteText: x.Key)).ToArray());

            PlaceUnplacedPaymentWithCreditNrNoteOrOcrReference(support, evt.Id, modifyPlacementInstruction: modifyPlacementInstruction);
        }

        public static void AssertIsSettled(CreditSupportShared support, string creditNr)
        {
            var isSettled = support.WithCreditDb(x => x.CreditHeaders.Any(x => x.CreditNr == creditNr && x.Status == CreditStatus.Settled.ToString()));
            Assert.That(isSettled, Is.True, $"Expected {creditNr} to be settled");
        }

        public static void PlaceUnplacedPaymentWithCreditNrNoteOrOcrReference<TSupport>(TSupport support, int unplacedBusinessEventId,
            Action<(string CreditNr, MultiCreditPaymentPlacementInstruction Instruction)>? modifyPlacementInstruction = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            var mgr = support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();
            var payments = new List<(int PaymentId, MultiCreditPaymentPlacementInstruction Instruction)>();
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                var paymentsRaw = context
                    .IncomingPaymentHeadersQueryable
                    .Where(x => x.Transactions.Any(y => y.BusinessEventId == unplacedBusinessEventId))
                    .Select(x => new
                    {
                        x.Id,
                        //Since we set this to the credit nr
                        NoteText = x.Items.Where(y => y.Name == "NoteText" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault(),
                        OcrReference = x.Items.Where(y => y.Name == "OcrReference" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault()
                    })
                    .ToList();
                foreach (var payment in paymentsRaw)
                {
                    string creditNr;
                    if (payment.OcrReference != null)
                    {
                        creditNr = context.DatedCreditStringsQueryable.Where(x => x.Name == "OcrPaymentReference" && x.Value == payment.OcrReference).Select(x => x.CreditNr).Distinct().Single();
                    }
                    else if (payment.NoteText != null)
                    {
                        creditNr = payment.NoteText;
                    }
                    else
                        throw new Exception("Missing credit nr");

                    var computeResult = mgr.ComputeMultiCreditPlacementInstruction(new PaymentPlacementSuggestionRequest
                    {
                        CreditNrs = new List<string> { creditNr },
                        PaymentId = payment.Id
                    });

                    modifyPlacementInstruction?.Invoke((CreditNr: creditNr, Instruction: computeResult.Instruction));

                    payments.Add((payment.Id, computeResult.Instruction));
                }
            }

            foreach (var payment in payments)
            {
                var isOk = mgr.TryPlaceFromUnplaced(payment.Instruction, payment.PaymentId, out var failedMessage);

                if (!isOk)
                {
                    TestContext.WriteLine(failedMessage);
                }

                Assert.That(isOk, Is.EqualTo(true));
            }
        }

        public static void PayOverdueNotifications<TSupport>(TSupport support, HashSet<string>? exceptTheseCreditNrs = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                var notificationIds = context.CreditNotificationHeadersQueryable
                    .Where(x => x.ClosedTransactionDate == null && x.DueDate <= support.Clock.Today)
                    .Select(x => x.Id).ToList();
                var models = CreditNotificationDomainModel.CreateForNotifications(notificationIds, context, support.PaymentOrder());

                var paymentPerCredit = models.Values.GroupBy(x => x.CreditNr).ToDictionary(x => x.Key, x => x.Sum(y => y.GetRemainingBalance(support.Clock.Today)));
                if (exceptTheseCreditNrs != null)
                    exceptTheseCreditNrs.ToList().ForEach(x => paymentPerCredit.Remove(x));

                CreateAndPlaceUnplacedPayments(support, paymentPerCredit);
            }
        }

        public static PaymentPlanCalculation GetCurrentPaymentPlan<TSupport>(TSupport support, string creditNr) where TSupport : SupportShared, ISupportSharedCredit
        {
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                var today = context.CoreClock.Today;
                var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, support.CreditEnvSettings);
                return credit.GetAmortizationModel(today).UsingActualAnnuityOrFixedMonthlyCapital(
                annuityAmount =>
                    PaymentPlanCalculation.BeginCreateWithAnnuity(
                        credit.GetNotNotifiedCapitalBalance(today),
                        annuityAmount,
                        credit.GetInterestRatePercent(today), null, support.CreditEnvSettings.CreditsUse360DayInterestYear)
                    .WithMonthlyFee(credit.GetNotificationFee(today))
                    .EndCreate(),
                fixedMonthlyAmount =>
                    PaymentPlanCalculation.BeginCreateWithFixedMonthlyCapitalAmount(
                        credit.GetNotNotifiedCapitalBalance(today),
                        fixedMonthlyAmount,
                        credit.GetInterestRatePercent(today), null, null, support.CreditEnvSettings.CreditsUse360DayInterestYear)
                    .WithMonthlyFee(credit.GetNotificationFee(today))
                    .EndCreate()
            );
            }
        }

        public static StartPaymentPlanResponse StartPaymentPlan<TSupport>(TSupport support, string creditNr, int nrOfPayments = 6) where TSupport : SupportShared, ISupportSharedCredit
        {
            var s = support.GetRequiredService<AlternatePaymentPlanService>();
            var paymentPlan = s.GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
            {
                CreditNr = creditNr,
                ForceStartNextMonth = false,
                NrOfPayments = nrOfPayments
            });
            return s.StartPaymentPlanFromSpecification(paymentPlan);
        }

        public static List<(DateTime DueDate, decimal Amount, string Ocr)> ScheduledDirectDebitPayments<TSupport>(TSupport support) where TSupport : SupportShared, ISupportSharedCredit
        {
            List<(DateTime DueDate, decimal Amount, string Ocr)> payments = new();
            var service = support.GetRequiredService<DirectDebitNotificationDeliveryService>();
            var result = service.CreateDelivery(includeTestingComment: true, observePayments: payments.Add);
            if (result.Errors != null && result.Errors.Count > 0)
                throw new Exception(string.Join(Environment.NewLine, result.Errors));

            return payments;
        }

        public static void PlaceUnplacedPaymentUsingSuggestion<TSupport>(TSupport support, int paymentId, string searchString,
            bool? onlyPlaceAgainstNotified = null,
            string? onlyPlaceAgainstPaymentOrderItemUniqueId = null,
            decimal? maxPlacedAmount = null) where TSupport : SupportShared, ISupportSharedCredit
        {
            var mgr = support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();
            var searchResult = mgr.FindPaymentPlacementCreditNrs(new FindPaymentPlacementCreditNrsRequest
            {
                SearchString = searchString
            });
            if (searchResult.FailedMessage != null)
                Assert.IsNull(searchResult.FailedMessage, $"FindPaymentPlacementCreditNrs did not find a credit to place against: {searchResult.FailedMessage}");
            var instruction = mgr.ComputeMultiCreditPlacementInstruction(new PaymentPlacementSuggestionRequest
            {
                CreditNrs = searchResult.CreditNrs,
                PaymentId = paymentId,
                OnlyPlaceAgainstNotified = onlyPlaceAgainstNotified,
                OnlyPlaceAgainstPaymentOrderItemUniqueId = onlyPlaceAgainstPaymentOrderItemUniqueId,
                MaxPlacedAmount = maxPlacedAmount
            }).Instruction;
            mgr.PlaceFromUnplaced(new PaymentPlacementRequest
            {
                Instruction = instruction,
                PaymentId = paymentId
            });
        }

        public static StartPaymentPlanResponse CreateAlternatePaymentPlan<TSupport>(TSupport support, string creditNr, int nrOfMonths) where TSupport : SupportShared, ISupportSharedCredit
        {
            var service = support.GetRequiredService<AlternatePaymentPlanService>();
            var planSuggestion = service.GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
            {
                CreditNr = creditNr,
                ForceStartNextMonth = false,
                NrOfPayments = nrOfMonths
            });
            return service.StartPaymentPlanFromSpecification(planSuggestion);
        }

        public static void SetCustomCosts<TSupport>(TSupport support, params CustomCost[] costs) where TSupport : SupportShared, ISupportSharedCredit
        {
            var customCostService = support.GetRequiredService<CustomCostTypeService>();
            customCostService.SetCosts(costs.ToList());
        }

        public static IDocumentRenderer CreateDocumentRenderer(Action<IDictionary<string, object>>? observeRenderContext = null) => new DocumentRenderer(observeRenderContext: observeRenderContext);

        public class NotificationRenderer : INotificationDocumentRenderer, INotificationDocumentBatchRenderer
        {
            public static Action<(IDictionary<string, object> Context, string ArchiveFilename)>? ObservePrintContext { get; set; } = null;

            private int i = 1;
            public string RenderDocumentToArchive(CreditType creditType, bool isForCoNotification, IDictionary<string, object> context, string archiveFilename)
            {
                ObservePrintContext?.Invoke((Context: context, ArchiveFilename: archiveFilename));
                printContexts[i] = context;
                return $"{i++}-{archiveFilename}";
            }

            private readonly Dictionary<int, IDictionary<string, object>> printContexts = new Dictionary<int, IDictionary<string, object>>();

            public IDictionary<string, object> GetLastPrintContext() => printContexts.Opt(i - 1);

            public T WithRenderer<T>(Func<INotificationDocumentRenderer, T> f)
            {
                return f(this);
            }
        }

        public class DocumentRenderer : IDocumentRenderer
        {
            private readonly Action<IDictionary<string, object>>? observeRenderContext;
            public static Action<(IDictionary<string, object> Context, string ArchiveFilename)>? ObservePrintContext { get; set; } = null;

            public DocumentRenderer(Action<IDictionary<string, object>>? observeRenderContext = null)
            {
                this.observeRenderContext = observeRenderContext;
            }
            private int i = 1;
            public void Dispose() { }
            public string RenderDocumentToArchive(string templateName, IDictionary<string, object> context, string archiveFilename)
            {
                printContexts[i] = context;
                observeRenderContext?.Invoke(context);
                ObservePrintContext?.Invoke((Context: context, ArchiveFilename: archiveFilename));
                return $"{i++}-{archiveFilename}";
            }

            private readonly Dictionary<int, IDictionary<string, object>> printContexts = new Dictionary<int, IDictionary<string, object>>();

            public IDictionary<string, object> GetPrintContext(int notificationNr) => printContexts.Opt(notificationNr);
        }

        internal class TestSnailMailLoanDeliveryService : ISnailMailLoanDeliveryService
        {
            public OutgoingCreditNotificationDeliveryFileHeader DeliverLoans(List<string> errors, DateTime today, ICustomerPostalInfoRepository customerPostalInfoRepository, INTechCurrentUserMetadata user, List<string>? onlyTheseCreditNrs = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}