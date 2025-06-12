using System.Web.Mvc;
using nSavings.Code;
using nSavings.Controllers.Api;
using nSavings.DbModel;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Email;

namespace nSavings.Controllers.Ui
{
    [NTechAuthorizeSavingsHigh]
    public class ScheduledTasksController : NController
    {
        public static void SetMenu(Controller c, string currentName)
        {
            c.ViewBag.CurrentTaskName = currentName;

            c.ViewBag.IsDailyKycScreenEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.kycbatchscreening");

            c.ViewBag.TrapetsAmlExportUrl = c.Url.Action("TrapetsAmlExport", "ScheduledTasks", new { });
            c.ViewBag.IsTrapetsAmlExportEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.trapetsaml.v1");
            c.ViewBag.UpdateDatawarehouseUrl = c.Url.Action("UpdateDatawarehouse", "ScheduledTasks", new { });
            c.ViewBag.BookkeepingFilesUrl = c.Url.Action("BookkeepingFiles", "ScheduledTasks", new { });
            c.ViewBag.DailyKycScreenUrl = c.Url.Action("DailyKycScreen", "ScheduledTasks", new { });
            c.ViewBag.FatcaExportUrl = c.Url.Action("FatcaExport", "ScheduledTasks", new { });
            c.ViewBag.IsCm1AmlExportEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.Cm1aml.v1");
            c.ViewBag.Cm1AmlExportUrl = c.Url.Action("Cm1AmlExport", "ScheduledTasks", new { });
            c.ViewBag.IsTreasuryAmlExportEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.Treasuryaml.v1");
            c.ViewBag.TreasuryAmlExportUrl = c.Url.Action("TreasuryAmlExport", "ScheduledTasks", new { });

            c.ViewBag.IsCustomsAccountsExportEnabled =
                NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.savingsCustomsAccountsExport.v1");
            c.ViewBag.CustomsAccountsExportUrl = c.Url.Action("CustomsAccountsExport", "ScheduledTasks", new { });
        }

        [HttpGet]
        [Route("Ui/BookkeepingFiles/List")]
        public ActionResult BookkeepingFiles()
        {
            SetMenu(this, "BookkeepingFiles");

            using (var context = new SavingsContext())
            {
                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    pending = new
                    {
                        Dates = ApiBookkeepingFilesController.GetDates(context, Clock)
                    },
                    today = Clock.Today.ToString("yyyy-MM-dd"),
                    createFileUrl = Url.Action("CreateFile", "ApiBookkeepingFiles"),
                    getFilesPageUrl = Url.Action("GetFilesPage", "ApiBookkeepingFiles"),
                    rulesAsXlsUrl = Url.Action("RulesAsXls", "ApiBookkeepingFiles"),
                    exportProfileName = NEnv.BookkeepingFileExportProfileName
                });
                return View();
            }
        }

        [Route("Ui/ScheduledTasks/Cm1AmlExport")]
        public ActionResult Cm1AmlExport()
        {
            SetMenu(this, "Cm1AmlExport");

            using (var context = new SavingsContext())
            {
                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    exportProfileName = "Cm1",
                    today = Clock.Today.ToString("yyyy-MM-dd"),
                    createExportUrl = Url.Action("CreateExport", "ApiCm1AmlExport"),
                    getFilesPageUrl = Url.Action("GetFilesPage", "ApiCm1AmlExport")
                });
                return View();
            }
        }

        [Route("Ui/ScheduledTasks/TreasuryAmlExport")]
        public ActionResult TreasuryAmlExport()
        {
            SetMenu(this, "TreasuryAmlExport");

            using (var context = new SavingsContext())
            {
                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    exportProfileName = "Treasury",
                    today = Clock.Today.ToString("yyyy-MM-dd"),
                    createExportUrl = Url.Action("CreateExport", "ApiTreasuryAmlExport"),
                    getFilesPageUrl = Url.Action("GetFilesPage", "ApiTreasuryAmlExport")
                });
                return View();
            }
        }

        [Route("Ui/ScheduledTasks/CustomsAccountsExport")]
        public ActionResult CustomsAccountsExport()
        {
            SetMenu(this, "CustomsAccountsExport");

            ViewBag.JsonInitialData = EncodeInitialData(new
            {
                today = Clock.Today.ToString("yyyy-MM-dd"),
                skipDeliver = NEnv.FinnishCustomsAccountsSettings.OptBool("skipDeliver")
            });
            return View();
        }

        [Route("Ui/ScheduledTasks/TrapetsAmlExport")]
        public ActionResult TrapetsAmlExport()
        {
            SetMenu(this, "TrapetsAmlExport");

            using (var context = new SavingsContext())
            {
                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    exportProfileName = NEnv.TrapetsAmlExportProfileName,
                    createExportUrl = Url.Action("CreateExport", "ApiTrapetsAmlExport"),
                    getFilesPageUrl = Url.Action("GetFilesPage", "ApiTrapetsAmlExport")
                });
                return View();
            }
        }

        [Route("Ui/ScheduledTasks/FatcaExport")]
        public ActionResult FatcaExport()
        {
            SetMenu(this, "FatcaExport");

            using (var context = new SavingsContext())
            {
                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    exportProfileName = NEnv.FatcaFileExportProfileName,
                    today = Clock.Today
                });
                return View();
            }
        }

        [HttpGet]
        [Route("Ui/DailyKycScreen/List")]
        public ActionResult DailyKycScreen()
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.kycbatchscreening"))
                return HttpNotFound();

            SetMenu(this, "DailyKycScreen");

            var screenDate = Clock.Today;

            using (var context = new SavingsContext())
            {
                var activeResult =
                    ApiDailyKycScreenController.GetActiveCustomerIdsAndAlreadyScreenedCount(context, screenDate);

                ViewBag.ShowTestEmails = !NEnv.IsProduction && NTechEmailServiceFactory.HasEmailProvider;
                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    pending = new
                    {
                        UnscreenedCount = activeResult.ActiveCustomerIds.Count - activeResult.ScreenedTodayCount
                    },
                    getFilesPageUrl = Url.Action("GetFilesPage", "ApiDailyKycScreen"),
                    screenCustomersUrl = Url.Action("ScreenCustomers", "ApiDailyKycScreen"),
                    today = Clock.Today.ToString("yyyy-MM-dd")
                });
                return View();
            }
        }
    }
}