using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public abstract class CreditDecision : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public string DecisionType { get; set; }
        public DateTimeOffset DecisionDate { get; set; }
        public bool WasAutomated { get; set; }
        public int DecisionById { get; set; }
        public virtual List<CreditDecisionPauseItem> PauseItems { get; set; }
        public virtual List<CreditDecisionSearchTerm> SearchTerms { get; set; }
        public virtual List<CreditDecisionItem> DecisionItems { get; set; }

        public class CreditDecisionJsModel
        {
            public int Id { get; set; }
            public string ApplicationNr { get; set; }
            public DateTimeOffset DecisionDate { get; set; }
            public bool WasAutomated { get; set; }
            public int DecisionById { get; set; }
            public List<PauseItemJsModel> PauseItems { get; set; }
            public List<ItemJsModel> Items { get; set; }
            public string RejectedDecisionModel { get; set; }
            public string AcceptedDecisionModel { get; set; }
        }
        public class PauseItemJsModel
        {
            public int Id { get; set; }
            public string ApplicationNr { get; set; }
            public string RejectionReasonName { get; set; }
            public int CustomerId { get; set; }
            public DateTime PausedUntilDate { get; set; }
            public int CreditDecisionId { get; set; }
        }
        public class ItemJsModel
        {
            public string ItemName { get; set; }
            public bool IsRepeatable { get; set; }
            public string Value { get; set; }
        }

        public CreditDecisionJsModel ToJsModel(bool arePauseItemsLoaded, bool areItemsLoaded)
        {
            return new CreditDecisionJsModel
            {
                Id = this.Id,
                ApplicationNr = this.ApplicationNr,
                WasAutomated = this.WasAutomated,
                DecisionById = this.DecisionById,
                DecisionDate = this.DecisionDate,
                AcceptedDecisionModel = (this as AcceptedCreditDecision)?.AcceptedDecisionModel,
                RejectedDecisionModel = (this as RejectedCreditDecision)?.RejectedDecisionModel,
                PauseItems = arePauseItemsLoaded ? this.PauseItems?.Select(x => new PauseItemJsModel
                {
                    Id = x.Id,
                    ApplicationNr = this.ApplicationNr,
                    CreditDecisionId = x.CreditDecisionId,
                    CustomerId = x.CustomerId,
                    PausedUntilDate = x.PausedUntilDate,
                    RejectionReasonName = x.RejectionReasonName
                })?.ToList() : null,
                Items = areItemsLoaded ? this.DecisionItems?.Select(x => new ItemJsModel
                {
                    ItemName = x.ItemName,
                    IsRepeatable = x.IsRepeatable,
                    Value = x.Value
                })?.ToList() : null
            };
        }
    }

    public enum CreditDecisionTypeCode
    {
        Initial,
        Final
    }
}