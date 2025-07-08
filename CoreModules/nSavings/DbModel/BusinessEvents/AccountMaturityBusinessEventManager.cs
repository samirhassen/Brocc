using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NTech;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using Serilog;

namespace nSavings.DbModel.BusinessEvents;

public class AccountMaturityBusinessEventManager(
    in int userId,
    in string informationMetadata,
    in IClock clock,
    in ISavingsContext ctx = null,
    InterestCapitalizationBusinessEventManager capMgr = null)
    : BusinessEventManagerBase(userId, informationMetadata, clock)
{
    private readonly ISavingsContext context = ctx ?? new SavingsContext();

    public async Task<(int, int)> RunAccountMaturityJobAsync(CancellationToken ct)
    {
        var applicableAccounts = await context.SavingsAccountHeadersQueryable
            .Where(a =>
                a.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount) &&
                a.Status == nameof(SavingsAccountStatusCode.Active) &&
                a.MaturesAt <= Clock.Today)
            .ToListAsync(ct);

        var successful = 0;
        var failed = 0;

        foreach (var account in applicableAccounts)
        {
            try
            {
                await ConvertMatureAccount(account, ct);
                successful++;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error running account maturity job for account {accountNr}", account.SavingsAccountNr);
                failed++;
                // Ignore and continue
            }
        }

        return (successful, failed);
    }

    private async Task ConvertMatureAccount(SavingsAccountHeader account, CancellationToken ct)
    {
        context.BeginTransaction();
        try
        {
            var evt = AddBusinessEvent(BusinessEventType.FixedInterestAccountMaturity, context);

            {
                // End capitalization up until account maturity date
                var mgr = capMgr ?? new InterestCapitalizationBusinessEventManager(
                    UserId, InformationMetadata, Clock, null, context);
                await mgr.RunInterestCapitalizationForAccountAsync(account.SavingsAccountNr,
                    account.MaturesAt!.Value, evt, true);
            }

            // Convert to standard account
            await ConvertAccount(account.SavingsAccountNr, evt, ct);
            
            context.CommitTransaction();
        }
        catch (Exception)
        {
            context.RollbackTransaction();
            throw;
        }
    }

    private async Task ConvertAccount(string accountNr, BusinessEvent evt, CancellationToken ct)
    {
        var account = await context.SavingsAccountHeadersQueryable
            .Include(a => a.Comments)
            .SingleAsync(a => a.SavingsAccountNr == accountNr, ct);

        account.AccountTypeCode = nameof(SavingsAccountTypeCode.StandardAccount);
        account.ChangedDate = Clock.Now;
        account.ChangedById = UserId;
        account.Comments.Add(new SavingsAccountComment
        {
            CommentById = UserId,
            CommentDate = Now,
            CommentText = "Converted from fixed interest account",
            SavingsAccount = account,
            SavingsAccountNr = accountNr,
            EventType = $"BusinessEvent_{evt.EventType}"
        });

        await context.SaveChangesAsync(ct);
    }
}