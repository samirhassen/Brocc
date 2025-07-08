using System;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.Code.Services;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Core.Savings.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api;

[NTechApi]
[NTechAuthorize(ValidateAccessToken = true)]
[RoutePrefix("Api/SavingsAccount")]
public class ApiCreateSavingsAccountController : NController
{
    [Route("Create")]
    [HttpPost]
    public ActionResult Create(CreateSavingsAccountRequest request)
    {
        try
        {
            var user = GetCurrentUserMetadata();
            var mailSender = new AccountActivationManagerBase(user, CoreClock.SharedInstance, NEnv.ClientCfgCore);
            var customerClient =
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                    NEnv.ServiceRegistry);
            var mgr = new CreateSavingsAccountBusinessEventManager(user, CoreClock.SharedInstance,
                Service.KeyValueStore(user),
                SavingsEnvSettings.Instance, NEnv.ClientCfgCore, customerClient, Service.ContextFactory);

            var service = new SavingsAccountCreationService(CoreClock.SharedInstance,
                SerilogLoggingService.SharedInstance, customerClient,
                (acc, ctx, loc) => mailSender.TrySendWelcomeEmail(acc, ctx, loc),
                CheckInterestRateExists, mgr, Service.ContextFactory, ControllerServiceFactory.CustomerRelationsMerge);

            return Json2(service.CreateAccount(request));
        }
        catch (NTechCoreWebserviceException ex)
        {
            if (ex.IsUserFacing && ex.ErrorHttpStatusCode == 400)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
            throw;
        }
    }

    private static bool CheckInterestRateExists(SavingsAccountTypeCode accountType, Guid? product, DateTime date)
    {
        using var context = new SavingsContext();
        return accountType switch
        {
            SavingsAccountTypeCode.StandardAccount => ChangeInterestRateBusinessEventManager
                .GetCurrentInterestRateForNewAccounts(context, accountType, date) != null,
            SavingsAccountTypeCode.FixedInterestAccount => FixedAccountProductBusinessEventManager.RateValidAt(context,
                product!.Value, date),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}