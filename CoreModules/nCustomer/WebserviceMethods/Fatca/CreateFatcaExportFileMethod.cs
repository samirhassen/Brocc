using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace nCustomer.WebserviceMethods.Fatca
{
    public class CreateFatcaExportFileMethod : FileStreamWebserviceMethod<CreateFatcaExportFileMethod.Request>
    {
        public CreateFatcaExportFileMethod() : base(usePost: true)
        {
        }

        public override string Path => "Fatca/CreateExportFile";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(r => r.ExportDate);
                x.Require(r => r.ReportingDate);
                x.Require(r => r.Accounts);
            });

            var customerIds = new HashSet<int>(request.Accounts.Select(x => x.CustomerId));

            var toFetch = new HashSet<string> { "firstName", "lastName", "addressCity", "addressCountry", "addressZipcode", "addressStreet", "tin" };
            var customers = requestContext
                .Service()
                .Customer
                .BulkFetch(customerIds, toFetch, requestContext.CurrentUserMetadata())
                .ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Name, y => y.Value));

            var clientCountry = NEnv.ClientCfg.Country.BaseCountry;

            var data = request.Accounts.Select(x =>
            {
                var cd = customers.Opt(x.CustomerId);
                var c = new AccountCustomerData
                {
                    FirstName = cd.Opt("firstName"),
                    LastName = cd.Opt("lastName"),
                    AddressCity = cd.Opt("addressCity"),
                    AddressCountryCode = cd.Opt("addressCountry") ?? clientCountry,
                    AddressPostCode = cd.Opt("addressZipcode"),
                    AddressStreet = cd.Opt("addressStreet"),
                    NationalityCountryCode = clientCountry,
                    Tin = cd.Opt("tin")
                };
                return Tuple.Create(x, c);
            }).ToList();

            var ms = new MemoryStream();
            CreateFatcaExport(ms, request.ReportingDate.Value, request.ExportDate.Value, data);
            ms.Position = 0;

            return File(ms);
        }

        private void CreateFatcaExport(Stream exportTarget, DateTime reportingPeriod, DateTime exportDate, List<Tuple<Account, AccountCustomerData>> accounts)
        {
            var d = NEnv.FatcaTemplateFile;

            Func<XElement, string, XElement> desc = (e, n) => e.Descendants().Where(x => x.Name.LocalName == n).Single();
            Func<XElement, string, XElement> child = (e, n) => e.Elements().Where(x => x.Name.LocalName == n).Single();

            var sendingCompanyINValue = desc(d.Root, "SendingCompanyIN").Value;

            var messageSpecElement = desc(d.Root, "MessageSpec");
            var messageRefId = $"{sendingCompanyINValue}-{reportingPeriod.Year}-1";
            child(messageSpecElement, "MessageRefId").Value = messageRefId;
            child(messageSpecElement, "ReportingPeriod").Value = reportingPeriod.ToString("yyyy-MM-dd");
            child(messageSpecElement, "Timestamp").Value = exportDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var reportingFiElement = desc(d.Root, "ReportingFI");
            var giin = child(reportingFiElement, "TIN").Value;

            int idCounter = 1;
            Func<string> newId = () => $"{giin}.{sendingCompanyINValue}-{reportingPeriod.Year}-{(idCounter++).ToString().PadLeft(3, '0')}-001";

            var docSpecElement = child(reportingFiElement, "DocSpec");
            child(docSpecElement, "DocRefId").Value = newId();

            var reportGroupElement = desc(d.Root, "ReportingGroup");
            var accountReportTemplateElement = child(reportGroupElement, "AccountReport");
            accountReportTemplateElement.Remove();

            foreach (var ac in accounts)
            {
                var a = ac.Item1;
                var c = ac.Item2;

                var e = new XElement(accountReportTemplateElement);
                child(child(e, "DocSpec"), "DocRefId").Value = newId();
                child(e, "AccountNumber").Value = a.AccountNumber;
                child(e, "AccountClosed").Value = a.IsClosed ? "true" : "false";
                var i = child(child(e, "AccountHolder"), "Individual");
                child(i, "TIN").Value = c.Tin;

                var n = child(i, "Name");
                child(n, "FirstName").Value = c.FirstName;
                child(n, "LastName").Value = c.LastName;

                var adr = child(i, "Address");
                child(adr, "CountryCode").Value = c.AddressCountryCode;
                var af = child(adr, "AddressFix");
                child(af, "Street").Value = c.AddressStreet;
                child(af, "PostCode").Value = c.AddressPostCode;
                child(af, "City").Value = c.AddressCity;

                child(i, "Nationality").Value = c.NationalityCountryCode;
                child(e, "AccountBalance").Value = a.AccountBalance.ToString("f2", CultureInfo.InvariantCulture);
                child(child(e, "Payment"), "PaymentAmnt").Value = a.AccountInterest.ToString("f2", CultureInfo.InvariantCulture);

                reportGroupElement.Add(e);
            }

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), //Skip byte order mark
                OmitXmlDeclaration = true,
                Indent = true
            };

            using (var w = XmlWriter.Create(exportTarget, settings))
            {
                d.Save(w);
            }
        }

        public class Request
        {
            public DateTime? ExportDate { get; set; }
            public DateTime? ReportingDate { get; set; }
            public List<Account> Accounts { get; set; }
        }

        public class Account
        {
            public string AccountNumber { get; set; }
            public bool IsClosed { get; set; }
            public int CustomerId { get; set; }
            public decimal AccountBalance { get; set; }
            public decimal AccountInterest { get; set; }
        }

        private class AccountCustomerData
        {
            public string Tin { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string AddressCountryCode { get; set; }
            public string NationalityCountryCode { get; set; }
            public string AddressStreet { get; set; }
            public string AddressPostCode { get; set; }
            public string AddressCity { get; set; }
        }
    }
}