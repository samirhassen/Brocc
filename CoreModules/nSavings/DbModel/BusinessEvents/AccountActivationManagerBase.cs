using System;
using System.Collections.Generic;
using System.Linq;
using nSavings.Code;
using nSavings.Code.Email;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using Serilog;

namespace nSavings.DbModel.BusinessEvents;

public class AccountActivationManagerBase(
    INTechCurrentUserMetadata user,
    ICoreClock clock,
    IClientConfigurationCore clientConfiguration)
    : BusinessEventManagerBaseCore(user, clock, clientConfiguration)
{
    public bool TrySendWelcomeEmail(string savingsAccountNr, ISavingsContext context, string sendingLocation)
    {
        try
        {
            var h = context
                .SavingsAccountHeadersQueryable
                .Where(x => x.SavingsAccountNr == savingsAccountNr)
                .Select(x => new
                {
                    Header = x,
                    IsActive = x.Status == nameof(SavingsAccountStatusCode.Active),
                    WasWelcomeEmailSent = x.DatedStrings.Any(y =>
                        y.Name == nameof(DatedSavingsAccountStringCode.WelcomeEmailSent)),
                    x.MainCustomerId
                })
                .SingleOrDefault();

            if (h == null)
                return false;
            if (!h.IsActive)
                return false;
            if (h.WasWelcomeEmailSent)
                return false;

            var cc = new CustomerClient();
            var email = cc.GetCustomerCardItems(h.MainCustomerId, "email").Opt("email");

            var evt = AddBusinessEvent(BusinessEventType.WelcomeEmailSent, context);
            if (string.IsNullOrWhiteSpace(email))
            {
                AddComment("Welcome email not sent since there is no email on the customer",
                    BusinessEventType.WelcomeEmailSent, context, savingsAccount: h.Header);
                return false;
            }

            var em = EmailServiceFactory.CreateEmailService();
            em.SendTemplateEmail(
                [email],
                "savings-account-opened",
                new Dictionary<string, string>(),
                $"savingsWelcomeEmail{sendingLocation}");

            AddDatedSavingsAccountString(nameof(DatedSavingsAccountStringCode.WelcomeEmailSent), "true", context,
                savingsAccount: h.Header, businessEvent: evt);

            return true;
        }
        catch (Exception ex)
        {
            NLog.Error(ex, $"Failed to send welcome email for account {savingsAccountNr}");
            return false;
        }
    }
}