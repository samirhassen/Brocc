using NTech.Services.Infrastructure;
using nUser.DbModel;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;

namespace nUser.Controllers
{
    [NTechApi]
    public class UserSettingController : NController
    {
        //NOTE: Value null = removed
        [HttpPost]
        public ActionResult Load(int? userId, string settingName)
        {
            var currentUserId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;

            if (string.IsNullOrWhiteSpace(settingName))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing settingName");
            }

            if (!userId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing userId");
            }

            if (currentUserId != userId.Value.ToString())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "User can only load own settings");
            }

            using (var context = new UsersContext())
            {
                var value = context
                    .UserSettings
                    .Where(x => x.UserId == userId.Value && x.Name == settingName)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Value)
                    .FirstOrDefault();

                return Json2(new
                {
                    HasValue = value != null,
                    Value = value
                });
            }
        }

        //NOTE: Value null = removed
        [HttpPost]
        public ActionResult Store(int? userId, string settingName, string settingValue)
        {
            var currentUserId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;

            if (string.IsNullOrWhiteSpace(settingName))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing settingName");
            }

            if (!userId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing userId");
            }

            if (currentUserId != userId.Value.ToString())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "User can only load own settings");
            }
            using (var context = new UsersContext())
            {
                var s = new UserSetting
                {
                    CreatedById = int.Parse(currentUserId),
                    CreationDate = DateTime.Now,
                    Name = settingName,
                    UserId = userId.Value,
                    Value = settingValue
                };
                context.UserSettings.Add(s);
                context.SaveChanges();
                return Json2(new { });
            }
        }
    }
}