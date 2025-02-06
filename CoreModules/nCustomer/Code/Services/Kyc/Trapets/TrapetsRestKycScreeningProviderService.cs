using Newtonsoft.Json;
using NTech.Banking.Shared.Globalization;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace nCustomer.Code.Services.Kyc.Trapets
{
    public class TrapetsRestKycScreeningProviderService : IKycScreeningProviderService
    {
        private Uri endpointUrl;
        private readonly bool useCivicRegNr;
        private TimeSpan timeout;
        private Action<string> rawLog;
        private readonly SelfRefreshingTrapetsKycRestAccessToken standardAccessToken;
        private readonly SelfRefreshingTrapetsKycRestAccessToken alternateSingleQueryAccessToken;

        public TrapetsRestKycScreeningProviderService(NTechSimpleSettings settings, Action<string> rawLog = null)
        {
            this.endpointUrl = new Uri(settings.Req("endpointUrl"));
            this.useCivicRegNr = settings.OptBool("useCivicRegNr");
            this.timeout = TimeSpan.FromSeconds(int.Parse(settings.Opt("timeoutInSeconds") ?? "30"));
            this.rawLog = rawLog;

            this.standardAccessToken = new SelfRefreshingTrapetsKycRestAccessToken(this.endpointUrl, settings.Req("username"), settings.Req("password"));

            var alternateSingleQueryUsername = settings.Opt("alternateSingleQueryUsername");

            if (alternateSingleQueryUsername != null)
                this.alternateSingleQueryAccessToken = new SelfRefreshingTrapetsKycRestAccessToken(this.endpointUrl,
                    alternateSingleQueryUsername,
                    settings.Req("alternateSingleQueryPassword"));
        }

        public IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All)
        {
            var client = new RestClient();
            var accessToken = items.Count == 1 && alternateSingleQueryAccessToken != null ? alternateSingleQueryAccessToken : standardAccessToken;

            var queryUrl = NTechServiceRegistry.CreateUrl(endpointUrl, "api/listlookup/do-query");
            var request = new
            {
                ServiceScope = ServiceScopeFromListCode(list),
                SubQueries = items.Select(x => new
                {
                    Id = x.ItemId,
                    Countries = ConvertCountries(x),
                    ItemDate = x.BirthDate.ToString("yyyy-MM-dd"),
                    Name = x.FullName,
                    ItemNumber = useCivicRegNr ? x.CivicRegNr : null //Setting to enable matching the use of the old soap service implementation that did not include civic regnr
                }).ToList()
            };
            var result = client.SendRequest(request,
                HttpMethod.Post,
                queryUrl.ToString(),
                bearerToken: accessToken.AccessToken,
                timeout: timeout);
            if (!result.Response.IsSuccessStatusCode)
                throw new NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethodException($"Trapets key screening attempt failed with http status {result.Response.StatusCode}");
            var jsonResult = result.ReadJsonBodyIfAny();
            var response = JsonConvert.DeserializeObject<TrapetsRestQueryResponse>(jsonResult);

            var queryItemById = items.ToDictionary(x => x.ItemId);
            IDictionary<string, List<KycScreeningListHit>> interpretation = response.ResponseItems.Select(x => Tuple.Create(x.SubQueryId, x.Individuals == null
                     ? new List<KycScreeningListHit>()
                     : x.Individuals.Select(y => new KycScreeningListHit
                     {
                         BirthDate = queryItemById.Opt(x.SubQueryId)?.BirthDate, //Note. BirthDate in the rest service is now a freetext string which can be anything so we ignore that
                         ExternalId = y.ExternalId?.NormalizeNullOrWhitespace(),
                         ExternalUrls = y.ExternalUrls == null ? new List<string>() : y.ExternalUrls.Select(z => z?.NormalizeNullOrWhitespace()).Where(z => z != null).ToList(),
                         IsPepHit = (y.ListType ?? "").ToLowerInvariant() == "pep",
                         IsSanctionHit = (y.ListType ?? "").ToLowerInvariant() == "sanction",
                         Name = y.Name?.NormalizeNullOrWhitespace(),
                         SourceName = y.SourceName?.NormalizeNullOrWhitespace(),
                         Ssn = y.Ssn?.NormalizeNullOrWhitespace(),
                         Title = y.Title?.NormalizeNullOrWhitespace(),
                         Comment = y.Comment?.NormalizeNullOrWhitespace(),
                         Addresses = y.Addresses == null ? new List<string>() : y.Addresses.Select(z => ConvertResponseAddressToString(z)).Where(z => z != null).ToList()
                     }).ToList()))
             .GroupBy(x => x.Item1)
             .ToDictionary(x => x.Key, x => x.SelectMany(y => y.Item2).ToList());

            if (rawLog != null)
            {
                var rs = JsonConvert.SerializeObject(new
                {
                    requestRaw = request,
                    resultRaw = jsonResult,
                    resultInterpreted = interpretation
                }, Formatting.Indented);
                rawLog(rs);
            }

            return interpretation;
        }

        private List<string> ConvertCountries(KycScreeningQueryItem item)
        {
            var countries = item?.TwoLetterIsoCountryCodes;
            if (countries == null || countries.Count == 0)
                return null;
            return countries.Select(x => NTechCountry.FromTwoLetterIsoCode(x).ThreeLetterIsoCountryCode).ToList();

        }

        private string ServiceScopeFromListCode(KycScreeningListCode code)
        {
            switch (code)
            {
                case KycScreeningListCode.Pep:
                    return "PEP";

                case KycScreeningListCode.Sanction:
                    return "SANCTION";

                case KycScreeningListCode.All:
                    return null;

                default:
                    throw new NotImplementedException();
            }
        }

        private string ConvertResponseAddressToString(TrapetsRestQueryResponse.Address address)
        {
            if (address == null)
                return null;
            var adr = $"{address.Street} {address.ZipCode} {address.City}".NormalizeNullOrWhitespace();
            //They return country even when there is no address so we dont want those at all
            return adr == null ? null : $"{adr} {address.Country}"?.NormalizeNullOrWhitespace();
        }

        private class TrapetsRestQueryResponse
        {
            public List<Warning> Warnings { get; set; }
            public List<Error> Errors { get; set; }
            public int? QueryCount { get; set; }
            public List<string> Services { get; set; }
            public List<ResponseItem> ResponseItems { get; set; }

            public class Warning
            {
                public string SubQueryId { get; set; }
                public int? Index { get; set; }
                public string Message { get; set; }
            }

            public class Error
            {
                public string SubQueryId { get; set; }
                public int? Index { get; set; }
                public string Message { get; set; }
            }

            public class Address
            {
                public string Street { get; set; }
                public string ZipCode { get; set; }
                public string City { get; set; }
                public string Country { get; set; }
            }

            public class Individual
            {
                public string Title { get; set; }
                public string Ssn { get; set; }
                public string ExternalId { get; set; }
                public string Name { get; set; }
                public string LastUpdate { get; set; }
                public string SourceName { get; set; }
                public string ListType { get; set; }
                public string Comment { get; set; }
                public List<Address> Addresses { get; set; }
                public List<string> ExternalUrls { get; set; }
            }

            public class ResponseItem
            {
                public string SubQueryId { get; set; }
                public List<Individual> Individuals { get; set; }
            }
        }

        private class SelfRefreshingTrapetsKycRestAccessToken
        {
            private readonly Uri serviceBaseUrl;
            private readonly string username;
            private readonly string password;

            private object lockObject = new object();
            private string accessToken;
            private DateTimeOffset tokenExpirationDate;

            public SelfRefreshingTrapetsKycRestAccessToken(Uri serviceBaseUrl, string username, string password)
            {
                this.serviceBaseUrl = serviceBaseUrl;
                this.username = username;
                this.password = password;
            }

            public string AccessToken
            {
                get
                {
                    if (accessToken == null || tokenExpirationDate < DateTimeOffset.UtcNow)
                    {
                        lock (lockObject)
                        {
                            if (accessToken == null || tokenExpirationDate < DateTimeOffset.UtcNow)
                            {
                                var client = new HttpClientSync();
                                var tokenUrl = NTechServiceRegistry.CreateUrl(serviceBaseUrl, "token");

                                var kvps = new[]
                                {
                                    new KeyValuePair<string, string>("username", username),
                                    new KeyValuePair<string, string>("password", password),
                                    new KeyValuePair<string, string>("grant_type", "password")
                                };
                                var message = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                                message.Content = new FormUrlEncodedContent(kvps);
                                message.Headers.Add("Cache-Control", "No-cache");
                                var response = client.Send(message);
                                if (!response.IsSuccessStatusCode)
                                {
                                    string actualErrorMessage = null;
                                    try
                                    {
                                        if (response.Content.Headers.ContentLength > 0 && response.Content.IsJson())
                                        {
                                            //Example: {"error":"Internal Error","error_description":"Unauthorized: Username 'ekomni_uat_apisingle' or password is wrong."}
                                            var rawContent = response.Content.ReadAsString();
                                            actualErrorMessage = JsonConvert.DeserializeAnonymousType(rawContent, new { error_description = "" })?.error_description;
                                        }
                                    }
                                    catch { /* ignored */ }

                                    throw new NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethodException($"Could not aquire trapets access token: {actualErrorMessage ?? "No error message from trapets"}")
                                    {
                                        ErrorCode = "errorResponseFromTrapets",
                                        IsUserFacing = false
                                    };
                                }

                                var content = response.Content;

                                if (!content.IsJson())
                                    throw new NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethodException("Could not aquire trapets access token")
                                    {
                                        ErrorCode = "nonJsonResponseFromTrapets",
                                        IsUserFacing = false
                                    };

                                var tokenResult = JsonConvert.DeserializeAnonymousType(content.ReadAsString(), new { access_token = "", expires_in = (int?)null });

                                accessToken = tokenResult.access_token?.NormalizeNullOrWhitespace();
                                if (accessToken == null)
                                    throw new NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethodException("Could not aquire trapets access token")
                                    {
                                        ErrorCode = "responseIsMissingToken",
                                        IsUserFacing = false
                                    };
                                //Trapets default is said to be 24 hours (~80k seconds) which is what we get in test also.
                                //We use min to guard against insane things like getting 0 back which would cause us to just spam the access token endpoint
                                var expirationSeconds = Math.Min(tokenResult.expires_in ?? 300, 300);
                                tokenExpirationDate = DateTimeOffset.UtcNow.AddSeconds(expirationSeconds);
                            }
                        }
                    }
                    return accessToken;
                }
            }
        }
    }
}