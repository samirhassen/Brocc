using NTech.ElectronicSignatures;
using System;

namespace nCredit.Code
{

    public class CommonSignatureClient : AbstractSystemUserServiceClient, ICommonSignatureClient
    {
        protected override string ServiceName => "nCustomer";

        public CommonElectronicIdSignatureSession CreateSession(SingleDocumentSignatureRequestUnvalidated request)
        {
            return Begin()
                .PostJson("api/ElectronicSignatures/Create-Session", request)
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null })
                ?.Session;
        }

        public CommonElectronicIdSignatureSession GetSession(string sessionId, bool firstCloseItIfOpen, Action<bool> observeWasClosed = null)
        {
            var response = Begin()
                .PostJson("api/ElectronicSignatures/Get-Session", new { sessionId, firstCloseItIfOpen });

            if (response.IsApiError && response.ParseApiError().ErrorCode == "noSuchSessionExists")
            {
                return null;
            }

            var result = response
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null, WasClosed = (bool?)null });

            if (result.WasClosed.HasValue)
                observeWasClosed?.Invoke(result.WasClosed.Value);

            return result?.Session;
        }
    }
}
