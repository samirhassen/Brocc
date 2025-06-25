using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Xml;
using nCreditReport.Models;
using nCreditReport.SoliditetFiWs;
using NTech.Banking.CivicRegNumbers;
using Polly;
using Serilog;

namespace nCreditReport.Code.BisnodeFi
{
    public class BisnodeService : PersonBaseCreditReportService
    {
        public BisnodeService(string providerName) : base(providerName)
        {
        }

        protected override Result DoTryBuyCreditReport(
            ICivicRegNumber civicRegNr,
            CreditReportRequestData requestData)
        {
            return buyCreditReportRetryPolicy.Execute(() => DoTryBuyCreditReportI(civicRegNr, requestData));
        }

        public static T RequestCreditReport<T>(
            ICivicRegNumber civicRegNr,
            string username,
            bool isAddressOnlyRequest,
            Func<BisnodeFiResponseParser.Result, T> handleResult)
        {
            var settings = NEnv.BisnodeFi;
            HttpBindingBase binding;

            void RemoveStupidSizeLimitDefaults(HttpBindingBase b)
            {
                b.MaxReceivedMessageSize = 20000000L;
                b.MaxBufferSize = 20000000;
                if (b.ReaderQuotas == null)
                {
                    b.ReaderQuotas = new XmlDictionaryReaderQuotas();
                }

                b.ReaderQuotas.MaxDepth = 32;
                b.ReaderQuotas.MaxArrayLength = 200000000;
                b.ReaderQuotas.MaxStringContentLength = 200000000;
            }

            if (settings.EndpointUrl.StartsWith("https"))
            {
                var b = new BasicHttpsBinding
                {
                    Security =
                    {
                        Mode = BasicHttpsSecurityMode.Transport
                    }
                };
                RemoveStupidSizeLimitDefaults(b);
                binding = b;
            }
            else
            {
                var b = new BasicHttpBinding
                {
                    Security =
                    {
                        Mode = BasicHttpSecurityMode.None
                    }
                };
                RemoveStupidSizeLimitDefaults(b);
                binding = b;
            }

            var address = new EndpointAddress(settings.EndpointUrl);

            var c = new SoliditetHenkiloLuottoTiedotPortTypeClient(binding, address);
            var request = new SoliditetHenkiloLuottoTiedotRequest
            {
                KayttajaTiedot = new KayttajaTiedot
                {
                    KayttajaTunnus = settings.UserId,
                    AsiakasTunnus = settings.CustomerId,
                    LoppuAsiakas_Nimi = settings.CustomerCode,
                    LoppuAsiakas_HenkiloNimi = username
                },
                KohdeTiedot = new KohdeTiedot
                {
                    HenkiloTunnus = civicRegNr.NormalizedValue,
                    SyyKoodi = KohdeTiedotSyyKoodi.Item1,
                    HenkiloTiedot = isAddressOnlyRequest
                        ? HenkiloTietoType.K
                        : settings.RequestBricVariables
                            ? HenkiloTietoType.X
                            : HenkiloTietoType.K,
                    HenkiloTiedotSpecified = true,
                    LuottoTietoMerkinnat = isAddressOnlyRequest
                        ? KohdeTiedotLuottoTietoMerkinnat.E
                        : KohdeTiedotLuottoTietoMerkinnat.S,
                    LuottoTietoMerkinnatSpecified = true,
                    YritysYhteydet = isAddressOnlyRequest ? KyllaEiType.E : KyllaEiType.K,
                    YritysYhteydetSpecified = true,
                    ScoreJaLuottoSuositus =
                        isAddressOnlyRequest
                            ? KyllaEiType.E
                            : KyllaEiType
                                .E, //! current E in both cases. Just to remember the adr only case if we start requesting it for scoring
                    ScoreJaLuottoSuositusSpecified = true
                }
            };

            NLog.Debug("Calling SoliditetHenkiloLuottoTiedotAsync");
            if (settings.DisableSslCheck)
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true;
            }

            var response = c.SoliditetHenkiloLuottoTiedotAsync(request).Result;
            NLog.Debug("SoliditetHenkiloLuottoTiedotAsync done");

            //Raw request
            var logFolder = NEnv.CreditReportLogFolder;
            if (logFolder != null)
            {
                Directory.CreateDirectory(logFolder);
                XmlSerializationUtil.Serialize(response.Response)
                    .Save(Path.Combine(logFolder, "bisnodefi-" + Guid.NewGuid() + ".xml"));
            }

            var p = new BisnodeFiResponseParser();
            var parsedResponse = p.Parse(response.Response, isAddressOnlyRequest);
            return handleResult(parsedResponse);
        }


        private Result DoTryBuyCreditReportI(
            ICivicRegNumber civicRegNr,
            CreditReportRequestData requestData)
        {
            return RequestCreditReport(civicRegNr, requestData.Username, false, parsedResponse =>
            {
                SaveCreditReportRequest report = null;
                if (!parsedResponse.IsError)
                {
                    report = CreateResult(civicRegNr, parsedResponse
                        .SuccessItems
                        .Select(x => new SaveCreditReportRequest.Item
                        {
                            Name = x.Name,
                            Value = x.Value
                        }), requestData);
                }

                //Add civicregnr and country
                return new Result
                {
                    IsError = parsedResponse.IsError,
                    IsInvalidCredentialsError = parsedResponse.IsInvalidCredentialsError,
                    ErrorMessage = parsedResponse.ErrorMessage,
                    CreditReport = report
                };
            });
        }

        public override string ForCountry => "FI";

        private static string FormatException(Exception ex)
        {
            var b = new StringBuilder();
            var guard = 0;
            while (ex != null && guard++ < 10)
            {
                b.AppendLine(ex.GetType().Name);
                b.AppendLine(ex.Message);
                b.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }

            return b.ToString();
        }

        private static bool IsRetryableBisnodeError(Exception ex)
        {
            if (ex == null)
                return false;
            var m = FormatException(ex);
            if (m == null)
                return false;
            return m.Contains("Could not establish secure channel for SSL/TLS with authority") ||
                   m.Contains("The remote server returned an unexpected response: (502) Proxy Error");
        }

        //Retry once on the wierd intermittent ssl error
        private static Policy buyCreditReportRetryPolicy = Policy
            .Handle<Exception>(IsRetryableBisnodeError)
            .WaitAndRetry(new[] { TimeSpan.FromSeconds(1) }, (ex, ts, context) =>
            {
                //On Retry
                NLog.Information("BisnodeFi buyCreditReport retried on {exceptionMessage}", ex?.ToString());
            });

        public override List<DictionaryEntry> FetchTabledValues(CreditReportRepository.FetchResult creditReport)
        {
            var results = new List<DictionaryEntry>();
            var creditReportFields = NEnv.CreditReportFields(ProviderName);

            void AddValue(string name, string value) => results.Add(new DictionaryEntry(name, value));

            AddValue("Credit report provider", ProviderName);

            // For every field that is saved in the database and exists in the CreditReport-Fields.json, add them to the resultlist and return. 
            foreach (var availableField in creditReport.Items)
            {
                var setting = creditReportFields.SingleOrDefault(f => f.Field == availableField.Name);
                if (setting != null)
                {
                    AddValue(setting.Title, availableField.Value);
                }
            }

            return results;
        }

        public override bool CanFetchTabledValues() => true;
    }
}