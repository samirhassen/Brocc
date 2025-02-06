using System.Collections.Generic;

namespace nPreCredit
{
    public class PartialCreditApplicationModelRequest
    {
        public bool ErrorIfGetNonLoadedField { get; set; }
        public List<string> ApplicationFields { get; set; }
        public List<string> ApplicantFields { get; set; }
        public List<string> DocumentFields { get; set; }
        public List<string> QuestionFields { get; set; }
        public List<string> CreditreportFields { get; set; }
        public List<string> ExternalFields { get; set; }
        public bool LoadChangedBy { get; set; }
    }
}