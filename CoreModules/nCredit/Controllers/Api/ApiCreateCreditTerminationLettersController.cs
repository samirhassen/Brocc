using nCredit.DomainModel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiCreateCreditTerminationLettersController : NController
    {
        private ActionResult CreateTerminationLettersI(bool createTerminationLetters, bool createDeliverFile, List<string> terminateTheseCreditNrs, bool useDelayedDocuments, CreditType creditType)
        {
            var service = Service.GetTerminationLetterService(useDelayedDocuments);
            try
            {
                var result = service.CreateTerminationLetters(createTerminationLetters, createDeliverFile, terminateTheseCreditNrs, creditType);
                return Json2(new { errors = result.Errors, totalMilliseconds = result.TotalMilliseconds, warnings = result.Warnings });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreateTerminationLetters crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [HttpPost]
        [Route("Api/Credit/CreateTerminationLetters")]
        public ActionResult CreateTerminationLetters(bool? onlyCreateDeliveryFile, bool? onlyCreateTerminationLetters, List<string> terminateTheseCreditNrs, IDictionary<string, string> schedulerData)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
            var useDelayedDocuments = getSchedulerData("useDelayedDocuments") == "true";

            bool createTerminationLetters;
            bool createDeliveryFile;

            if (onlyCreateTerminationLetters.HasValue && onlyCreateDeliveryFile.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "onlyCreateTerminationLetters and onlyCreateDeliveryFile cannot be combined");
            }
            else if (onlyCreateDeliveryFile.HasValue && onlyCreateDeliveryFile.Value == true)
            {
                createTerminationLetters = false;
                createDeliveryFile = true;
            }
            else if (onlyCreateTerminationLetters.HasValue && onlyCreateTerminationLetters.Value == true)
            {
                createTerminationLetters = true;
                createDeliveryFile = false;
            }
            else
            {
                var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";
                createTerminationLetters = true;
                createDeliveryFile = !skipDeliveryExport;
            }

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createterminationletters",
                    () => CreateTerminationLettersI(createTerminationLetters, createDeliveryFile, terminateTheseCreditNrs, useDelayedDocuments, NEnv.ClientCreditType),
                    () => Json2(new { errors = new[] { "Job is already running" } })
            );
        }

        [HttpPost]
        [Route("Api/Credit/TerminationLetterStatus")]
        public ActionResult GetTerminationLetterStatus()
        {
            using (var context = Service.ContextFactory.CreateContext())
            {
                var eligableCount = Service.TerminationLetterCandidateService.GetEligableForTerminationLettersCount(context);
                return Json2(new { eligableCount = eligableCount });
            }
        }

        [HttpPost]
        [Route("Api/Credit/GetTerminationLetterFilesPage")]
        public ActionResult GetFilesPage(int pageSize, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingCreditTerminationLetterDeliveryFileHeaders;

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
                        LettersCount = x.TerminationLetters.Count,
                        UserId = x.ChangedById,
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.LettersCount,
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString()),
                        x.FileArchiveKey,
                        ArchiveDocumentUrl = Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }),
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