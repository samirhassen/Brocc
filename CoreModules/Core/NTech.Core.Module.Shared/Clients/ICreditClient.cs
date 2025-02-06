using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public interface ICreditClient
    {
        List<HistoricalCreditExtended> GetCustomerCreditHistory(List<int> customerIds);
        string NewCreditNumber();
        Task<List<HistoricalCreditExtended>> GetCustomerCreditHistoryByCreditNrsAsync(List<string> creditNrs);
        List<HistoricalCreditExtended> GetCustomerCreditHistoryByCreditNrs(List<string> creditNrs);
        Task<decimal> GetCurrentReferenceInterestAsync();
        decimal GetCurrentReferenceInterest();
        CreateCreditCommentResponse CreateCreditComment(string creditNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl,
            string attachedFileName, int? customerSecureMessageId);
        int CreateCreditCommentCore(string creditNr, string commentText, string eventType, string attachedFileAsDataUrl,
            string attachedFileName, int? customerSecureMessageId);
        Task<List<CustomCostClientItem>> GetCustomCostsAsync();
        List<CustomCostClientItem> GetCustomCosts();
        Task CreateCreditsAsync(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests);
        void CreateCredits(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests);
    }

    public class CustomCostClientItem
    {
        public string Code { get; set; }
        public string Text { get; set; }
    }

    public class CreateCreditCommentResponse
    {
        public int Id { get; set; }
        public CommentModel comment { get; set; }

        public class CommentModel
        {
            public string EventType { get; set; }
            public DateTimeOffset CommentDate { get; set; }
            public string CommentText { get; set; }
            public List<string> ArchiveLinks { get; set; }
            public string DisplayUserName { get; set; }
            public int? CustomerSecureMessageId { get; set; }
        }
    }


    //NOTE: Make sure when using this for other than ul legacy that CustomerCreditHistoryCoreRepository has replaced CustomerCreditHistoryRepository everywhere
    public class HistoricalCreditExtended : HistoricalCredit
    {
        public decimal? InitialCapitalBalance { get; set; }
        public decimal? InitialAnnuityAmount { get; set; }
        public decimal? InitialMarginInterestRatePercent { get; set; }
        public decimal? InitialReferenceInterestRatePercent { get; set; }
        public decimal? InitialCapitalizedInitialFeeAmount { get; set; }
        public decimal? InitialNotificationFeeAmount { get; set; }
    }
}
