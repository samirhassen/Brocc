using NTech.ElectronicSignatures;
using System;

namespace nCredit.Code
{
    public interface ICommonSignatureClient
    {
        CommonElectronicIdSignatureSession CreateSession(SingleDocumentSignatureRequestUnvalidated request);
        CommonElectronicIdSignatureSession GetSession(string sessionId, bool firstCloseItIfOpen, Action<bool> observeWasClosed = null);
    }
}
