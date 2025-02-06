using System.Collections.Generic;

namespace nCredit.Code
{
    public interface ISecureMessageService
    {
        bool SendSecureMessageWithTemplate(string templateName, string channelType, int customerId, string creditNr, Dictionary<string, string> mines, bool throwIfError);
    }
}