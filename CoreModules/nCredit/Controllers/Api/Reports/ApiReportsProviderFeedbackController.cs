using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiReportsProviderFeedbackController : NController
    {

        [Route("Api/Reports/GetProviderFeedback")]
        [HttpGet()]
        public ActionResult Get(DateTime date)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                using (var context = new CreditContext())
                {
                    var d = date.Date;

                    var events = context
                        .OutgoingPaymentHeaders
                        .Where(x => x.CreatedByEvent.EventType == BusinessEventType.NewCredit.ToString() || x.CreatedByEvent.EventType == BusinessEventType.NewAdditionalLoan.ToString())
                        .Select(x => new
                        {
                            x.CreatedByEvent,
                            OutgoingPaymentFile = x.OutgoingPaymentFile,
                            Credit = x
                                .Transactions
                                .Where(y => y.BusinessEventId == x.CreatedByBusinessEventId && y.CreditNr != null)
                                .Select(y => y.Credit)
                                .FirstOrDefault(),
                            ApplicationNr = x
                                .Items
                                .Where(y => y.Name == OutgoingPaymentHeaderItemCode.ApplicationNr.ToString() && !y.IsEncrypted)
                                .OrderByDescending(y => y.Timestamp)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            ApplicationProviderName = x
                                .Items
                                .Where(y => y.Name == OutgoingPaymentHeaderItemCode.ApplicationProviderName.ToString() && !y.IsEncrypted)
                                .OrderByDescending(y => y.Timestamp)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            PaidAmount = x
                                .Transactions
                                .Where(y => y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
                                .Sum(y => (decimal?)y.Amount) ?? 0m,
                        })
                        .Where(x => x.OutgoingPaymentFile != null && x.OutgoingPaymentFile.TransactionDate <= d)
                        .OrderBy(x => x.OutgoingPaymentFile.TransactionDate)
                        .ThenBy(x => x.OutgoingPaymentFile.Timestamp)
                        .ThenBy(x => x.CreatedByEvent.Timestamp)
                        .Select(x => new
                        {
                            x.Credit.CreditNr,
                            x.OutgoingPaymentFile.TransactionDate,
                            x.CreatedByEvent.EventType,
                            x.PaidAmount,
                            x.ApplicationNr,
                            x.ApplicationProviderName
                        })
                        .ToList();

                    var creditNrs = events.Select(x => x.CreditNr).Distinct().ToList();

                    var credits = context
                        .CreditHeaders.Where(x => creditNrs.Contains(x.CreditNr))
                        .Select(x => new
                        {
                            x.CreditNr,
                            Ts = x.CreatedByEvent.Timestamp,
                            CreationDate = x.CreatedByEvent.TransactionDate,
                            MarginInterestRate = x
                                .DatedCreditValues
                                .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .Select(y => (decimal?)y.Value)
                                .FirstOrDefault(),
                            ReferenceInterestRate = x
                                .DatedCreditValues
                                .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .Select(y => (decimal?)y.Value)
                                .FirstOrDefault(),
                            CapitalDebt = x
                                .Transactions
                                .Where(y => y.TransactionDate <= d && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                .Sum(y => (decimal?)y.Amount),
                            x.ProviderName,
                            Status = x
                                .DatedCreditStrings
                                .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            SourceChannel = x
                                .DatedCreditStrings
                                .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.InitialLoanSourceChannel.ToString())
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            ApplicationNr = x
                                .DatedCreditStrings
                                .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.ApplicationNr.ToString())
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            IntialLoanCampaignCode = x
                                .DatedCreditStrings
                                .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.IntialLoanCampaignCode.ToString())
                                .OrderByDescending(y => y.TransactionDate)
                                .ThenByDescending(y => y.Timestamp)
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            Customers = x.CreditCustomers.Select(y => new
                            {
                                y.ApplicantNr,
                                y.CustomerId
                            }),
                        })
                        .OrderBy(x => x.CreationDate)
                        .ThenBy(x => x.Ts)
                        .Select(x => new
                        {
                            x.CreditNr,
                            x.ApplicationNr,
                            x.CreationDate,
                            x.MarginInterestRate,
                            x.ReferenceInterestRate,
                            x.CapitalDebt,
                            x.ProviderName,
                            x.Status,
                            x.SourceChannel,
                            x.Customers,
                            x.IntialLoanCampaignCode
                        })
                        .ToDictionary(x => x.CreditNr);

                    var creditsMissingApplicationNr = credits.Values.Where(x => x.ApplicationNr == null).Select(x => x.CreditNr).ToList();
                    IDictionary<string, string> applicationNrByCreditNr;
                    if (creditsMissingApplicationNr.Count > 0)
                    {
                        applicationNrByCreditNr = new PreCreditClient().GetApplicationNrsByCreditNrs(new HashSet<string>(creditsMissingApplicationNr));
                    }
                    else
                    {
                        applicationNrByCreditNr = new Dictionary<string, string>();
                    }

                    var customerClient = new CreditCustomerClient();

                    IDictionary<int, string> civicRegNrsByCustomerId;
                    if (credits.Count > 0)
                    {
                        var civicRegNrResult = customerClient.BulkFetchPropertiesByCustomerIdsD(
                            new HashSet<int>(credits.Values.SelectMany(x => x.Customers).Select(x => x.CustomerId)), "civicRegNr");

                        civicRegNrsByCustomerId = civicRegNrResult.ToDictionary(x => x.Key, x => x.Value.Opt("civicRegNr") ?? "-");
                    }
                    else
                    {
                        civicRegNrsByCustomerId = new Dictionary<int, string>();
                    }

                    var request = new DocumentClientExcelRequest
                    {
                        Sheets = new DocumentClientExcelRequest.Sheet[]
                        {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Provider feedback ({date.ToString("yyyy-MM-dd")})"
                            }
                        }
                    };

                    var s = request.Sheets[0];
                    s.SetColumnsAndData(events,
                        events.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                        events.Col(x =>
                            x.EventType == BusinessEventType.NewCredit.ToString()
                                ? (credits[x.CreditNr].ApplicationNr ?? (applicationNrByCreditNr.ContainsKey(x.CreditNr) ? applicationNrByCreditNr[x.CreditNr] : null))
                                : x.ApplicationNr,
                            ExcelType.Text, "Application nr"),
                        events.Col(x => credits[x.CreditNr].Customers.OrderBy(y => y.ApplicantNr).Select(y => civicRegNrsByCustomerId[y.CustomerId]).First(), ExcelType.Text, "Civic nr 1"),
                        events.Col(x => credits[x.CreditNr].Customers.OrderBy(y => y.ApplicantNr).Skip(1).Select(y => civicRegNrsByCustomerId[y.CustomerId]).FirstOrDefault(), ExcelType.Text, "Civic nr 2"),
                        events.Col(x => credits[x.CreditNr].CreationDate, ExcelType.Date, "Creation date"),
                        events.Col(x => x.TransactionDate, ExcelType.Date, "Event date"),
                        events.Col(x => x.EventType, ExcelType.Text, "Event type"),
                        events.Col(x => x.PaidAmount, ExcelType.Number, "Payment amount"),
                        events.Col(x => credits[x.CreditNr].MarginInterestRate.HasValue && credits[x.CreditNr].ReferenceInterestRate.HasValue ? new decimal?((credits[x.CreditNr].MarginInterestRate.Value + credits[x.CreditNr].ReferenceInterestRate.Value) / 100m) : null, ExcelType.Percent, "Interest rate"),
                        events.Col(x => credits[x.CreditNr].CapitalDebt, ExcelType.Number, "Capital balance"),
                        events.Col(x => ProviderDisplayNames.GetProviderDisplayName(
                            x.EventType == BusinessEventType.NewCredit.ToString()
                                ? credits[x.CreditNr].ProviderName
                                : x.ApplicationProviderName),
                            ExcelType.Text, "Provider"),
                        events.Col(x => credits[x.CreditNr].Status, ExcelType.Text, "Status"),
                        events.Col(x => credits[x.CreditNr].IntialLoanCampaignCode, ExcelType.Text, "Campaign code"),
                        events.Col(x => credits[x.CreditNr].SourceChannel, ExcelType.Text, "Initial channel"));



                    var client = Service.DocumentClientHttpContext;
                    var result = client.CreateXlsx(request);

                    return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"ProviderFeedback-{date.ToString("yyyy-MM-dd")}.xlsx" };
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create providerfeedback report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}