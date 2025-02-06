using System;
using System.Collections.Generic;

namespace nPreCredit.DbModel
{
    public class Campaign
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? InactivatedOrDeletedByUserId { get; set; }
        public DateTime? InactivatedOrDeletedDate { get; set; }
        public virtual List<CampaignCode> CampaignCodes { get; set; }
    }
}