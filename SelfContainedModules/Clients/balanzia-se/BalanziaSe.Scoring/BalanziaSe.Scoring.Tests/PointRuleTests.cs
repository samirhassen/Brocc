using System;
using BalanziaSe.Scoring.PointRules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Banking.ScoringEngine;

namespace BalanziaSe.Scoring.Tests
{
    [TestClass]
    public class PointRuleTests : ScoringRuleTestBase
    {
        [TestMethod]
        public void NetRevenuePointRule()
        {
            var r = new NetRevenuePointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattning", Kilo(3500), null)
                .Set("creditReportBokslutDatum", "2019-01-01", null),
                1.0m, 4m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattning", 0m, null)
                .Set("creditReportBokslutDatum", "2019-01-01", null),
                1.0m, 1m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattning", Kilo(3500), null)
                .Set("creditReportBokslutDatum", "missing", null),
                 1.0m, 5m);
        }

        [TestMethod]
        public void NetRevenueYearlyChangePointsRule()
        {
            var r = new NetRevenueYearlyChangePointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattningFg", Kilo(2800), null)
                .Set("creditReportNettoOmsattning", Kilo(3500), null),      
                 0.54m, 10.0m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattningFg", Kilo(0), null)
                .Set("creditReportNettoOmsattning", Kilo(3500), null),
                 0.54m, 5m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattningFg", Kilo(2800), null)
                .Set("creditReportNettoOmsattning", Kilo(0), null),
                 0.54m, 1m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattningFg", Kilo(2800), null)
                .Set("creditReportNettoOmsattning", "missing", null),
                 0.54m, 1m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattningFg", "missing", null)
                .Set("creditReportNettoOmsattning", Kilo(3500), null),
                 0.54m, 5m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportNettoOmsattningFg", "missing", null)
                .Set("creditReportNettoOmsattning", "missing", null),
                 0.54m, 5m);
        }

        [TestMethod]
        public void AdjustedEKPointsRule()
        {
            var r = new AdjustedEKPointsPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportSummaEgetKapital", Kilo(750), null)
                .Set("creditReportSummaObeskattadeReserver", Kilo(0), null)
                .Set("creditReportSummaImmateriellaTillgangar", Kilo(0), null),
                 3m, 5m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportSummaEgetKapital", Kilo(750), null)
                .Set("creditReportSummaObeskattadeReserver", Kilo(1000), null)
                .Set("creditReportSummaImmateriellaTillgangar", Kilo(0), null),
                 3m, 10m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportSummaEgetKapital", Kilo(750), null)
                .Set("creditReportSummaObeskattadeReserver", Kilo(1000), null)
                .Set("creditReportSummaImmateriellaTillgangar", Kilo(1000), null),
                 3m, 3m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportSummaEgetKapital", Kilo(0), null)
                .Set("creditReportSummaObeskattadeReserver", Kilo(0), null)
                .Set("creditReportSummaImmateriellaTillgangar", Kilo(1000), null),
                 3m, 1m);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportSummaEgetKapital", 88038000, null)
                .Set("creditReportSummaObeskattadeReserver", "missing", null)
                .Set("creditReportSummaImmateriellaTillgangar", 0, null),
                 3m, 15m);
        }

        [TestMethod]
        public void ReturnOnCapitalPointsRule()
        {
            var r = new ReturnOnCapitalPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "missing", null)
                .Set("creditReportAvkastningTotKapProcent", "missing", null), 
                1, 5);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportAvkastningTotKapProcent", "8.0", null),
                1, 4);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportAvkastningTotKapProcent", "19.7", null),
                1, 9);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportAvkastningTotKapProcent", "56", null),
                1, 15);
        }

        [TestMethod]
        public void CashLiquidityRule()
        {
            var r = new CashLiquidityPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "missing", null)
                .Set("creditReportKassalikviditetProcent", "missing", null),
                1, 5);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportKassalikviditetProcent", "0.01", null), 
                1, 0);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportKassalikviditetProcent", "0.01", null),
                1, 0);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportKassalikviditetProcent", "50", null),
                1, 5);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportKassalikviditetProcent", "115", null),
                1, 11);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportKassalikviditetProcent", "160", null),
                1, 15);
        }

        [TestMethod]
        public void SolidityPointsRule()
        {
            var r = new SolidityPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "missing", null)
                .Set("creditReportSoliditetProcent", "missing", null),
                1, 5);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportSoliditetProcent", "0.01", null),
                1, 1);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportSoliditetProcent", "14", null),
                1, 3);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportBokslutDatum", "2019-01-01", null)
                .Set("creditReportSoliditetProcent", "150", null),
                1, 15);
        }

        [TestMethod]
        public void CreditReportRiskClassRule()
        {
            var r = new CreditReportRiskClassPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskklassForetag", "missing", null),
                2, 0);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskklassForetag", "1", null),
                2, 2);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskklassForetag", "4", null),
                2, 11);
        }

        [TestMethod]
        public void CreditReportRiskPercentPointsRule()
        {
            var r = new CreditReportRiskPercentPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskprognosForetagProcent", "missing", null),
                3, 0);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskprognosForetagProcent", "900", null),
                3, 0);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskprognosForetagProcent", "8", null),
                3, 1);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskprognosForetagProcent", "4", null),
                3, 2);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskprognosForetagProcent", "0.8", null),
                3, 10);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportRiskprognosForetagProcent", "0.5", null),
                3, 15);
        }

        [TestMethod]
        public void CreditReportParentRiskClassPointsRule()
        {
            var r = new CreditReportParentRiskClassPointsRule();

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportModerbolagRiskklassForetag", "missing", null)
                .Set("creditReportAntalModerbolag", "0", null)
                .Set("creditReportRiskklassForetag", "missing", null),
                2, 0);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportModerbolagRiskklassForetag", "missing", null)
                .Set("creditReportAntalModerbolag", "0", null)
                .Set("creditReportRiskklassForetag", "4", null),
                2, 11);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportModerbolagRiskklassForetag", "1", null)
                .Set("creditReportAntalModerbolag", "1", null)
                .Set("creditReportRiskklassForetag", "missing", null),
                2, 2);

            AssertScorePoints(r, new ScoringDataModel()
                .Set("creditReportModerbolagRiskklassForetag", "5", null)
                .Set("creditReportAntalModerbolag", "1", null)
                .Set("creditReportRiskklassForetag", "missing", null),
                2, 14);
        }

        [TestMethod]
        public void CreditReportCompanyAgePointsRule()
        {
            var r = new CreditReportCompanyAgePointsRule();

            AssertScorePoints(r, 
                new ScoringDataModel().Set("creditReportForetagAlderIManader", "missing", null), 
                2, 0);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportForetagAlderIManader", "60", null),
                2, 10);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportForetagAlderIManader", "120", null),
                2, 15);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportForetagAlderIManader", "200", null),
                2, 15); 
        }

        [TestMethod]
        public void BoardMemberMonthsPointsRule()
        {
            var r = new BoardMemberMonthsPointsRule();

            AssertScorePoints(r, 
                new ScoringDataModel().Set("creditReportAntalStyrelseLedamotsManader", "missing", null),
                2, 0);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportAntalStyrelseLedamotsManader", "1", null),
                2, 0);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportAntalStyrelseLedamotsManader", "10", null),
                2, 1);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportAntalStyrelseLedamotsManader", "40", null),
                2, 6);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportAntalStyrelseLedamotsManader", "60", null),
                2, 10);


            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportAntalStyrelseLedamotsManader", "100", null),
                2, 15);
        }

        [TestMethod]
        public void BoardMemberBankruptcyPointsRule()
        {
            var r = new BoardMemberBankruptcyPointsRule();

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseKonkursengagemang", "missing", null),
                3, 0);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseKonkursengagemang", "true", null),
                3, 1);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseKonkursengagemang", "false", null),
                3, 8);
        }

        [TestMethod]
        public void BoardMemberPaymentRemarkPointsRule()
        {
            var r = new BoardMemberPaymentRemarkPointsRule();

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseBetAnmarkningar", "missing", null),
                1, 0);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseBetAnmarkningar", "true", null),
                1, 1);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseBetAnmarkningar", "false", null),
                1, 8);
        }

        [TestMethod]
        public void BoardMemberBankruptcyAppPointsRule()
        {
            var r = new BoardMemberBankruptcyAppPointsRule();

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseKonkursansokningar", "missing", null),
                2, 0);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseKonkursansokningar", "true", null),
                2, 1);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportFinnsStyrelseKonkursansokningar", "false", null),
                2, 8);
        }

        [TestMethod]
        public void BoardMemberRevisorKodPointsRule()
        {
            var r = new BoardMemberRevisorKodPointsRule();

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportStyrelseRevisorKod", "Ingen", null),
                1, 1);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportStyrelseRevisorKod", "Godkänd revisor", null),
                1, 5);

            AssertScorePoints(r,
                new ScoringDataModel().Set("creditReportStyrelseRevisorKod", "Auktoriserad revisor", null),
                1, 8);
        }

        [TestMethod]
        public void NetDebtEbitApproximationPointsRule()
        {
            var r = new NetDebtEbitApproximationPointsRule();

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationCompanyYearlyResult", "750", null)
                .Set("applicationCompanyCurrentDebtAmount", "7500", null),
                5, 1);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationCompanyYearlyResult", "7500", null)
                .Set("applicationCompanyCurrentDebtAmount", "7500", null),
                5, 16);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationCompanyYearlyResult", "750", null)
                .Set("applicationCompanyCurrentDebtAmount", "1500", null),
                5, 12);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationCompanyYearlyResult", "300", null)
                .Set("applicationCompanyCurrentDebtAmount", "2100", null),
                5, 5);
        }

        [TestMethod]
        public void ManagementCompetencyPointsRule()
        {
            var r = new ManagementCompetencyPointsRule();

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("creditReportForetagAlderIManader", "5", null),
                4, 0);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("creditReportForetagAlderIManader", "36", null),
                4, 5);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("creditReportForetagAlderIManader", "96", null),
                4, 8);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("creditReportForetagAlderIManader", "130", null),
                4, 14);
        }

        [TestMethod]
        public void LoanPurposePointsRule()
        {
            var r = new LoanPurposePointsRule();

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationLoanPurposeCode", "Förvärv", null),
                3, 7);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationLoanPurposeCode", "Annat", null),
                3, 3);

            AssertScorePoints(r,
                new ScoringDataModel()
                .Set("applicationLoanPurposeCode", "Inköp av lager", null),
                3, 12);
        }
    }
}
