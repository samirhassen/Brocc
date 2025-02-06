using nCustomer.Code.Services.Aml.Cm1;
using Newtonsoft.Json;
using NTech;
using NTech.Banking.CivicRegNumbers.Se;
using NTech.Banking.Shared.Globalization;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace nCustomer.Code.Services.Kyc.Cm1
{
    public class CM1KycScreeningProviderService : IKycScreeningProviderService
    {
        public CM1KycScreeningProviderService(string baseCountry, Action<string> logRequest, Cm1KycSettings settings, bool isProduction)
        {
            this.baseCountry = baseCountry;
            this.logRequest = logRequest;
            this.settings = settings;
            this.isProduction = isProduction;
        }

        private static readonly Dictionary<string, string> ssnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SE", "Swedish" },
                { "FI", "Finnish" }
            };

        private readonly string baseCountry;
        private readonly Action<string> logRequest;
        private readonly Cm1KycSettings settings;
        private readonly bool isProduction;

        public IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All)
        {
            var persons = items.Select(x =>
            {
                return new
                {
                    CustomerReferenceId = x.ItemId,
                    x.FirstName,
                    x.LastName,
                    SSNType = ssnTypes.Opt(baseCountry) ?? baseCountry,
                    SSN = FormatCivicRegNr(x.CivicRegNr),
                    BirthYear = x.BirthDate.Year.ToString(),
                    BirthMonth = x.BirthDate.Month.ToString(),
                    BirthDay = x.BirthDate.Day.ToString(),
                    Nationality = NTechCountry.FromTwoLetterIsoCode(baseCountry).ThreeLetterIsoCountryCode,
                    ScreenOnly = !settings.ForceDisableScreenOnly,
                    ScreenPEPLists = list == KycScreeningListCode.All || list == KycScreeningListCode.Pep,
                    ScreenSanctionLists = list == KycScreeningListCode.All || list == KycScreeningListCode.Sanction,
                    ContactInformation = x.ContactInfo == null ? null : new
                    {
                        AddressRow1 = x.ContactInfo.StreetAddress,
                        AddressRow2 = x.ContactInfo.CareOfAddress,
                        PostalNumber = x.ContactInfo.ZipCode,
                        PostalRegion = x.ContactInfo.City,
                        Country = NTechCountry.FromTwoLetterIsoCode(x.ContactInfo.Country, returnNullWhenNotExists: true)?.ThreeLetterIsoCountryCode
                    }
                };
            }).ToList();

            var request = new
            {
                ScreenPersons = persons
            };
            var c = new RestClient();
            string jsonRequest = "";
            System.Security.Cryptography.X509Certificates.X509Certificate2 clientCertificate = null;
            if (!string.IsNullOrWhiteSpace(settings.ClientCertificateThumbprint))
                clientCertificate = c.LoadClientCertificateUsingThumbPrint(settings.ClientCertificateThumbprint);
            else if (!string.IsNullOrWhiteSpace(settings.ClientCertificateFilePath))
                clientCertificate = c.LoadClientCertificateFromFile(settings.ClientCertificateFilePath, certificatePassword: settings.ClientCertificateFilePassword);

            var result = c.SendRequest(request, HttpMethod.Post, new Uri(new Uri(settings.Endpoint), "api/v2/Person/Screen").ToString(), observeJsonRequest: x =>
            {
                jsonRequest = x;
            },
            setupMessage: (m) =>
            {
                m.Headers.Add("X-Identifier", settings.XIdentifier);
                if (!NEnv.IsProduction)
                {
                    //Used to test against the test module
                    m.Headers.Add("X-NTech-IsCm1Screening", "1");
                }
            },
            clientCertificate: clientCertificate);

            string jsonResponse = null;
            string htmlResponse = null;

            var r = result.ReadJsonOrHtmlBodyIfAny(
                x => { jsonResponse = x; return JsonConvert.DeserializeObject<ApiResponseModel>(x); },
                x => { htmlResponse = x; return null; });

            var correlationId = Guid.NewGuid().ToString();
            if (logRequest != null)
            {
                var msg = "--Request--"
                    + Environment.NewLine + jsonRequest
                    + Environment.NewLine + "--Response--"
                    + Environment.NewLine + $"Response code: {result.Response.StatusCode.ToString()}, Correlation id={correlationId}"
                    + Environment.NewLine + jsonResponse ?? htmlResponse;
                logRequest(msg);
            }
            if (!result.Response.IsSuccessStatusCode || jsonResponse == null)
            {
                var responseFragment = "";
                if (htmlResponse != null || jsonResponse != null)
                {
                    var response = htmlResponse ?? jsonResponse;
                    if (response.Length > 100)
                        response = response.Substring(0, 100);
                    responseFragment = "ResponsePart=" + response;
                }
                throw new Exception($"Failed with http status {result.Response.StatusCode.ToString()}. Log correlation id={correlationId}. {responseFragment}");
            }

            var d = JsonConvert.DeserializeObject<ApiResponseModel>(jsonResponse);

            if (!d.ReturnCode)
            {
                throw new Exception($"Failed with message {d.ErrorMessage}. Log correlation id={correlationId}.");
            }

            var hits = d
                .ScreenResult
                .Where(x => x.Match == true)
                .SelectMany(x => x.Persons.Select(y => new
                {
                    x.CustomerReferenceId,
                    QualityInt = Numbers.ParseInt32OrNull(y.Quality) ?? 1, //1 is the best so we default to that to make sure it's not filtered out
                    Hit = new KycScreeningListHit
                    {
                        IsPepHit = y.IsPEP.GetValueOrDefault() || y.IsRCA.GetValueOrDefault(),
                        IsSanctionHit = y.IsSanction.GetValueOrDefault(),
                        Addresses = null,
                        BirthDate = null,
                        Comment = $"Gender = {y.Gender}, Quality = {y.Quality}",
                        ExternalId = y.PersonId,
                        ExternalUrls = null,
                        Name = (y.PrimaryFirstname + " " + y.PrimaryLastname)?.Trim(),
                        SourceName = y.SourceSystem,
                        Ssn = null,
                        Title = null
                    }
                }))
                .Where(x => x.QualityInt <= settings.QualityCutoff)
                .GroupBy(x => x.CustomerReferenceId)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Hit).ToList());

            var allKeys = hits.Keys.Concat(items.Select(x => x.ItemId)).ToHashSet();
            return allKeys.ToDictionary(x => x, x => hits.ContainsKey(x) ? hits[x] : new List<KycScreeningListHit>());
        }

        private string FormatCivicRegNr(string civicRegNrRaw)
        {
            if (baseCountry == "SE" && CivicRegNumberSe.TryParse(civicRegNrRaw, out var n))
            {
                return $"{n.NormalizedValue.Substring(0, 8)}-{n.NormalizedValue.Substring(8, 4)}";
            }

            return civicRegNrRaw;
        }

        private class ApiResponseModel
        {
            public bool ReturnCode { get; set; }
            public string ErrorMessage { get; set; }
            public List<ResultItem> ScreenResult { get; set; }

            public class ResultItem
            {
                public string CustomerReferenceId { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public bool? Checked { get; set; }
                public bool? Match { get; set; }
                public bool? IsPEP { get; set; }
                public bool? IsSanction { get; set; }
                public List<PersonItem> Persons { get; set; }
                public string ErrorMessage { get; set; }
            }

            public class PersonItem
            {
                public string PersonId { get; set; }
                public string PrimaryFirstname { get; set; }
                public string PrimaryLastname { get; set; }
                public bool? IsPEP { get; set; }
                public bool? IsSanction { get; set; }
                public bool? IsRCA { get; set; }
                public string Quality { get; set; }
                public string Gender { get; set; }
                public string SourceSystem { get; set; }
            }
        }
    }
}