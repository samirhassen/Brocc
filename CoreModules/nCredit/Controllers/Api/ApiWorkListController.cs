using nCredit.DbModel.Repository;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [RoutePrefix("Api/WorkList")]
    [NTechApi]
    public class ApiWorkListController : NController
    {
        private WorkListRepository CreateRepo()
        {
            return new WorkListRepository(this.CurrentUserId, this.Clock, this.InformationMetadata, Service.DocumentClientHttpContext);
        }

        [HttpPost]
        [Route("TryTakeWorkListItem")]
        public ActionResult TryTakeWorkListItem(int workListHeaderId, int userId)
        {
            if (NEnv.IsProduction && userId != this.CurrentUserId)
                throw new Exception("Can only impersonate other users in test");

            var repo = CreateRepo();
            bool isConcurrencyProblem = false;
            Action onConcurrencyIssue = () => isConcurrencyProblem = true;

            var takenId = repo.TryTakeWorkListItem(workListHeaderId, userId, onConcurrencyIssue);

            return Json2(new
            {
                wasItemTaken = takenId != null,
                takenItemId = takenId,
                isConcurrencyProblem,
            });
        }

        [HttpPost]
        [Route("TryReplaceWorkListItem")]
        public ActionResult TryReplaceWorkListItem(int workListHeaderId, string itemId)
        {
            var repo = CreateRepo();
            var wasReplaced = repo.TryReplaceWorkListItem(workListHeaderId, itemId);

            return Json2(new
            {
                wasReplaced
            });
        }

        [HttpPost]
        [Route("TryCompleteWorkListItem")]
        public ActionResult TryCompleteWorkListItem(int workListHeaderId, string itemId)
        {
            var repo = CreateRepo();
            var wasCompleted = repo.TryCompleteWorkListItem(workListHeaderId, itemId);

            return Json2(new
            {
                wasCompleted
            });
        }

        [NTechAuthorizeCreditHigh]
        [HttpGet]
        [Route("WorkListInitialStateAsXlsx")]
        public ActionResult GetWorkListInitialStateAsXlsx(int workListHeaderId)
        {
            try
            {
                var repo = CreateRepo();
                var result = repo.CreateWorkListInitialStateAsXlsx(workListHeaderId);
                return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"WorkListContent_{workListHeaderId}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create worklist content report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [NTechAuthorizeCreditHigh]
        [HttpGet]
        [Route("WorkListResultAsXlsx")]
        public ActionResult GetWorkListResultAsXlsx(int workListHeaderId)
        {
            try
            {
                var repo = CreateRepo();
                var result = repo.CreateWorkListResultAsXlsx(workListHeaderId, this.GetUserDisplayNameByUserId);
                return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"WorkListResult_{workListHeaderId}.xlsx" };
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create worklist result report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        public class HistoricalWorkListsFilter
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string WorkListTypeName { get; set; }
            public bool OnlyClosed { get; set; }
        }

        [NTechAuthorizeCreditHigh]
        [HttpPost]
        [Route("GetHistoricalWorkListsPage")]
        public ActionResult GetHistoricalWorkListsPage(int pageSize, HistoricalWorkListsFilter filter = null, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .WorkListHeaders
                    .Where(x => !x.IsUnderConstruction);

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.CreationDate >= fd);
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date.Date.AddDays(1);
                    baseResult = baseResult.Where(x => x.CreationDate < td);
                }

                if (filter != null && !string.IsNullOrWhiteSpace(filter.WorkListTypeName))
                {
                    baseResult = baseResult.Where(x => x.ListType == filter.WorkListTypeName);
                }

                if (filter != null && filter.OnlyClosed)
                {
                    baseResult = baseResult.Where(x => x.ClosedByUserId.HasValue);
                }

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Id)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.CreationDate,
                        x.ClosedDate,
                        x.CreatedByUserId
                    })
                    .ToList()
                    .Select(x => new
                    {
                        WorkListHeaderId = x.Id,
                        CreatedDate = x.CreationDate,
                        ClosedDate = x.ClosedDate,
                        UserId = x.CreatedByUserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.CreatedByUserId.ToString()),
                        SelectionUrl = Url.Action("GetWorkListInitialStateAsXlsx", new { workListHeaderId = x.Id }),
                        ResultUrl = Url.Action("GetWorkListResultAsXlsx", new { workListHeaderId = x.Id })
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList(),
                    Filter = filter
                });
            }
        }
    }
}