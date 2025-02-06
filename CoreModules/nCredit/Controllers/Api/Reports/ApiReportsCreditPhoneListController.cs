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
    public class ApiReportsCreditPhoneListController : NController
    {
        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        [Route("Api/Reports/CreditPhoneList")]
        [HttpPost]
        public ActionResult Post(List<string> creditNrs)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                creditNrs = creditNrs ?? new List<string>();

                Dictionary<string, Tuple<int, int?>> customerIdsByCreditNr = new Dictionary<string, Tuple<int, int?>>(StringComparer.InvariantCultureIgnoreCase);
                Dictionary<int, string> phoneNrByCustomerId = new Dictionary<int, string>();

                foreach (var creditNrGroup in SplitIntoGroupsOfN(creditNrs.ToArray(), 100))
                {
                    using (var context = new CreditContext())
                    {
                        var credits = context.CreditHeaders.Where(x => creditNrGroup.Contains(x.CreditNr)).Select(x => new
                        {
                            x.CreditNr,
                            Applicant1CustomerId = x.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => (int?)y.CustomerId).FirstOrDefault(),
                            Applicant2CustomerId = x.CreditCustomers.Where(y => y.ApplicantNr == 2).Select(y => (int?)y.CustomerId).FirstOrDefault()
                        })
                        .ToList();

                        foreach (var c in credits)
                        {
                            customerIdsByCreditNr[c.CreditNr] = Tuple.Create(c.Applicant1CustomerId.Value, c.Applicant2CustomerId);
                        }
                    }
                }

                var customerClient = new CreditCustomerClient();
                var result = new List<Tuple<string, string, string>>();

                var allCustomerIds = new HashSet<int>(customerIdsByCreditNr
                    .Values
                    .Select(x => x.Item1)
                    .Concat(customerIdsByCreditNr.Values.Where(y => y.Item2.HasValue).Select(y => y.Item2.Value)).ToList())
                    .ToArray();

                foreach (var customerGroup in SplitIntoGroupsOfN(allCustomerIds, 300))
                {
                    var props = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int>(customerGroup), "phone");
                    foreach (var c in customerGroup)
                    {
                        phoneNrByCustomerId[c] = props.Opt(c)?.Opt("phone") ?? "";
                    }
                }

                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Phone list ({Clock.Today.ToString("yyyy-MM-dd")})"
                            }
                    }
                };

                Func<string, bool, string> getApplicantPhoneNr = (creditNr, isMainApplicant) =>
                {
                    if (!customerIdsByCreditNr.ContainsKey(creditNr))
                        return "";
                    var c = customerIdsByCreditNr[creditNr];
                    var customerId = isMainApplicant ? (int?)c.Item1 : c.Item2;
                    if (!customerId.HasValue)
                        return "";

                    if (!phoneNrByCustomerId.ContainsKey(customerId.Value))
                        return "";
                    return phoneNrByCustomerId[customerId.Value];
                };

                var reportItems = creditNrs.Select(x => new
                {
                    CreditNr = x,
                    Applicant1PhoneNr = getApplicantPhoneNr(x, true),
                    Applicant2PhoneNr = getApplicantPhoneNr(x, false)
                }).ToList();

                var s = request.Sheets[0];
                s.SetColumnsAndData(reportItems,
                    reportItems.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    reportItems.Col(x => x.Applicant1PhoneNr, ExcelType.Text, "Applicant 1 phone"),
                    reportItems.Col(x => x.Applicant2PhoneNr, ExcelType.Text, "Applicant 2 phone"));

                var client = Service.DocumentClientHttpContext;
                var report = client.CreateXlsx(request);

                return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"CreditPhoneList-{Clock.Today.ToString("yyyy-MM-dd")}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create credit phonelist report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}