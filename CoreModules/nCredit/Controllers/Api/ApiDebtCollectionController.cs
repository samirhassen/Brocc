using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiDebtCollectionController : NController
    {
        private ActionResult Guarded(Func<ActionResult> f)
        {
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.debtcollectionexport",
                    f,
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running")
            );
        }

        [HttpPost]
        [Route("Api/Credit/SendAllEligableToDebtCollection")]
        public ActionResult SendAllEligableCreditsToDebtCollection()
        {
            return Guarded(() => SendCreditsToDebtCollection(null, true));
        }

        [HttpPost]
        [Route("Api/Credit/SendSpecificToDebtCollection")]
        public ActionResult SendSpecificCreditsToDebtCollection(List<string> creditNrs)
        {
            return Guarded(() =>
            {
                if (creditNrs == null || !creditNrs.Any())
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Parameters creditNrs must be a list of at least one creditnr");
                return SendCreditsToDebtCollection(creditNrs, null);
            });
        }

        private ActionResult SendCreditsToDebtCollection(List<string> creditNrs, bool? allEligable)
        {
            var w = Stopwatch.StartNew();
            int? count = null;
            try
            {
                var mgr = Service.CreditDebtCollectionBusinessEventManager;
                IDictionary<string, string> skippedCreditNrsWithReasons;
                using (var context = CreateCreditContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;

                    if (allEligable.HasValue && allEligable.Value && creditNrs == null)
                    {
                        count = mgr.SendEligibleCreditsToDebtCollection(context, out skippedCreditNrsWithReasons);
                    }
                    else if (creditNrs != null && creditNrs.Any() && !allEligable.HasValue)
                    {
                        count = mgr.SendCreditsToDebtCollection(new HashSet<string>(creditNrs), context, out skippedCreditNrsWithReasons);
                    }
                    else
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Requires exactly one of the parameters creditNrs and allEligable=true");
                    }
                    context.ChangeTracker.DetectChanges();
                    context.SaveChanges();
                }
                return Json2(new
                {
                    exportedCount = count,
                    warnings = skippedCreditNrsWithReasons.Any() ? skippedCreditNrsWithReasons.Select(x => $"Skipped credit {x.Key}: {x.Value}").ToList() : null,
                    totalMilliseconds = w.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "SendCreditsToDebtCollection crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
            finally
            {
                w.Stop();
            }
        }

        [HttpPost]
        [Route("Api/Credit/GetDebtCollectionFilesPage")]
        public ActionResult GetDebtCollectionFilesPage(int pageSize, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingDebtCollectionFileHeaders;

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FileArchiveKey,
                        x.XlsFileArchiveKey,
                        CreditsCount = (context.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.DebtCollectionFileExternalId.ToString() && y.Value == x.ExternalId).Count()),
                        UserId = x.ChangedById,
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.CreditsCount,
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString()),
                        x.XlsFileArchiveKey,
                        XlsArchiveDocumentUrl = Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.XlsFileArchiveKey, setFileDownloadName = true }, Request.Url.Scheme),
                        x.FileArchiveKey,
                        FileArchiveDocumentUrl = x.FileArchiveKey == null ? null : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }, Request.Url.Scheme),
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                });
            }
        }
    }
}