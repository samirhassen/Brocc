using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanRejectionScoringSetup
    {
        public Dictionary<string, string> GetRejectionReasonDisplayNameByReasonName()
        {
            return RejectionReasons
                .ToDictionary(x => x.Name, x => x.DisplayName);
        }

        public Dictionary<string, string> GetRejectionReasonNameByRuleName()
        {
            return RejectionReasons
                .SelectMany(x => x.ScoringRules.Select(y => new
                {
                    reasonName = x.Name,
                    ruleName = y.Name
                }))
                .ToDictionary(x => x.ruleName, x => x.reasonName);
        }

        public Func<string, bool> GetIsKnownRejectionReason()
        {
            var r = this.RejectionReasons.Select(x => x.Name).ToHashSet();
            return x => x != null && r.Contains(x, StringComparer.OrdinalIgnoreCase);
        }

        public Func<string, int?> GetRejectionReasonToPauseDaysMapping()
        {
            var d = RejectionReasons.ToDictionary(x => x.Name, x => x);
            return x => d.ContainsKey(x) ? d[x]?.PauseDays : this.OtherPauseDays;
        }

        public class ScoringRule
        {
            public string Name { get; set; }
            public bool ForceManualCheck { get; set; }
        }

        public class RejectionReason
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public IList<ScoringRule> ScoringRules { get; set; }
            public int? PauseDays { get; set; }
        }

        public int? OtherPauseDays { get; set; }

        public IList<RejectionReason> RejectionReasons { get; set; }

        public class RejectionEmail
        {
            public string TemplateName { get; set; }
            public ISet<string> RequiredRejectionReasons { get; set; }
        }

        public IList<RejectionEmail> RejectionEmails { get; set; }

        public string GetRejectionEmailTemplateNameByRejectionReasons(IList<string> rejectionReasonNames)
        {
            foreach (var r in RejectionEmails)
            {
                if (r.RequiredRejectionReasons.IsSubsetOf(rejectionReasonNames))
                {
                    return r.TemplateName;
                }
            }
            return null;
        }

        public static CompanyLoanRejectionScoringSetup Instance
        {
            get
            {
                return Parse(NEnv.CompanyLoanRejectionReasonsFile);
            }
        }

        public static CompanyLoanRejectionScoringSetup Parse(XDocument d)
        {
            var rejectionReasonsElement = d
                            .Descendants()
                            .Where(x => x.Name.LocalName.Equals("RejectionReasons", StringComparison.OrdinalIgnoreCase))
                            .Single();

            var otherPauseDays = rejectionReasonsElement.Attribute("otherPauseDays")?.Value;

            Func<string, decimal?> pDec = s => string.IsNullOrWhiteSpace(s) ? new decimal?() : decimal.Parse(s, CultureInfo.InvariantCulture);
            Func<string, int?> pInt = s => string.IsNullOrWhiteSpace(s) ? new int?() : int.Parse(s, CultureInfo.InvariantCulture);

            var rejectionReasons = rejectionReasonsElement
                .Descendants()
                .Where(x => x.Name.LocalName.Equals("RejectionReason", StringComparison.OrdinalIgnoreCase))
                .Select(rejectionReasonElement =>
                {
                    var name = rejectionReasonElement.Attribute("name").Value;
                    var displayName = rejectionReasonElement.Attribute("displayName").Value;
                    var pauseDays = rejectionReasonElement.Attribute("pauseDays")?.Value;
                    var ruleElements = rejectionReasonElement
                        .Descendants()
                        .Where(x => x.Name.LocalName.Equals("RejectionReasonScoringRule", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var rules = ruleElements.Select(x => new ScoringRule
                    {
                        Name = x.Value,
                        ForceManualCheck = x.Attribute("forceManualCheck")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
                    }).ToList();
                    return new RejectionReason
                    {
                        Name = name,
                        DisplayName = displayName,
                        ScoringRules = rules,
                        PauseDays = string.IsNullOrWhiteSpace(pauseDays) ? null : new int?(int.Parse(pauseDays))
                    };
                })
                .ToList();

            var rejectionEmails = d
                .Descendants()
                .Where(x => x.Name.LocalName.Equals("RejectionEmail", StringComparison.OrdinalIgnoreCase))
                .Select(rejectionReasonElement =>
                {
                    var templateName = rejectionReasonElement.Attribute("templateName").Value;
                    var requiredRejectionReasons = new HashSet<string>(rejectionReasonElement
                        .Descendants()
                        .Where(x => x.Name.LocalName.Equals("RequiredRejectionReason", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Value));
                    return new RejectionEmail
                    {
                        TemplateName = templateName,
                        RequiredRejectionReasons = requiredRejectionReasons
                    };
                })
                .ToList();

            return new CompanyLoanRejectionScoringSetup
            {
                OtherPauseDays = string.IsNullOrWhiteSpace(otherPauseDays) ? new int?() : int.Parse(otherPauseDays),
                RejectionReasons = rejectionReasons,
                RejectionEmails = rejectionEmails
            };
        }
    }
}