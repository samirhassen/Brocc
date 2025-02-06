using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class ContactListReportMethod : FileStreamWebserviceMethod<ContactListReportMethod.Request>
    {
        public ContactListReportMethod() : base(usePost: true, allowDirectFormPost: true)
        {

        }

        public override string Path => "Reports/ContactList";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled || NEnv.IsMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            HashSet<int> customerIds = new HashSet<int>();

            List<string> missingApplicationNrs = null;
            var requestedApplicationNrs = ParseRequestLists(request.ApplicationNrs, request.ApplicationNrsFlat);
            if (requestedApplicationNrs.Count > 0)
            {
                var r = GetCustomerIdsAndMissingApplicationNrs(requestedApplicationNrs);
                missingApplicationNrs = r.Item2;
                r.Item1.ForEach(x => customerIds.Add(x));
            }
            else
                missingApplicationNrs = new List<string>();

            List<string> missingCreditNrs = null;
            var requestedCreditNrs = ParseRequestLists(request.CreditNrs, request.CreditNrsFlat);
            if (requestedCreditNrs.Count > 0)
            {
                var r = GetCustomerIdsAndMissingCreditNrs(requestedCreditNrs);
                missingCreditNrs = r.Item2;
                r.Item1.ForEach(x => customerIds.Add(x));
            }
            else
                missingCreditNrs = new List<string>();

            var customerClient = new Code.CreditCustomerClient();

            var customerData = new Dictionary<int, Dictionary<string, string>>(customerIds.Count);
            foreach (var customerIdsGroup in customerIds.ToArray().SplitIntoGroupsOfN(500))
            {
                var result = customerClient.BulkFetchPropertiesByCustomerIdsD(
                    customerIdsGroup.ToHashSet(),
                    "addressStreet",
                    "addressZipcode",
                    "addressCity",
                    "firstName",
                    "lastName",
                    "phone",
                    "email",
                    "isCompany",
                    "companyName");
                foreach (var c in result)
                    customerData[c.Key] = c.Value;
            }

            var excelRequest = new DocumentClientExcelRequest
            {

            };
            var sheets = new List<DocumentClientExcelRequest.Sheet>();
            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Contact list"
            });
            if (missingApplicationNrs.Any() || missingCreditNrs.Any())
            {
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Missing nrs"
                });
            }
            excelRequest.Sheets = sheets.ToArray();

            Func<int, string, string> getP = (customerId, propertyName) => customerData.Opt(customerId).Opt(propertyName);

            var customerIdsList = customerIds.ToList();

            Func<int, string> getName = customerId =>
            {
                if (getP(customerId, "isCompany") == "true")
                    return getP(customerId, "companyName");
                else
                    return getP(customerId, "firstName") + " " + getP(customerId, "lastName");
            };

            excelRequest.Sheets[0].SetColumnsAndData(customerIdsList,
                customerIdsList.Col(x => getName(x), ExcelType.Text, "Name"),
                customerIdsList.Col(x => getP(x, "addressStreet"), ExcelType.Text, "Adr street"),
                customerIdsList.Col(x => getP(x, "addressZipcode"), ExcelType.Text, "Adr zipcode"),
                customerIdsList.Col(x => getP(x, "addressCity"), ExcelType.Text, "Adr city"),
                customerIdsList.Col(x => getP(x, "phone"), ExcelType.Text, "Phone"),
                customerIdsList.Col(x => getP(x, "email"), ExcelType.Text, "Email"));

            if (excelRequest.Sheets.Length > 1)
            {
                var missingItems = new List<Tuple<string, string>>();
                missingApplicationNrs.ForEach(x => missingItems.Add(Tuple.Create("Application nr", x)));
                missingCreditNrs.ForEach(x => missingItems.Add(Tuple.Create("Credit nr", x)));
                excelRequest.Sheets[1].SetColumnsAndData(missingItems,
                    missingItems.Col(x => x.Item1, ExcelType.Text, "Type"),
                    missingItems.Col(x => x.Item2, ExcelType.Text, "Nr"));
            }

            var excelClient = requestContext.Service().DocumentClientHttpContext;
            var report = excelClient.CreateXlsx(excelRequest);

            return File(report, downloadFileName: "ContactList.xlsx");
        }

        private Tuple<List<int>, List<string>> GetCustomerIdsAndMissingApplicationNrs(List<string> requestedApplicationNrs)
        {
            var applicationNrsByCustomerId = new Dictionary<int, List<string>>();
            Action<int, List<string>> add = (customerId, applicationNrs) =>
            {
                if (applicationNrsByCustomerId.ContainsKey(customerId))
                    applicationNrsByCustomerId[customerId] = applicationNrsByCustomerId[customerId].Union(applicationNrs).ToList();
                else
                    applicationNrsByCustomerId[customerId] = applicationNrs;
            };

            var pc = new PreCreditClient();
            foreach (var appNrGroup in requestedApplicationNrs.Distinct().ToArray().SplitIntoGroupsOfN(500))
            {
                foreach (var kvp in (pc.FetchCustomerIdsByApplicationNrs(appNrGroup.ToHashSet()) ?? new Dictionary<int, List<string>>()))
                {
                    add(kvp.Key, kvp.Value);
                }
            }
            var missingApplicationNrs = requestedApplicationNrs.Except(applicationNrsByCustomerId.SelectMany(x => x.Value)).ToList();

            return Tuple.Create(applicationNrsByCustomerId.Keys.ToList(), missingApplicationNrs);
        }

        private Tuple<List<int>, List<string>> GetCustomerIdsAndMissingCreditNrs(List<string> requestedCreditNrs)
        {
            var creditNrsByCustomerId = new Dictionary<int, List<string>>();
            Action<int, string> add = (customerId, creditNr) =>
            {
                if (!creditNrsByCustomerId.ContainsKey(customerId))
                    creditNrsByCustomerId[customerId] = new List<string>();

                creditNrsByCustomerId[customerId].Add(creditNr);
            };

            var pc = new PreCreditClient();

            foreach (var creditNrGroup in requestedCreditNrs.Distinct().ToArray().SplitIntoGroupsOfN(500))
            {
                using (var context = new CreditContext())
                {
                    var customers = context
                        .CreditCustomers
                        .Where(x => creditNrGroup.Contains(x.CreditNr))
                        .Select(x => new { x.CreditNr, x.CustomerId })
                        .ToList();
                    foreach (var c in customers)
                    {
                        add(c.CustomerId, c.CreditNr);
                    }
                }
            }
            var missingCreditNrs = requestedCreditNrs.Except(creditNrsByCustomerId.SelectMany(x => x.Value)).ToList();

            return Tuple.Create(creditNrsByCustomerId.Keys.ToList(), missingCreditNrs);
        }

        private class ProviderRejectionModel
        {
            public string ApplicationNr { get; set; }
            public string RejectionReasons { get; set; }
        }

        private List<string> ParseRequestLists(List<string> items, string flatItems)
        {
            var result = items ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(flatItems))
            {
                var r = new System.IO.StringReader(flatItems);
                string line;
                while ((line = r.ReadLine()) != null)
                    result.Add(line);
            }

            return result;
        }

        public class Request
        {
            public List<string> ApplicationNrs { get; set; }
            public string ApplicationNrsFlat { get; set; }
            public List<string> CreditNrs { get; set; }
            public string CreditNrsFlat { get; set; }
        }
    }
}