using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace nCreditReport.Code.UcSe
{
    public class UcService : PersonBaseCreditReportService
    {
        private readonly NEnv.UcSeSettings settings;
        private readonly string logFolder;
        private readonly IDocumentClient documentClient;

        public UcService(string providerName) : this(NEnv.UcSe, NEnv.CreditReportLogFolder, providerName, new DocumentClient())
        {
        }

        public UcService(NEnv.UcSeSettings settings, string logFolder, string providerName, IDocumentClient documentClient) : base(providerName)
        {
            this.settings = settings;
            this.logFolder = logFolder;
            this.documentClient = documentClient;
        }

        protected override Result DoTryBuyCreditReport(
            ICivicRegNumber civicRegNr,
            CreditReportRequestData requestData)
        {
            HttpBindingBase binding;
            EndpointAddress address;
            if (settings.EndpointUrl.StartsWith("https"))
            {
                var b = new BasicHttpsBinding();
                b.MaxReceivedMessageSize = 2000000000L;
                b.Security.Mode = BasicHttpsSecurityMode.Transport;
                binding = b;
                address = new EndpointAddress(settings.EndpointUrl);
            }
            else
            {
                var b = new BasicHttpBinding();
                b.MaxReceivedMessageSize = 2000000000L;
                b.Security.Mode = BasicHttpSecurityMode.None;
                binding = b;
                address = new EndpointAddress(settings.EndpointUrl);
            }

            var c = new UcSeService2.ucOrdersClient(binding, address);
            var request = new UcSeService2.individualReport
            {
                customer = new UcSeService2.customer
                {
                    userId = settings.UserId,
                    password = settings.Password
                },
                individualReportQuery = new UcSeService2.reportQuery
                {
                    @object = civicRegNr.NormalizedValue.Substring(2)
                }
            };

            var template = requestData.AdditionalParameters?.Opt("template") ?? settings.Template;
            if (!string.IsNullOrWhiteSpace(template))
            {
                request.individualReportQuery.template = new UcSeService2.template
                {
                    id = template
                };
            }

            if (settings.SaveHtmlReplyInArchive || settings.SavePdfReplyInArchive)
            {
                request.individualReportQuery.xmlReply = true;
                request.individualReportQuery.pdfReply = settings.SavePdfReplyInArchive;
                request.individualReportQuery.pdfReplySpecified = settings.SavePdfReplyInArchive;
                request.individualReportQuery.htmlReply = settings.SaveHtmlReplyInArchive;
            }

            var product = (requestData.AdditionalParameters?.Opt("product") ?? settings.Product)
                ?.NormalizeNullOrWhitespace()
                ?.ToUpperInvariant();

            UcSeService2.individualProduct productCode;
            if (product == null || product == "3")
                productCode = UcSeService2.individualProduct.Item3;
            else if (product == "F31")
                productCode = UcSeService2.individualProduct.F31;
            else if (product == "K39")
                productCode = UcSeService2.individualProduct.K39;
            else
                throw new NotImplementedException();

            request.product = productCode;

            NLog.Debug("Calling UC");
            var response = c.individualReportAsync(request).Result;
            NLog.Debug("UC call done");

            string htmlReplyArchiveKey = null;
            string pdfReplyArchiveKey = null;
            string xmlReplyArchiveKey = null;
            if (response?.ucReply?.ucReport != null && response.ucReply.ucReport.Length > 0)
            {
                var r1 = response.ucReply.ucReport[0];
                if (settings.SaveHtmlReplyInArchive && r1.htmlReply != null)
                {
                    //The first line of their html is a strange <!--Ù--><?xml version="1.0" encoding="ISO-8859-1"?>. We want a normal utf-8 preamble instead
                    var html = r1.htmlReply.Replace("ISO-8859-1", "UTF-8").Replace("<!--Ù-->", "");
                    htmlReplyArchiveKey = this.documentClient.ArchiveStore(Encoding.UTF8.GetBytes(html), "text/html", $"uc_{civicRegNr.NormalizedValue}.html");
                }
                if (settings.SavePdfReplyInArchive && r1.pdfReply != null)
                {
                    pdfReplyArchiveKey = this.documentClient.ArchiveStore(r1.pdfReply, "application/pdf", $"uc_{civicRegNr.NormalizedValue}.pdf");
                }
            }

            //To remove these from the text logs
            if (response?.ucReply?.ucReport != null)
            {
                foreach (var r in response.ucReply.ucReport)
                {
                    r.htmlReply = null;
                    r.pdfReply = null;
                }
            }
            Lazy<XDocument> loggedResponse = new Lazy<XDocument>(() => XmlSerializationUtil.Serialize(response.ucReply));

            if (settings.SaveXmlReplyInArchive)
            {
                xmlReplyArchiveKey = this.documentClient.ArchiveStore(Encoding.UTF8.GetBytes(loggedResponse.Value.ToString()), "application/xml", $"uc_{civicRegNr.NormalizedValue}.xml");
            }

            if (logFolder != null)
            {
                Directory.CreateDirectory(logFolder);
                var responseId = Guid.NewGuid();
                loggedResponse.Value.Save(Path.Combine(logFolder, $"ucse-{responseId}.xml"));
            }

            var p = new UcSeResponseParser();
            var parsedResponse = p.Parse(response.ucReply);

            List<SaveCreditReportRequest.Item> items = new List<SaveCreditReportRequest.Item>();
            if (!parsedResponse.IsError)
            {
                items.AddRange(parsedResponse
                        .SuccessItems
                        .Select(x => new SaveCreditReportRequest.Item
                        {
                            Name = x.Name,
                            Value = x.Value
                        }));
            }

            if (htmlReplyArchiveKey != null)
                items.Add(new SaveCreditReportRequest.Item { Name = "htmlReportArchiveKey", Value = htmlReplyArchiveKey });
            if (pdfReplyArchiveKey != null)
                items.Add(new SaveCreditReportRequest.Item { Name = "pdfReportArchiveKey", Value = pdfReplyArchiveKey });
            if (xmlReplyArchiveKey != null)
                items.Add(new SaveCreditReportRequest.Item { Name = "xmlReportArchiveKey", Value = xmlReplyArchiveKey });

            SaveCreditReportRequest report = null;
            if (!parsedResponse.IsError)
            {
                report = CreateResult(civicRegNr, items, requestData);
            }

            return new Result
            {
                IsError = parsedResponse.IsError,
                IsInvalidCredentialsError = parsedResponse.IsInvalidCredentialsError,
                ErrorMessage = parsedResponse.ErrorMessage,
                CreditReport = report
            };
        }

        public override string ForCountry
        {
            get
            {
                return "SE";
            }
        }
    }
}