using Newtonsoft.Json.Linq;
using nPreCredit.Code;
using nPreCredit.DbModel;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace nPreCredit.Controllers.Api
{
    public partial class ApiUpdateDataWarehouseController
    {
        private void Merge_Fact_CreditApplicationSnapshot(DateTime transactionDate)
        {
            Func<string, DateTime?> parseMonth = s =>
            {
                if (s == null)
                    return null;
                DateTime d;
                if (DateTime.TryParseExact(s + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                    return d;
                return null;
            };


            Func<string, DateTime?> parseDate = s =>
            {
                if (s == null)
                    return null;
                DateTime d;
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                    return d;
                return null;
            };

            Func<string, decimal?> parseDec = s =>
            {
                if (s == null)
                    return null;
                decimal d;
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d))
                    return d;
                return null;
            };

            Func<string, int?> parseInt = s =>
            {
                if (s == null)
                    return null;
                int d;
                if (int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d))
                    return d;
                return null;
            };

            Func<string, bool?> parseBool = s =>
            {
                if (s == null)
                    return null;

                return s.ToLower() == "true";
            };

            Func<List<CreditApplicationSnapshotModel>, PreCreditContext, List<ExpandoObject>> toDwItems = (items, context) =>
            {
                var encryptedItems = items.SelectMany(x => x.FetchedItems).Where(x => x.IsEncrypted).ToList();
                if (encryptedItems.Any())
                {
                    var decryptedValues = EncryptionContext.Load(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray(), NEnv.EncryptionKeys.AsDictionary());
                    foreach (var item in encryptedItems)
                    {
                        item.Value = decryptedValues[long.Parse(item.Value)];
                    }
                }

                Func<IEnumerable<CreditApplicationSnapshotModel.Item>, string, string, string> getAppItemValue = (i, g, n) =>
                    i.Where(y => y.GroupName == g && y.Name == n).Select(y => y.Value).SingleOrDefault();

                Func<string, decimal?> toDecimal = s => s == null ? new decimal?() : decimal.Parse(s, CultureInfo.InvariantCulture);
                Func<string, int?> toInt = s => s == null ? new int?() : int.Parse(s, CultureInfo.InvariantCulture);

                var loanTypes = new string[] { "mortgageLoan", "carOrBoatLoan", "studentLoan", "otherLoan", "creditCard" };

                return items
                .Select(item =>
                {
                    var e = new ExpandoObject();
                    dynamic ed = e;
                    var dd = e as IDictionary<string, object>;

                    var repaymentTimeInYears = toInt(getAppItemValue(item.FetchedItems, "application", "repaymentTimeInYears"));
                    var applicantsHaveSameAddress = getAppItemValue(item.FetchedItems, "application", "applicantsHaveSameAddress");
                    var loansToSettleAmount = toDecimal(getAppItemValue(item.FetchedItems, "application", "loansToSettleAmount"));

                    decimal? offeredAmount = null;
                    int? offeredRepaymentTimeInMonths = null;
                    decimal? offeredMarginInterestRatePercent = null;
                    string offeredAdditionalLoanCreditNr = null;
                    decimal? offeredAdditionalLoanNewMarginInterestRatePercent = null;
                    decimal? offeredAdditionalLoanNewAnnuityAmount = null;
                    int? score = null;
                    decimal? leftToLiveOn = null;
                    string riskGroup = null;
                    string rejectionReasons = null;
                    JObject acceptedOrRejectedModel = null;

                    var ad = item.CurrentCreditDecision as AcceptedCreditDecision;
                    if (ad != null)
                    {
                        acceptedOrRejectedModel = JObject.Parse(ad.AcceptedDecisionModel);
                        var newLoanOfferParsed = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(ad.AcceptedDecisionModel);
                        var additionalLoanOfferParsed = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(ad.AcceptedDecisionModel);
                        if (newLoanOfferParsed != null)
                        {
                            offeredAmount = newLoanOfferParsed?.amount;
                            offeredRepaymentTimeInMonths = newLoanOfferParsed?.repaymentTimeInMonths;
                            offeredMarginInterestRatePercent = newLoanOfferParsed?.marginInterestRatePercent;
                        }
                        else if (additionalLoanOfferParsed != null)
                        {
                            offeredAmount = additionalLoanOfferParsed.amount;
                            offeredAdditionalLoanCreditNr = additionalLoanOfferParsed.creditNr;
                            offeredAdditionalLoanNewMarginInterestRatePercent = additionalLoanOfferParsed.newMarginInterestRatePercent;
                            offeredAdditionalLoanNewAnnuityAmount = additionalLoanOfferParsed.newAnnuityAmount;

                        }
                        else
                            throw new NotImplementedException();
                    }

                    var rd = item.CurrentCreditDecision as RejectedCreditDecision;
                    if (rd != null)
                    {
                        acceptedOrRejectedModel = JObject.Parse(rd.RejectedDecisionModel);
                        var rejectionReasonsParsed = CreditDecisionModelParser.ParseRejectionReasons(rd.RejectedDecisionModel);

                        rejectionReasons = rejectionReasonsParsed != null ? string.Join(",", rejectionReasonsParsed) : null;
                    }

                    if (acceptedOrRejectedModel != null)
                    {
                        var adLoansToSettleAmountTemp = acceptedOrRejectedModel?.SelectToken("$.application.application.loansToSettleAmount")?.Value<string>();
                        if (adLoansToSettleAmountTemp != null)
                        {
                            var adLoansToSettleAmount = parseDec(adLoansToSettleAmountTemp.Trim().Length == 0 ? "0" : adLoansToSettleAmountTemp);
                            if (adLoansToSettleAmount.HasValue)
                            {
                                loansToSettleAmount = adLoansToSettleAmount;
                            }
                        }

                        score = acceptedOrRejectedModel?.SelectToken("$.recommendation.Score")?.Value<int?>();
                        leftToLiveOn = acceptedOrRejectedModel?.SelectToken("$.recommendation.LeftToLiveOn")?.Value<decimal?>();
                        riskGroup = acceptedOrRejectedModel?.SelectToken("$.recommendation.RiskGroup")?.Value<string>();
                    }

                    //Applicant fields
                    foreach (var applicantNr in Enumerable.Range(1, 2))
                    {
                        var namePrefix = $"Applicant{applicantNr}";
                        var applicantGroupName = $"applicant{applicantNr}";

                        dd[$"{namePrefix}Housing"] = applicantNr > item.NrOfApplicants ? null : getAppItemValue(item.FetchedItems, applicantGroupName, "housing");
                        dd[$"{namePrefix}Education"] = applicantNr > item.NrOfApplicants ? null : getAppItemValue(item.FetchedItems, applicantGroupName, "education");
                        dd[$"{namePrefix}Employment"] = applicantNr > item.NrOfApplicants ? null : getAppItemValue(item.FetchedItems, applicantGroupName, "employment");
                        dd[$"{namePrefix}Marriage"] = applicantNr > item.NrOfApplicants ? null : getAppItemValue(item.FetchedItems, applicantGroupName, "marriage");
                        dd[$"{namePrefix}EmployedSince"] = applicantNr > item.NrOfApplicants ? null : parseMonth(getAppItemValue(item.FetchedItems, applicantGroupName, "employedSinceMonth"));
                        dd[$"{namePrefix}HousingCostPerMonth"] = applicantNr > item.NrOfApplicants ? null : parseDec(getAppItemValue(item.FetchedItems, applicantGroupName, "housingCostPerMonthAmount"));
                        dd[$"{namePrefix}IncomePerMonth"] = applicantNr > item.NrOfApplicants ? null : parseDec(getAppItemValue(item.FetchedItems, applicantGroupName, "incomePerMonthAmount"));
                        dd[$"{namePrefix}Employer"] = null; //TODO: To be removed entirely in a future version
                        dd[$"{namePrefix}NrOfChildren"] = applicantNr > item.NrOfApplicants ? null : parseInt(getAppItemValue(item.FetchedItems, applicantGroupName, "nrOfChildren"));

                        foreach (var loanType in loanTypes)
                        {
                            var ltName = loanType.Substring(0, 1).ToUpper() + loanType.Substring(1);
                            dd[$"{namePrefix}{ltName}Amount"] = applicantNr > item.NrOfApplicants ? new decimal?() : (parseDec(getAppItemValue(item.FetchedItems, applicantGroupName, $"{loanType}Amount")) ?? 0m);
                            dd[$"{namePrefix}{ltName}CostPerMonth"] = applicantNr > item.NrOfApplicants ? new decimal?() : (parseDec(getAppItemValue(item.FetchedItems, applicantGroupName, $"{loanType}CostPerMonthAmount")) ?? 0m);
                        }

                        //NOTE: Legacy properties
                        dd[$"{namePrefix}HasNegativeBusinessConnection"] = new bool?();
                        dd[$"{namePrefix}HasPositiveBusinessConnection"] = new bool?();
                        dd[$"{namePrefix}BricRiskOfPaymentRemark"] = (string)null;
                        dd[$"{namePrefix}DomesticAddressSinceDate"] = new DateTime?();
                        dd[$"{namePrefix}HasPaymentRemark"] = new bool?();
                    }

                    ed.Date = transactionDate;
                    ed.ApplicationNr = item.ApplicationNr;
                    ed.CreditNr = item.IsFinalDecisionMade ? getAppItemValue(item.FetchedItems, "application", "creditnr") : null;
                    ed.RequestedAmount = toDecimal(getAppItemValue(item.FetchedItems, "application", "amount"));
                    ed.CampaignCode = getAppItemValue(item.FetchedItems, "application", "campaignCode");
                    ed.DecisionStatus = item.DecisionStatus;
                    ed.DecisionDate = item.DecisionDate;
                    ed.OfferedAmount = offeredAmount;
                    ed.LoansToSettleAmount = loansToSettleAmount;
                    ed.RequestedRepaymentTimeInMonths = repaymentTimeInYears.HasValue ? repaymentTimeInYears.Value * 12 : new int?();
                    ed.OfferedRepaymentTimeInMonths = offeredRepaymentTimeInMonths;
                    ed.OfferedMarginInterestRatePercent = offeredMarginInterestRatePercent;
                    ed.OfferedAdditionalLoanCreditNr = offeredAdditionalLoanCreditNr;
                    ed.OfferedAdditionalLoanNewMarginInterestRatePercent = offeredAdditionalLoanNewMarginInterestRatePercent;
                    ed.OfferedAdditionalLoanNewAnnuityAmount = offeredAdditionalLoanNewAnnuityAmount;
                    ed.Score = score;
                    ed.ScoreGroup = riskGroup;
                    ed.LeftToLiveOn = leftToLiveOn;
                    ed.RejectionReasons = rejectionReasons;
                    ed.ApplicantsHaveSameAddress = item.NrOfApplicants == 1
                            ? new bool?()
                            : (applicantsHaveSameAddress == null
                                ? new bool?()
                                : applicantsHaveSameAddress == "true");
                    ed.PartiallyApprovedDate = item.PartiallyApprovedDate;

                    ed.Applicant1CreditDecisionCreditReportId = null;
                    ed.Applicant2CreditDecisionCreditReportId = null;

                    List<CreditReportUsedModel> creditReportsUsed = null;

                    if (acceptedOrRejectedModel != null)
                    {
                        creditReportsUsed = acceptedOrRejectedModel.SelectToken("$.creditReportsUsed")?.ToObject<List<CreditReportUsedModel>>();
                    }

                    if (creditReportsUsed != null)
                    {
                        var b = creditReportsUsed.Where(x => x.ProviderName != "SatFi").OrderByDescending(x => x.CreditReportId);
                        ed.Applicant1CreditDecisionCreditReportId = b.Where(x => x.ApplicantNr == 1).Select(x => x.CreditReportId).FirstOrDefault();
                        ed.Applicant2CreditDecisionCreditReportId = b.Where(x => x.ApplicantNr == 2).Select(x => x.CreditReportId).FirstOrDefault();
                    }

                    ed.Applicant1CreditDecisionSatCreditReportId = null;
                    ed.Applicant2CreditDecisionSatCreditReportId = null;
                    ed.Applicant1SatConsentStatus = null;
                    ed.Applicant2SatConsentStatus = null;

                    Func<int, string> getSatStatus = applicantNr =>
                    {
                        if (applicantNr > item.NrOfApplicants)
                            return null;

                        var i = item.FetchedItems.Where(y => y.GroupName == $"applicant{applicantNr}" && y.Name == "approvedSat").SingleOrDefault();

                        if (i == null)
                            return "Missing";
                        else if (i.Value == "true")
                            return i.AddedInStepName == "AddManualSatConsent" ? "Manual" : "Yes";
                        else
                            return "No";
                    };

                    if (creditReportsUsed != null)
                    {
                        var b = creditReportsUsed.Where(x => x.ProviderName == SatProviderName).OrderByDescending(x => x.CreditReportId);
                        ed.Applicant1CreditDecisionSatCreditReportId = b.Where(x => x.ApplicantNr == 1).Select(x => x.CreditReportId).FirstOrDefault();
                        ed.Applicant2CreditDecisionSatCreditReportId = b.Where(x => x.ApplicantNr == 2).Select(x => x.CreditReportId).FirstOrDefault();

                        ed.Applicant1SatConsentStatus = getSatStatus(1);
                        ed.Applicant2SatConsentStatus = getSatStatus(2);
                    }

                    return e;
                })
                .ToList();
            };

            const string FactName = "CreditApplicationSnapshot";
            Func<List<string>, PreCreditContext, List<ExpandoObject>> toDwItemsFromApplicationNrs = (batchApplicationNrs, context) =>
            {
                var tmp = batchApplicationNrs
                    .GroupBy(x => x)
                    .Select(x => new
                    {
                        ApplicationNr = x.Key,
                        Count = x.Count()
                    })
                    .Where(x => x.Count > 1)
                    .ToList();
                var foo = CreditApplicationSnapshotModelQuery(context).Where(x => batchApplicationNrs.Contains(x.ApplicationNr)).ToList();
                return toDwItems(foo, context);
            };
            Merge_Fact_CreditApplicationSnapshot_ByApplications(FactName, toDwItemsFromApplicationNrs);
            Merge_Fact_CreditApplicationSnapshot_ByApplicationItems(FactName, toDwItemsFromApplicationNrs);
        }

        private  const string SatProviderName = "SatFi";

        private void Merge_Fact_CreditApplicationSnapshot_ByApplicationItems(string factName, Func<List<string>, PreCreditContext, List<ExpandoObject>> toDwItemsFromApplicationNrs)
        {
            Func<PreCreditContext, byte[]> getGlobalMaxTs = context => context.CreditApplicationItems.Max(x => x.Timestamp);
            Func<PreCreditContext, int, byte[], byte[], Tuple<List<string>, byte[]>> getBatch = (context, batchSize, latestSeenTs, globalMaxTs) =>
            {
                var q = context
                    .CreditApplicationItems
                    .Where(x => !x.CreditApplication.ArchivedDate.HasValue)
                    .Select(x => new { x.ApplicationNr, x.Timestamp })
                    .Where(x => BinaryComparer.Compare(x.Timestamp, globalMaxTs) <= 0);

                if (latestSeenTs != null)
                    q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                var batch = q
                .GroupBy(x => x.ApplicationNr)
                .Select(x => new
                {
                    ApplicationNr = x.Key,
                    MinTs = x.Min(y => y.Timestamp),
                    MaxTs = x.Max(y => y.Timestamp),
                })
                .OrderBy(x => x.MinTs)
                .Take(batchSize);

                return Tuple.Create(
                    batch.Select(x => x.ApplicationNr).ToList().Distinct().ToList(),
                    batch.Select(x => x.MinTs).OrderByDescending(x => x).FirstOrDefault());
            };

            MergeFastUsingIds(getGlobalMaxTs, getBatch, SystemItemCode.DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot_ItemTs, factName, toDwItemsFromApplicationNrs, true, 300);
        }

        private void Merge_Fact_CreditApplicationSnapshot_ByApplications(string factName, Func<List<string>, PreCreditContext, List<ExpandoObject>> toDwItemsFromApplicationNrs)
        {
            Func<PreCreditContext, byte[]> getGlobalMaxTs = context => context.CreditApplicationHeaders.Max(x => x.Timestamp);
            Func<PreCreditContext, int, byte[], byte[], Tuple<List<string>, byte[]>> getBatch = (context, batchSize, latestSeenTs, globalMaxTs) =>
            {
                var q = context
                    .CreditApplicationHeaders
                    .Where(x => !x.ArchivedDate.HasValue)
                    .Select(x => new { x.ApplicationNr, x.Timestamp })
                    .Where(x => BinaryComparer.Compare(x.Timestamp, globalMaxTs) <= 0);

                if (latestSeenTs != null)
                    q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

                var batch = q
                .OrderBy(x => x.Timestamp)
                .Take(batchSize);

                return Tuple.Create(
                    batch.Select(x => x.ApplicationNr).ToList().Distinct().ToList(),
                    batch.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault());
            };

            MergeFastUsingIds(getGlobalMaxTs, getBatch, SystemItemCode.DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot, factName, toDwItemsFromApplicationNrs, true, 300);
        }

        private class CreditReportUsedModel
        {
            public int? CreditReportId { get; set; }
            public int? ApplicantNr { get; set; }
            public string ProviderName { get; set; }
        }

        private class CreditApplicationSnapshotModel
        {
            public string ApplicationNr { get; internal set; }
            public string DecisionStatus { get; internal set; }
            public DateTimeOffset? DecisionDate { get; internal set; }
            public bool IsFinalDecisionMade { get; set; }
            public DateTimeOffset? PartiallyApprovedDate { get; set; }
            public CreditDecision CurrentCreditDecision { get; set; }

            public int NrOfApplicants { get; internal set; }
            public IEnumerable<Item> FetchedItems { get; set; }

            public class Item
            {
                public string AddedInStepName { get; set; }
                public string GroupName { get; set; }
                public string Name { get; set; }
                public string Value { get; set; }
                public bool IsEncrypted { get; set; }
            }
        }

        private static string[] ApplicationSnapshotItemsNamesToFetch = new string[]
            {
                "amount", "repaymentTimeInYears", "campaignCode", "applicantsHaveSameAddress", "creditnr", "loansToSettleAmount", "housing",
                "education", "employment", "employedSinceMonth", "housingCostPerMonthAmount",
                "incomePerMonthAmount", "marriage", "nrOfChildren",
                "mortgageLoanAmount", "mortgageLoanCostPerMonthAmount", "carOrBoatLoanAmount", "carOrBoatLoanCostPerMonthAmount", "studentLoanAmount", "studentLoanCostPerMonthAmount", "otherLoanAmount", "otherLoanCostPerMonthAmount", "creditCardAmount", "creditCardCostPerMonthAmount", "approvedSat" };

        private IQueryable<CreditApplicationSnapshotModel> CreditApplicationSnapshotModelQuery(PreCreditContext context)
        {
            var cl = CreditApplicationTypeCode.companyLoan.ToString();
            return context
                .CreditApplicationHeaders
                .Where(x => x.ApplicationType != cl)
                .Select(x => new
                {
                    App = x,
                    x.CurrentCreditDecision,
                    AppItems = x.Items.Where(y => ApplicationSnapshotItemsNamesToFetch.Contains(y.Name)),
                })
                .Select(x => new
                {
                    x.App,
                    x.CurrentCreditDecision,
                    AppItems = x.AppItems
                })
                .Select(x => new
                {
                    ApplicationNr = x.App.ApplicationNr,
                    NrOfApplicants = x.App.NrOfApplicants,
                    DecisionStatus = x.App.IsCancelled ? "Cancelled" : (x.App.IsRejected ? "Rejected" : x.App.IsPartiallyApproved ? "Approved" : "Initial"),
                    DecisionDate = x.App.IsCancelled ? x.App.CancelledDate : (x.App.IsRejected ? x.App.RejectedDate : x.App.IsPartiallyApproved ? x.App.PartiallyApprovedDate : x.App.ApplicationDate),
                    IsFinalDecisionMade = x.App.IsFinalDecisionMade,
                    PartiallyApprovedDate = (!x.App.IsRejected && !x.App.IsCancelled) ? x.App.PartiallyApprovedDate : null,
                    FetchedItems = x.AppItems.Select(y => new CreditApplicationSnapshotModel.Item
                    {
                        AddedInStepName = y.AddedInStepName,
                        GroupName = y.GroupName,
                        IsEncrypted = y.IsEncrypted,
                        Name = y.Name,
                        Value = y.Value
                    }),
                    CurrentCreditDecision = x.CurrentCreditDecision
                })
                .Select(x => new CreditApplicationSnapshotModel
                {
                    ApplicationNr = x.ApplicationNr,
                    CurrentCreditDecision = x.CurrentCreditDecision,
                    DecisionDate = x.DecisionDate,
                    DecisionStatus = x.DecisionStatus,
                    FetchedItems = x.FetchedItems,
                    IsFinalDecisionMade = x.IsFinalDecisionMade,
                    NrOfApplicants = x.NrOfApplicants,
                    PartiallyApprovedDate = x.PartiallyApprovedDate
                });
        }
    }
}