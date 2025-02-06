using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services.Kyc.Trapets
{
    public class TrapetsSoapKycScreeningProviderService : IKycScreeningProviderService
    {
        private string endpointUrl;
        private TimeSpan timeout;
        private Action<string> rawLog;
        private readonly bool skipSoundex;
        private readonly Tuple<string, string> usernameAndPassword;
        private readonly Tuple<string, string> alternateSingleQueryUsernameAndPassword;

        public TrapetsSoapKycScreeningProviderService(string username, string password, string endpointUrl, TimeSpan? timeout = null, Action<string> rawLog = null, bool skipSoundex = false, Tuple<string, string> alternateSingleQueryUsernameAndPassword = null)
        {
            this.endpointUrl = endpointUrl;
            this.timeout = timeout ?? TimeSpan.FromSeconds(15);
            this.rawLog = rawLog;
            this.skipSoundex = skipSoundex;
            this.usernameAndPassword = Tuple.Create(username, password);
            this.alternateSingleQueryUsernameAndPassword = alternateSingleQueryUsernameAndPassword;
        }

        private string ListNameFromListCode(KycScreeningListCode code)
        {
            switch (code)
            {
                case KycScreeningListCode.Pep:
                    return "Pep";

                case KycScreeningListCode.Sanction:
                    return "Sanction";

                case KycScreeningListCode.All:
                    return null;

                default:
                    throw new NotImplementedException();
            }
        }

        public IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All)
        {
            Func<string, string> n = t => string.IsNullOrWhiteSpace(t) ? null : t.Trim();

            var unp = (items.Count == 1 ? this.alternateSingleQueryUsernameAndPassword : usernameAndPassword) ?? usernameAndPassword;

            var s = new TrapetsKycService.ListLookupSoapClient(CreateBinding(timeout), new System.ServiceModel.EndpointAddress(endpointUrl));
            try
            {
                var requestRaw = new
                {
                    methodName = "DoQuery",
                    parameters = new
                    {
                        username = unp.Item1,
                        password = unp.Item2,
                        serviceName = ListNameFromListCode(list),
                        items = items.Select(x => new TrapetsKycService.QueryAttributes
                        {
                            AttributeId = x.ItemId,
                            Name = x.FullName,
                            BirthDate = x.BirthDate.ToString("yyyy-MM-dd"),
                            Countries = x.TwoLetterIsoCountryCodes == null
                                ? null
                                : x.TwoLetterIsoCountryCodes.Select(y => new TrapetsKycService.Country { TwoLetter = y }).ToArray()
                        }).ToArray(),
                        matchAllNames = true,
                        useSoundex = !this.skipSoundex
                    }
                };
                var result = s
                    .DoQuery(requestRaw.parameters.username, requestRaw.parameters.password, requestRaw.parameters.serviceName, requestRaw.parameters.items, requestRaw.parameters.matchAllNames, requestRaw.parameters.useSoundex);

                IDictionary<string, List<KycScreeningListHit>> interpretation = result.Select(x => Tuple.Create(x.AttributeId, x.Individuals == null
                         ? new List<KycScreeningListHit>()
                         : x.Individuals.Select(y => new KycScreeningListHit
                         {
                             BirthDate = y.BirthDate,
                             ExternalId = n(y.ExternalId),
                             ExternalUrls = y.ExternalUrls == null ? new List<string>() : y.ExternalUrls.Select(z => n(z)).Where(z => z != null).ToList(),
                             IsPepHit = (y.ListType ?? "").ToLowerInvariant() == "pep",
                             IsSanctionHit = (y.ListType ?? "").ToLowerInvariant() == "sanction",
                             Name = n(y.Name),
                             SourceName = n(y.SourceName),
                             Ssn = n(y.Ssn),
                             Title = n(y.Title),
                             Comment = n(y.Comment),
                             Addresses = y.Addresses == null ? new List<string>() : y.Addresses.Select(z => n(z)).Where(z => z != null).ToList()
                         }).ToList()))
                 .GroupBy(x => x.Item1)
                 .ToDictionary(x => x.Key, x => x.SelectMany(y => y.Item2).ToList());

                if (rawLog != null)
                {
                    var rs = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        requestRaw = requestRaw,
                        resultRaw = result,
                        resultInterpreted = interpretation
                    }, Newtonsoft.Json.Formatting.Indented);
                    rawLog(rs);
                }

                return interpretation;
            }
            catch
            {
                try { s.Close(); } catch { }
                throw;
            }
        }

        private System.ServiceModel.Channels.Binding CreateBinding(TimeSpan timeout)
        {
            var b = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport);
            b.MaxReceivedMessageSize = 2097152; //2GB
            b.SendTimeout = timeout;
            b.ReceiveTimeout = timeout;
            b.CloseTimeout = timeout;
            return b;
        }
    }
}