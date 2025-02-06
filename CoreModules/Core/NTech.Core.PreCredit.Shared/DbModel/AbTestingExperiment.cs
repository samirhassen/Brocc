using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit.DbModel
{
    public class AbTestingExperiment : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string ExperimentName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxCount { get; set; }
        public bool IsActive { get; set; }
        public decimal? VariationPercent { get; set; }
        public string VariationName { get; set; }
        public int CreatedById { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }
}