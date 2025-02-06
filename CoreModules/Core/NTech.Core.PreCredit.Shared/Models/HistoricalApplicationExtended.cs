using NTech.Banking.ScoringEngine;
using System;

namespace NTech.Core.PreCredit.Shared.Models
{
    public class HistoricalApplicationExtended : HistoricalApplication
    {
        public DateTimeOffset? CurrentCreditDecisionDate { get; set; }
    }
}
