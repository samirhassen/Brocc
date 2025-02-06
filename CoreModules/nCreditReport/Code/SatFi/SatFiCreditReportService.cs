using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace nCreditReport.Code.SatFi
{
    /// <summary>
    /// Explain difference between this and SatFi here, for future reference. 
    /// </summary>
    public class SatFiCreditReportService : PersonBaseCreditReportService
    {

        public override string ForCountry => "FI";
        private readonly IDocumentClient documentClient;
        private readonly SatAccountInfo satAccountInfo;
        private readonly Encoding DocumentEncoding = Encoding.UTF8;

        public SatFiCreditReportService(bool isProduction, IDocumentClient documentClient, SatAccountInfo satAccount) : base(ProviderNames.SatFiCreditReport)
        {
            this.documentClient = documentClient;
            satAccountInfo = satAccount;
        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            var exchangeToCivicRegnr = NEnv.IsProduction == false ? NEnv.CreditReportExchangeToCivicNr : null;

            var results = new List<SaveCreditReportRequest.Item>();
            var allXmls = new List<Tuple<string, string>>();

            void CallSatAndPopulateInternalValues(string qType)
            {
                var xml = GetResponseFromSat(SetupRequestUrl(civicRegNr, qType, exchangeToCivicRegnr), qType);
                var archiveKey = documentClient.ArchiveStore(DocumentEncoding.GetBytes(xml), "application/xml", $"sat_q{qType}_{civicRegNr.NormalizedValue}.xml");

                var parser = new SatFiCreditReportParser();
                parser.Initiate(xml, qType);
                results.AddRange(parser.ParseInternalValues().Select(item => new SaveCreditReportRequest.Item { Name = item.name, Value = item.value }).ToList());

                results.Add(new SaveCreditReportRequest.Item { Name = $"XmlQ{qType}ArchiveKey", Value = archiveKey });
                allXmls.Add(Tuple.Create(qType, xml));
            }

            CallSatAndPopulateInternalValues("37");
            CallSatAndPopulateInternalValues("41");

            if (allXmls.Count > 0)
            {
                var archiveKey = documentClient.ArchiveStore(DocumentEncoding.GetBytes(MergeXmls(allXmls)), "application/xml", $"satXmlRawData.xml");
                results.Add(new SaveCreditReportRequest.Item { Name = $"xmlReportArchiveKey", Value = archiveKey });
            }

            var saveResult = CreateResult(civicRegNr, results, requestData);

            return new Result { CreditReport = saveResult };
        }

        private string MergeXmls(List<Tuple<string, string>> xmls)
        {
            if (xmls.Count == 0)
                return null;

            var baseDocument = XDocument.Parse(xmls[0].Item2);
            baseDocument.Root.Remove();
            var responsesElement = new XElement("Responses");
            baseDocument.Add(responsesElement);
            foreach (var xml in xmls)
            {
                var d = XDocument.Parse(xml.Item2);
                responsesElement.Add(new XElement("ResponseWrapper", new XAttribute("qType", xml.Item1), d.Root));
            }
            return baseDocument.ToString();
        }

        public Uri SetupRequestUrl(ICivicRegNumber civicRegNr, string qType, string exchangeCivicNrTo = null)
        {
            if (exchangeCivicNrTo != null)
            {
                civicRegNr = new CivicRegNumberParser("FI").Parse(exchangeCivicNrTo);
            }

            var enduser = "system";
            var timestamp = DateTimeOffset.UtcNow.AddHours(2).ToString("yyyyMMddHHmmss00zz000000");
            var r1 = SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes($"{satAccountInfo.UserId}&{enduser}&{timestamp}&{satAccountInfo.HashKey}&"));
            var checksum = BitConverter.ToString(r1).Replace("-", string.Empty);

            var url = NTechServiceRegistry.CreateUrl(
                new Uri(satAccountInfo.EndpointUrl), "services/consumer5/REST",
                Tuple.Create("version", "2018"),
                Tuple.Create("userid", satAccountInfo.UserId),
                Tuple.Create("passwd", satAccountInfo.Password),
                Tuple.Create("enduser", enduser),
                Tuple.Create("idnumber", civicRegNr.NormalizedValue),
                Tuple.Create("reqmsg", "CONSUMER"),
                Tuple.Create("lang", "EN"), //TODO: What does this do?
                Tuple.Create("qtype", qType),
                Tuple.Create("request", "H"), //TODO: What does this do?
                Tuple.Create("format", "xml"), //What are the other options?
                Tuple.Create("purpose", "1"), //What are the other options?
                Tuple.Create("level", "1"),
                Tuple.Create("timestamp", timestamp),
                Tuple.Create("checksum", checksum));

            return url;
        }

        public string GetResponseFromSat(Uri url, string qType)
        {
            var logFile = new RotatingLogFile(
                @"c:\temp\sat-fi-creditreport-logs",
                $"sat-fi-creditreport-{qType}",
                () => DateTime.Now, new RotatingLogFile.FileSystem());

            var client = NTechHttpClient.Create(log: x => logFile.Log(x));
            client.Timeout = TimeSpan.FromSeconds(60);

            try
            {
                var result = client.GetAsync(url).Result;

                result.EnsureSuccessStatusCode();

                var contentType = result.Content.Headers.ContentType;
                var rawResponse = Encoding.UTF8.GetString(result.Content.ReadAsByteArrayAsync().Result);

                var xml = XDocument.Parse(rawResponse);
                var errorMessage = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "errorMessage");
                if (errorMessage != null)
                {
                    var errorText = errorMessage.Descendants().FirstOrDefault(x => x.Name.LocalName == "errorText")
                        ?.Value ?? "unknown";
                    var errorCode = errorMessage.Descendants().FirstOrDefault(x => x.Name.LocalName == "errorCode")
                        ?.Value ?? "Unknown";
                    throw new SatFiCreditReportException($"Error from SAT: {errorText} ({errorCode})")
                    {
                        SatErrorMessage = errorText,
                        SatErrorCode = errorCode
                    };
                }

                return rawResponse;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    // Took too long
                }

                throw;
            }
            catch (Exception)
            {
                throw;
            }

        }

        public override List<DictionaryEntry> FetchTabledValues(CreditReportRepository.FetchResult creditReport)
        {
            var key37 = creditReport.Items.Single(x => x.Name == "XmlQ37ArchiveKey").Value;
            var key41 = creditReport.Items.Single(x => x.Name == "XmlQ41ArchiveKey").Value;

            var xml37 = documentClient.FetchRawString(key37, DocumentEncoding);
            var xml41 = documentClient.FetchRawString(key41, DocumentEncoding);

            var creditReportFields = NEnv.CreditReportFields(ProviderNames.SatFiCreditReport);

            var parser = new SatFiCreditReportParser();
            var parsedValues = new List<DictionaryEntry>();
            parser.Initiate(xml37, "37", creditReportFields);
            parsedValues.AddRange(parser.ParseTabledValues());
            parser.Initiate(xml41, "41", creditReportFields);
            parsedValues.AddRange(parser.ParseTabledValues());
            parsedValues.Add(new DictionaryEntry("Credit report provider", ProviderName));

            return parsedValues;
        }

        public override bool CanFetchTabledValues() => true;

        public class SatFiCreditReportException : Exception
        {
            public SatFiCreditReportException(string message) : base(message) { }

            public string SatErrorCode { get; set; }
            public string SatErrorMessage { get; set; }
        }

    }


}