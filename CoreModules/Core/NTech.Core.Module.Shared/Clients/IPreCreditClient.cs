using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Module.Shared.Clients
{
    public interface IPreCreditClient
    {
        void AddCommentToApplication(string applicationNr, string commentText, int? customerSecureMessageId);
        void ReportKycQuestionSessionCompleted(string sessionId);
        void LoanStandardApproveKycStep(string applicationNr, bool isApproved, bool isAutomatic);
        IDictionary<string, string> GetApplicationNrsByCreditNrs(ISet<string> creditNrs);

        /// <summary>
        /// Example: 
        /// BulkFetchCreditApplicationItems(["A123"], ["customerId"])
        /// { "A123" : { "applicant1" : { "customerId" : "42" }, "applicant2" : { "customerId" : "43" } }
        /// </summary>
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> BulkFetchCreditApplicationItems(BulkFetchCreditApplicationItemsRequest request);
    }

    public class BulkFetchCreditApplicationItemsRequest
    {
        [Required]
        public List<string> ApplicationNrs { get; set; }
        public List<string> ItemNames { get; set; }
    }
}
