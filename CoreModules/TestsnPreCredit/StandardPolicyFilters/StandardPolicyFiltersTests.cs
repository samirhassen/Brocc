using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using nPreCredit.Code.StandardPolicyFilters;
using nPreCredit.Code.StandardPolicyFilters.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit.StandardPolicyFilters
{
    [TestClass]
    public class StandardPolicyFiltersTests
    {
        [TestMethod]
        public void MinAllowedLeftToLiveOnRule_BelowMinimum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MinAllowedLeftToLiveOn");

            //5000 < 0 => Accepted
            Application_ShouldBeAccepted(
                SingleApplicationVariable("leftToLiveOnAmount", "5000"),
                SingleIntStaticParameter("minLeftToLiveOnAmount", 0), r);

            //-5000 < 0 => Rejected
            Application_ShouldBeRejected(
                SingleApplicationVariable("leftToLiveOnAmount", "-5000"),
                SingleIntStaticParameter("minLeftToLiveOnAmount", 0), r);

            //-500 < -1000 => Accepted
            Application_ShouldBeAccepted(
                SingleApplicationVariable("leftToLiveOnAmount", "-500"),
                SingleIntStaticParameter("minLeftToLiveOnAmount", -1000), r);

            //-5000 < -1000 => Rejected
            Application_ShouldBeRejected(
                SingleApplicationVariable("leftToLiveOnAmount", "-5000"),
                SingleIntStaticParameter("minLeftToLiveOnAmount", -1000), r);
        }

        [TestMethod]
        public void MinAllowedApplicantAge_BelowMinimum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MinAllowedApplicantAge");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantAgeInYears", "35", "17"),
                SingleIntStaticParameter("minApplicantAgeInYears", 18), r);
        }

        [TestMethod]
        public void MaxAllowedApplicantAge_AboveMaximum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedApplicantAge");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantAgeInYears", "60", "70"),
                SingleIntStaticParameter("maxApplicantAgeInYears", 65), r);
        }

        [TestMethod]
        public void BannedEmployment_InList_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("BannedEmployment");
            var variables = SingleApplicantVariable("applicantEmploymentFormCode", "self_employed", "unemployed");
            var parameterSet = StaticParameterSet.CreateEmpty().SetStringList("bannedEmploymentFormCodes", new List<string> { "unemployed" });

            Applicants_FirstAccepted_SecondRejected(variables, parameterSet, r);
        }

        [TestMethod]
        public void BannedEmployment_CanRender_RuleSetListUi()
        {
            var r = RuleFactory.GetRuleByName("BannedEmployment");

            var parameterSet = StaticParameterSet.CreateEmpty().SetStringList("bannedEmploymentFormCodes", new List<string>
            {
                NTech.Services.Infrastructure.CreditStandard.CreditStandardEmployment.Code.full_time.ToString(),
                NTech.Services.Infrastructure.CreditStandard.CreditStandardEmployment.Code.part_time.ToString()
            });

            //Rule column
            Assert.AreEqual("Banned employment forms", r.GetDisplayName(ClientCountry, "en"));

            //Description column
            Assert.AreEqual("v:applicantEmploymentFormCode in s:bannedEmploymentFormCodes", r.GetDescription(ClientCountry, "en"));

            //Static parameters column
            Assert.AreEqual("s:bannedEmploymentFormCodes=[Full time,Part time]", r.GetStaticParametersDisplay(ClientCountry, "en", parameterSet, true));
        }

        [TestMethod]
        public void MinAllowedApplicantAge_CanRender_PolicyFilterDetailsUi()
        {
            var ruleSet = new RuleSet
            {
                InternalRules = new RuleAndStaticParameterValues[]
                {
                    new RuleAndStaticParameterValues("MinAllowedApplicantAge", SingleIntStaticParameter("minApplicantAgeInYears", 18), RuleFactory.GetRuleByName("MinAllowedApplicantAge").DefaultRejectionReasonName)
                }
            };
            var dataSource = new SingleValueDataSource("applicantAgeInYears", applicant1Value: "35", applicant2Value: "16");
            var engine = new PolicyFilterEngine();

            var result = engine.Evaluate(ruleSet, dataSource);

            Assert.AreEqual(false, result.IsAcceptRecommended);
            Assert.AreEqual(true, result.InternalResult.IsRejectedByAnyRule());

            //////////////////
            /// Applicant 1 //
            //////////////////            
            var applicant1Result = result.InternalResult.RuleResults[0];
            var r = RuleFactory.GetRuleByName(applicant1Result.RuleName);

            //Rule
            Assert.AreEqual("Min allowed applicant age", r.GetDisplayName(ClientCountry, DisplayLanguage));

            //Applicant
            Assert.AreEqual("1", applicant1Result.ForApplicantNr?.ToString());

            //Policy
            Assert.AreEqual("18", r.GetStaticParametersDisplay(ClientCountry, DisplayLanguage, applicant1Result.StaticParameters, false));

            //Result
            Assert.AreEqual(false, applicant1Result.IsRejectedByRule);
            Assert.AreEqual("35", r.GetVariableDisplay(ClientCountry, DisplayLanguage, applicant1Result.GetScopedVariables(result.VariableSet)));

            //////////////////
            /// Applicant 2 //
            //////////////////            
            var applicant2Result = result.InternalResult.RuleResults[1];
            r = RuleFactory.GetRuleByName(applicant2Result.RuleName);

            //Rule
            Assert.AreEqual("Min allowed applicant age", r.GetDisplayName(ClientCountry, DisplayLanguage));

            //Applicant
            Assert.AreEqual("2", applicant2Result.ForApplicantNr?.ToString());

            //Policy
            Assert.AreEqual("18", r.GetStaticParametersDisplay(ClientCountry, DisplayLanguage, applicant2Result.StaticParameters, false));

            //Result
            Assert.AreEqual(true, applicant2Result.IsRejectedByRule);
            Assert.AreEqual("16", r.GetVariableDisplay(ClientCountry, DisplayLanguage, applicant2Result.GetScopedVariables(result.VariableSet)));
        }

        [TestMethod]
        public void MinAllowedApplicantIncome_BelowMinimum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MinAllowedApplicantIncome");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantIncomePerMonth", "25000", "10000"),
                SingleIntStaticParameter("minApplicantIncomePerMonth", 15000), r);
        }

        [TestMethod]
        public void MaxAllowedApplicantIncome_AboveMaximum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedApplicantIncome");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantIncomePerMonth", "60000", "80000"),
                SingleIntStaticParameter("maxApplicantIncomePerMonth", 75000), r);
        }

        [TestMethod]
        public void MaxAllowedMainApplicantPaymentRemarks_AboveMaximum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedMainApplicantPaymentRemarks");
            var p = SingleIntStaticParameter("maxNrOfPaymentRemarks", 1);

            Application_ShouldBeAccepted(
                SingleApplicationVariable("mainApplicantCreditReportNrOfPaymentRemarks", "1"),
                p, r);

            Application_ShouldBeRejected(
                SingleApplicationVariable("mainApplicantCreditReportNrOfPaymentRemarks", "2"),
                p, r);
        }

        [TestMethod]
        public void MaxAllowedCoApplicantPaymentRemarks_NoCoApplicant_IsAccepted()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedCoApplicantPaymentRemarks");
            var p = SingleDecimalStaticParameter("maxNrOfPaymentRemarks", 1);

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("coApplicantCreditReportNrOfPaymentRemarks", "noCoApp"),
                p, r);
        }

        [TestMethod]
        public void MaxAllowedMainApplicantCreditReportRiskValueRule_AboveMaximum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedMainApplicantCreditReportRiskValue");
            var p = SingleDecimalStaticParameter("maxCreditReportRiskValue", 7.5m);

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("mainApplicantCreditReportRiskValue", 6.5m),
                p, r);

            Application_ShouldBeRejected(
                new VariableSet(1).SetApplicationValue("mainApplicantCreditReportRiskValue", 8.5m),
                p, r);
        }

        [TestMethod]
        public void MaxAllowedCoApplicantCreditReportRiskValueRule_AboveMaximum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedCoApplicantCreditReportRiskValue");
            var p = SingleDecimalStaticParameter("maxCreditReportRiskValue", 7.5m);

            Application_ShouldBeAccepted(
                new VariableSet(2).SetApplicationValue("coApplicantCreditReportRiskValue", 6.5m),
                p, r);

            Application_ShouldBeRejected(
                new VariableSet(2).SetApplicationValue("coApplicantCreditReportRiskValue", 8.5m),
                p, r);
        }

        [TestMethod]
        public void MaxAllowedCoApplicantCreditReportRiskValueRule_NoCoApplicant_IsAccepted()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedCoApplicantCreditReportRiskValue");
            var p = SingleDecimalStaticParameter("maxCreditReportRiskValue", 7.5m);

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("coApplicantCreditReportRiskValue", "noCoApp"),
                p, r);
        }

        [TestMethod]
        public void ApplicantHasKfmBalanceRule_HasKfmBalance_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantHasKfmBalance");

            Applicants_FirstAccepted_SecondRejected(new VariableSet(2)
                .SetApplicantValue("applicantCreditReportHasKfmBalance", 1, false)
                .SetApplicantValue("applicantCreditReportHasKfmBalance", 2, true),
                null, r);
        }

        [TestMethod]
        public void ApplicantHasBoxAddress_HasBoxAddress_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantHasBoxAddress");

            Applicants_FirstAccepted_SecondRejected(new VariableSet(2)
                .SetApplicantValue("applicantHasBoxAddress", 1, false)
                .SetApplicantValue("applicantHasBoxAddress", 2, true),
                null, r);
        }

        [TestMethod]
        public void ApplicantHasPosteRestanteAddress_HasBoxAddress_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantHasPosteRestanteAddress");

            Applicants_FirstAccepted_SecondRejected(new VariableSet(2)
                .SetApplicantValue("applicantHasPosteRestanteAddress", 1, false)
                .SetApplicantValue("applicantHasPosteRestanteAddress", 2, true),
                null, r);
        }

        [TestMethod]
        public void MaxInternalVsExternalIncomePercent_PercentDiffOverMax_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxInternalVsExternalIncomePercentDifference");

            Applicants_FirstAccepted_SecondRejected(new VariableSet(2)
                .SetApplicantValue("applicantCreditReportIncomePerMonth", 1, 1000)
                .SetApplicantValue("applicantIncomePerMonth", 1, 1050)
                .SetApplicantValue("applicantCreditReportIncomePerMonth", 2, 1000)
                .SetApplicantValue("applicantIncomePerMonth", 2, 1150),
                SingleIntStaticParameter("maxAllowedIncomeDiffPercent", 10), r);
        }

        [TestMethod]
        public void MaxInternalVsExternalIncomePercent_OneIncomeZero_IsOneHundredPercentDiff()
        {
            var r = RuleFactory.GetRuleByName("MaxInternalVsExternalIncomePercentDifference");
            var variables = new VariableSet(1)
                .SetApplicantValue("applicantCreditReportIncomePerMonth", 1, 0)
                .SetApplicantValue("applicantIncomePerMonth", 1, 950000);

            var displayValue = r.GetVariableDisplay(ClientCountry, DisplayLanguage, new ScopedVariableSet(variables, 1));

            Assert.AreEqual("100,00 % (950000 vs 0)", displayValue);
        }

        [TestMethod]
        public void MaxInternalVsExternalIncomePercent_BothIncomesZero_IsZeroPercentDiff()
        {
            var r = RuleFactory.GetRuleByName("MaxInternalVsExternalIncomePercentDifference");
            var variables = new VariableSet(1)
                .SetApplicantValue("applicantCreditReportIncomePerMonth", 1, 0)
                .SetApplicantValue("applicantIncomePerMonth", 1, 0);

            var displayValue = r.GetVariableDisplay(ClientCountry, DisplayLanguage, new ScopedVariableSet(variables, 1));

            Assert.AreEqual("0,00 % (0 vs 0)", displayValue);
        }

        [TestMethod]
        public void MaxInternalVsExternalIncomePercent_StaticParameter_DisplayedWithPercentSign()
        {
            var r = RuleFactory.GetRuleByName("MaxInternalVsExternalIncomePercentDifference");
            var parameters = StaticParameterSet.CreateEmpty().SetPercent("maxAllowedIncomeDiffPercent", 12.34m);

            var displayValue = r.GetStaticParametersDisplay(ClientCountry, DisplayLanguage, parameters, false);

            Assert.AreEqual("12,34 %", displayValue);
        }

        [TestMethod]
        public void ApplicantMissingAddress_NotHasAddress_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantMissingAddress");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantHasAddress", "true", "false"),
                null, r);
        }

        [TestMethod]
        public void MaxAllowedDbrRule_DbrAboveMax_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedDbr");
            var p = StaticParameterSet.CreateEmpty().SetDecimal("maxAllowedDbr", 1.4m);

            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("debtBurdenRatio", 1.39m), p, r);
            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("debtBurdenRatio", 1.41m), p, r);
        }

        [TestMethod]
        public void ApplicantHasGuardian_HasGuardianTrue_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantHasGuardian");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantHasGuardian", "false", "true"),
                null, r);
        }

        [TestMethod]
        public void ApplicantStatusCode_StatusCodeNotNormal_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantStatusCode");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantStatusCode", "normal", "deactivated"),
                null, r);
        }

        [TestMethod]
        public void ApplicantHasActiveLoan_OneTrueOneFalse_ShouldMatchBoth()
        {
            var rule = RuleFactory.GetRuleByName("ApplicantHasActiveLoan");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantHasLoanInSystem", "false", "true"),
                null, rule);
        }

        [TestMethod]
        public void ApplicantHasOtherActiveApplication_Test()
        {
            var rule = RuleFactory.GetRuleByName("ApplicantHasOtherActiveApplication");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("applicantHasOtherActiveApplicationsInSystem", "false", "true"),
                null, rule);
        }

        [TestMethod]
        public void RuleAddUi_Data_CanBeRendered()
        {
            var rulesNames = RuleFactory.GetAllRuleNames();
            var data = rulesNames.Select(x =>
            {
                var rule = RuleFactory.GetRuleByName(x);
                return new PolicyFilterRuleUiModel
                {
                    RuleName = rule.Name,
                    RuleDisplayName = rule.GetDisplayName(ClientCountry, DisplayLanguage),
                    Description = rule.GetDescription(ClientCountry, DisplayLanguage),
                    StaticParameters = rule.StaticParameters.Select(y => new PolicyFilterRuleUiModel.StaticParameter
                    {
                        Name = y.Name,
                        IsList = y.IsList,
                        TypeCode = y.TypeCode.ToString(),
                        Options = y.Options?.Select(z => new PolicyFilterRuleUiModel.Option
                        {
                            Code = z.Value,
                            DisplayName = z.GetDisplayName(DisplayLanguage)
                        }).ToList(),
                    }).ToList()
                };
            }).ToList();
            Console.WriteLine(JsonConvert.SerializeObject(data));
        }

        [TestMethod]
        public void ApplicantMissingCreditReportConsent_MissingConsent_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("ApplicantMissingCreditReportConsent");

            Applicants_FirstAccepted_SecondRejected(
                SingleApplicantVariable("isApplicantMissingCreditReportConsent", "false", "true"),
                null, r);
        }

        [TestMethod]
        public void ApplicantMissingCreditReportConsent_BooleanDisplay_IsYesNo()
        {
            var r = RuleFactory.GetRuleByName("ApplicantMissingCreditReportConsent");
            var variables = SingleApplicantVariable("isApplicantMissingCreditReportConsent", "true", "false");

            var displays = string.Join("_", new[] { 1, 2 }.Select(x =>
                r.GetVariableDisplay("SE", "sv", new ScopedVariableSet(variables, x))));

            Assert.AreEqual("Yes_No", displays);
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_CanDetectInvalidExpressions()
        {
            Assert.IsFalse(BannedPropertyZipCodeRule.IsZipCodeExpressionValid(""));
            Assert.IsFalse(BannedPropertyZipCodeRule.IsZipCodeExpressionValid(null));
            Assert.IsFalse(BannedPropertyZipCodeRule.IsZipCodeExpressionValid("1111y")); //Invalid char y
            Assert.IsFalse(BannedPropertyZipCodeRule.IsZipCodeExpressionValid("12345**"));
            Assert.IsFalse(BannedPropertyZipCodeRule.IsZipCodeExpressionValid("*1234")); //Wildcards must be suffix
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_OneCharPrefix()
        {
            var expression = "1*";
            Assert.IsTrue(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "19999"));
            Assert.IsFalse(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "29999"));
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_TwoCharPrefix()
        {
            var expression = "12*";
            Assert.IsTrue(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "12999"));
            Assert.IsFalse(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "19999"));
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_Exact()
        {
            var expression = "12340";
            Assert.IsTrue(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "12340"));
            Assert.IsFalse(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "19999"));
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_Comma_Means_Or()
        {
            var expression = "12340,54*,74321";
            Assert.IsTrue(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "12340"));
            Assert.IsTrue(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "54321"));
            Assert.IsTrue(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "74321"));
            Assert.IsFalse(BannedPropertyZipCodeRule.DoesZipCodeMatchExpression(expression, "19999"));
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_ExpressionMatch_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("BannedPropertyZipCode");
            var p = StaticParameterSet.CreateEmpty().SetString("bannedZipCodesExpression", "12*");

            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("objectZipCode", "12340"), p, r);
            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("objectZipCode", "22340"), p, r);
        }

        [TestMethod]
        public void BannedPropertyZipCodeRule_RangeExpression_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("BannedPropertyZipCode");
            var p = StaticParameterSet.CreateEmpty().SetString("bannedZipCodesExpression", "12*-145*");

            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("objectZipCode", "12340"), p, r);
            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("objectZipCode", "14599"), p, r);
            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("objectZipCode", "12999"), p, r);
            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("objectZipCode", "13999"), p, r);

            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("objectZipCode", "14999"), p, r);
            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("objectZipCode", "22340"), p, r);
            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("objectZipCode", "14600"), p, r);
        }

        [TestMethod]
        public void MaxAllowedLtiRule_LtiAboveMax_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedLti");
            var p = StaticParameterSet.CreateEmpty().SetDecimal("maxAllowedLti", 1.4m);

            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("loanToIncome", 1.39m), p, r);
            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("loanToIncome", 1.41m), p, r);
        }

        [TestMethod]
        public void MaxAllowedLtvPercentRule_LtvPercentAboveMax_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MaxAllowedLtvPercent");
            var p = StaticParameterSet.CreateEmpty().SetDecimal("maxAllowedLtvPercent", 70m);

            Application_ShouldBeAccepted(new VariableSet(1).SetApplicationValue("loanToValuePercent", 69.99m), p, r);
            Application_ShouldBeRejected(new VariableSet(1).SetApplicationValue("loanToValuePercent", 70.01m), p, r);
        }

        [TestMethod]
        public void MinAllowedSettlementInterestRateRule_BelowMinimum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MinAllowedSettlementInterestRate");

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("minSettlementInterestRatePercent", 11m).SetApplicationValue("hasLoansToSettle", true),
                SingleDecimalStaticParameter("minInterestRatePercent", 10m), r);

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("hasLoansToSettle", false),
                SingleDecimalStaticParameter("minInterestRatePercent", 10m), r);

            Application_ShouldBeRejected(
                new VariableSet(1).SetApplicationValue("minSettlementInterestRatePercent", 9m).SetApplicationValue("hasLoansToSettle", true),
                SingleDecimalStaticParameter("minInterestRatePercent", 10m), r);
        }

        [TestMethod]
        public void MinAllowedWeightedAverageSettlementInterestRateRule_BelowMinimum_IsRejected()
        {
            var r = RuleFactory.GetRuleByName("MinAllowedWeightedAverageSettlementInterestRate");

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("weightedAverageSettlementInterestRatePercent", 11m).SetApplicationValue("hasLoansToSettle", true),
                SingleDecimalStaticParameter("minInterestRatePercent", 10m), r);

            Application_ShouldBeAccepted(
                new VariableSet(1).SetApplicationValue("hasLoansToSettle", false),
                SingleDecimalStaticParameter("minInterestRatePercent", 10m), r);

            Application_ShouldBeRejected(
                new VariableSet(1).SetApplicationValue("weightedAverageSettlementInterestRatePercent", 9m).SetApplicationValue("hasLoansToSettle", true),
                SingleDecimalStaticParameter("minInterestRatePercent", 10m), r);
        }

        #region "Helper code"
        private const string ClientCountry = "SE";
        private const string DisplayLanguage = "sv";

        private class SingleValueDataSource : IPolicyFilterDataSource
        {
            private readonly string variableName;
            private readonly string applicationValue;
            private readonly string applicant1Value;
            private readonly string applicant2Value;

            public SingleValueDataSource(string variableName, string applicationValue = null, string applicant1Value = null, string applicant2Value = null)
            {
                this.variableName = variableName;
                this.applicationValue = applicationValue;
                this.applicant1Value = applicant1Value;
                this.applicant2Value = applicant2Value;
            }

            public VariableSet LoadVariables(ISet<string> applicationVariableNames, ISet<string> applicantVariableNames)
            {
                var result = new VariableSet(applicant2Value != null ? 2 : 1);
                if (applicationValue != null && applicationVariableNames.Contains(variableName))
                    result.SetApplicationValue(variableName, applicationValue);
                if (applicant1Value != null && applicantVariableNames.Contains(variableName))
                    result.SetApplicantValue(variableName, 1, applicant1Value);
                if (applicant2Value != null && applicantVariableNames.Contains(variableName))
                    result.SetApplicantValue(variableName, 2, applicant2Value);
                return result;
            }
        }

        private void AssertIsRejected(bool? expectedResult, RuleResult actualResult, Rule rule, VariableSet variables)
        {
            if (actualResult.IsRejectedByRule == expectedResult)
            {
                Assert.AreEqual(rule.IsEvaluatedPerApplicant, actualResult.ForApplicantNr.HasValue, "Rule evaluated on the wrong level (applicant vs application)");
            }
            else
            {
                if (actualResult.IsSkipped)
                {
                    Assert.Fail($"Failed because evaluation was skipped. Missing: {actualResult.MissingVariableName}");
                }
                else if (actualResult.IsMissingApplicationLevelVariable)
                {
                    Assert.Fail($"Failed because of a missing application level variable: {actualResult.MissingVariableName}");
                }
                else if (actualResult.IsMissingApplicantLevelVariable)
                {
                    Assert.Fail($"Failed because of a missing applicant level variable: {actualResult.MissingVariableName} for applicants: {string.Join(",", actualResult.MissingApplicantLevelApplicantNrs)}");
                }
                else
                {
                    Assert.AreEqual(IsRejectedToString(expectedResult), IsRejectedToString(actualResult.IsRejectedByRule), JsonConvert.SerializeObject(new { result = actualResult, variables = variables }, Formatting.Indented));
                }
            }
        }

        private string IsRejectedToString(bool? isRejected)
        {
            if (!isRejected.HasValue)
                return "unknown";
            else
                return isRejected.Value ? "rejected" : "accepted";
        }

        private VariableSet SingleApplicationVariable(string name, string value)
        {
            var v = new VariableSet(1);
            v.SetApplicationValue(name, value);
            return v;
        }

        private VariableSet SingleApplicantVariable(string name, string valueApplicant1, string valueApplicant2)
        {
            var v = new VariableSet(valueApplicant2 == null ? 1 : 2);
            if (!string.IsNullOrWhiteSpace(valueApplicant1))
                v.SetApplicantValue(name, 1, valueApplicant1);
            if (!string.IsNullOrWhiteSpace(valueApplicant2))
                v.SetApplicantValue(name, 2, valueApplicant2);
            return v;
        }

        private StaticParameterSet SingleIntStaticParameter(string name, int value) => StaticParameterSet.CreateEmpty().SetInt(name, value);
        private StaticParameterSet SingleDecimalStaticParameter(string name, decimal value) => StaticParameterSet.CreateEmpty().SetDecimal(name, value);

        private void Application_ShouldBeRejected(VariableSet variables, StaticParameterSet parameterSet, Rule rule) =>
            RunTestScenario(variables, parameterSet, 1, rule, x =>
            {
                if (x == null) return true;
                throw new Exception("Invalid context. Expected application only");
            });

        private void Application_ShouldBeAccepted(VariableSet variables, StaticParameterSet parameterSet, Rule rule) =>
            RunTestScenario(variables, parameterSet, 1, rule, x =>
            {
                if (x == null) return false;
                throw new Exception("Invalid context. Expected application only");
            });

        private void Applicants_FirstAccepted_SecondRejected(VariableSet variables, StaticParameterSet parameterSet, Rule rule)
        {
            RunTestScenario(variables, parameterSet, 2, rule, x =>
            {
                if (x.HasValue)
                    return x.Value == 1 ? false : true;
                else
                    throw new Exception("Invalid context. Expected applicant only");
            });
        }

        private void RunTestScenario(VariableSet variables, StaticParameterSet parameterSet, int nrOfApplicants, Rule rule, Func<int?, bool?> getExpectedResult)
        {
            parameterSet = parameterSet ?? StaticParameterSet.CreateEmpty();
            void RunScopedScenario(int? forApplicantNr)
            {
                var context = new EvaluateRuleContext(new ScopedVariableSet(variables, forApplicantNr), parameterSet);
                var result = rule.EvaluateRule(context);
                AssertIsRejected(getExpectedResult(forApplicantNr), result, rule, variables);
            }
            if (rule.IsEvaluatedPerApplicant)
            {
                foreach (var applicantNr in Enumerable.Range(1, nrOfApplicants))
                    RunScopedScenario(applicantNr);
            }
            else
            {
                RunScopedScenario(null);
            }
        }
        #endregion
    }

    public class PolicyFilterRuleUiModel
    {
        public string RuleName { get; set; }
        public string RuleDisplayName { get; set; }
        public string Description { get; set; }
        public List<StaticParameter> StaticParameters { get; set; }

        public class StaticParameter
        {
            public string Name { get; set; }
            public bool IsList { get; set; }
            public string TypeCode { get; set; }
            public List<Option> Options { get; set; }
        }

        public class Option
        {
            public string Code { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
