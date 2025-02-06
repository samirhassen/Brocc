using BalanziaSe.Scoring.PointRules;
using NTech.Banking.ScoringEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring
{
    public class BalanziaSeCompanyLoanInitialScoringProcess : PluginScoringProcess
    {
        public BalanziaSeCompanyLoanInitialScoringProcess() :
            base(new List<MinimumDemandPass>
            {
                new MinimumDemandPass
                {
                    PassName = "Internal",
                    Rules = new List<MinimumDemandScoringRule>
                    {
                        new InitialCompanyAgeScoringRule(),
                        new InitialCompanyYearlyRevenueScoringRule(),
                        new InitialActiveLoanRule(),
                        new InitialPaymentHistoryRule(),
                        new InitialHistoricalDebtCollectionRule(),
                        new PausedByPriorApplicationRule(),
                        new ActiveApplicationRule(),
                        new CashflowSensitivityRule()
                    }
                },
                new MinimumDemandPass
                {
                    PassName = "External",
                    Rules = new List<MinimumDemandScoringRule>
                    {
                        new ExternalCompanyAgeScoringRule(),
                        new ExternalCreditReportRiskClassRule(),
                        new ExternalCreditReportCompanyTypeRule(),
                        new ExternalCreditReportCompanyStatusRule(),
                        new ExternalCreditReportCompanyKeyNumbersRule(),
                        new ExternalBoardMembershipAgeRule(),
                        new ExternalKFMRiskRule()
                    }
                }
            }, new List<WeightedDecimalScorePointScoringRule>()
            {
                new NetRevenuePointsRule(),
                new NetRevenueYearlyChangePointsRule(),
                new AdjustedEKPointsPointsRule(),
                new ReturnOnCapitalPointsRule(),
                new CashLiquidityPointsRule(),
                new SolidityPointsRule(),
                new CreditReportRiskClassPointsRule(),
                new CreditReportRiskPercentPointsRule(),
                new CreditReportParentRiskClassPointsRule(),
                new CreditReportCompanyAgePointsRule(),
                new BoardMemberMonthsPointsRule(),
                new BoardMemberBankruptcyPointsRule(),
                new BoardMemberPaymentRemarkPointsRule(),
                new BoardMemberBankruptcyAppPointsRule(),
                new BoardMemberRevisorKodPointsRule(),
                new InternalHistoryPointsRule(),
                new NetDebtEbitApproximationPointsRule(),
                new ManagementCompetencyPointsRule(),
                new LoanPurposePointsRule()
            }, new BalanziaSeCompanyLoanPricingModelRule())
        {
        }

        public override string Name => "CompanyLoanInitial";

        public override IScoringDataModelConsumer PrefetchedVariables => prefetchedVariables;

        private DirectScoringDataConsumer prefetchedVariables = new DirectScoringDataConsumer
        {
            RequiredApplicantItems = new HashSet<string>(),
            RequiredApplicationItems = new HashSet<string>
            {
                "applicationAmount",
                "applicationRepaymentTimeInMonths"
            }
        };

        private class DirectScoringDataConsumer : IScoringDataModelConsumer
        {
            public ISet<string> RequiredApplicationItems { get; set; }

            public ISet<string> RequiredApplicantItems { get; set; }
        }
    }
}
