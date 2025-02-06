using Microsoft.EntityFrameworkCore;
using Moq;
using nCredit;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.Excel;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.BookKeeping;
using NTech.Banking.SieFiles;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Module.Shared.Clients;
using static NTech.Core.Host.IntegrationTests.Shared.CreditCycleAssertionBuilder;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Utilities
{
    internal static class CreditsMlStandard
    {
        public static CreditHeader CreateCredit(MlStandardTestRunner.TestSupport support, int creditNumber, int? mainApplicantCustomerId = null, int? coApplicantCustomerId = null,
            decimal loanAmount = 2000000m, decimal drawnFromLoanAmountInitialFees = 0m, decimal notificationFeeAmount = 0m,
            decimal? marginInterestRatePercent = null,
            decimal? referenceInterestRatePercent = null,
            int? interestRebindMounthCount = null, bool skipCoApplicant = false, string? loanOwnerName = null,
            (DateTime ToDate, decimal Amount)? amortizationException = null, decimal? monthlyAmoritzationAmount = null)
        {
            using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
            {
                return context.UsingTransaction(() =>
                {
                    var c = CreateCreditComposable(support, context, creditNumber, mainApplicantCustomerId: mainApplicantCustomerId, loanAmount: loanAmount,
                        drawnFromLoanAmountInitialFees: drawnFromLoanAmountInitialFees, notificationFeeAmount: notificationFeeAmount, coApplicantCustomerId: coApplicantCustomerId,
                        marginInterestRatePercent: marginInterestRatePercent, referenceInterestRatePercent: referenceInterestRatePercent,
                        interestRebindMounthCount: interestRebindMounthCount, skipCoApplicant: skipCoApplicant, loanOwnerName: loanOwnerName, amortizationException: amortizationException,
                        monthlyAmoritzationAmount: monthlyAmoritzationAmount);
                    context.SaveChanges();
                    return c;
                });
            }
        }

        public static CreditHeader CreateCreditComposable(MlStandardTestRunner.TestSupport support, CreditContextExtended context, int creditNumber,
            int? mainApplicantCustomerId = null, int? coApplicantCustomerId = null,
            decimal loanAmount = 2000000m, decimal drawnFromLoanAmountInitialFees = 0m, decimal notificationFeeAmount = 0m,
            decimal? marginInterestRatePercent = null,
            decimal? referenceInterestRatePercent = null,
            int? interestRebindMounthCount = null,
            bool skipCoApplicant = false, string? loanOwnerName = null,
            (DateTime ToDate, decimal Amount)? amortizationException = null,
            decimal? monthlyAmoritzationAmount = null)
        {
            var contextKey = $"TestCredit{creditNumber}_Header";
            if (support.Context.ContainsKey(contextKey))
            {
                throw new Exception($"Credit {creditNumber} has already been created");
            }

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

            var applicants = new List<MortgageLoanRequest.Applicant>();

            mainApplicantCustomerId = mainApplicantCustomerId ?? TestPersons.EnsureTestPerson(support, GetPersonSeed(false));
            applicants.Add(new MortgageLoanRequest.Applicant
            {
                ApplicantNr = 1,
                CustomerId = mainApplicantCustomerId.Value,
                OwnershipPercent = skipCoApplicant ? 100m : 50m
            });

            if (!skipCoApplicant)
            {
                coApplicantCustomerId = coApplicantCustomerId ?? TestPersons.EnsureTestPerson(support, GetPersonSeed(true));
                applicants.Add(new MortgageLoanRequest.Applicant
                {
                    ApplicantNr = 2,
                    CustomerId = coApplicantCustomerId.Value,
                    OwnershipPercent = 50m
                });
            }

            var creditNr = $"ML987{creditNumber}"; //The 987 prefix is just to make the length a bit more realistic. It has no significance.

            //////////////////////////
            /// Create loan       ////
            //////////////////////////
            monthlyAmoritzationAmount = monthlyAmoritzationAmount ?? Math.Ceiling(loanAmount / (40m * 12m));
            var localRebindCount = interestRebindMounthCount ?? support.CreditEnvSettings.MortgageLoanInterestBindingMonths ?? 3;
            var mlCreationService = support.GetRequiredService<SwedishMortageLoanCreationService>();

            var ltvFraction = 0.8m;
            var currentCombinedYearlyIncomeAmount = 800000m;
            var creationResult = mlCreationService.CreateLoans(new SwedishMortgageLoanCreationRequest
            {
                Loans = new List<SwedishMortgageLoanCreationRequest.SeMortgageLoanModel>
                {
                    new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                    {
                        MonthlyFeeAmount = notificationFeeAmount,
                        NominalInterestRatePercent = marginInterestRatePercent ?? 2m,
                        Applicants = applicants,
                        ProviderName = "self",
                        LoanAmount = loanAmount,
                        ProviderApplicationId = null,
                        EndDate = support.Clock.Today.AddYears(40),
                        ActiveDirectDebitAccount = null,
                        
                        ApplicationNr = null,
                        CreditNr = creditNr,
                        Documents = null,
                        KycQuestionsJsonDocumentArchiveKey = null,
                        FixedMonthlyAmortizationAmount = monthlyAmoritzationAmount,
                        AmortizationExceptionUntilDate = amortizationException?.ToDate,
                        ExceptionAmortizationAmount = amortizationException?.Amount,
                        AmortizationExceptionReasons = amortizationException.HasValue ? new List<string> { "nyproduktion" } : null,
                        NextInterestRebindDate = support.Clock.Today.AddMonths(localRebindCount),
                        InterestRebindMounthCount = localRebindCount,
                        ReferenceInterestRate = referenceInterestRatePercent ?? 0.2m,
                        ConsentingPartyCustomerIds = new List<int>
                        {
                            mainApplicantCustomerId.Value
                        },
                        PropertyOwnerCustomerIds = applicants.Select(x => x.CustomerId).ToList(),
                        DrawnFromLoanAmountInitialFeeAmount = drawnFromLoanAmountInitialFees > 0 ? drawnFromLoanAmountInitialFees : new decimal?(),
                        LoanOwnerName = loanOwnerName
                    }
                },
                NewCollateral = CreateNewCollateralRequest(),
                AmortizationBasis = new Credit.Shared.Models.SwedishMortgageLoanAmortizationBasisModel
                {
                    CurrentCombinedYearlyIncomeAmount = 800000m,
                    OtherMortageLoansAmount = 0m,
                    LtvFraction = ltvFraction,
                    LtiFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLti(currentCombinedYearlyIncomeAmount, loanAmount, 0m),
                    ObjectValue = Math.Round(loanAmount / ltvFraction),
                    ObjectValueDate = support.Clock.Today.AddDays(-200),
                    Loans = new List<Credit.Shared.Models.SwedishMortgageLoanAmortizationBasisModel.LoanModel>
                    {
                        new Credit.Shared.Models.SwedishMortgageLoanAmortizationBasisModel.LoanModel
                        {
                            CreditNr = creditNr,
                            RuleCode = "r201723",
                            CurrentCapitalBalanceAmount = loanAmount,
                            MaxCapitalBalanceAmount = loanAmount,
                            IsUsingAlternateRule = false,
                            MonthlyAmortizationAmount = monthlyAmoritzationAmount.Value
                        }
                    }
                }
            });
            context.SaveChanges();

            var collateralId = creationResult.CollateralId;
            support.Context["Collateral1_Id"] = collateralId;

            Assert.That(
                context.CollateralItems.Single(x => x.CollateralHeaderId == collateralId && x.ItemName == "seBrfName").StringValue,
                Is.EqualTo("Nakter gallant AB"));

            var createdCredit = context.CreditHeaders.Include(x => x.CreditCustomers).Single(x => x.CreditNr == creditNr);

            context.SaveChanges();

            support.Context[contextKey] = createdCredit;

            return createdCredit;
        }

        public static CreditHeader GetCreatedCredit(MlStandardTestRunner.TestSupport support, int creditNumber)
        {
            var contextKey = $"TestCredit{creditNumber}_Header";

            if (!support.Context.ContainsKey(contextKey))
            {
                throw new Exception($"Credit {creditNumber} has not been created");
            }

            return (CreditHeader)support.Context[contextKey];
        }

        public static string GetCreditsWithAgreementCreditNr(int creditIndex) => $"L100{creditIndex.ToString().PadLeft(3, '0')}";

        public static SwedishMortgageLoanCreationResponse CreateCreditsWithAgreement(MlStandardTestRunner.TestSupport support, int minCreditIndex, int nrOfLoans, int interestRebindMounthCount)
        {
            const decimal OneMillion = 1000000m;
            var service = support.GetRequiredService<SwedishMortageLoanCreationService>();

            var loanRequest = new SwedishMortgageLoanCreationRequest
            {
                AgreementNr = "A" + GetCreditsWithAgreementCreditNr(minCreditIndex),
                NewCollateral = new SwedishMortgageLoanCreationRequest.CollateralModel
                {
                    IsBrfApartment = true,
                    BrfOrgNr = "5590406483", //Nakergals orgnr
                    BrfName = "Nakter gallant AB",
                    BrfApartmentNr = "S42",
                    TaxOfficeApartmentNr = "1105",
                    AddressStreet = "High mountain way 12",
                    AddressZipcode = "111 11",
                    AddressCity = "Le town",
                    AddressMunicipality = "Le city"
                },
                Loans = Enumerable.Range(minCreditIndex, nrOfLoans).Select(creditIndex => new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                    {
                        MonthlyFeeAmount = 20m,
                        NominalInterestRatePercent = 5m,
                        Applicants = new List<MortgageLoanRequest.Applicant>
                            {
                                new MortgageLoanRequest.Applicant
                                {
                                    ApplicantNr = 1,
                                    CustomerId = TestPersons.EnsureTestPerson(support, 1),
                                    OwnershipPercent = 50m
                                },
                                new MortgageLoanRequest.Applicant
                                {
                                    ApplicantNr = 2,
                                    CustomerId = TestPersons.EnsureTestPerson(support, 2),
                                    OwnershipPercent = 50m
                                },
                            },
                        ProviderName = "self",
                        LoanAmount = OneMillion,
                        EndDate = support.Clock.Today.AddYears(40),                        
                        CreditNr = GetCreditsWithAgreementCreditNr(creditIndex),
                        NextInterestRebindDate = support.Clock.Today.AddMonths(interestRebindMounthCount),
                        InterestRebindMounthCount = interestRebindMounthCount,
                        ReferenceInterestRate = 0.2m,
                        ConsentingPartyCustomerIds = new List<int>
                                {
                                    TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                                },
                        PropertyOwnerCustomerIds = new List<int>
                                {
                                    TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                                }
                    })
                .ToList()
            };

            loanRequest.Loans.First().ActiveDirectDebitAccount = new MortgageLoanRequest.ActiveDirectDebitAccountModel
            {
                ActiveSinceDate = support.Clock.Today,
                BankAccountNr = BankAccountNumberSe.Parse("3300" + TestPersons.GetTestPersonDataByCustomerId(support, 1)["civicRegNr"].Substring(2)).NormalizedValue,
                BankAccountNrOwnerApplicantNr = 1
            };

            var totalLoanAmount = loanRequest.Loans.Sum(x => x.LoanAmount ?? 0m);
            
            loanRequest.AmortizationBasis = SwedishMortgageLoanAmortizationBasisService.CalculateSuggestedAmortizationBasis(
                new CalculateMortgageLoanAmortizationBasisRequest
                {
                    CombinedYearlyIncomeAmount = 5m * totalLoanAmount,
                    ObjectValueAmount = 2m * totalLoanAmount,
                    OtherMortageLoansBalanceAmount = 0m,
                    NewLoans = loanRequest.Loans.Select(x => new CalculateMortgageLoanAmortizationBasisRequest.MlAmortizationBasisRequestNewLoan
                    {
                        CreditNr = x.CreditNr,
                        CurrentBalanceAmount = x.LoanAmount ?? 0m
                    }).ToList()
                },
                support.Clock.Today);

            loanRequest.Loans.ForEach(x =>
            {
                var basisLoan = loanRequest.AmortizationBasis.Loans.Single(y => y.CreditNr == x.CreditNr);
                x.FixedMonthlyAmortizationAmount = loanRequest.AmortizationBasis.Loans.Single(y => y.CreditNr == x.CreditNr).MonthlyAmortizationAmount;
            });
            return service.CreateLoans(loanRequest);
        }

        public static void RunOneMonth(
            MlStandardTestRunner.TestSupport support,
            Action? doOnFirstOfMonth = null,
            Action? doBeforeDebtCollection = null,
            Action? doAfterNotification = null,
            Action? doBeforeTerminationLetters = null,
            Action? doAfterTerminationLetters = null,
            Action? doBeforeNotification = null,
            Action? doAfterDebtCollection = null,
            Action<IDictionary<string, object>>? observeTerminationLetterPrintContext = null,
            Action<int>? beforeDay = null,
            Action<int>? afterDay = null,
            bool payNotificationsOnDueDate = false,
            bool payDirectDebitOnSchedule = false,
            HashSet<string>? skipNotificationPaymentsOnTheseCreditNrs = null,
            Action<(SieBookKeepingFile File, Dictionary<string, decimal> BalancePerAccount, Dictionary<string, string> NamePerAccount, int DayNr)>? observerBookKeeping = null,
            (CreditCycleAssertion Assertion, int MonthNr)? creditCycleAssertion = null)
        {
            support.AssertDayOfMonth(1);
            var bookKeeping = CreateBookKeepingGenerator(support, 
                observeSieFile: x => observerBookKeeping?.Invoke((File: x.File, BalancePerAccount: x.BalancePerAccount, NamePerAccount: x.NamePerAccount, DayNr: support.Clock.Today.Day)));
            var lastDateOfMonth = new DateTime(support.Clock.Today.Year, support.Clock.Today.Month, 1).AddMonths(1).AddDays(-1);
            List<Flaggable<(DateTime DueDate, decimal Amount, string Ocr)>> scheduledDirectDebitPayments = new();
            do
            {
                var dayOfMonth = support.Clock.Today.Day;

                beforeDay?.Invoke(dayOfMonth);

                //Do every day first in the schedule
                {
                    var services = CreateChangeTermsServices(support);
                    services.UpdateService.UpdateChangeTerms();

                    var customerClient = TestPersons.CreateRealisticCustomerClient(support);
                    var rebindReminderService = new BoundInterestExpirationReminderService(support.CreateCreditContextFactory(), support.CurrentUser, support.Clock,
                        support.ClientConfiguration, customerClient.Object, support.CreditEnvSettings, support.CreateCachedSettingsService());
                    rebindReminderService.SendReminderMessages();
                }

                if (dayOfMonth == 1)
                {
                    doOnFirstOfMonth?.Invoke();
                }
                else if (dayOfMonth == 14)
                {
                    doBeforeNotification?.Invoke();
                    Credits.NotifyCredits(support);
                    doAfterNotification?.Invoke();

                    Credits.RemindCredits(support);

                    doBeforeTerminationLetters?.Invoke();
                    Credits.CreateTerminationLetters(support, observeTerminationLetterPrintContext);
                    doAfterTerminationLetters?.Invoke();
                }
                else if (dayOfMonth == 20)
                {
                    doBeforeDebtCollection?.Invoke();
                    var dailyScheduledDirectDebitPayments = Credits.ScheduledDirectDebitPayments(support);
                    if (dailyScheduledDirectDebitPayments.Count > 0)
                        scheduledDirectDebitPayments.AddRange(dailyScheduledDirectDebitPayments.Select(x => Flaggable.Create(x)));
                    Credits.SendCreditsToDebtCollectionExtended(support);
                    doAfterDebtCollection?.Invoke();
                }
                else if (dayOfMonth == 28)
                {
                    if (payNotificationsOnDueDate)
                    {
                        Credits.PayOverdueNotifications(support, exceptTheseCreditNrs: skipNotificationPaymentsOnTheseCreditNrs);
                    }
                    Credits.RemindCredits(support);
                }

                if (payDirectDebitOnSchedule)
                {
                    var dailyDueDirectDebitPayments = scheduledDirectDebitPayments.Where(x => !x.IsFlagged && x.Item.DueDate == support.Clock.Today).ToList();
                    if (dailyDueDirectDebitPayments.Count > 0)
                    {
                        Credits.CreateAndImportPaymentFileWithOcr(support, dailyDueDirectDebitPayments
                            .GroupBy(x => x.Item.Ocr)
                            .ToDictionary(x => x.Key, x => x.Sum(y => y.Item.Amount)));
                        dailyDueDirectDebitPayments.ForEach(x => x.IsFlagged = true);
                    }
                }

                bookKeeping.GenerateBookKeeping();

                afterDay?.Invoke(dayOfMonth);

                if (creditCycleAssertion != null)
                {
                    creditCycleAssertion.Value.Assertion.DoAssert(support, creditCycleAssertion.Value.MonthNr, dayOfMonth);
                }

                support.MoveForwardNDays(1);
            } while (support.Clock.Today <= lastDateOfMonth);
        }

        public static (MortgageLoansCreditTermsChangeBusinessEventManager Manager, MortgageLoansUpdateChangeTermsService UpdateService) CreateChangeTermsServices(MlStandardTestRunner.TestSupport support)
        {
            var signatureClient = new Mock<ICommonSignatureClient>(MockBehavior.Strict);
            var mgr = support.GetRequiredService<MortgageLoansCreditTermsChangeBusinessEventManager>();
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var changeTermsService = new MortgageLoansUpdateChangeTermsService(mgr, support.LoggingService, support.GetNotificationProcessSettingsFactory(),
                customerClient.Object, support.CreditEnvSettings);
            return (Manager: mgr, UpdateService: changeTermsService);
        }

        public static void ScheduleTermChange(MlStandardTestRunner.TestSupport support, string creditNr, int newFixedMonthCount, decimal newMarginInterestRatePercent)
        {
            var changeTermsServices = CreateChangeTermsServices(support);
            var mgr = changeTermsServices.Manager;
            var newTerms = new MlNewChangeTerms
            {
                NewFixedMonthsCount = newFixedMonthCount,
                NewInterestBoundFrom = support.Clock.Today.AddDays(1),
                NewMarginInterestRatePercent = newMarginInterestRatePercent
            };
            var customerClient = TestPersons.CreateRealisticCustomerClient(support).Object;
            mgr.TryComputeMlTermsChangeData(creditNr, newTerms, out MlTermsChangeData termsChangeData, out var failedMessage).AssertTrue(failedMessage);
            var startResult = mgr.MlStartCreditTermsChange(creditNr, newTerms, () => Credits.CreateDocumentRenderer(), customerClient);
            startResult.IsSuccess.AssertTrue($"Failed to start term change for {creditNr}");
            mgr.AttachSignedAgreement(startResult.TermChange.Id, $"termchange-{creditNr}-{support.Clock.Today:yyyy-MM-dd}"); //Just anything unique but not random
            mgr.TryScheduleCreditTermsChange(startResult.TermChange.Id, out var failedMessage2).AssertTrue(failedMessage2);
        }

        public static void SetupFixedInterestRates(MlStandardTestRunner.TestSupport support, Dictionary<int, decimal> ratePercentByMonthCount)
        {
            if (!ratePercentByMonthCount.ContainsKey(3))
                throw new Exception("Must include at least 3 months");

            var mgr = new MortgageLoanFixedInterestChangeEventManager(support.CurrentUser, support.CreateCreditContextFactory(), support.CreditEnvSettings,
                support.Clock, support.ClientConfiguration);

            mgr.BeginChange(ratePercentByMonthCount);
            mgr.CommitChange(true);
        }

        public static SwedishMortgageLoanCreationRequest.CollateralModel CreateNewCollateralRequest()
        {
            return new SwedishMortgageLoanCreationRequest.CollateralModel
            {
                AddressCity = "Le town",
                AddressMunicipality = "Le city",
                IsBrfApartment = true,
                AddressStreet = "High mountain way 12",
                AddressZipcode = "111 11",
                BrfApartmentNr = "S42",
                TaxOfficeApartmentNr = "1105",
                BrfName = "Nakter gallant AB",
                BrfOrgNr = "5590406483", //Nakergals orgnr
                ObjectId = null
            };
        }

        public static void ChangeCollateralStringItems(MlStandardTestRunner.TestSupport support, int collateralId, Dictionary<string, string> newValues)
        {
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                //NOTE: Currently there is no event that actually changes the collateral fields but we want to test that since it will 100% be added in the future hence this hack
                var evt = context.FillInfrastructureFields(new BusinessEvent { TransactionDate = support.Clock.Today, EventType = "NoSuchEvent", BookKeepingDate = support.Clock.Today });
                context.AddBusinessEvent(evt);
                support.GetRequiredService<MortgageLoanCollateralService>().SetCollateralStringItems(context, collateralId, evt, newValues);

                context.SaveChanges();
            }
        }

        private static (Action GenerateBookKeeping, Dictionary<string, decimal> BalancePerAccount, Dictionary<string, string> NamePerAccount) CreateBookKeepingGenerator(MlStandardTestRunner.TestSupport support,
            Action<(SieBookKeepingFile File, Dictionary<string, decimal> BalancePerAccount, Dictionary<string, string> NamePerAccount)>? observeSieFile = null)
        {
            NtechBookKeepingRuleFile ruleSet = NtechBookKeepingRuleFile.Parse(EmbeddedResources.LoadEmbeddedXmlDocument("MlStandard-BookkeepingRules.xml"));
            NtechAccountPlanFile accountPlan = NtechAccountPlanFile.Parse(EmbeddedResources.LoadEmbeddedXmlDocument("MlStandard-BookkeepingAccountPlan.xml"));
            BookKeepingFileManager m = new BookKeepingFileManager(support.CurrentUser, support.ClientConfiguration, support.Clock, support.CreditEnvSettings, support.CreateCreditContextFactory(), () => ruleSet);

            void GenerateBookKeeping()
            {
                using (var context = support.CreateCreditContextFactory().CreateContext())
                {
                    context.BeginTransaction();
                    var dates = BookKeepingFileManager.GetDatesToHandle(context);
                    var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
                    var i = 0;
                    documentClient
                        .Setup(x => x.ArchiveStore(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Returns<byte[], string, string>((fileData, mimeType, fileName) => i++.ToString());
                    documentClient
                        .Setup(x => x.CreateXlsxToArchive(It.IsAny<DocumentClientExcelRequest>(), It.IsAny<string>()))
                        .Returns<DocumentClientExcelRequest, string>((request, archiveFileName) => i++.ToString());
                    var balancePerAccount = support.BookKeepingBalancePerAccount;
                    var namePerAccount = support.BookKeepingNamePerAccount;

                    m.TryCreateBookKeepingFile(context, dates, documentClient.Object, null, new HashSet<string> { "self" },
                        support.GetRequiredService<IKeyValueStoreService>(), accountPlan,
                        out var h, out var warnings, observeFile: sieFile =>
                        {
                            foreach (var ver in sieFile.Verifications)
                            {
                                foreach (var transaction in ver.Transactions)
                                {
                                    var account = accountPlan.Accounts.Single(z => z.InitialAccountNr == transaction.Account);
                                    balancePerAccount.AddOrUpdate(account.InitialAccountNr, transaction.Amount, x => x + transaction.Amount);                                    
                                    namePerAccount[account.InitialAccountNr] = $"{account.InitialAccountNr} [{account.Name}]";
                                }
                            }
                            observeSieFile?.Invoke((File: sieFile, BalancePerAccount: balancePerAccount, NamePerAccount: namePerAccount));

                        });
                    context.SaveChanges();
                    context.CommitTransaction();
                }
            }
            return (GenerateBookKeeping: GenerateBookKeeping, BalancePerAccount: support.BookKeepingBalancePerAccount, NamePerAccount: support.BookKeepingNamePerAccount);
        }
    }
}
