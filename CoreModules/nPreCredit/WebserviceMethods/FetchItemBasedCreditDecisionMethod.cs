using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchItemBasedCreditDecisionMethod : TypedWebserviceMethod<FetchItemBasedCreditDecisionMethod.Request, FetchItemBasedCreditDecisionMethod.Response>
    {
        public override string Path => "CreditDecision/Fetch-ItemBased";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var q = context
                    .CreditDecisions
                    .Include("CreditApplication")
                    .Include("DecisionItems")
                    .Where(x => x.ApplicationNr == request.ApplicationNr);

                if (request.MustBeCurrent.GetValueOrDefault())
                    q = q.Where(x => x.CreditApplication.CurrentCreditDecisionId == x.Id);
                if (request.MustBeAccepted.GetValueOrDefault())
                    q = q.OfType<AcceptedCreditDecision>();
                if (request.MustBeRejected.GetValueOrDefault())
                    q = q.OfType<RejectedCreditDecision>();

                int? minIdOfDifferentType = null;
                int? maxIdOfDifferentType = null;
                if (!string.IsNullOrWhiteSpace(request.OnlyDecisionType))
                {
                    var r = q
                        .Where(x => x.DecisionType != request.OnlyDecisionType)
                        .GroupBy(x => x.ApplicationNr)
                        .Select(x => new
                        {
                            MaxId = (int?)x.Max(y => y.Id),
                            MinId = (int?)x.Min(y => y.Id)
                        })
                        .SingleOrDefault(); //Since there is only one application
                    minIdOfDifferentType = r?.MinId;
                    maxIdOfDifferentType = r?.MaxId;
                    q = q.Where(x => x.DecisionType == request.OnlyDecisionType);
                }

                q = q.OrderByDescending(x => x.Id);
                if (request.MaxCount.GetValueOrDefault() > 0)
                    q = q.Take(request.MaxCount.GetValueOrDefault());

                var decisionsPre = q
                    .Select(x => new
                    {
                        D = x,
                        x.CreditApplication,
                        x.DecisionItems,
                        ExistsLaterDecisionOfDifferentType = x.CreditApplication.CreditDecisions.Any(y => y.DecisionType != x.DecisionType && y.Id > x.Id),
                        ExistsEarlierDecisionOfDifferentType = x.CreditApplication.CreditDecisions.Any(y => y.DecisionType != x.DecisionType && y.Id < x.Id)
                    })
                    .ToList();

                Dictionary<string, string> rejectionReasonToDisplayNameMapping = null;
                if (request.IncludeRejectionReasonToDisplayNameMapping.GetValueOrDefault())
                {
                    string applicationType;
                    if (decisionsPre.Count > 0)
                        applicationType = q.First().CreditApplication.ApplicationType;
                    else
                        applicationType = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == request.ApplicationNr).ApplicationType;

                    if (applicationType == CreditApplicationTypeCode.mortgageLoan.ToString())
                        rejectionReasonToDisplayNameMapping = NEnv.MortgageLoanScoringSetup.RejectionReasons.ToDictionary(x => x.Name, x => x.DisplayName);
                    else if (applicationType == CreditApplicationTypeCode.companyLoan.ToString())
                        rejectionReasonToDisplayNameMapping = Code.Services.CompanyLoans.CompanyLoanRejectionScoringSetup.Instance.GetRejectionReasonDisplayNameByReasonName();
                    else
                        throw new NotImplementedException();
                }
                var decisions =
                    decisionsPre
                    .Select(x =>
                    {
                        return new Response.DecisionModel
                        {
                            Id = x.D.Id,
                            IsAccepted = (x.D as AcceptedCreditDecision) != null,
                            WasAutomated = x.D.WasAutomated,
                            DecisionDate = x.D.DecisionDate,
                            DecisionType = x.D.DecisionType,
                            IsCurrent = x.CreditApplication.CurrentCreditDecisionId == x.D.Id,
                            UniqueItems = x.DecisionItems
                                 .Where(y => !y.IsRepeatable)
                                 .ToDictionary(y => y.ItemName, y => y.Value),
                            RepeatingItems = x.DecisionItems
                                 .Where(y => y.IsRepeatable)
                                 .GroupBy(y => y.ItemName)
                                 .ToDictionary(y => y.Key, y => y.Select(z => z.Value).ToList()),
                            ExistsLaterDecisionOfDifferentType = x.ExistsLaterDecisionOfDifferentType,
                            ExistsEarlierDecisionOfDifferentType = x.ExistsEarlierDecisionOfDifferentType
                        };
                    }).ToList();

                var response = new Response
                {
                    Decisions = decisions,
                    RejectionReasonToDisplayNameMapping = rejectionReasonToDisplayNameMapping
                };
                if (!string.IsNullOrWhiteSpace(request.OnlyDecisionType))
                {
                    if (response.Decisions.Count == 0)
                    {
                        response.ExistsEarlierDecisionOfDifferentType = minIdOfDifferentType.HasValue; //Any decision will be before since none exist yet so the first created will be higher than all ids
                        response.ExistsLaterDecisionOfDifferentType = false; //Later cannot exist since we dont have any
                    }
                    else
                    {
                        var minActual = response.Decisions.Min(x => x.Id);
                        var maxActual = response.Decisions.Max(x => x.Id);
                        response.ExistsEarlierDecisionOfDifferentType = minIdOfDifferentType.HasValue && minIdOfDifferentType.Value < minActual;
                        response.ExistsLaterDecisionOfDifferentType = maxIdOfDifferentType.HasValue && maxIdOfDifferentType.Value > maxActual;
                    }
                }
                return response;
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public bool? MustBeCurrent { get; set; }
            public bool? MustBeAccepted { get; set; }
            public bool? MustBeRejected { get; set; }
            public string OnlyDecisionType { get; set; }
            public int? MaxCount { get; set; }
            public bool? IncludeRejectionReasonToDisplayNameMapping { get; set; }
        }

        public class Response
        {
            public List<DecisionModel> Decisions { get; set; }
            public Dictionary<string, string> RejectionReasonToDisplayNameMapping { get; set; }

            public class DecisionModel
            {
                public int Id { get; set; }
                public bool IsAccepted { get; set; }
                public DateTimeOffset DecisionDate { get; set; }
                public string DecisionType { get; set; }
                public bool WasAutomated { get; set; }
                public bool IsCurrent { get; set; }
                public bool ExistsLaterDecisionOfDifferentType { get; set; }
                public bool ExistsEarlierDecisionOfDifferentType { get; set; }
                public Dictionary<string, string> UniqueItems { get; set; }
                public Dictionary<string, List<string>> RepeatingItems { get; set; }
            }

            //Only included if OnlyDecisionType is included
            public bool? ExistsLaterDecisionOfDifferentType { get; set; }

            //Only included if OnlyDecisionType is included
            public bool? ExistsEarlierDecisionOfDifferentType { get; set; }
        }
    }
}