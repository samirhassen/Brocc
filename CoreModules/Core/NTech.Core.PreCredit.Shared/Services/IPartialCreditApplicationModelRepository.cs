using System.Collections.Generic;

namespace nPreCredit
{
    public interface IPartialCreditApplicationModelRepository
    {
        bool ExistsAll(string applicationNr, out string missingFieldsMessage, List<string> applicationFields = null, List<string> applicantFields = null, List<string> documentFields = null, List<string> questionFields = null, List<string> externalFields = null);
        PartialCreditApplicationModel Get(string applicationNr, List<string> applicationFields = null, List<string> applicantFields = null, List<string> documentFields = null, List<string> questionFields = null, List<string> creditreportFields = null, List<string> externalFields = null, bool errorIfGetNonLoadedField = false);
        PartialCreditApplicationModel Get(string applicationNr, PartialCreditApplicationModelRequest request);
    }
}