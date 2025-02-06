using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Banking.LoanModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_loan_applications
    {

		public static Dictionary<int, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, CrossRunCacheDb db)
		{
			using (var preCreditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.PreCredit))
			{
				var query =
@"with CreditApplicationCustomer
as
(
	select	i.ApplicationNr,
			1 as ApplicantNr,
			cast(i.[Value] as int) as CustomerId
	from	CreditApplicationItem i where i.GroupName = 'Applicant1' and i.[Name] = 'customerId'
	union all
	select	i.ApplicationNr,
			2 as ApplicantNr,
			cast(i.[Value] as int)
	from	CreditApplicationItem i where i.GroupName = 'Applicant2' and i.[Name] = 'customerId'
),
SlxApplicationPre
as
(
select	c.ApplicationNr as loan_application_id,
		c.CustomerId as customer_id,
		case 
			when c.ApplicantNr = 1 then (select top 1 cc.CustomerId from CreditApplicationCustomer cc where cc.ApplicationNr = c.ApplicationNr and cc.ApplicantNr = 2)
			else (select top 1 cc.CustomerId from CreditApplicationCustomer cc where cc.ApplicationNr = c.ApplicationNr and cc.ApplicantNr = 1)
		end as co_applicant_id,
		case
			when h.IsFinalDecisionMade = 1 then (select top 1 i.[Value] from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'creditNr')
			else null
		end as product_number,
		case 
			when h.IsFinalDecisionMade = 1 then 'converted'
			when h.IsCancelled = 1 then 'canceled'
			when h.IsRejected = 1 and isnull(d.WasAutomated, 0) = 1 then 'automatically_rejected'
			when h.IsRejected = 1 then 'manually_rejected'
			when h.CreditCheckStatus = 'Accepted' and h.AgreementStatus = 'Initial' and isnull(d.WasAutomated, 0) = 1 then 'automatically_approved'
			when h.CreditCheckStatus = 'Accepted' and h.AgreementStatus = 'Initial' then 'manually_approved'
			when h.CreditCheckStatus = 'Accepted' and h.AgreementStatus = 'Accepted' then 'signed'
			when h.CreditCheckStatus = 'Initial' then 'manual_check'
			when h.CreditCheckStatus = 'Rejected' and isnull(d.WasAutomated, 0) = 1 then 'automatically_rejected'
			when h.CreditCheckStatus = 'Rejected' then 'manually_rejected'
			else 'other'
		end
		as [status],
		'unsecured_variable_rate_annuity_loan' as loan_type,
		'FI' as loan_market,
		case (select top 1 isnull(i.[Value], 'other') from CreditApplicationItem i where [Name] = 'loan_purpose' and i.ApplicationNr = h.ApplicationNr)
			when 'consumption' then 'consumption'
			when 'investment' then 'capital_purchase'
			when 'relative' then 'other'
			else 'other'
		end as loan_purpose,
		CAST(0 as bit) as payment_protection_insurance,
		12 as repayment_periodicity_per_year,
		'monthly' as repayment_periodicity,
        'annuity' as repayment_method,
		0 as requested_consolidation_amount,
		h.ProviderName as [source],
		(select top 1 convert(nvarchar, c.CommentDate, 23) from CreditApplicationComment c where c.ApplicationNr = h.ApplicationNr and c.EventType = 'AgreementSentForSigning' order by c.Id desc) as agreement_sent_date,
		case 
			when h.AgreementStatus = 'Accepted' then (select top 1 convert(nvarchar, c.CommentDate, 23) from CreditApplicationComment c where c.ApplicationNr = h.ApplicationNr and c.EventType = 'AgreementSigned' order by c.Id desc)
			else null
		end as accept_date,
		case 
			when h.AgreementStatus = 'Accepted' then (select top 1 convert(nvarchar, c.CommentDate, 23) from CreditApplicationComment c where c.ApplicationNr = h.ApplicationNr and c.EventType = 'AgreementSigned' order by c.Id desc)
			else null
		end as signed_date,
		null as maximum_loanable_amount,
		case 
			when exists(select 1 from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'repaymentTimeInMonths') then
				(select top 1 cast(i.[Value] as int) from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'repaymentTimeInMonths')
			else (select top 1 cast(i.[Value] as int) * 12 from CreditApplicationItem i where i.ApplicationNr = h.ApplicationNr and i.[Name] = 'repaymentTimeInYears')
		end as original_requested_repayment_periods,
		'' as utm_source
from	CreditApplicationCustomer c
join	CreditApplicationHeader h on h.ApplicationNr = c.ApplicationNr
left outer join CreditDecision d on d.Id = h.CurrentCreditDecisionId
where	h.ArchivedDate is null
)
select	*
from	SlxApplicationPre p
where	p.customer_id in @customerIds";
				var applications = preCreditConnection.Query<object>(query, param: new { customerIds }).Select(JObject.FromObject).ToList();

				var cachedDecisionsByKey = GetCachedDecisionsForCustomers(customerIds, db);
				foreach(var application in applications)
                {
					var applicationNr = application["loan_application_id"].Value<string>();
					var customerId = application["customer_id"].Value<int>();
					var decisionKey = GetDecisionKey(applicationNr, customerId);
					var decision = cachedDecisionsByKey.GetWithDefault(decisionKey);
					application.AddOrReplaceJsonProperty("amount", new JValue(decision.amount), true);
					application.AddOrReplaceJsonProperty("period_cost", new JValue(decision.annuityAmount ?? decision.newAnnuityAmount), true);
					application.AddOrReplaceJsonProperty("term_in_days", new JValue(decision.repaymentTimeInMonths * 30), true);
					application.AddOrReplaceJsonProperty("repayment_periods", new JValue(decision.repaymentTimeInMonths), true);
					application.AddOrReplaceJsonProperty("interest_rate", new JValue(decision.getTotalInterestRatePercent()), true);
					application.AddOrReplaceJsonProperty("effective_interest_rate", new JValue(decision.effectiveInterestRatePercent), true);
					application.AddOrReplaceJsonProperty("loan_application_fee", new JValue(decision.initialFeeAmount), true);
					application.AddOrReplaceJsonProperty("consolidated_left_to_live_on", new JValue(decision.recommendationLeftToLiveOn), true);
				}

				return applications
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.ToList());
			}
		}

		private static string GetDecisionKey(string applicationNr, int customerId) => $"{applicationNr}#{customerId}";

		private static Dictionary<string, CachedCurrentCreditDecision> GetCachedDecisionsForCustomers(HashSet<int> customerIds, CrossRunCacheDb db)
        {
			Dictionary<string, CachedCurrentCreditDecision> result = null;
			db.WithConnection(connection =>
			{
				result = connection
					.Query<CachedCurrentCreditDecision>("select d.* from CurrentCreditDecision d where d.customerId in @customerIds", param: new { customerIds })
					.GroupBy(x => GetDecisionKey(x.applicationNr, x.customerId))
					.ToDictionary(x => x.Key, x => x.Single());
			});
			return result;
		}

		public static void EnsureCurrentCreditDecisionCache(CrossRunCacheDb db, ConnectionFactory connectionFactory)
        {
			Console.WriteLine("Building credit decision cache. Will only be slow once.");
			db.WithConnection(cacheConnection =>
			{
				cacheConnection.Execute(
@"create table if not exists CurrentCreditDecision (
	applicationNr TEXT NOT NULL,
    customerId NUMBER NOT NULL,
	id INT NOT NULL, 
	code TEXT NOT NULL,
	amount NUMBER,
    annuityAmount NUMBER,
    repaymentTimeInMonths NUMBER,
    marginInterestRatePercent NUMBER,
    referenceInterestRatePercent NUMBER,
	initialFeeAmount NUMBER,
	notificationFeeAmount NUMBER,
	effectiveInterestRatePercent NUMBER,
	totalPaidAmount NUMBER,
	initialPaidToCustomerAmount NUMBER,
	creditNr TEXT,
	newAnnuityAmount NUMBER,
	newMarginInterestRatePercent NUMBER,
	newNotificationFeeAmount NUMBER,
    rejectionReasonsJson TEXT,
    recommendationLeftToLiveOn NUMBER,
    recommendationRiskGroup TEXT,
    recommendationScore NUMBER,
    primary key(applicationNr, customerId)
)");

				using (var preCreditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.PreCredit))
				{
					while(true)
                    {
						var maxCachedId = cacheConnection.ExecuteScalar<int?>("select max(id) from CurrentCreditDecision") ?? 0;
						var decisions = preCreditConnection.Query<PreCreditDecisionData>(
@"select top 500 d.Id,
		isnull(d.AcceptedDecisionModel, d.RejectedDecisionModel) as Model,
		d.Discriminator,
		cast(a1.[Value] as int) as Applicant1CustomerId,
		cast(a2.[Value] as int) as Applicant2CustomerId,
		h.ApplicationNr
from	CreditDecision d
join	CreditApplicationHeader h on h.CurrentCreditDecisionId = d.Id
join	CreditApplicationItem a1 on a1.GroupName = 'Applicant1' and a1.[Name] = 'customerId' and a1.ApplicationNr = h.ApplicationNr
left outer join	CreditApplicationItem a2 on a2.GroupName = 'Applicant2' and a2.[Name] = 'customerId' and a2.ApplicationNr = h.ApplicationNr
where   d.Id > @maxCachedId
and		h.ArchivedDate is null
order by d.Id asc", param: new { maxCachedId }).ToList();

						using (var tr = cacheConnection.BeginTransaction())
                        {
							try
                            {
								const string RejectionCode = "r";
								const string NewLoanCode = "n";
								const string AdditionalLoanCode = "a";

								foreach(var decision in decisions)
                                {
									try
									{
										var parsedDecision = CreditDecisionModelParser.ParseUnsecuredLoanCreditDecision(decision.Model, decision.Discriminator == "RejectedCreditDecision");
										var parsedRecommendation = CreditDecisionModelParser.ParseRecommendation(decision.Model);

										string code;
										if (parsedDecision?.RejectionReasons != null)
											code = RejectionCode;
										else if (parsedDecision?.NewCreditOffer != null)
											code = NewLoanCode;
										else if (parsedDecision?.AdditionalLoanOffer != null)
											code = AdditionalLoanCode;
										else
											throw new Exception($"{decision.ApplicationNr} is missing credit a decision model");

										var customerIds = decision.Applicant2CustomerId.HasValue
											? new[] { decision.Applicant1CustomerId, decision.Applicant2CustomerId.Value }
											: new[] { decision.Applicant1CustomerId };
										foreach (var customerId in customerIds)
										{
											var storedDecision = new CachedCurrentCreditDecision
											{
												applicationNr = decision.ApplicationNr,
												customerId = customerId,
												id = decision.Id,
												code = code,
												amount = parsedDecision.NewCreditOffer?.amount ?? parsedDecision.AdditionalLoanOffer?.amount,
												annuityAmount = parsedDecision.NewCreditOffer?.annuityAmount,
												repaymentTimeInMonths = parsedDecision.NewCreditOffer?.repaymentTimeInMonths,
												marginInterestRatePercent = parsedDecision.NewCreditOffer?.marginInterestRatePercent,
												referenceInterestRatePercent = parsedDecision.NewCreditOffer?.referenceInterestRatePercent,
												initialFeeAmount = parsedDecision.NewCreditOffer?.initialFeeAmount,
												notificationFeeAmount = parsedDecision.NewCreditOffer?.notificationFeeAmount,
												effectiveInterestRatePercent = parsedDecision.NewCreditOffer?.effectiveInterestRatePercent ?? parsedDecision.AdditionalLoanOffer?.effectiveInterestRatePercent,
												totalPaidAmount = parsedDecision.NewCreditOffer?.totalPaidAmount,
												initialPaidToCustomerAmount = parsedDecision.NewCreditOffer?.initialPaidToCustomerAmount,
												creditNr = parsedDecision.AdditionalLoanOffer?.creditNr,
												newAnnuityAmount = parsedDecision.AdditionalLoanOffer?.newAnnuityAmount,
												newMarginInterestRatePercent = parsedDecision.AdditionalLoanOffer?.newMarginInterestRatePercent,
												newNotificationFeeAmount = parsedDecision.AdditionalLoanOffer?.newNotificationFeeAmount,
												rejectionReasonsJson = code == RejectionCode ? JsonConvert.SerializeObject(parsedDecision.RejectionReasons) : null,
												recommendationLeftToLiveOn = parsedRecommendation?.LeftToLiveOn,
												recommendationRiskGroup = parsedRecommendation?.RiskGroup,
												recommendationScore = parsedRecommendation?.Score
											};
											if (code == NewLoanCode && (!storedDecision.annuityAmount.HasValue || !storedDecision.effectiveInterestRatePercent.HasValue || !storedDecision.totalPaidAmount.HasValue || !storedDecision.initialPaidToCustomerAmount.HasValue))
											{
												//Some values are missing on older applications before these things were added to the offers
												var n = parsedDecision.NewCreditOffer;

												var paymentPlan = PaymentPlanCalculation.BeginCreateWithRepaymentTime(
													n.amount.Value,
													n.repaymentTimeInMonths.Value,
													n.marginInterestRatePercent.Value + n.referenceInterestRatePercent.Value, true, null, false)
												.EndCreate();

												storedDecision.annuityAmount = paymentPlan.AnnuityAmount;
												storedDecision.effectiveInterestRatePercent = paymentPlan.EffectiveInterestRatePercent;
												storedDecision.totalPaidAmount = paymentPlan.TotalPaidAmount;
												storedDecision.initialPaidToCustomerAmount = paymentPlan.InitialPaidToCustomerAmount;
											}
											cacheConnection.Execute(
												@"insert into CurrentCreditDecision 
											    (applicationNr, customerId, id, code, amount, annuityAmount, repaymentTimeInMonths, marginInterestRatePercent, referenceInterestRatePercent, initialFeeAmount, notificationFeeAmount, effectiveInterestRatePercent, totalPaidAmount, initialPaidToCustomerAmount, creditNr, newAnnuityAmount, newMarginInterestRatePercent, newNotificationFeeAmount, rejectionReasonsJson, recommendationLeftToLiveOn, recommendationRiskGroup, recommendationScore)
												values 
												(@applicationNr, @customerId, @id, @code, @amount, @annuityAmount, @repaymentTimeInMonths, @marginInterestRatePercent, @referenceInterestRatePercent, @initialFeeAmount, @notificationFeeAmount, @effectiveInterestRatePercent, @totalPaidAmount, @initialPaidToCustomerAmount, @creditNr, @newAnnuityAmount, @newMarginInterestRatePercent, @newNotificationFeeAmount, @rejectionReasonsJson, @recommendationLeftToLiveOn, @recommendationRiskGroup, @recommendationScore )",
												param: storedDecision, transaction: tr);
										}
									}
									catch (Exception ex)
                                    {
										throw new Exception($"Decision parsing error on {decision.ApplicationNr}", ex);
                                    }
								}
								
								tr.Commit();

								if (decisions.Count == 0)
                                {									
									return;
								}
								maxCachedId = decisions[decisions.Count - 1].Id;
                            }
							catch
                            {
								tr.Rollback();
								throw;								
                            }
                        }
                    }
				}
			});
        }

		private class PreCreditDecisionData
        {
			public int Id { get; set; }
			public string ApplicationNr { get;set; }
			public string Model { get; set; }
			public string Discriminator { get; set; }
			public int Applicant1CustomerId { get; set; }
			public int? Applicant2CustomerId { get; set; }
		}
	}
}
