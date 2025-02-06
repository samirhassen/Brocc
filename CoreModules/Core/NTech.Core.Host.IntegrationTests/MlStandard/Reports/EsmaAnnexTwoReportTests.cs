using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Reports
{
    public class EsmaAnnexTwoReportTests
    {
        [Test]
        public void TestReport()
        {
            const decimal LoanAmount = 1000000m;
            const decimal InitialFeeAmount = 1000m;
            const decimal NotificationFeeAmount = 20m;

            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var months = new List<(Action<int> DoBeforeDay, Action<List<EsmaAnnexTwoLoan>> Verify)>();
                void AddTestCase(Action<int> doBeforeDay, Action<List<EsmaAnnexTwoLoan>> verify) => months!.Add((doBeforeDay, verify));
                EsmaAnnexTwoLoan GetLoan(List<EsmaAnnexTwoLoan> loans, int nr) => loans.Single(x => x.CreditNr == CreditsMlStandard.GetCreatedCredit(support, nr).CreditNr);

                AddTestCase( //Month 1
                    dayNr =>
                    {
                        if (dayNr == 8)
                        {
                            //Add loan 1 (that will be settled)
                            CreditsMlStandard.CreateCredit(support, 1,
                                loanAmount: LoanAmount,
                                drawnFromLoanAmountInitialFees: InitialFeeAmount,
                                notificationFeeAmount: NotificationFeeAmount,
                                monthlyAmoritzationAmount: Math.Round(LoanAmount / 120m)); //Pay over ~10 years

                            //Add loan 3 (that will be have a revaluation done)
                            CreditsMlStandard.CreateCredit(support, 3,
                                loanAmount: LoanAmount,
                                drawnFromLoanAmountInitialFees: InitialFeeAmount,
                                notificationFeeAmount: NotificationFeeAmount);

                        }
                        else if(dayNr == 12)
                        {
                            //Add extra amortization to loan 1
                            var creditNr1 = CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr;
                            Credits.CreateAndPlaceUnplacedPayment(support, creditNr1, 500m);

                            //Revalue loan 3
                            var creditNr3 = CreditsMlStandard.GetCreatedCredit(support, 3).CreditNr;
                            var revaluationService = support.GetRequiredService<MlStandardSeRevaluationService>();
                            var revaluation = revaluationService.CalculateRevaluate(new MlStandardSeRevaluationCalculateRequest
                            {
                                CreditNr = creditNr3,
                                NewValuationAmount = LoanAmount * 5,
                                OtherMortageLoansAmount = 0m,
                                NewValuationDate = support.Clock.Today,
                                CurrentCombinedYearlyIncomeAmount = 50000 * 12 * 2m
                            });
                            revaluationService.CommitRevaluate(new MlStandardSeRevaluationCommitRequest
                            {                               
                                NewBasis = revaluation.MlStandardSeRevaluationCalculateResult.NewBasis                               
                            });
                        }
                    },
                    loans =>
                    {
                        Assert.That(loans.Count, Is.EqualTo(2));
                        
                        var loan1 = GetLoan(loans, 1);
                        var lastMonth = Month.ContainingDate(support.Clock.Today).PreviousMonth;
                        Assert.That(Month.ContainingDate(loan1.CreatedDate), Is.EqualTo(lastMonth));
                        Assert.That(loan1.ClosedDate, Is.Null);
                        Assert.That(loan1.ClosedStatus, Is.Null);
                        Assert.That(loan1.CurrentLastAmortizationPlanDueDate?.ToString("yyyy-MM-dd"), Is.EqualTo("2032-03-28"));
                        Assert.That(loan1.InitialRepaymentTimeInMonths, Is.EqualTo(120));
                        Assert.That(loan1.InitialCapitalDebt, Is.EqualTo(LoanAmount));
                        Assert.That(loan1.LatestExtraAmortizationDate, Is.EqualTo(lastMonth.GetDayDate(12)));
                        Assert.That(loan1.CollateralTypeCode, Is.EqualTo("seBrf"));
                        Assert.That(loan1.CollateralId, Is.GreaterThan(0));
                        Assert.That(loan1.InitialAmortizationBasisCollateralValue, Is.EqualTo(loan1.CurrentAmortizationBasisCollateralValue));
                        Assert.That(loan1.InitialAmortizationBasisCollateralValueDate, Is.EqualTo(loan1.CurrentAmortizationBasisCollateralValueDate));

                        var loan3 = GetLoan(loans, 3);
                        Assert.That(loan3.InitialAmortizationBasiLtvFraction, Is.GreaterThan(0));
                        Assert.That(loan3.InitialAmortizationBasisCollateralValue, Is.EqualTo(1250000m));
                        Assert.That(loan3.InitialAmortizationBasisCollateralValueDate, Is.EqualTo(new DateTime(2021, 9, 20)));
                        Assert.That(loan3.CurrentAmortizationBasisCollateralValue, Is.EqualTo(LoanAmount * 5));
                        Assert.That(loan3.CurrentAmortizationBasisCollateralValueDate, Is.EqualTo(lastMonth.GetDayDate(12)));

                    });

                AddTestCase( //Month 2
                    dayNr =>
                    {
                        if (dayNr == 8)
                        {
                            //Change collateral type and settle loan 1
                            var loan1 = CreditsMlStandard.GetCreatedCredit(support, 1);

                            CreditsMlStandard.ChangeCollateralStringItems(support, loan1.CollateralHeaderId!.Value, new Dictionary<string, string>
                            {
                                ["objectTypeCode"] = "seFastighet"
                            });

                            Credits.CreateAndPlaceUnplacedPayment(support, loan1.CreditNr, 50000000);
                            Credits.AssertIsSettled(support, loan1.CreditNr);

                            //Add loan 2
                            var credit = CreditsMlStandard.CreateCredit(support, 2,
                                loanAmount: LoanAmount,
                                drawnFromLoanAmountInitialFees: InitialFeeAmount,
                                notificationFeeAmount: NotificationFeeAmount,
                                loanOwnerName: "Owner 1",
                                marginInterestRatePercent: 0.25m,
                                referenceInterestRatePercent: ThreeMonthInterest,
                                interestRebindMounthCount: 3);

                            support.Context["stopPayingCreditNrs"] = new HashSet<string> { credit.CreditNr };
                        }
                    },
                    loans =>
                    {
                        Assert.That(loans.Count, Is.EqualTo(3));

                        var loan1 = GetLoan(loans, 1);
                        Assert.That(loan1.ClosedDate, Is.Not.Null);
                        Assert.That(loan1.ClosedStatus, Is.EqualTo("Settled"));

                        var loan2 = GetLoan(loans, 2);
                        Assert.That(loan2.LoanOwnerName, Is.EqualTo("Owner 1"));
                        Assert.That(loan2.CurrentMarginInterestRate, Is.EqualTo(0.25m));
                        Assert.That(loan2.CurrentReferenceInterestRate, Is.EqualTo(ThreeMonthInterest));
                        Assert.That(loan2.CurrentInterestRebindMonthCount, Is.EqualTo(3));
                        Assert.That(loan2.NextInterestRebindDate, Is.LessThan(support.Clock.Today.AddDays(90)));
                        Assert.That(loan1.CollateralTypeCode, Is.EqualTo("seFastighet"));
                    });

                AddTestCase( //Month 3
                    dayNr =>
                    {
                        if (dayNr == 10)
                        {
                            //Change the loan owner name of loan 2
                            var creditNr2 = CreditsMlStandard.GetCreatedCredit(support, 2).CreditNr;
                            var loanOwnerManagementService = support.GetRequiredService<MortageLoanOwnerManagementService>();
                            loanOwnerManagementService.EditOwner(new LoanOwnerManagementRequest
                            {
                                CreditNr = creditNr2,
                                LoanOwnerName = "Owner 2"
                            });

                            //Change to bound interest on loan 2
                            CreditsMlStandard.ScheduleTermChange(support, creditNr2, 24, 0.25m);
                        }
                    },
                    loans =>
                    {
                        var loan2 = GetLoan(loans, 2);
                        Assert.That(loan2.LoanOwnerName, Is.EqualTo("Owner 2"));
                        var lastMonth = Month.ContainingDate(support.Clock.Today).PreviousMonth;
                        Assert.That(loan2.LoanOwnerDate?.ToString("yyyy-MM-dd"), Is.EqualTo(lastMonth.GetDayDate(10).ToString("yyyy-MM-dd")));
                        Assert.That(loan2.LatestNotificationDueDate, Is.Not.Null);
                        Assert.That(Month.ContainingDate(loan2.NextFutureNotificationDueDate!.Value),
                            Is.EqualTo(Month.ContainingDate(loan2.LatestNotificationDueDate!.Value).NextMonth));
                        Assert.That(loan2.CurrentInterestRebindMonthCount, Is.EqualTo(24));
                        Assert.That(loan2.NextInterestRebindDate, Is.GreaterThan(support.Clock.Today.AddDays(600)));
                        Assert.That(loan2.CurrentReferenceInterestRate, Is.EqualTo(TwoYearInterest));
                        Assert.That(loan2.LatestTermChangeDate, Is.EqualTo(lastMonth.GetDayDate(11)));
                        Assert.That(loan2.LatestMissedDueDate, Is.EqualTo(lastMonth.GetDayDate(28)));
                        Assert.That(loan2.CurrentNrOfOverdueDays, Is.EqualTo(2));
                    });

                //Month 4, 5 and 6 (waiting for loan 2 to be sent to debt collection)
                AddTestCase(dayNr => { }, loans => { });
                AddTestCase(dayNr => { }, loans => { });
                AddTestCase(dayNr => { }, loans => { });

                AddTestCase( //Month 7
                    dayNr =>
                    {
                    },
                    loans =>
                    {
                        var loan2 = GetLoan(loans, 2);
                        Assert.That(loan2.ClosedStatus, Is.EqualTo("SentToDebtCollection"));
                        Assert.That(loan2.DebtCollectionExportCapitalAmount, Is.EqualTo(997916m));
                        Assert.That(loan2.TotalWrittenOffCapitalAmount, Is.EqualTo(loan2.DebtCollectionExportCapitalAmount));
                        Assert.That(loan2.TotalWrittenOffInterestAmount, Is.GreaterThan(0m));
                        Assert.That(loan2.TotalWrittenOffFeesAmount, Is.GreaterThan(0m));
                    });

                RunTest(support, months);
            });
        }

        private void RunTest(MlStandardTestRunner.TestSupport support, List<(Action<int> DoBeforeDay, Action<List<EsmaAnnexTwoLoan>> Verify)> months)
        {
            var reportService = support.GetRequiredService<AnnexTwoEsmaReportService>();
            SwedishMortgageLoanReportData.OverrideGraceDays = 0; //Just to make testing late payments easier. Otherwise we would need to wait an extra month.

            support.MoveToNextDayOfMonth(1);

            CreditsMlStandard.SetupFixedInterestRates(support, new Dictionary<int, decimal>
            {
                [3] = ThreeMonthInterest,
                [24] = TwoYearInterest
            });

            bool isSqlPrinted = false;
            //One extra month here since we act on the current month with DoBeforeDay but the assert is run on the 3d of the nextd month to test the result.
            for (var monthIndex = 0; monthIndex < (months.Count + 1); monthIndex++)
            {
                var stopPayingCreditNrs = support.Context.Opt("stopPayingCreditNrs") as HashSet<string>;

                CreditsMlStandard.RunOneMonth(support, beforeDay: dayNr =>
                {
                    if (monthIndex < months.Count)
                        months[monthIndex].DoBeforeDay(dayNr);

                    if (monthIndex > 0 && dayNr == 3)
                    {
                        //Grab the report for the previous month and check the effects.
                        var reportMonth = Month.ContainingDate(support.Clock.Today.AddMonths(-1));
                        var request = new FromDateToDateReportRequest { FromDate = reportMonth.FirstDate, ToDate = reportMonth.LastDate };
                        var loans = reportService.GetAnnexTwoReportData(request, observeSqlQuery: sql => 
                        {
                            if(!isSqlPrinted)
                            {
                                TestContext.WriteLine(sql);
                                isSqlPrinted = true;
                            }
                        }).Loans;
                        try
                        {
                            months[monthIndex - 1].Verify(loans);
                        }
                        catch
                        {
                            TestContext.WriteLine($"Month {monthIndex}. fromDate = {request.FromDate:yyyy-MM-dd} toDate = {request.ToDate:yyyy-MM-dd}");
                            throw;
                        }
                    }
                }, 
                payNotificationsOnDueDate: true,
                skipNotificationPaymentsOnTheseCreditNrs: stopPayingCreditNrs);
            }
        }

        private const decimal ThreeMonthInterest = 4m;
        private const decimal TwoYearInterest = 6m;
    }
}