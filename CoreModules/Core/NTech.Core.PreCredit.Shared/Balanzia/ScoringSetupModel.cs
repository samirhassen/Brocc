using nPreCredit.Code.Balanzia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using static nPreCredit.Code.ScoringSetupModel;

namespace nPreCredit.Code
{
    public class ScoringSetupModel
    {        
        private ScoringSetupModel()
        {
            
        }

        private int? OtherPauseDays { get; set; }

        public IList<RejectionReason> RejectionReasons { get; set; }
        private IList<RejectionEmail> RejectionEmails { get; set; }
        private IList<ManualControlOnAcceptRule> ManualControlOnAcceptRules { get; set; }

        public class ManualControlOnAcceptRule
        {
            public string Name { get; set; }
            public string DisplayText { get; set; }
            public Dictionary<string, string> Settings { get; set; }
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

        public class RejectionEmail
        {
            public string TemplateName { get; set; }
            public ISet<string> RequiredRejectionReasons { get; set; }
        }

        public IDictionary<string, string> GetScoringRuleToRejectionReasonMapping()
        {
            return RejectionReasons
                .SelectMany(x => x.ScoringRules.Select(y => new { reason = x.Name, rule = y.Name }))
                .GroupBy(x => x.rule)
                .ToDictionary(x => x.Key, x => x.First().reason);
        }

        public IDictionary<string, string> GetRejectionReasonToDisplayNameMapping()
        {
            return RejectionReasons.ToDictionary(x => x.Name, x => x.DisplayName ?? x.Name);
        }

        public IDictionary<string, string> GetManualControlReasonToDisplayTextMapping()
        {
            return ManualControlOnAcceptRules.ToDictionary(x => x.Name, x => x.DisplayText ?? x.Name);
        }

        public bool IsKnownRejectionReason(string name)
        {
            return RejectionReasons?.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? false;
        }


        public Func<string, int?> GetRejectionReasonToPauseDaysMapping()
        {
            var d = RejectionReasons.ToDictionary(x => x.Name, x => x);
            return x => d.ContainsKey(x) ? d[x]?.PauseDays : this.OtherPauseDays;
        }

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

        public Func<string, bool> GetIsKnownRejectionReason()
        {
            var r = this.RejectionReasons.Select(x => x.Name).ToHashSetShared();
            return x => x != null && r.Contains(x, StringComparer.OrdinalIgnoreCase);
        }

        public static ScoringSetupModel CreateDirect(int? otherPauseDays, List<RejectionReason> rejectionReasons, List<RejectionEmail> rejectionEmails,
            List<ManualControlOnAcceptRule> manualControlOnAcceptRules)
        {
            return new ScoringSetupModel()
            {
                OtherPauseDays = otherPauseDays,
                RejectionReasons = rejectionReasons,
                RejectionEmails = rejectionEmails,
                ManualControlOnAcceptRules = manualControlOnAcceptRules
            };
        }

        public static ScoringSetupModel Parse(XDocument d)
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

            var manualControlOnAcceptRules = d
                .Descendants()
                .Where(x => x.Name.LocalName.Equals("ManualControlOnAcceptRule", StringComparison.OrdinalIgnoreCase))
                .Select(x => new ManualControlOnAcceptRule
                {
                    Name = x.Attribute("name").Value,
                    DisplayText = x.Attribute("displayText")?.Value,
                    Settings = x
                        .Descendants()
                        .Where(y => y.Name.LocalName == "ManualControlOnAcceptRuleSetting")
                        .Select(y => new { name = y.Attribute("name")?.Value, value = y.Attribute("value")?.Value })
                        .ToDictionary(y => y.name, y => y.value)
                })
                .ToList();

            return CreateDirect(string.IsNullOrWhiteSpace(otherPauseDays) ? new int?() : int.Parse(otherPauseDays),
                rejectionReasons, rejectionEmails, manualControlOnAcceptRules);
        }
    }
}