using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    public abstract class ScoringRuleBase : IScoringDataModelConsumer
    {
        protected class RuleContext
        {
            private ScoringRuleBase scoringRule;
            private ScoringDataModel input;
            private IScoringContext scoreContext;

            public RuleContext(ScoringRuleBase scoringRule, ScoringDataModel input, IScoringContext scoreContext)
            {
                this.scoringRule = scoringRule;
                this.input = input;
                this.scoreContext = scoreContext;
            }

            private void CheckDeclared(string name, int? applicantNr)
            {
                var s = applicantNr.HasValue ? scoringRule.RequiredApplicantItems : scoringRule.RequiredApplicationItems;
                if (!s.Contains(name))
                    throw new Exception($"ScoringRule {scoringRule.RuleName} is attempting to use undeclared value {(applicantNr.HasValue ? "applicant" : "application")}.{name}");
            }

            public string RequireString(string name, int? applicantNr = null)
            {
                CheckDeclared(name, applicantNr);
                var s = input.GetString(name, applicantNr);
                if (string.IsNullOrWhiteSpace(s))
                    throw MissingRequiredScoringDataException.Create(name, applicantNr);
                return s;
            }

            public decimal RequireDecimal(string name, int? applicantNr)
            {
                CheckDeclared(name, applicantNr);
                var s = input.GetDecimal(name, applicantNr);
                if (!s.HasValue)
                    throw MissingRequiredScoringDataException.Create(name, applicantNr);
                return s.Value;
            }

            public int RequireInt(string name, int? applicantNr)
            {
                CheckDeclared(name, applicantNr);
                var s = input.GetInt(name, applicantNr);
                if (!s.HasValue)
                    throw MissingRequiredScoringDataException.Create(name, applicantNr);
                return s.Value;
            }

            public bool RequireBool(string name, int? applicantNr)
            {
                CheckDeclared(name, applicantNr);
                var s = input.GetBool(name, applicantNr);
                if (!s.HasValue)
                    throw MissingRequiredScoringDataException.Create(name, applicantNr);
                return s.Value;
            }

            public IEnumerable<int> RequireApplicantNrs()
            {
                return Enumerable.Range(1, RequireInt("nrOfApplicants", null));
            }

            public bool ForAnyApplicant(Func<int, bool> f)
            {
                return RequireApplicantNrs().Any(applicantNr => f(applicantNr));
            }

            public bool ForAllApplicants(Func<int, bool> f)
            {
                return RequireApplicantNrs().All(applicantNr => f(applicantNr));
            }

            public enum MimumDemandsResultCode
            {
                Accepted,
                Rejected,
                AcceptedWithManualAttention,
                RejectedWithManualAttention
            }

            public MimumDemandsResultCode RejectIfForAnyApplicant(Func<int, bool> f)
            {
                return ForAnyApplicant(f) ? MimumDemandsResultCode.Rejected : MimumDemandsResultCode.Accepted;
            }

            public MimumDemandsResultCode RejectIfForAllApplicants(Func<int, bool> f)
            {
                return ForAllApplicants(f) ? MimumDemandsResultCode.Rejected : MimumDemandsResultCode.Accepted;
            }

            public void RunExternalRuleWithSameDataAgainstDifferentContext(MinimumDemandScoringRule rule, IScoringContext context)
            {
                rule.Score(this.input, context);
            }

            public void InjectScoringVariables(Action<IScoringDataModelPopulator> a)
            {
                a(this.input);
            }

            public void SetDebugData(string debugData)
            {
                this.scoreContext.SetDebugData(this.scoringRule.RuleName, debugData);                
            }
        }
        
        private ISet<string> requiredApplicationItems = null;
        private ISet<string> requiredApplicantItems = null;

        public ISet<string> RequiredApplicationItems
        {
            get
            {
                if (requiredApplicationItems == null)
                {
                    requiredApplicationItems = DeclareRequiredApplicationItems();
                }
                return requiredApplicationItems;
            }
        }

        public ISet<string> RequiredApplicantItems
        {
            get
            {
                if (requiredApplicantItems == null)
                {
                    requiredApplicantItems = DeclareRequiredApplicantItems();
                }
                return requiredApplicantItems;
            }
        }                

        protected int RoundedMeanValue(int value, int divider)
        {
            return (int)Math.Round(((decimal)value) / ((decimal)divider));
        }

        
        protected ISet<string> ToSet(params string[] args)
        {
            return new HashSet<string>(args);
        }

        protected abstract ISet<string> DeclareRequiredApplicationItems();
        protected abstract ISet<string> DeclareRequiredApplicantItems();

        public virtual string RuleName
        {
            get
            {
                var className = this.GetType().Name;
                if (className.EndsWith("Rule"))
                    return className.Substring(0, className.Length - "Rule".Length);
                else
                    return className;
            }
        }
    }

    public interface IScoringDataModelConsumer
    {
        ISet<string> RequiredApplicationItems { get; }
        ISet<string> RequiredApplicantItems { get; }
    }
}