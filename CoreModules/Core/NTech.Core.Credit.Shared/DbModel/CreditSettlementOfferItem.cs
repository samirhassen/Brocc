using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditSettlementOfferItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int CreatedByEventId { get; set; }
        public int CreditSettlementOfferHeaderId { get; set; }
        public CreditSettlementOfferHeader CreditSettlementOffer { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public enum CreditSettlementOfferItemCode
        {
            SettlementAmount,
            SwedishRseEstimatedAmount,
            SwedishRseInterestRatePercent
        }
    }
}