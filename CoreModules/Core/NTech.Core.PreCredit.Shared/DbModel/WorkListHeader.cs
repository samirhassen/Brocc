using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nPreCredit.DbModel
{
    public class WorkListHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string ListType { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int? ClosedByUserId { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string CustomData { get; set; }
        public bool IsUnderConstruction { get; set; }
        public virtual IList<WorkListItem> Items { get; set; }
        public virtual IList<WorkListFilterItem> FilterItems { get; set; }
    }
}