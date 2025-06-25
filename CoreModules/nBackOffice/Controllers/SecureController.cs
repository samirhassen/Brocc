using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using Serilog;
using Serilog.Context;

namespace nBackOffice.Controllers
{
    public class SecureController : NController
    {
        public ActionResult ActivateGroupMembership()
        {
            return View();
        }

        public ActionResult MissingGroups()
        {
            if (User?.Identity is ClaimsIdentity p)
            {
                ViewBag.Groups = p.Claims.Where(x => x.Type == p.RoleClaimType).Select(x => x.Value).ToList();
            }
            else
            {
                ViewBag.Groups = new List<string>();
            }

            return View();
        }

        public ActionResult ChooseGroup()
        {
            return RedirectToAction("NavMenu");
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            using (var _ = LogContext.PushProperties(NTechLoggingMiddleware
                       .GetProperties(this?.Request?.GetOwinContext()).ToArray()))
            {
                NLog.Information("{EventType}", "UserLogout");
            }

            if (User?.Identity?.IsAuthenticated ?? false)
            {
                Session.Abandon();
                Request.GetOwinContext().Authentication.SignOut();
            }

            return RedirectToAction("LoggedOut", "Secure");
        }

        [AllowAnonymous]
        public ActionResult LoggedOut()
        {
            if (this?.User?.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Logout");
            }

            return View();
        }

        public ActionResult NavMenu(string testRoles)
        {
            var (m, subGroupOrder) = MenuWithSubGroups;

            List<string> menuRoles;
            Func<string, bool> isUserInRole;
            if (!string.IsNullOrWhiteSpace(testRoles) && !NEnv.IsProduction)
            {
                menuRoles = testRoles.Split(',').ToList();
                isUserInRole = testRoles.Contains;
            }
            else
            {
                var allMenuRoles = new HashSet<string>(m
                    .SelectMany(x => x.Items.Where(y => y.RequiredRoleName != null).Select(y => y.RequiredRoleName))
                    .Concat(m.SelectMany(x => x.SubGroups.SelectMany(y =>
                        y.Items.Where(z => z.RequiredRoleName != null).Select(z => z.RequiredRoleName)))));
                menuRoles = allMenuRoles.Where(this.User.IsInRole).ToList();
                isUserInRole = this.User.IsInRole;
            }

            var model = new
            {
                currentUser = new
                {
                    menuRoles = menuRoles
                },
                menuGroups = m.Select(x => new
                {
                    groupName = x.GroupName,
                    iconUrl = Url.Content($"~/Content/img/{x.Icon ?? "System.png"}"),
                    items = x.Items
                        .Where(r => r.RequiredRoleName == null || isUserInRole(r.RequiredRoleName))
                        .Select(y => new
                        {
                            url = y.AbsoluteUri,
                            name = y.FunctionName,
                            requiredRole = y.RequiredRoleName
                        }).ToList(),
                    subGroups = x.SubGroups.Select(y => new
                    {
                        name = y.SubGroupName,
                        items = y.Items
                            .Where(r => r.RequiredRoleName == null || isUserInRole(r.RequiredRoleName))
                            .Select(z => new
                            {
                                url = z.AbsoluteUri,
                                name = z.FunctionName,
                                requiredRole = z.RequiredRoleName
                            }).ToList()
                    })
                }),
                subGroupOrder = subGroupOrder,
                allowShowDetails = NEnv.ShowNavMenuDebugInfo
            };

            ViewBag.JsonInitialData =
                Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(model)));
            ViewBag.InitialData = model;
            ViewBag.IsNavPage = true;
            ViewBag.ReleaseNumber =
                NTechSimpleSettings.GetValueFromClientResourceFile("CurrentReleaseMetadata.txt", "releaseNumber");

            return View();
        }

        [HttpGet]
        [Route("Ui/CrossModuleNavigate")]
        public ActionResult CrossModuleNavigate(string targetCode, string backTargetCode)
        {
            var s = NTechCache.WithCache("fdc8dacf-c4e5-425d-9c26-5baeaf7ecd5c", TimeSpan.FromMinutes(15),
                () => new CrossModuleNavigationService());

            if (!s.TryGetUrlFromCrossModuleNavigationToken(targetCode, NEnv.ServiceRegistry, out var url,
                    out var errorMessage, backTargetCode: backTargetCode.NormalizeNullOrWhitespace()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);

            return Redirect(url.ToString());
        }

        [HttpPost]
        [NTechApi]
        [Route("Api/CrossModuleNavigate/Resolve")]
        public ActionResult CrossModuleNavigateResolve(string targetCode)
        {
            var s = NTechCache.WithCache("fdc8dacf-c4e5-425d-9c26-5baeaf7ecd5c", TimeSpan.FromMinutes(15),
                () => new CrossModuleNavigationService());

            string moduleName = null;
            if (!s.TryGetUrlFromCrossModuleNavigationToken(targetCode, NEnv.ServiceRegistry, out var url,
                    out var errorMessage, observeTargetModuleName: x => moduleName = x))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);

            string localEmbeddedBackofficeUrl = null;
            if (moduleName?.ToLowerInvariant() == "nbackoffice" && url.LocalPath.StartsWith("/s"))
            {
                localEmbeddedBackofficeUrl = url.LocalPath.Substring(2);
            }

            return new JsonNetActionResult
            {
                Data = new
                {
                    Url = url,
                    LocalEmbeddedBackofficeUrl =
                        localEmbeddedBackofficeUrl //Helps the embedded angular app having to avoid a bunch of reloads due to passing the gateway going between it's own pages
                }
            };
        }

        [HttpPost]
        [NTechApi]
        [AllowAnonymous]
        public ActionResult FetchNavigationLinks()
        {
            var allFunctions = NTechCache.WithCache("f99b6e95-d3e4-4b74-ac67-66ca4afa9a605", TimeSpan.FromHours(1),
                () =>
                {
                    var m = Menu;
                    return m
                        .SelectMany(x => x.Items)
                        .Select(x => new
                        {
                            x.RequiredRoleName,
                            x.FunctionName,
                            x.AbsoluteUri,
                            SubGroupName = (string)null
                        })
                        .Concat(m.SelectMany(x => x.SubGroups.SelectMany(y => y.Items.Select(z => new
                        {
                            z.RequiredRoleName,
                            z.FunctionName,
                            z.AbsoluteUri,
                            y.SubGroupName
                        }))))
                        .Select(x => new
                        {
                            name = string.IsNullOrWhiteSpace(x.SubGroupName)
                                ? x.FunctionName
                                : $"{x.SubGroupName}: {x.FunctionName}",
                            url = x.AbsoluteUri,
                            requiredRole = x.RequiredRoleName
                        });
                });

            return Json(new
            {
                allFunctions = allFunctions
            });
        }
    }
}