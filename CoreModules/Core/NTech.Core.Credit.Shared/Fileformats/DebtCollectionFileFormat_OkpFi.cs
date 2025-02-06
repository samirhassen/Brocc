using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Banking.LoanModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Fileformats
{
    public class DebtCollectionFileFormat_OkpFi
    {
        private T[] SkipNulls<T>(params T[] args) where T : class
        {
            if (args == null) return null;
            return args.Where(x => x != null).ToArray();
        }

        private class DebtColCustomer
        {
            public DebtCollectionFileModel.Customer Customer { get; set; }
            public int ApplicantNr { get; set; }
        }

        private class OkpFiCreditModel
        {
            public string LoanNumber { get; set; }
            public OkpFiCustomerModel MainApplicant { get; set; }
            public OkpFiCustomerModel CoApplicant { get; set; }
            public decimal Capital { get; set; }
            public DateTime LoanStartDate { get; set; }
            public DateTime DueDate1 { get; set; } //Due date
            public string InvoiceNumber { get; set; }
            public string Description { get; set; }
            public string ReferenceNumber { get; set; }
            public decimal InterestNominal { get; set; }
            public DateTime TerminationDate { get; set; }
            public decimal OriginalPaidAmount { get; set; }
            public decimal? InterestEffective { get; set; }
            public decimal OpeningFee { get; set; }
            public DateTime InvoiceDate { get; set; }
            public DateTime DueDate2 { get; set; } //Due date
            public decimal UnpaidInterest { get; set; }
            public DateTime? InterestDate { get; set; }
            public DateTime? InterestDueDate { get; set; }
            public decimal ReminderFee { get; set; }
            public DateTime? ReminderFeeDate { get; set; }
            public DateTime? ReminderFeeDueDate { get; set; }
            public decimal MonthlyFee { get; set; }
            public DateTime? MonthlyFeeDate { get; set; }
            public DateTime? MonthlyFeeDueDate { get; set; }
            public string FraudDeadMarker { get; set; }
            public string CampaignCode { get; set; }
        }

        private class OkpFiCustomerModel
        {
            public string FirstNames { get; set; }
            public string Surname { get; set; }
            public string Address { get; set; }
            public string Zipcode { get; set; }
            public string City { get; set; }
            public string SSN { get; set; }
            public string MobilePhoneNr { get; set; }
            public string Email { get; set; }
        }

        public static IDictionary<string, decimal> GetInitialEffectiveInterstRateForCredits(ISet<string> creditNrs, ICreditContextExtended context, ICreditEnvSettings envSettings)
        {
            var credits = context.CreditHeadersQueryable.Where(x => creditNrs.Contains(x.CreditNr)).Select(x => new
            {
                x.CreditNr,
                NewCreditCapitalAmount = x
                    .Transactions
                    .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString())
                    .Sum(y => (decimal?)y.Amount) ?? 0m,
                CapitalizedInitialFeeAmount = x
                    .Transactions
                    .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BusinessEvent.EventType == BusinessEventType.CapitalizedInitialFee.ToString())
                    .Sum(y => (decimal?)y.Amount) ?? 0m,
                InitialAnnuityAmount = x
                    .DatedCreditValues
                    .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString() && y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString())
                    .Select(y => (decimal?)y.Value)
                    .FirstOrDefault(),
                InitialNotificationFeeAmount = x
                    .DatedCreditValues
                    .Where(y => y.Name == DatedCreditValueCode.NotificationFee.ToString() && y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString())
                    .Select(y => (decimal?)y.Value)
                    .FirstOrDefault(),
                InitialMarginInterestRatePercent = x
                    .DatedCreditValues
                    .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString() && y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString())
                    .Select(y => (decimal?)y.Value)
                    .FirstOrDefault(),
                InitialReferenceInterestRatePercent = x
                    .DatedCreditValues
                    .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString() && y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString())
                    .Select(y => (decimal?)y.Value)
                    .FirstOrDefault(),
            }).ToList();

            var result = new Dictionary<string, decimal>();
            foreach (var c in credits)
            {
                var t = PaymentPlanCalculation
                    .BeginCreateWithAnnuity(c.NewCreditCapitalAmount, c.InitialAnnuityAmount ?? 0m, (c.InitialMarginInterestRatePercent ?? 0m) + (c.InitialReferenceInterestRatePercent ?? 0m), null, envSettings.CreditsUse360DayInterestYear)
                    .WithInitialFeeCapitalized(c.CapitalizedInitialFeeAmount)
                    .WithMonthlyFee(c.InitialNotificationFeeAmount ?? 0m)
                    .EndCreate();
                if (t.EffectiveInterestRatePercent.HasValue)
                    result[c.CreditNr] = t.EffectiveInterestRatePercent.Value;
            }
            return result;
        }

        public string CreateExcelFileInArchive(DebtCollectionFileModel f, DateTimeOffset now, string filename, IDictionary<string, decimal> initialEffectiveInterestRateByCreditNr,
            IDictionary<string, decimal> notNotifiedInterestAmountUntilTerminationLetterDueDate, IDocumentClient documentClient, PaymentOrderService paymentOrderService)
        {
            //Need to figure out how to add these other costs to their format. Can we just add more columns?
            if (paymentOrderService.HasCustomCosts())
                throw new NotImplementedException();

            Func<DebtCollectionFileModel.Customer, OkpFiCustomerModel> transformCustomer = x => new OkpFiCustomerModel
            {
                SSN = x.CivicRegNrOrOrgnr,
                FirstNames = x.FirstName,
                Surname = x.LastName,
                Address = x.Adr?.Street,
                City = x.Adr?.City,
                Zipcode = x.Adr?.Zipcode,
                MobilePhoneNr = x.Phone,
                Email = x.Email
            };

            var capitalUniqueId = PaymentOrderItem.GetUniqueId(CreditDomainModel.AmountType.Capital);
            var interestUniqueId = PaymentOrderItem.GetUniqueId(CreditDomainModel.AmountType.Interest);
            var notificationFeeUniqueId = PaymentOrderItem.GetUniqueId(CreditDomainModel.AmountType.NotificationFee);
            var reminderFeeUniqueId = PaymentOrderItem.GetUniqueId(CreditDomainModel.AmountType.ReminderFee);

            var okpCredits = f.Credits.Select(x =>
            {
                var capitalDebt =
                    x.NotNotifiedCapitalAmount
                    + x.Notifications
                        .SelectMany(y => y.Amounts.Where(z => z.Key == capitalUniqueId))
                        .Sum(y => y.Value);

                var interestDebt = (notNotifiedInterestAmountUntilTerminationLetterDueDate.OptS(x.CreditNr) ?? 0m) + x.Notifications
                        .SelectMany(y => y.Amounts.Where(z => z.Key == interestUniqueId))
                        .Sum(y => y.Value);

                var ma = x.OrderedCustomersWithRoles.Single(y => x.ApplicantNrByCustomerId[y.Item1.CustomerId] == 1)?.Item1;
                var ca = x.OrderedCustomersWithRoles.SingleOrDefault(y => x.ApplicantNrByCustomerId[y.Item1.CustomerId] == 2)?.Item1;

                var paidOrWrittenOffAmount = x.NewCreditCapitalAmount + x.AdditionalLoanCapitalAmount + x.CapitalizedInitialFeeAmount - capitalDebt;

                //The client wants to pretend that opening fee is not capitalized when sending to debt collection which does of course not work since
                //all other calculations are based on this being part of the capital but this is the way they want it.
                var fakePaidCapitalizedFeeAmount = Math.Min(x.CapitalizedInitialFeeAmount, paidOrWrittenOffAmount);
                var fakeCurrentCapitalizedInitialFeeAmount = x.CapitalizedInitialFeeAmount - fakePaidCapitalizedFeeAmount;
                var fakeCurrentCapitalBalance = capitalDebt - fakeCurrentCapitalizedInitialFeeAmount;

                var oldestNotificationWithUnpaidNotificationFee = x
                    .Notifications
                    .OrderBy(y => y.DueDate)
                    .Where(y => (y.Amounts.OptS(notificationFeeUniqueId) ?? 0m) > 0)
                    .FirstOrDefault();
                var oldestNotificationWithUnpaidReminderFee = x
                    .Notifications
                    .OrderBy(y => y.DueDate)
                    .Where(y => (y.Amounts.OptS(reminderFeeUniqueId) ?? 0m) > 0)
                    .FirstOrDefault();

                return new OkpFiCreditModel
                {
                    LoanNumber = x.CreditNr,
                    MainApplicant = transformCustomer(ma),
                    CoApplicant = ca == null ? null : transformCustomer(ca),
                    Capital = fakeCurrentCapitalBalance,
                    LoanStartDate = x.StartDate,
                    DueDate1 = x.TerminationLetterDueDate,
                    InvoiceNumber = x.CreditNr,
                    Description = "Vakuudeton kertaluotto",
                    ReferenceNumber = x.Ocr,
                    InterestNominal = x.InterestRatePercent,
                    TerminationDate = x.TerminationLetterDueDate,
                    OriginalPaidAmount = x.NewCreditCapitalAmount + x.AdditionalLoanCapitalAmount, //The client wants additional loans to be included here. Not entirely clear why.
                    InterestEffective = initialEffectiveInterestRateByCreditNr.OptS(x.CreditNr), //This value basically has no meaning when there are additional loans but the client want it anyway
                    OpeningFee = fakeCurrentCapitalizedInitialFeeAmount,
                    InvoiceDate = x.StartDate,
                    DueDate2 = x.TerminationLetterDueDate,
                    UnpaidInterest = interestDebt,
                    InterestDate = x.StartDate,
                    InterestDueDate = x.TerminationLetterDueDate,

                    ReminderFee = x.Notifications.Sum(y => y.Amounts.OptS(reminderFeeUniqueId) ?? 0m),
                    ReminderFeeDate = oldestNotificationWithUnpaidReminderFee?.NotificationDate,
                    ReminderFeeDueDate = oldestNotificationWithUnpaidReminderFee?.DueDate,

                    MonthlyFee = x.Notifications.Sum(y => y.Amounts.OptS(notificationFeeUniqueId) ?? 0m),
                    MonthlyFeeDate = oldestNotificationWithUnpaidNotificationFee?.NotificationDate,
                    MonthlyFeeDueDate = oldestNotificationWithUnpaidNotificationFee?.DueDate,

                    FraudDeadMarker = null,
                    CampaignCode = x.InitialLoanCampaignCode
                };
            }).ToList();

            var request = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"Okp export ({now.ToString("yyyy-MM-dd")})"
                    }
                }
            };

            var s = request.Sheets[0];

            s.SetColumnsAndData(okpCredits,
                okpCredits.Col(x => x.LoanNumber, ExcelType.Text, "Loan number"),

                okpCredits.Col(x => x.MainApplicant?.FirstNames, ExcelType.Text, "Main applicant First Names"),
                okpCredits.Col(x => x.MainApplicant?.Surname, ExcelType.Text, "Main applicant Surname"),
                okpCredits.Col(x => x.MainApplicant?.Address, ExcelType.Text, "Main applicant Address"),
                okpCredits.Col(x => x.MainApplicant?.Zipcode, ExcelType.Text, "Main applicant Zipcode"),
                okpCredits.Col(x => x.MainApplicant?.City, ExcelType.Text, "Main applicant City"),
                okpCredits.Col(x => x.MainApplicant?.SSN, ExcelType.Text, "Main applicant SSN"),
                okpCredits.Col(x => x.MainApplicant?.MobilePhoneNr, ExcelType.Text, "Main applicant Mobile number"),
                okpCredits.Col(x => x.MainApplicant?.Email, ExcelType.Text, "Main applicant E-mail"),

                okpCredits.Col(x => x.CoApplicant?.FirstNames, ExcelType.Text, "Coapplicant First Names"),
                okpCredits.Col(x => x.CoApplicant?.Surname, ExcelType.Text, "Coapplicant Surname"),
                okpCredits.Col(x => x.CoApplicant?.Address, ExcelType.Text, "Coapplicant Address"),
                okpCredits.Col(x => x.CoApplicant?.Zipcode, ExcelType.Text, "Coapplicant Zipcode"),
                okpCredits.Col(x => x.CoApplicant?.City, ExcelType.Text, "Coapplicant City"),
                okpCredits.Col(x => x.CoApplicant?.SSN, ExcelType.Text, "Coapplicant SSN"),
                okpCredits.Col(x => x.CoApplicant?.MobilePhoneNr, ExcelType.Text, "Coapplicant Mobile number"),
                okpCredits.Col(x => x.CoApplicant?.Email, ExcelType.Text, "Coapplicant E-mail"),

                okpCredits.Col(x => x.Capital, ExcelType.Number, "Capital"),
                okpCredits.Col(x => x.LoanStartDate, ExcelType.Date, "Loan start date"),
                okpCredits.Col(x => x.DueDate1, ExcelType.Date, "Due date"),
                okpCredits.Col(x => x.InvoiceNumber, ExcelType.Text, "Invoice number"),
                okpCredits.Col(x => x.Description, ExcelType.Text, "Description"),
                okpCredits.Col(x => x.ReferenceNumber, ExcelType.Text, "Reference number	"),
                okpCredits.Col(x => x.InterestNominal / 100m, ExcelType.Percent, "Interest (nominal)"), //TODO: Verify percent vs number
                okpCredits.Col(x => x.TerminationDate, ExcelType.Date, "Termination date"),
                okpCredits.Col(x => x.OriginalPaidAmount, ExcelType.Number, "Original paid amount"),
                okpCredits.Col(x => x.InterestEffective / 100m, ExcelType.Percent, "Interest (effective)"), //TODO: Verify percent vs number
                okpCredits.Col(x => x.OpeningFee, ExcelType.Number, "Opening fee"),
                okpCredits.Col(x => x.InvoiceDate, ExcelType.Date, "Invoice date"),
                okpCredits.Col(x => x.DueDate2, ExcelType.Date, "Due date"),

                okpCredits.Col(x => x.UnpaidInterest, ExcelType.Number, "Unpaid interest"),
                okpCredits.Col(x => x.InterestDate, ExcelType.Date, "Interest date"),
                okpCredits.Col(x => x.InterestDueDate, ExcelType.Date, "Interest Due date"),

                okpCredits.Col(x => x.ReminderFee, ExcelType.Number, "Reminder fee"),
                okpCredits.Col(x => x.ReminderFeeDate, ExcelType.Date, "Reminder date"),
                okpCredits.Col(x => x.ReminderFeeDueDate, ExcelType.Date, "Reminder due date"),

                okpCredits.Col(x => x.MonthlyFee, ExcelType.Number, "Monthly fee"),
                okpCredits.Col(x => x.MonthlyFeeDate, ExcelType.Date, "Monthly fee date"),
                okpCredits.Col(x => x.MonthlyFeeDueDate, ExcelType.Date, "Monthly fee Due date"),

                okpCredits.Col(x => x.FraudDeadMarker, ExcelType.Text, "Fraud, Dead"),
                okpCredits.Col(x => x.CampaignCode, ExcelType.Text, "Campaign code")
                );

            return documentClient.CreateXlsxToArchive(request, filename);
        }
    }
}