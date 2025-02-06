using NTech.Banking.CivicRegNumbers;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using System.Collections.Generic;
using System.Linq;

namespace nCustomerPages.Code
{
    public class SystemUserCustomerClient : AbstractSystemUserServiceClient, ISystemUserCustomerClient
    {
        protected override string ServiceName => "nCustomer";

        private class GetCustomerIdResult
        {
            public int CustomerId { get; set; }
        }

        public int GetCustomerId(ICivicRegNumber civicRegNr)
        {
            return Begin().PostJson("api/CustomerIdByCivicRegNr", new
            {
                civicRegNr = civicRegNr.NormalizedValue,
            }).ParseJsonAs<GetCustomerIdResult>().CustomerId;
        }

        public IDictionary<string, int> GetCustomerIdsByCivicRegNrs(params string[] civicRegNrs)
        {
            return Begin().PostJson("Customer/GetCustomerIdsByCivicRegNrs", new { civicRegNrs })
            .ParseJsonAsAnonymousType(new { CustomerIdsByCivicRegNrs = (Dictionary<string, int>)null })
            .CustomerIdsByCivicRegNrs;
        }

        public CommonElectronicIdSignatureSession HandleProviderSignatureEvent(Dictionary<string, string> providerEventData)
        {
            return Begin()
                .PostJson("api/ElectronicSignatures/Handle-Event", new { providerEventData })
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null })
                ?.Session;
        }

        public CommonElectronicIdSignatureSession GetSignatureSessioBySearchTerm(string customSearchTermName, string customSearchTermValue)
        {
            return Begin()
                .PostJson("api/ElectronicSignatures/Get-Session", new { customSearchTermName, customSearchTermValue })
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null })
                ?.Session;
        }

        public CommonElectronicIdSignatureSession GetSignatureSessionByLocalSessionId(string sessionId)
        {
            return Begin()
                .PostJson("api/ElectronicSignatures/Get-Session", new { sessionId })
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null })
                ?.Session;
        }

        private class CustomerPropertyModel
        {
            public string Name { get; set; }
            public string Group { get; set; }
            public int CustomerId { get; set; }
            public string Value { get; set; }
            public bool IsSensitive { get; set; }
        }

        public IDictionary<string, string> GetCustomerCardItems(int customerId, params string[] names)
        {
            return Begin()
                .PostJson("Customer/GetDecryptedProperties", new
                {
                    customerId = customerId,
                    names = new HashSet<string>(names).ToList()
                })
                .ParseJsonAs<List<CustomerPropertyModel>>()
                .ToDictionary(x => x.Name, x => x.Value);
        }

        public ICivicRegNumber GetCivicRegNumber(int customerId)
        {
            var c = GetCustomerCardItems(customerId, "civicRegNr");
            if (c == null || c.Count != 1)
                return null;
            else
                return NEnv.BaseCivicRegNumberParser.Parse(c.Single().Value);
        }

        public (CommonElectronicAuthenticationSession Session, bool WasAuthenticated) HandleElectronicIdAuthenticationProviderEvent(string localSessionId, Dictionary<string, string> providerEventData)
        {
            return Begin()
                .PostJson("Api/ElectronicIdAuthentication/Handler-Provider-Event", new { localSessionId, providerEventData })
                .HandlingApiError(x =>
                {
                    var result = x.ParseJsonAsAnonymousType(new { Session = (CommonElectronicAuthenticationSession)null, WasAuthenticated = (bool?)null });
                    return (Session: result?.Session, WasAuthenticated: result?.WasAuthenticated ?? false);
                }, x =>
                {
                    if (x.ErrorCode == "noSuchSessionExists")
                        return (Session: (CommonElectronicAuthenticationSession)null, WasAuthenticated: false);
                    else
                        throw new System.Exception(x.ErrorCode);
                });
        }

        public CommonElectronicAuthenticationSession GetElectronicIdAuthenticationSession(string localSessionId)
        {
            return Begin()
                .PostJson("Api/ElectronicIdAuthentication/Get-Session", new { localSessionId })
                .HandlingApiError(
                    x => x.ParseJsonAsAnonymousType(new { Session = (CommonElectronicAuthenticationSession)null })?.Session,
                    x =>
                    {
                        if (x.ErrorCode == "noSuchSessionExists")
                            return null;
                        else
                            throw new System.Exception(x.ErrorCode);
                    });
        }

        /// <summary>
        /// You can use the token {localSessionId} in returnUrl and it will be replaced with the actual local sessions id after it's been created.
        /// </summary>
        public CommonElectronicAuthenticationSession CreateElectronicIdAuthenticationSession(string civicRegNumber, Dictionary<string, string> customData, string returnUrl)
        {
            return Begin()
                .PostJson("Api/ElectronicIdAuthentication/Create-Session", new { civicRegNumber, customData, returnUrl })
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicAuthenticationSession)null })
                ?.Session;
        }

        public Dictionary<string, string> LoadSettings(string settingCode) =>
            Begin()
                .PostJson("api/Settings/LoadValues", new { settingCode })
                .ParseJsonAsAnonymousType(new { SettingValues = (Dictionary<string, string>)null })
                ?.SettingValues;
    }

    public interface ISystemUserCustomerClient
    {
        IDictionary<string, int> GetCustomerIdsByCivicRegNrs(params string[] civicRegNrs);
        Dictionary<string, string> LoadSettings(string settingCode);
    }
}