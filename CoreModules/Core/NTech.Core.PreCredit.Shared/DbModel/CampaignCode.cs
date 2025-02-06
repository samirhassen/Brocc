using System;

namespace nPreCredit.DbModel
{
    public class CampaignCode
    {
        public int Id { get; set; }
        public string CampaignId { get; set; }
        public Campaign Campaign { get; set; }
        public string Code { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? DelatedDate { get; set; }
        public int? DeletedByUserId { get; set; }
        public string CommentText { get; set; }
        public bool IsGoogleCampaign { get; set; }
    }
}