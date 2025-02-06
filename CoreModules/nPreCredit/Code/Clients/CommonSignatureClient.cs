using NTech.ElectronicSignatures;

namespace nPreCredit.Code
{
    public class CommonSignatureClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nCustomer";

        public CommonElectronicIdSignatureSession CreateSession(SingleDocumentSignatureRequest request)
        {
            return Begin()
                .PostJson("api/ElectronicSignatures/Create-Session", request)
                .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null })
                ?.Session;
        }

        public (CommonElectronicIdSignatureSession Session, bool WasClosed, bool IsUnsupportedSessionType, bool IsNoSuchSessionExists) GetSession(string sessionId, bool firstCloseItIfOpen, bool allowUnsupportedSessionType, bool allowSessionDoesNotExist = false)
        {
            var response = Begin()
                .PostJson("api/ElectronicSignatures/Get-Session", new { sessionId, firstCloseItIfOpen });
            if (!response.IsApiError)
            {
                var result = response
                    .ParseJsonAsAnonymousType(new { Session = (CommonElectronicIdSignatureSession)null, WasClosed = (bool?)null });

                return (Session: result?.Session, WasClosed: result.WasClosed.GetValueOrDefault(), IsUnsupportedSessionType: false, IsNoSuchSessionExists: false);
            }
            else
            {
                var apiError = response.ParseApiError();
                if (allowUnsupportedSessionType && apiError.ErrorCode == "unsupportedSessionType")
                {
                    return (Session: null, WasClosed: false, IsUnsupportedSessionType: true, IsNoSuchSessionExists: false);
                }
                else if(allowSessionDoesNotExist && apiError.ErrorCode == "noSuchSessionExists")
                {
                    return (Session: null, WasClosed: false, IsUnsupportedSessionType: false, IsNoSuchSessionExists: true);
                }                    
                else
                {
                    response.EnsureSuccessStatusCode();
                    return default((CommonElectronicIdSignatureSession Session, bool WasClosed, bool IsUnsupportedSessionType, bool IsNoSuchSessionExists)); //This will never happen, ensure will throw the compiler just doesnt know that.
                }
            }
        }
    }
}
