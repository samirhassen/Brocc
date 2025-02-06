using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit
{
    public class WorkListItem : InfrastructureBaseItem
    {
        public int WorkListHeaderId { get; set; }
        public WorkListHeader WorkList { get; set; }
        public string ItemId { get; set; }
        public int OrderNr { get; set; }
        public int? TakenByUserId { get; set; }
        public DateTime? TakenDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public virtual IList<WorkListItemProperty> Properties { get; set; }
    }
}