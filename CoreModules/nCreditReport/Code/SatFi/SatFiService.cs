using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace nCreditReport.Code.SatFi
{
    public class SatFiService : PersonBaseCreditReportService
    {
        public override string ForCountry => "FI";

        private readonly bool isProduction;
        private readonly SatAccountInfo satAccount;

        public TimeSpan? RequestTimeout { get; set; }

        private string EndpointUrl => this.satAccount.EndpointUrl ?? (this.isProduction ? "https://www.asiakastieto.fi/services/consumer5/SOAP" : "https://demo.asiakastieto.fi/services/consumer5/SOAP");
        private string Target => satAccount?.OverrideTarget ?? (this.isProduction ? "PAP1" : "TAP1");

        //They also sometimes call userid username
        public SatFiService(bool isProduction, SatAccountInfo satAccount) : base(ProviderNames.SatFi)
        {
            if (satAccount == null)
                throw new ArgumentException("satAccount cannot be null");
            this.isProduction = isProduction;
            this.satAccount = satAccount;
        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            if (civicRegNr.Country != ForCountry)
                throw new Exception("SatFi can only score finnish persons");

            var result = GetConsumerLoanSummary(civicRegNr.NormalizedValue, $"User{requestData.UserId}", observeRawResponse: (x => LogResponse(civicRegNr, x)));
            switch (result.ResponseStatus)
            {
                case ResponseStatusCode.ErrorCheckSumTooOld:
                    return new Result
                    {
                        IsError = true,
                        ErrorMessage = "[ErrorCheckSumTooOld] Checksum is too old. (Most likely their server and our server disagree on the time. SAT seems to accept about 15 minutes which is super low)"
                    };

                case ResponseStatusCode.ErrorTimeout:
                    return new Result
                    {
                        IsError = true,
                        IsTimeoutError = true,
                        ErrorMessage = "[ErrorTimeout] The request timed out"
                    };

                case ResponseStatusCode.ErrorWrongCredentials:
                    return new Result
                    {
                        IsError = true,
                        IsInvalidCredentialsError = true,
                        ErrorMessage = "[ErrorWrongCredentials] Invalid credentials"
                    };

                case ResponseStatusCode.Ok:
                    {
                        var items = TranslateSatResponse(result);
                        return new Result
                        {
                            CreditReport = this.CreateResult(civicRegNr, items, requestData),
                            IsError = false
                        };
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        internal static List<SaveCreditReportRequest.Item> TranslateSatResponse(SatConsumerLoanSummaryResponse r)
        {
            var items = new List<SaveCreditReportRequest.Item>();

            //count Total nr of open credits (not transmitted as a field from SAT for some wierd reason)
            //c01 Total amount of loans (The capital of the person’s open credits is included here)
            //c03 Over 60 days unpaid loans (This is the entire remaining capital and interests of credits overdue by more than 60 days)
            //c04 Monthly payments (Instalments and interests in total for the current month)
            items.AddRange(
                r.Rows
                .Select(x => new SaveCreditReportRequest.Item { Name = x.Key, Value = x.Value?.Value?.Trim() }));

            if (r.CountLoans.HasValue)
                items.Add(new SaveCreditReportRequest.Item { Name = "count", Value = r.CountLoans.Value.ToString() });

            return items;
        }

        private static void LogResponse(ICivicRegNumber civicRegNr, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return;

            var logFolder = NEnv.CreditReportLogFolder;
            if (logFolder == null)
                return;

            Directory.CreateDirectory(logFolder);
            File.WriteAllText(Path.Combine(logFolder, "satfi-response-" + Guid.NewGuid() + ".xml"), response);
        }

        private class SatConsumerLoanSummaryRequest
        {
            public string EndUser { get; set; }//They dont seem to understand encodings very well so keep this without national characters
            public string CivicRegNr { get; set; }
            public string ResponseLanguage { get; set; }
        }

        public enum ResponseStatusCode
        {
            Ok,
            ErrorTimeout,
            ErrorWrongCredentials,
            ErrorCheckSumTooOld //Happens when the clock are out of synch
        }

        public class SatConsumerLoanSummaryResponse
        {
            public ResponseStatusCode ResponseStatus { get; set; }
            public IDictionary<string, ConsumerLoanRow> Rows { get; set; }
            public int? CountLoans { get; set; }
        }

        public class ConsumerLoanRow
        {
            public string Code { get; set; }
            public string Value { get; set; }
            public string Text { get; set; }
        }

        private SatConsumerLoanSummaryResponse GetConsumerLoanSummary(string civicRegNr, string endUser, string responseLanguageCountryIsoCode = "FI", Action<string> observeRawResponse = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(civicRegNr))
                    throw new ArgumentException("Missing civicRegNr");
                if (string.IsNullOrWhiteSpace(endUser))
                    throw new ArgumentException("Missing endUser");
                return GetConsumerLoanSummaryI(civicRegNr, endUser, responseLanguageCountryIsoCode: responseLanguageCountryIsoCode, observeRawResponse: observeRawResponse);
            }
            catch (AggregateException ex)
            {
                var tc = ex.InnerException as System.Threading.Tasks.TaskCanceledException;
                if (tc != null)
                {
                    return new SatConsumerLoanSummaryResponse
                    {
                        ResponseStatus = ResponseStatusCode.ErrorTimeout
                    };
                }
                else
                    throw;
            }
        }

        private SatConsumerLoanSummaryResponse GetConsumerLoanSummaryI(string civicRegNr, string endUser, string responseLanguageCountryIsoCode = "FI", Action<string> observeRawResponse = null)
        {
            var timestamp = (satAccount.ClockDrag.HasValue ? DateTimeOffset.UtcNow.Subtract(satAccount.ClockDrag.Value) : DateTimeOffset.UtcNow).ToString("yyyyMMddHHmmss00zz00000");
            var input = $"{satAccount.UserId}&{endUser}&{timestamp}&{satAccount.HashKey}&";
            var r1 = SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

            var checkSum = BitConverter.ToString(r1).Replace("-", string.Empty);

            var requestDocument = CreateRequest(satAccount.UserId, satAccount.Password, timestamp, checkSum, Target, endUser, civicRegNr, responseLanguageCountryIsoCode);
            using (var c = new HttpClient())
            {
                try
                {
                    c.Timeout = RequestTimeout ?? TimeSpan.FromSeconds(10);
                    c.DefaultRequestHeaders.Add("SOAPAction", "http://www.asiakastieto.fi/consumer_5_00/getConsumerLoanDetails");
                    var response = c.PostAsync(EndpointUrl, new StringContent(requestDocument.ToString(), Encoding.UTF8, "application/soap+xml")).Result;
                    response.EnsureSuccessStatusCode();
                    var stringResponse = response.Content.ReadAsStringAsync().Result;
                    observeRawResponse?.Invoke(stringResponse);
                    return ParseResponse(stringResponse);
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException != null && (ex.InnerException as System.Threading.Tasks.TaskCanceledException) != null)
                        return new SatConsumerLoanSummaryResponse { ResponseStatus = ResponseStatusCode.ErrorTimeout };
                    throw;
                }
            }
        }

        public static SatConsumerLoanSummaryResponse ParseResponse(string responseText)
        {
            var response = XDocuments.Parse(responseText);

            Func<XElement, Func<XElement, bool>, string> single = (root, predicate) =>
            {
                return root.Descendants().Single(predicate).Value;
            };
            Func<XElement, string, string> singleN = (root, name) =>
            {
                return single(root, x => x.Name.LocalName == name);
            };

            if (singleN(response.Root, "currencyCode") != "EUR")
                throw new Exception("Response currency is no longer EUR!");

            var responseStatus = singleN(response.Root, "responseStatus");

            if (responseStatus == "0")
            {
                return new SatConsumerLoanSummaryResponse
                {
                    ResponseStatus = ResponseStatusCode.Ok,
                    CountLoans = int.Parse(singleN(response.Root, "count")),
                    Rows = response
                        .Descendants()
                        .Where(x => x.Name.LocalName == "consumerLoanRow")
                        .Select(x => new ConsumerLoanRow
                        {
                            Code = singleN(x, "code"),
                            Value = singleN(x, "value"),
                            Text = singleN(x, "text")
                        })
                        .ToDictionary(x => x.Code)
                };
            }
            else
            {
                var errorCode = singleN(response.Root, "errorCode");
                if (errorCode == "103")
                    return new SatConsumerLoanSummaryResponse { ResponseStatus = ResponseStatusCode.ErrorWrongCredentials };
                else if (errorCode == "505")
                    return new SatConsumerLoanSummaryResponse { ResponseStatus = ResponseStatusCode.ErrorCheckSumTooOld };
                else
                {
                    var errorText = singleN(response.Root, "errorText");
                    throw new Exception($"Unkown error from SAT: code={errorCode}, text={errorText}");
                }
            }
        }

        private static XDocument CreateRequest(string userId, string password, string timestamp, string checksum, string target, string endUser, string personId, string responseLanguageCountryIsoCode)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"nCreditReport.Code.SatFi.RequestTemplate.xml";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                var d = XDocuments.Load(stream);
                Action<string, string> replace = (n, v) =>
                {
                    d.Descendants().Where(x => x.Name.LocalName == n).Single().Value = v;
                };
                replace("Username", userId);
                replace("Password", password);
                replace("timeStamp", timestamp);
                replace("checkSum", checksum);
                replace("endUser", endUser);
                replace("personId", personId);
                replace("target", target);
                replace("languageCode", responseLanguageCountryIsoCode);
                return d;
            }
        }
    }
}