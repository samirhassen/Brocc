using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class CreateItemBasedCreditDecisionMethod : TypedWebserviceMethod<CreateItemBasedCreditDecisionMethod.Request, CreateItemBasedCreditDecisionMethod.Response>
    {
        public override string Path => "CreditDecision/Create-ItemBased";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (((request?.UniqueItems?.Count() ?? 0) + (request?.RepeatingItems?.Count() ?? 0)) == 0)
                return Error("At least one item in either UniqueItems or RepeatingItems is required", errorCode: "missingItems");

            //Check for duplicate names
            var un = request.UniqueItems?.Keys?.ToHashSet();
            var rn = request.RepeatingItems?.Keys?.ToHashSet();
            if (un != null && rn != null)
            {
                var overlappingNames = un.Intersect(rn, StringComparer.OrdinalIgnoreCase).ToList();
                if (overlappingNames.Count > 0)
                    return Error(string.Join(", ", overlappingNames) + " is in both UniqueItems and RepeatingItems", errorCode: "duplicateNames");
            }

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                CreditDecision d;
                if (request.IsAccepted.Value)
                    d = context.FillInfrastructureFields(new AcceptedCreditDecision { AcceptedDecisionModel = CreditRecommendationUlStandardService.DecisionModelMarker.Value });
                else
                    d = context.FillInfrastructureFields(new RejectedCreditDecision { RejectedDecisionModel = CreditRecommendationUlStandardService.DecisionModelMarker.Value });
                d.DecisionById = context.CurrentUserId;
                d.DecisionDate = context.Clock.Now;
                d.DecisionType = request.DecisionType;
                d.WasAutomated = request.WasAutomated.GetValueOrDefault();

                var a = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == request.ApplicationNr);
                if (request.SetAsCurrent.GetValueOrDefault() || string.IsNullOrWhiteSpace(request.ChangeCreditCheckStatusTo))
                {
                    d.CreditApplication = a;
                    if (request.SetAsCurrent.GetValueOrDefault())
                    {
                        a.CurrentCreditDecision = d;
                    }
                    if (!string.IsNullOrWhiteSpace(request.ChangeCreditCheckStatusTo))
                    {
                        a.CreditCheckStatus = request.ChangeCreditCheckStatusTo;
                    }
                }
                else
                {
                    d.ApplicationNr = request.ApplicationNr;
                }

                if (request.UniqueItems != null)
                {
                    foreach (var i in request.UniqueItems)
                    {
                        context.CreditDecisionItems.Add(new CreditDecisionItem
                        {
                            IsRepeatable = false,
                            ItemName = i.Key,
                            Value = i.Value
                        });
                    }
                }

                if (request.RepeatingItems != null)
                {
                    foreach (var i in request.RepeatingItems.SelectMany(x => x.Value.Select(y => new { x.Key, Value = y })))
                    {
                        context.CreditDecisionItems.Add(new CreditDecisionItem
                        {
                            IsRepeatable = true,
                            ItemName = i.Key,
                            Value = i.Value
                        });
                    }
                }

                if (!request.IsAccepted.GetValueOrDefault())
                {
                    Func<string, bool> isKnownRejectionReason = null;
                    if (a.ApplicationType == "mortgageLoan")
                    {
                        isKnownRejectionReason = NEnv.MortgageLoanScoringSetup.GetIsKnownRejectionReason();
                    }
                    else if (a.ApplicationType == "companyLoan")
                    {
                        isKnownRejectionReason = Code.Services.CompanyLoans.CompanyLoanRejectionScoringSetup.Instance.GetIsKnownRejectionReason();
                    }
                    else
                    {
                        isKnownRejectionReason = NEnv.ScoringSetup.GetIsKnownRejectionReason();
                    }

                    if (!string.IsNullOrWhiteSpace(request.RejectionReasonsItemName))
                    {
                        var rejectionReasons = request.RepeatingItems[request.RejectionReasonsItemName];
                        Code.CreditCheckCompletionProviderApplicationUpdater.AddRejectionReasonSearchTerms(rejectionReasons, isKnownRejectionReason, d, context);
                    }

                    a.IsActive = false;
                    a.IsRejected = true;
                    a.RejectedById = context.CurrentUserId;
                    a.RejectedDate = context.Clock.Now;
                }

                context.SaveChanges();

                return new Response
                {
                    Id = d.Id
                };
            }
        }
               

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public bool? IsAccepted { get; set; }

            [Required]
            public bool? SetAsCurrent { get; set; }

            public Dictionary<string, string> UniqueItems { get; set; }
            public Dictionary<string, List<string>> RepeatingItems { get; set; }

            public string DecisionType { get; set; }
            public bool? WasAutomated { get; set; }
            public string ChangeCreditCheckStatusTo { get; set; }

            public string RejectionReasonsItemName { get; set; }
        }

        public class Response
        {
            public int Id { get; set; }
        }
    }
}