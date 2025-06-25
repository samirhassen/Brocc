using System.Linq;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.DbModel.BusinessEvents
{
    public class ResolveAccountCreationRemarksBusinessEventManager : AccountActivationManagerBase
    {
        private readonly SavingsContextFactory contextFactory;

        public ResolveAccountCreationRemarksBusinessEventManager(INTechCurrentUserMetadata user, ICoreClock clock,
            SavingsContextFactory contextFactory, IClientConfigurationCore clientConfiguration) : base(user, clock,
            clientConfiguration)
        {
            this.contextFactory = contextFactory;
        }

        public bool TryResolve(
            string savingsAccountNr,
            string resolutionAction,
            out string failedMessage)
        {
            using (var context = new SavingsContext())
            {
                if (string.IsNullOrWhiteSpace(savingsAccountNr))
                {
                    failedMessage = "Missing savingsAccountNr";
                    return false;
                }

                var account = context.SavingsAccountHeaders.Single(x => x.SavingsAccountNr == savingsAccountNr);
                if (account == null)
                {
                    failedMessage = "No such account";
                    return false;
                }

                if (account.Status != SavingsAccountStatusCode.FrozenBeforeActive.ToString())
                {
                    failedMessage = "Wrong account status";
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.AccountCreationRemarkResolution, context);
                var wasOpened = false;
                switch (resolutionAction)
                {
                    case "Open":
                        SetStatus(account, SavingsAccountStatusCode.Active, evt, context);
                        AddComment("Account opened after pending remarks checked",
                            "AccountCreationRemarkResolution_Opened", account, context);
                        wasOpened = true;
                        break;
                    case "Close":
                        SetStatus(account, SavingsAccountStatusCode.Closed, evt, context);
                        AddComment("Account closed after pending remarks checked",
                            "AccountCreationRemarkResolution_Closed", account, context);
                        break;
                    default:
                        failedMessage = "Invalid resolutionAction";
                        return false;
                }

                context.SaveChanges();

                if (wasOpened)
                {
                    TrySendWelcomeEmail(account.SavingsAccountNr, context,
                        $"OnResolveRemarks_{account.SavingsAccountNr}");
                    context.SaveChanges();
                }

                failedMessage = null;

                return true;
            }
        }

        private SavingsAccountComment AddComment(string commentText, string eventType,
            SavingsAccountHeader savingsAccount, ISavingsContext context)
        {
            var c = new SavingsAccountComment
            {
                CommentById = UserId,
                CommentDate = Now,
                CommentText = commentText,
                SavingsAccount = savingsAccount,
                EventType = eventType,
            };
            FillInInfrastructureFields(c);
            context.AddSavingsAccountComments(c);
            return c;
        }
    }
}