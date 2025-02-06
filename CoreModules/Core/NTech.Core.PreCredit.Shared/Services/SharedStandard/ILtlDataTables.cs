namespace nPreCredit.Code.Services.SharedStandard
{
    public interface ILtlDataTables
    {
        decimal? IncomeTaxMultiplier { get; }
        int? DefaultChildAgeInYears { get; }
        decimal StressInterestRatePercent { get; }
        int? DefaultApplicantAgeInYears { get; }
        decimal GetHouseholdMemberCountCost(int memberCount);
        decimal GetIndividualAgeCost(int ageInYears);
        bool CreditsUse360DayInterestYear { get; }
    }
}