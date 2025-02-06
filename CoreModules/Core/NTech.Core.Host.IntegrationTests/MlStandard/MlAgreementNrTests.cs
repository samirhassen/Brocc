using Microsoft.EntityFrameworkCore.Internal;
using Moq;
using nCredit;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using NTech.Core.Module.Infrastrucutre;
using NTech.Core.Module.Shared.Clients;
using System.Text;
using System.Dynamic;
using NTech.Core.Host.IntegrationTests.Shared;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public class MlAgreementNrTests
    {
        [Test]
        public void CoNotificationCausedByMortgageLoanAgreementNr()
        {
            var masterCreditNr = "L10001";
            var otherCreditNr = "L10002";
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNotification(14, masterCreditNr, dueDay: 28, initialAmount: 2156.22m)
                .ExpectNotification(14, otherCreditNr, dueDay: 28, initialAmount: 1451.91m)
                .ExpectScheduledDirectDebitPayment(20, masterCreditNr, 2156.22m + 1451.91m)
                .ExpectNoScheduledDirectDebitPayment(20, otherCreditNr)
                .ExpectNotificationFullyPaid(28, masterCreditNr, dueDay: 28)

                .ForMonth(2)
                .ExpectNotification(14, masterCreditNr, dueDay: 28)
                .ExpectNotification(14, otherCreditNr, dueDay: 28)

                .ForMonth(5)
                .ExpectTerminationLetter(14, masterCreditNr)
                .ExpectTerminationLetter(14, otherCreditNr)

                .ForMonth(6)
                .ExpectDebtCollectionExport(20, masterCreditNr)
                .ExpectDebtCollectionExport(20, otherCreditNr)
                .AssertCustom(20, t =>
                {
                    //agreement nr should cause there to only be one reminder fee per co notification
                    var reminders = t.Context.CreditReminderHeadersQueryable.Select(x => new
                    {
                        x.CreditNr,
                        ReminderFeeAmount = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.ReminderFeeDebt.ToString()).Sum(y => y.Amount)
                    }).ToList();
                    Assert.That(reminders.Count(x => x.CreditNr == masterCreditNr), Is.EqualTo(5));
                    Assert.That(reminders.Count(x => x.CreditNr == otherCreditNr), Is.EqualTo(5));
                    Assert.That(reminders.Where(x => x.CreditNr == masterCreditNr).Sum(x => x.ReminderFeeAmount), Is.EqualTo(3 * 60m));
                    Assert.That(reminders.Where(x => x.CreditNr == otherCreditNr).Sum(x => x.ReminderFeeAmount), Is.EqualTo(0m));
                })

                .End();

            RunTest(skipDirectDebit: false, assertion);
        }

        [Test]
        public void CoNotification_SnailmailWorksWhenCreditLacksDirectDebit()
        {
            var masterCreditNr = "L10001";
            var otherCreditNr = "L10002";
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNotification(14, masterCreditNr, dueDay: 28, initialAmount: 2156.22m, isSnailMailDeliveryExpected: true)
                .ExpectNotification(14, otherCreditNr, dueDay: 28, initialAmount: 1451.91m, isSnailMailDeliveryExpected: false)
                .ExpectNoScheduledDirectDebitPayment(20, masterCreditNr)
                .ExpectNoScheduledDirectDebitPayment(20, otherCreditNr)

                .End();

            RunTest(skipDirectDebit: true, assertion);
        }

        private void RunTest(bool skipDirectDebit, CreditCycleAssertionBuilder.CreditCycleAssertion assertion)
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.Now = new DateTimeOffset(2022, 1, 1, support.Now.Hour, support.Now.Minute, support.Now.Second, support.Now.Offset);

                string MortgageLoanAgreementNr = CreateLoans(support, skipDirectDebit: skipDirectDebit);

                SetupDocumentPrintDebugging();
                Credits.NotificationRenderer.ObservePrintContext = x =>
                {
                    Assert.IsNotNull(x.Context.Opt("sharedOcrPaymentReference"), "sharedOcrPaymentReference");
                    Assert.That(x.Context.Opt("mortgageLoanAgreementNr") as string, Is.EqualTo(MortgageLoanAgreementNr));

                    AssertNotificationInterestDates(x.Context);

                    OnDocumentPrinted(x.Context, x.ArchiveFilename);
                };
                Credits.DocumentRenderer.ObservePrintContext = x =>
                {
                    OnDocumentPrinted(x.Context, x.ArchiveFilename);
                };
                try
                {
                    List<(DateTime DueDate, decimal Amount, string Ocr)> monthDirectDebitPayments = new();
                    var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
                    documentClient.Setup(x => x.ArchiveStore(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>())).Returns("...");
                    bool willDirectDebitSucceed = true;
                    void BeforeDay(int dayNr)
                    {
                        if (dayNr == 20)
                        {
                            //Create direct debit file
                            var directDebit = new DirectDebitNotificationDeliveryService(documentClient.Object,
                                support.CreatePaymentAccountService(support.CreditEnvSettings),
                                support.CreateCreditContextFactory(),
                                support.CreditEnvSettings,
                                support.GetRequiredService<PaymentOrderService>(), support.ClientConfiguration);
                            monthDirectDebitPayments.Clear();
                            var result = directDebit.CreateDelivery(observePayments: x => monthDirectDebitPayments.Add(x), includeTestingComment: true);
                            Assert.That((result.Errors ?? new List<string>()).Count, Is.EqualTo(0), result.Errors?.FirstOrDefault());
                        }
                        else if (dayNr == 28)
                        {
                            Assert.That(monthDirectDebitPayments.All(x => x.DueDate == support.Clock.Today));
                            if (monthDirectDebitPayments.Count > 0 && willDirectDebitSucceed)
                            {
                                willDirectDebitSucceed = false;
                                SimulateIncomingPaymentFile(support, monthDirectDebitPayments);
                            }
                        }
                        /* Test a payment the day after a reminder to see how autoplacing works
                        if(support.Clock.Today.ToString("yyyy-MM-dd") == "2022-03-15")
                        {
                            SimulateIncomingPaymentFile(support, new List<(decimal Amount, string Ocr)>()
                            {
                                (Amount: 3916.48m, Ocr: "1111111108")
                            });
                            throw new Exception("Break");
                        }
                        */
                    }
                    int maxMonthNr = assertion.MaxMonthNr;
                    foreach (var monthNr in Enumerable.Range(1, maxMonthNr))
                    {
                        CreditsMlStandard.RunOneMonth(support, beforeDay: BeforeDay, afterDay: x =>
                        {
                            assertion.DoAssert(support, monthNr, x);
                        }, payNotificationsOnDueDate: false);
                    }

                }
                finally
                {
                    Credits.NotificationRenderer.ObservePrintContext = null;
                    Credits.DocumentRenderer.ObservePrintContext = null;
                }
            });
        }


        /// <summary>
        /// Since notification headers are commited to the db before rendering the master and the other notifications
        /// had a bug where the non master notifications were picking up the interest dates from "after" the current one.
        /// This test is for fixing and ensuring this bug does not return.
        /// </summary>
        private static void AssertNotificationInterestDates(IDictionary<string, object> context)
        {
            var allCreditsRaw = context.Opt("allCredits") as List<object>;
            var allCredits = allCreditsRaw?.OfType<ExpandoObject>()?.ToList();
            var firstCredit = allCredits?.FirstOrDefault();
            Assert.That(firstCredit, Is.Not.Null);
            Assert.That(firstCredit.Opt("currentInterestFromDate"), Is.Not.Null);
            foreach (var otherCredit in allCredits!.Skip(1))
            {
                Assert.That(firstCredit.Opt("currentInterestFromDate"), Is.EqualTo(otherCredit.Opt("currentInterestFromDate")), "interestFromDate");
                Assert.That(firstCredit.Opt("currentInterestToDate"), Is.EqualTo(otherCredit.Opt("currentInterestToDate")), "interestToDate");
            }
        }

        private static void SimulateIncomingPaymentFile(MlStandardTestRunner.TestSupport support, List<(DateTime DueDate, decimal Amount, string Ocr)> payments) =>
            SimulateIncomingPaymentFile(support, payments.Select(x => (x.Amount, x.Ocr)).ToList());
        
        private static void SimulateIncomingPaymentFile(MlStandardTestRunner.TestSupport support, List<(decimal Amount, string Ocr)> payments)
        {
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                try
                {
                    context.BeginTransaction();

                    var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);
                    var paymentFile = new Banking.IncomingPaymentFiles.IncomingPaymentFileWithOriginal
                    {
                        Accounts = new List<Banking.IncomingPaymentFiles.IncomingPaymentFile.Account>
                            {
                                new Banking.IncomingPaymentFiles.IncomingPaymentFile.Account
                                {
                                    AccountNr = new Banking.IncomingPaymentFiles.IncomingPaymentFile.BankAccountNr((BankGiroNumberSe)paymentAccountService.GetIncomingPaymentBankAccountNr()),
                                    Currency = "SEK",
                                    DateBatches = new List<Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatch>
                                    {
                                        new Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatch
                                        {
                                            BookKeepingDate = support.Clock.Today,
                                            Payments = payments.Select(p => new Banking.IncomingPaymentFiles.IncomingPaymentFile.AccountDateBatchPayment
                                            {
                                                Amount = p.Amount,
                                                OcrReference = p.Ocr
                                            }).ToList()
                                        }
                                    }
                                }
                            },
                        ExternalId = "c8445965-06cb-4e0d-a936-3ee1dbfa3b8c",
                        OriginalFileName = "c8445965-06cb-4e0d-a936-3ee1dbfa3b8c.txt",
                        OriginalFileData = Encoding.UTF8.GetBytes("Test payments c8445965-06cb-4e0d-a936-3ee1dbfa3b8c")
                    };

                    var paymentMgr = support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();
                    var isOk = paymentMgr.TryImportFile(paymentFile, true, true, out var failedMessage, out var placementMessage);
                    Assert.That(isOk, Is.True, failedMessage);
                    Console.WriteLine(placementMessage);
                    Console.WriteLine(failedMessage);

                    context.SaveChanges();
                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        private static string CreateLoans(MlStandardTestRunner.TestSupport support, bool skipDirectDebit = false)
        {
            const decimal OneMillion = 1000000m;
            var mgr = support.GetRequiredService<NewMortgageLoanBusinessEventManager>();
            var collateralService = new MortgageLoanCollateralService(support.CurrentUser, support.Clock, support.ClientConfiguration);
            var relationsService = new CustomerRelationsMergeService(TestPersons.CreateRealisticCustomerClient(support).Object,
                support.CreateCreditContextFactory());
            var service = new SwedishMortageLoanCreationService(mgr, support.CreateCreditContextFactory(), support.Clock, support.ClientConfiguration,
                support.CreditEnvSettings, collateralService, relationsService);

            const string MortgageLoanAgreementNr = "A123456";
            var loanRequest = new SwedishMortgageLoanCreationRequest
            {
                AgreementNr = MortgageLoanAgreementNr,
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
                Loans = new List<SwedishMortgageLoanCreationRequest.SeMortgageLoanModel>
                    {
                        new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                        {
                            MonthlyFeeAmount = 20m,
                            NominalInterestRatePercent = 1.5m,
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
                            CreditNr = "L10001",
                            NextInterestRebindDate = support.Clock.Today.AddMonths(3),
                            InterestRebindMounthCount = 3,
                            ReferenceInterestRate = 0.2m,
                            ConsentingPartyCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            },
                            PropertyOwnerCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            },
                            ActiveDirectDebitAccount = skipDirectDebit ? null : new MortgageLoanRequest.ActiveDirectDebitAccountModel
                            {
                                ActiveSinceDate = support.Clock.Today.AddDays(-2),
                                BankAccountNr = "33007611264032",
                                BankAccountNrOwnerApplicantNr = 2
                            }
                        },
                        new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                        {
                            MonthlyFeeAmount = 20m,
                            NominalInterestRatePercent = 2.5m,
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
                            LoanAmount = OneMillion / 2m,
                            EndDate = support.Clock.Today.AddYears(40),
                            CreditNr = "L10002",
                            NextInterestRebindDate = support.Clock.Today.AddMonths(12),
                            InterestRebindMounthCount = 12,
                            ReferenceInterestRate = 0.2m,
                            ConsentingPartyCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            },
                            PropertyOwnerCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            }
                        }
                    }
            };

            loanRequest.AmortizationBasis = SwedishMortgageLoanAmortizationBasisService.CalculateSuggestedAmortizationBasis(
                new CalculateMortgageLoanAmortizationBasisRequest
                {
                    CombinedYearlyIncomeAmount = OneMillion * 0.70m,
                    ObjectValueAmount = 1.7m * loanRequest.Loans.Sum(x => x.LoanAmount ?? 0m),
                    OtherMortageLoansBalanceAmount = OneMillion * 0.15m,
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

            service.CreateLoans(loanRequest);
            return MortgageLoanAgreementNr;
        }

        #region "Debug print"
        /*
         Left this in hidden behind settings since being able to generate pdfs over and over again with arbitrary changes to the data
         is super useful when changing the pdfs which seems likely we will do more of. This is intended for use on localhost by a developer.

         Note that you need your localhost iis testing setup to work but it does not need to be the same client as the only thing we rely on
         is the pdf renderer in nDocument. The only important bit is prince vs weasy

         Add a normal appsetting file called appsettings.local.json to the project to use this. Example:
            {
              "isReminderDebugPrintEnabled": "false",
              "isNotificationDebugPrintEnabled": "false",
              "systemUserName": "<some username>",
              "systemUserPassword": "<some password>"
            }
         */
        private const string TestPdfPath = @"C:\Naktergal\EnklaMlDev\NaktergalTemp\DebugPrintDocuments";
        private static Lazy<bool> IsPrintDebugEnabled => new Lazy<bool>(() => TestConfiguration.Opt("isReminderDebugPrintEnabled") == "true" 
            || TestConfiguration.Opt("isNotificationDebugPrintEnabled") == "true" || TestConfiguration.Opt("isTerminationLetterDebugPrintEnabled") == "true");
        private static void SetupDocumentPrintDebugging()
        {
            if (!IsPrintDebugEnabled.Value) return;

            Directory.CreateDirectory(TestPdfPath);
            foreach (var file in Directory.GetFiles(TestPdfPath, "*.pdf"))
                File.Delete(file);
        }

        private static void OnDocumentPrinted(IDictionary<string, object> printContext, string filename)
        {
            if (!IsPrintDebugEnabled.Value) return;

            var isNotification = filename.ToLowerInvariant().Contains("notification");
            var isReminder = filename.ToLowerInvariant().Contains("reminder");
            var isTerminationLetter = filename.ToLowerInvariant().Contains("terminationletter");

            if (isNotification && TestConfiguration.Opt("isNotificationDebugPrintEnabled") == "true")
            {
                TestContext.WriteLine($"Notification({filename}):{Environment.NewLine}{JsonConvert.SerializeObject(printContext, Formatting.Indented)}");
                RenderDocument("mortgageloan-co-notification", printContext, filename);
            }

            if (isReminder && TestConfiguration.Opt("isReminderDebugPrintEnabled") == "true")
            {
                TestContext.WriteLine($"Reminder({filename}):{Environment.NewLine}{JsonConvert.SerializeObject(printContext, Formatting.Indented)}");
                RenderDocument("mortgageloan-co-reminder", printContext, filename);
            }
            
            if(isTerminationLetter && TestConfiguration.Opt("isTerminationLetterDebugPrintEnabled") == "true")
            {
                TestContext.WriteLine($"TerminationLetter({filename}):{Environment.NewLine}{JsonConvert.SerializeObject(printContext, Formatting.Indented)}");
                RenderDocument("mortgageloan-co-terminationletter", printContext, filename);
            }
        }

        private static void RenderDocument(string templateName, IDictionary<string, object> printContext, string filename)
        {
            var user = NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(() => new HttpClient(), new Uri("http://localhost:2635"),
                TestConfiguration.Req("systemUserName"), TestConfiguration.Req("systemUserPassword"));
            var documentClient = new DocumentClient(new NHttpServiceSystemUser(user), new ServiceClientFactory(x =>
            {
                if (x != "nDocument") throw new NotImplementedException();
                return new Uri("http://localhost:3412");
            }, () => new HttpClient(), new ServiceClientSyncConverterCore()));

            var pdfData = documentClient.PdfRenderDirect(GetPdfTemplate(templateName), printContext);
            File.WriteAllBytes(Path.Combine(TestPdfPath, filename), pdfData);
        }

        private static byte[] GetPdfTemplate(string templateName)
        {
            var path = Path.Combine(@"C:\projects\core-naktergal\SelfContainedModules\SharedClientResources\PdfTemplates\SE", templateName);
            var tempfile = Path.GetTempFileName();
            File.Delete(tempfile);
            try
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(path, tempfile);
                return File.ReadAllBytes(tempfile);
            }
            finally
            {
                File.Delete(tempfile);
            }
        }
        #endregion
    }
}
