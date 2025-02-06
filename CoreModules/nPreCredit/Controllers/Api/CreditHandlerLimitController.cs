using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/CreditHandlerLimit")]
    public class CreditHandlerLimitController : NController
    {
        [Route("Get")]
        [HttpPost]
        public ActionResult Get()
        {
            using (var context = new PreCreditContext())
            {
                var existingSettings = context
                     .HandlerLimitLevels
                     .Select(x => new { x.HandlerUserId, x.IsOverrideAllowed, x.LimitLevel })
                     .ToDictionary(x => x.HandlerUserId);

                var u = new UserClient();
                var handlerUserIds = u.GetAllUsersInGroup("ConsumerCredit", "Middle");
                var allUsersWithNames = u.GetUserDisplayNamesByUserId(forceRefresh: true);

                var users = handlerUserIds.Select(x => new
                {
                    UserId = x,
                    DisplayName = allUsersWithNames[x.ToString()],
                    LimitLevel = existingSettings.ContainsKey(x) ? existingSettings[x].LimitLevel : 0,
                    IsOverrideAllowed = existingSettings.ContainsKey(x) ? existingSettings[x].IsOverrideAllowed : false,
                });

                var levels = HandlerLimitEngine
                    .ParseLimitLevelsFromClientConfig(NEnv.ClientCfgCore)
                    .Select((x, i) => new
                    {
                        LimitLevel = i + 1,
                        MaxAmount = x
                    })
                    .ToList();

                return Json2(new
                {
                    users,
                    levels
                });
            }
        }


        [Route("Edit")]
        [HttpPost]
        public ActionResult Edit(int userId, int limitLevel, bool isOverrideAllowed)
        {
            var existingLevels = HandlerLimitEngine.ParseLimitLevelsFromClientConfig(NEnv.ClientCfgCore);

            if (limitLevel < 0 || limitLevel > existingLevels.Count + 1)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such limitLevel");

            using (var context = new PreCreditContext())
            {
                var current = context.HandlerLimitLevels.SingleOrDefault(x => x.HandlerUserId == userId);

                if (current == null)
                {
                    current = new DbModel.HandlerLimitLevel
                    {
                        HandlerUserId = userId
                    };
                    context.HandlerLimitLevels.Add(current);
                }
                current.ChangedById = CurrentUserId;
                current.ChangedDate = Clock.Now;
                current.InformationMetaData = InformationMetadata;
                current.IsOverrideAllowed = isOverrideAllowed;
                current.LimitLevel = limitLevel;

                context.SaveChanges();
            }

            return Json2(new { });
        }


        [Route("CheckIfOver")]
        [HttpPost]
        public ActionResult CheckIfOverHandlerLimit(string applicationNr, decimal newLoanAmount, bool? isCompanyLoan)
        {
            bool isOverHandlerLimit;
            bool? isAllowedToOverrideHandlerLimit;

            var services = DependancyInjection.Services;
            var appRepo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

            ISet<int> customerIdsOverride = null;
            if (NEnv.IsCompanyLoansEnabled)
                customerIdsOverride = OverrideCustomerIdsIfCompanyLoan(applicationNr, isCompanyLoan, appRepo);

            var creditClient = LegacyServiceClientFactory.CreateCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            HandlerLimitEngine.CheckIfOverHandlerLimitShared(applicationNr, newLoanAmount, CurrentUserId, out isOverHandlerLimit, out isAllowedToOverrideHandlerLimit, appRepo,
                creditClient, services.Resolve<IPreCreditContextFactoryService>(), NEnv.ClientCfgCore, customerIdsOverride);

            return Json2(new
            {
                isOverHandlerLimit = isOverHandlerLimit,
                isAllowedToOverrideHandlerLimit = isAllowedToOverrideHandlerLimit
            });
        }

        private ISet<int> OverrideCustomerIdsIfCompanyLoan(string applicationNr, bool? isCompanyLoan, IPartialCreditApplicationModelRepository appRepo)
        {
            if (!isCompanyLoan.HasValue)
            {
                using (var context = new PreCreditContext())
                {
                    isCompanyLoan = context.CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr && x.ApplicationType == CreditApplicationTypeCode.companyLoan.ToString()).Count() > 0;
                }
            }

            if (!isCompanyLoan.GetValueOrDefault())
                return null;

            var customerIds = new HashSet<int>();
            var a = appRepo.Get(applicationNr, applicationFields: new List<string> { "applicantCustomerId", "companyCustomerId" });
            customerIds.Add(a.Application.Get("applicantCustomerId").IntValue.Required);
            customerIds.Add(a.Application.Get("companyCustomerId").IntValue.Required);

            return customerIds;
        }

        [Route("CheckAmount")]
        [HttpPost]
        public ActionResult CheckAmount(decimal amount)
        {
            var appRepo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
            var creditBalance = 0; // Set in engine for existing loans for the applicants. We only check for a single amount at this point.  

            var engine = DependancyInjection.Services.Resolve<HandlerLimitEngine>();
            engine.CheckHandlerLimits(amount, creditBalance, CurrentUserId,
                out var isOverHandlerLimit, out var isAllowedToOverrideHandlerLimit);

            return Json2(new
            {
                isOverHandlerLimit,
                isAllowedToOverrideHandlerLimit
            });
        }

    }
}