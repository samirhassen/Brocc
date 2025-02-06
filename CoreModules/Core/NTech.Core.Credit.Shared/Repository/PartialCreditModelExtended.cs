namespace nCredit.DbModel.Repository
{
    public class PartialCreditModelExtended<TExtraCreditData> : PartialCreditModel
    {
        public PartialCreditModelExtended(PartialCreditModelRequestSet requestSet, PartialCreditModelBasicCreditData basicCreditData, TExtraCreditData extraCreditData)
            : base(requestSet, basicCreditData)
        {
            ExtraData = extraCreditData;
        }

        public TExtraCreditData ExtraData { get; }
    }
}