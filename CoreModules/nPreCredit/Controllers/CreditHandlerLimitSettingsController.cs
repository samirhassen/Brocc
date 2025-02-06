using nPreCredit.Code;
using nPreCredit.Code.Clients;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    [RoutePrefix("CreditHandlerLimitSettings")]
    public class CreditHandlerLimitSettingsController : NController
    {
        protected override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        [Route("Index")]
        public ActionResult Index()
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

                SetInitialData(new
                {
                    users = users,
                    levels = levels,
                    disableBackUrlSupport = true
                });

                return View();
            }
        }

        [HttpPost]
        [Route("Edit")]
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
    }
}