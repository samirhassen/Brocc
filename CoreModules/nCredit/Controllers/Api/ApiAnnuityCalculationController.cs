using nCredit.Code;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Banking.Conversion;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiAnnuityCalculationController : NController
    {
        //Intentionally allow get or post
        [Route("Api/Credit/AnnuityCalculationDetailsExcel")]
        public ActionResult GetAnnuityCalculation(
            decimal? loanAmount, decimal? yearlyInterestRateInPercent,
            int? repaymentTimeInMonths, decimal? annuityAmount,
            decimal? monthlyFee,
            decimal? capitalizedInitialFee,
            decimal? initialFeeDrawnFromLoanAmount,
            decimal? initialFeePaidOnFirstNotification,
            decimal? fixedMonthlyCapitalAmount,
            string interestModelCode)
        {
            if (!loanAmount.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing loanAmount");
            if (!yearlyInterestRateInPercent.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing yearlyInterestRateInPercent");

            InterestModelCode interestModel;
            if (!string.IsNullOrWhiteSpace(interestModelCode))
            {
                interestModel = Enums.Parse<InterestModelCode>(interestModelCode) ?? NEnv.ClientInterestModel;
            }
            else
                interestModel = NEnv.ClientInterestModel;

            PaymentPlanCalculation.PaymentPlanCalculationBuilder b = null;
            var counts = 0;
            if (repaymentTimeInMonths.HasValue)
            {
                b = PaymentPlanCalculation.BeginCreateWithRepaymentTime(loanAmount.Value, repaymentTimeInMonths.Value, yearlyInterestRateInPercent.Value, true, null, interestModel == InterestModelCode.Actual_360);
                counts++;
            }
            if (annuityAmount.HasValue)
            {
                b = PaymentPlanCalculation.BeginCreateWithAnnuity(loanAmount.Value, annuityAmount.Value, yearlyInterestRateInPercent.Value, null, interestModel == InterestModelCode.Actual_360);
                counts++;
            }

            if (fixedMonthlyCapitalAmount.HasValue)
            {
                b = PaymentPlanCalculation.BeginCreateWithFixedMonthlyCapitalAmount(loanAmount.Value, fixedMonthlyCapitalAmount.Value, yearlyInterestRateInPercent.Value, null, null, interestModel == InterestModelCode.Actual_360);
                counts++;
            }

            if (counts != 1)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Must specify exactly one of repaymentTimeInMonths, monthlyAnnuityAmount or fixedMonthlyCapitalAmount");

            if (monthlyFee.HasValue)
                b = b.WithMonthlyFee(monthlyFee.Value);

            if (capitalizedInitialFee.HasValue)
                b = b.WithInitialFeeCapitalized(capitalizedInitialFee.Value);

            if (initialFeeDrawnFromLoanAmount.HasValue)
                b = b.WithInitialFeeDrawnFromLoanAmount(initialFeeDrawnFromLoanAmount.Value);

            if (initialFeePaidOnFirstNotification.HasValue)
                b = b.WithInitialFeePaidOnFirstNotification(initialFeePaidOnFirstNotification.Value);

            List<Tuple<decimal, decimal>> effRateLoans = null;
            List<Tuple<decimal, decimal>> effRatePayments = null;
            decimal? effRatePercentExact = null;
            Action<List<Tuple<decimal, decimal>>, List<Tuple<decimal, decimal>>, decimal> observeEffRateLoansAndPayments = (loans, payments, r) =>
            {
                effRateLoans = loans;
                effRatePayments = payments;
                effRatePercentExact = r;
            };
            var c = b
                .WithEffectiveInterestCalculationDetailsObserver(observeEffRateLoansAndPayments)
                .EndCreate();

            var effectiveInterestRatePercent = c.EffectiveInterestRatePercent;

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            //Tab 1 - Overview
            var overviewItems = new List<Tuple<string, decimal?>>();
            overviewItems.Add(Tuple.Create("LoanAmount", (decimal?)b.Terms.LoanAmount));
            overviewItems.Add(Tuple.Create("YearlyInterestRateAsPercent", (decimal?)b.Terms.YearlyInterestRateAsPercent));
            overviewItems.Add(Tuple.Create("MonthlyFee", (decimal?)b.Terms.MonthlyFee));
            overviewItems.Add(Tuple.Create("InitialFeeCapitalized", (decimal?)b.Terms.InitialFeeCapitalized));
            overviewItems.Add(Tuple.Create("InitialFeeDrawnFromLoanAmount", (decimal?)b.Terms.InitialFeeDrawnFromLoanAmount));
            overviewItems.Add(Tuple.Create("initialFeePaidOnFirstNotification", (decimal?)b.Terms.InitialFeePaidOnFirstNotification));
            overviewItems.Add(Tuple.Create("RepaymentTimeInMonths", (decimal?)c.Payments.Count));

            if (c.UsesAnnuities)
                overviewItems.Add(Tuple.Create("AnnuityAmount" + (interestModel == InterestModelCode.Actual_360 ? " (I360)" : ""), (decimal?)c.AnnuityAmount));
            else
                overviewItems.Add(Tuple.Create("FixedMonthlyCapitalAmount", (decimal?)c.FixedMonthlyCapitalAmount));

            overviewItems.Add(Tuple.Create("EffectiveInterestRatePercent", (decimal?)c.EffectiveInterestRatePercent));
            overviewItems.Add(Tuple.Create("TotalPaidAmount", (decimal?)c.TotalPaidAmount));
            overviewItems.Add(Tuple.Create("InitialCapitalDebtAmount", (decimal?)c.InitialCapitalDebtAmount));
            overviewItems.Add(Tuple.Create("InitialPaidToCustomerAmount", (decimal?)c.InitialPaidToCustomerAmount));
            var sheet1 = new DocumentClientExcelRequest.Sheet
            {
                Title = $"Overview",
                AutoSizeColumns = true
            };
            sheet1.SetColumnsAndData(overviewItems,
                overviewItems.Col(x => x.Item1, ExcelType.Text, "Item"),
                overviewItems.Col(x => x.Item2, ExcelType.Number, "Value"));
            sheets.Add(sheet1);

            //Tab 2 - Amortization plan
            var sheet2 = new DocumentClientExcelRequest.Sheet
            {
                Title = $"Amort. plan",
                AutoSizeColumns = true
            };
            var pi = c.Payments.Select((x, i) => new
            {
                Month = i + 1,
                x.Capital,
                x.Interest,
                x.MonthlyFee,
                x.InitialFee,
                x.TotalAmount
            }).ToList();
            sheet2.SetColumnsAndData(pi,
                pi.Col(x => x.Month, ExcelType.Number, "Month", nrOfDecimals: 0, includeSum: false),
                pi.Col(x => x.Capital, ExcelType.Number, "Capital", includeSum: true),
                pi.Col(x => x.Interest, ExcelType.Number, "Interest", includeSum: true),
                pi.Col(x => x.MonthlyFee, ExcelType.Number, "MonthlyFee", includeSum: true),
                pi.Col(x => x.InitialFee, ExcelType.Number, "InitialFee", includeSum: true),
                pi.Col(x => x.TotalAmount, ExcelType.Number, "TotalAmount", includeSum: true));
            sheets.Add(sheet2);

            //Tab 3 - Effective interest calculation details
            if (effRatePercentExact.HasValue && effRateLoans != null && effRatePayments != null)
            {
                var sheet3 = new DocumentClientExcelRequest.Sheet
                {
                    Title = $"Eff. int. details",
                    AutoSizeColumns = true
                };
                var r = effRatePercentExact.Value / 100m;
                var items = effRateLoans.Select(x => new
                {
                    IsLoan = true,
                    EffRatePercentExact = effRatePercentExact,
                    Month = x.Item2 * 12m,
                    R = r,
                    Amount = x.Item1,
                    Time = x.Item2,
                    NowAmount = ((decimal)Math.Pow(1d + (double)r, -(double)x.Item2)) * x.Item1
                }).Concat(effRatePayments.Select(x => new
                {
                    IsLoan = false,
                    EffRatePercentExact = effRatePercentExact,
                    Month = x.Item2 * 12m,
                    R = r,
                    Amount = -x.Item1,
                    Time = x.Item2,
                    NowAmount = -((decimal)Math.Pow(1d + (double)r, -(double)x.Item2)) * x.Item1
                }))
                .OrderBy(x => x.Time)
                .ThenBy(x => x.IsLoan ? 0 : 1)
                .ToList();

                sheet3.SetColumnsAndData(items,
                    items.Col(x => x.IsLoan ? "Loan" : "Payment", ExcelType.Text, "Type"),
                    items.Col(x => x.Month, ExcelType.Number, "Month"),
                    items.Col(x => x.Amount, ExcelType.Number, "Amount", includeSum: true),
                    items.Col(x => x.Time, ExcelType.Number, "T"),
                    items.Col(x => x.R, ExcelType.Number, "R", nrOfDecimals: 4),
                    items.Col(x => string.Format("POWER(1+{0},-{1})*{2}"
                        , "OFFSET([[CELL]],0,-1,1,1)" //R
                        , "OFFSET([[CELL]],0,-2,1,1)" //T
                        , "OFFSET([[CELL]],0,-3,1,1)" //Amount
                    ), ExcelType.NumberFormula, "NowAmount", includeSum: true));

                sheets.Add(sheet3);
            }

            var er = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var dc = Service.DocumentClientHttpContext;
            var excelStream = dc.CreateXlsx(er);
            var f = new FileStreamResult(excelStream, XlsxContentType);
            f.FileDownloadName = "AnnuityCalculation.xlsx";
            return f;
        }

        //Intentionally allow get or post
        [Route("Api/Credit/ClientPaymentPlanDetailsExcel")]
        public ActionResult GetClientAmortizationPlan(
            decimal? loanAmount,
            decimal? yearlyInterestRateInPercent,
            decimal? monthlyFee,
            decimal? capitalizedInitialFee,
            decimal? annuityOrFixedMonthlyCapitalAmount,
            DateTime? loanCreationDate,
            int? dueDay,
            string interestModelCode)
        {
            if (!loanAmount.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing loanAmount");
            if (!yearlyInterestRateInPercent.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing yearlyInterestRateInPercent");

            if (!annuityOrFixedMonthlyCapitalAmount.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing annuityOrFixedMonthlyCapitalAmount");

            var creditType = (NEnv.IsMortgageLoansEnabled
                    ? CreditType.MortgageLoan : (NEnv.IsCompanyLoansEnabled ? CreditType.CompanyLoan : CreditType.UnsecuredLoan));

            var notificationDueDay = NEnv.HasPerLoanDueDay ? (dueDay ?? 28) : new int?();

            InterestModelCode interestModel;
            if (!string.IsNullOrWhiteSpace(interestModelCode))
            {
                interestModel = Enums.Parse<InterestModelCode>(interestModelCode) ?? NEnv.ClientInterestModel;
            }
            else
                interestModel = NEnv.ClientInterestModel;


            var notificationSettings = NEnv.NotificationProcessSettings.GetByCreditType(creditType);
            var singlePaymentLoanRepaymentDays = new int?();
            List<Tuple<DateTime, decimal>> dailyInterestAmounts = new List<Tuple<DateTime, decimal>>();
            if (!TryGetClientAmortizationPlan(
                loanAmount, yearlyInterestRateInPercent, monthlyFee, capitalizedInitialFee,
                annuityOrFixedMonthlyCapitalAmount, singlePaymentLoanRepaymentDays, loanCreationDate, notificationDueDay, interestModel, creditType,
                new CoreClock(), NEnv.ClientCfgCore, notificationSettings,
                out var failedMessage, out var p,
                observeDailyInterestAmount: (d, a) => dailyInterestAmounts.Add(Tuple.Create(d, a))
                ))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            var am = p.AmortizationModel;

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            //Tab 1 - Overview
            var overviewItems = new List<Tuple<string, decimal?>>();
            overviewItems.Add(Tuple.Create("LoanAmount", loanAmount));
            overviewItems.Add(Tuple.Create("YearlyInterestRateAsPercent", yearlyInterestRateInPercent));
            overviewItems.Add(Tuple.Create("MonthlyFee", monthlyFee));
            overviewItems.Add(Tuple.Create("InitialFeeCapitalized", capitalizedInitialFee));
            overviewItems.Add(Tuple.Create("RepaymentTimeInMonths", (decimal?)p.NrOfRemainingPayments));
            overviewItems.Add(Tuple.Create(
                $"{(am.UsesAnnuities ? "AnnuityAmount" : "FixedMonthlyCapitalAmount")}",
                new decimal?(am.UsesAnnuities ? am.GetActualAnnuityOrException() : am.GetActualFixedMonthlyPaymentOrException())));
            var sheet1 = new DocumentClientExcelRequest.Sheet
            {
                Title = $"Overview",
                AutoSizeColumns = true
            };
            sheet1.SetColumnsAndData(overviewItems,
                overviewItems.Col(x => x.Item1, ExcelType.Text, "Item"),
                overviewItems.Col(x => x.Item2, ExcelType.Number, "Value"));
            sheets.Add(sheet1);

            //Tab 2 - Amortization plan
            var sheet2 = new DocumentClientExcelRequest.Sheet
            {
                Title = $"Amort. plan",
                AutoSizeColumns = true
            };
            sheet2.SetColumnsAndData(p.Items,
                p.Items.Col(x => x.EventTransactionDate, ExcelType.Date, "Date"),
                p.Items.Col(x => x.FutureItemDueDate, ExcelType.Date, "Due Date"),
                p.Items.Col(x => x.CapitalTransaction, ExcelType.Number, "Capital", includeSum: true),
                p.Items.Col(x => x.InterestTransaction, ExcelType.Number, "Interest", includeSum: true),
                p.Items.Col(x => x.CapitalBefore, ExcelType.Number, "Capital before"));
            sheets.Add(sheet2);

            //Tab 3 - Daily interest amounts
            var sheet3 = new DocumentClientExcelRequest.Sheet
            {
                Title = $"I - {interestModel}",
                AutoSizeColumns = true
            };
            sheet3.SetColumnsAndData(dailyInterestAmounts,
                dailyInterestAmounts.Col(x => x.Item1, ExcelType.Date, "Date"),
                dailyInterestAmounts.Col(x => x.Item2, ExcelType.Number, "Amount", includeSum: true));
            sheets.Add(sheet3);

            var er = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var dc = Service.DocumentClientHttpContext;
            var excelStream = dc.CreateXlsx(er);
            var f = new FileStreamResult(excelStream, XlsxContentType);
            f.FileDownloadName = "AmortizationPlan.xlsx";
            return f;
        }

        public static bool TryGetClientAmortizationPlan(
            decimal? loanAmount,
            decimal? yearlyInterestRateInPercent,
            decimal? monthlyFee,
            decimal? capitalizedInitialFee,
            decimal? annuityOrFixedMonthlyCapitalAmount,
            int? singlePaymentLoanRepaymentDays,
            DateTime? loanCreationDate,
            int? dueDay,
            InterestModelCode interestModel,
            CreditType creditType,
            ICoreClock clock,
            IClientConfigurationCore clientConfiguration,
            NotificationProcessSettings notificationProcessSettings,
            out string failedMessage,
            out AmortizationPlan paymentplan,
            Action<DateTime, decimal> observeDailyInterestAmount = null)
        {
            if (!loanAmount.HasValue)
            {
                failedMessage = "Missing loanAmount";
                paymentplan = null;
                return false;
            }

            if (!yearlyInterestRateInPercent.HasValue)
            {
                failedMessage = "Missing yearlyInterestRateInPercent";
                paymentplan = null;
                return false;
            }

            if (!annuityOrFixedMonthlyCapitalAmount.HasValue)
            {
                failedMessage = "Missing annuityOrFixedMonthlyCapitalAmount";
                paymentplan = null;
                return false;
            }

            var am = (creditType == CreditType.MortgageLoan && clientConfiguration.Country.BaseCountry != "FI")
                ? CreditAmortizationModel.CreateMonthlyFixedCapitalAmount(annuityOrFixedMonthlyCapitalAmount.Value, null, null, null)
                : CreditAmortizationModel.CreateAnnuity(annuityOrFixedMonthlyCapitalAmount.Value, null);
            var hm = new HistoricalCreditModel
            {
                AmortizationModel = am,
                CreatedByEvent = new HistoricalCreditModel.ModelBusinessEvent
                {
                    TransactionDate = loanCreationDate ?? clock.Today,
                    EventType = "New loan"
                },
                CreditType = creditType.ToString(),
                MarginInterestRatePercent = yearlyInterestRateInPercent.Value,
                ReferenceInterestRatePercent = 0m,
                NotificationDueDay = dueDay,
                NotificationFee = monthlyFee ?? 0m,
                NrOfPaidNotifications = 0,
                PendingFuturePaymentFreeMonths = new List<HistoricalCreditModel.PendingFuturePaymentFreeMonthModel>(),
                Status = CreditStatus.Normal.ToString(),
                SinglePaymentLoanRepaymentDays = singlePaymentLoanRepaymentDays
            };
            hm.IsMortgageLoan = hm.CreditType == CreditType.MortgageLoan.ToString();
            hm.Transactions = new List<HistoricalCreditModel.ModelTransaction>();
            Action<decimal?> addInitialCapital = amt =>
            {
                if (!amt.HasValue)
                    return;

                hm.Transactions.Add(new HistoricalCreditModel.ModelTransaction
                {
                    AccountCode = TransactionAccountType.CapitalDebt.ToString(),
                    Amount = amt.Value,
                    BusinessEvent = hm.CreatedByEvent
                });
                hm.Transactions.Add(new HistoricalCreditModel.ModelTransaction
                {
                    AccountCode = TransactionAccountType.NotNotifiedCapital.ToString(),
                    Amount = amt.Value,
                    BusinessEvent = hm.CreatedByEvent
                });
            };

            addInitialCapital(loanAmount);
            addInitialCapital(capitalizedInitialFee);

            List<Tuple<DateTime, decimal>> dailyInterestAmounts = new List<Tuple<DateTime, decimal>>();
            if (!AmortizationPlan.TryGetAmortizationPlan(hm, notificationProcessSettings,
                out var p, out var fm, clock, clientConfiguration, CreditDomainModel.GetInterestDividerOverrideByCode(interestModel),
                observeDailyInterestAmount: observeDailyInterestAmount))
            {
                failedMessage = fm;
                paymentplan = null;
                return false;
            }

            paymentplan = p;
            failedMessage = null;
            return true;
        }
    }
}