using nCreditReport.Code.TestOnly;
using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace nCreditReport.Code.CreditSafeSe
{
    public class CreditSafeSeService : PersonBaseCreditReportService
    {
        private readonly CreditSafeSeSettings settings;
        private readonly string logFolder;
        private readonly DocumentClient documentClient;

        public CreditSafeSeService(string providerName) : base(providerName)
        {
            settings = NEnv.CreditSafeSe;
            logFolder = NEnv.CreditReportLogFolder;
            documentClient = new DocumentClient();
        }

        public override string ForCountry => "SE";

        private LoggedSoapServiceClient<CreditSafeSeCreditApproval.Cas_ServiceSoap,
            CreditSafeSeCreditApproval.Cas_ServiceSoapClient> GetTemplateClient()
        {
            var endpointUrl = NEnv.IsProduction
                ? "https://webservice.creditsafe.se/CAS/cas_service.asmx"
                : "https://testwebservice.creditsafe.se/CAS/cas_service.asmx";
            return new LoggedSoapServiceClient<CreditSafeSeCreditApproval.Cas_ServiceSoap,
                CreditSafeSeCreditApproval.Cas_ServiceSoapClient>(
                endpointUrl,
                (x, y) => new CreditSafeSeCreditApproval.Cas_ServiceSoapClient(x, y));
        }

        private LoggedSoapServiceClient<CreditSafeSeGetData.GetDataSoap, CreditSafeSeGetData.GetDataSoapClient>
            GetDataClient()
        {
            var endpointUrl = NEnv.IsProduction
                ? "https://webservice.creditsafe.se/getdata/getdata.asmx"
                : "https://testwebservice.creditsafe.se/GetData/getdata.asmx";
            return new LoggedSoapServiceClient<CreditSafeSeGetData.GetDataSoap, CreditSafeSeGetData.GetDataSoapClient>(
                endpointUrl,
                (x, y) => new CreditSafeSeGetData.GetDataSoapClient(x, y));
        }

        private (XDocument RawResponse, Dictionary<string, string> TemplateItems, List<(string Code, string Text)>
            TemplateErrors) ExecuteTemplateRequest(ICivicRegNumber civicRegNr, string correlationId)
        {
            var templateClient = GetTemplateClient();

            var templateResultLogged = templateClient.ExecuteLogged(x =>
                x.CasPersonService(new CreditSafeSeCreditApproval.CAS_PERSON_REQUEST
                {
                    account = new CreditSafeSeCreditApproval.Account
                    {
                        Language = CreditSafeSeCreditApproval.LANGUAGE.SWE,
                        UserName = settings.UserName,
                        Password = settings.Password
                    },
                    SearchNumber = NEnv.IsProduction
                        ? civicRegNr.NormalizedValue
                        : (settings.TestCivicRegNr ?? civicRegNr.NormalizedValue),
                    Templates = settings.Template
                }));
            var templateResult = templateResultLogged.Result;
            if (logFolder != null)
            {
                Directory.CreateDirectory(logFolder);
                if (templateResultLogged.RawRequest != null)
                    templateResultLogged.RawRequest.Save(Path.Combine(logFolder,
                        $"{correlationId}-creditsafe-se-request-template.xml"));
                if (templateResultLogged.RawResponse != null)
                    templateResultLogged.RawResponse.Save(Path.Combine(logFolder,
                        $"{correlationId}-creditsafe-se-response-template.xml"));
            }

            var parsed = CreditSafeSeResponseParser.ParseTemplateXml(templateResultLogged.RawResponse.Root);

            HandleServiceLevelErrors("CasPersonService", parsed.Errors);
            return (templateResultLogged.RawResponse, TemplateItems: parsed.TemplateItems,
                TemplateErrors: parsed.Errors);
        }

        private (XDocument RawResponse, Dictionary<string, string> DataItems) ExecuteDataRequest(
            ICivicRegNumber civicRegNr, string correlationId)
        {
            var dataClient = GetDataClient();
            var dataResultLogged = dataClient.ExecuteLogged(x => x.GetDataBySecure(
                new CreditSafeSeGetData.GETDATA_REQUEST
                {
                    account = new CreditSafeSeGetData.Account
                    {
                        Language = CreditSafeSeGetData.LANGUAGE.SWE,
                        UserName = settings.UserName,
                        Password = settings.Password
                    },
                    Block_Name = settings.DataBlock,
                    SearchNumber = NEnv.IsProduction
                        ? civicRegNr.NormalizedValue
                        : (settings.TestCivicRegNr ?? civicRegNr.NormalizedValue),
                    FormattedOutput =
                        "1" //Supposedly you get things like 'KALLE ANKA' if you put 0 here and 'Kalle Anka' if you put 1
                }));
            var dataResult = dataResultLogged.Result;
            if (logFolder != null)
            {
                Directory.CreateDirectory(logFolder);
                if (dataResultLogged.RawRequest != null)
                    dataResultLogged.RawRequest.Save(Path.Combine(logFolder,
                        $"{correlationId}-creditsafe-se-request-data.xml"));
                if (dataResultLogged.RawResponse != null)
                    dataResultLogged.RawResponse.Save(Path.Combine(logFolder,
                        $"{correlationId}-creditsafe-se-response-data.xml"));
            }

            if (!string.IsNullOrWhiteSpace(dataResult.Error?.Cause_of_Reject))
                HandleServiceLevelErrors("GetDataBySecure",
                    new List<(string Code, string Text)>
                        { (Code: dataResult.Error.Cause_of_Reject, Text: dataResult.Error.Reject_text) });

            var dataParsed = CreditSafeSeResponseParser.ParseDataXml(dataResultLogged.RawResponse.Root);

            return (dataResultLogged.RawResponse, dataParsed.DataItems);
        }

        private void HandleServiceLevelErrors(string functionName, List<(string Code, string Text)> errors)
        {
            foreach (var error in errors)
            {
                if (int.TryParse(error.Code, out var intErrorCode) &&
                    intErrorCode !=
                    15) //15 is 'ingen träff' which we handle with personstatus = nodata rather than 'blow up'
                {
                    throw new NTechCoreWebserviceException(
                            $"CreditSafe.{functionName} service error {error.Text} ({error.Code}")
                        { ErrorCode = "creditSafeServiceError_" + intErrorCode };
                }
            }
        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                var templateResult = ExecuteTemplateRequest(civicRegNr, correlationId);
                var dataResult = ExecuteDataRequest(civicRegNr, correlationId);

                var creditReportItems = CreditSafeSeResponseParser.GetCreditReportItems(dataResult.DataItems,
                    templateResult.TemplateItems, templateResult.TemplateErrors);

                var storedXmlData = new XDocument(
                    new XElement("Responses",
                        new XElement("Version", "1"),
                        new XElement("TemplateResponse", templateResult.RawResponse.Root),
                        new XElement("DataResponse", dataResult.RawResponse.Root)));

                var xmlReplyArchiveKey = documentClient.ArchiveStore(Encoding.UTF8.GetBytes(storedXmlData.ToString()),
                    "application/xml", $"creditsafe_{civicRegNr.NormalizedValue}.xml");
                creditReportItems.Add(new SaveCreditReportRequest.Item
                    { Name = "xmlReportArchiveKey", Value = xmlReplyArchiveKey });

                return new Result
                {
                    IsError = false,
                    IsInvalidCredentialsError = false,
                    IsTimeoutError = false,
                    ErrorMessage = null,
                    CreditReport = CreateResult(civicRegNr, creditReportItems, requestData)
                };
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.ErrorCode != null && ex.ErrorCode.StartsWith("creditSafeServiceError_"))
                {
                    if (ex.ErrorCode == "creditSafeServiceError_9")
                        return new Result
                        {
                            IsError = true,
                            IsInvalidCredentialsError = true,
                            ErrorMessage = ex.Message
                        };
                    else
                        return new Result
                        {
                            IsError = true,
                            IsInvalidCredentialsError = false,
                            ErrorMessage = ex.Message
                        };
                }

                throw;
            }
        }

        public override bool CanFetchTabledValues() => true;

        public override List<DictionaryEntry> FetchTabledValues(CreditReportRepository.FetchResult creditReport)
        {
            try
            {
                var xmlReportArchiveKey = creditReport.Items.Single(x => x.Name == "xmlReportArchiveKey")?.Value;
                var rawData = documentClient.FetchRaw(xmlReportArchiveKey, out var _, out var __);
                var xmlData = XDocument.Parse(Encoding.UTF8.GetString(rawData));
                return CreditSafeSeResponseParser.GetTabledValuesFromStoredXml(xmlData);
            }
            catch
            {
                return new List<DictionaryEntry> { new DictionaryEntry { Key = "Report missing", Value = "" } };
            }
        }
    }

    public class CreditSafeSeSettings : ICreditReportCommonTestSettings
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Template { get; set; }
        public string DataBlock { get; set; }
        public string TestModuleMode { get; set; }
        public string TestCivicRegNr { get; set; }
    }
}