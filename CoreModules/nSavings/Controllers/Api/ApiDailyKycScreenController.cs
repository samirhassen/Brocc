using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Code.Email;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiDailyKycScreenController : NController
    {
        [Route("Api/Kyc/ScreenCustomers")]
        [HttpPost]
        public ActionResult ScreenCustomers(DateTime? screenDate)
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.kycbatchscreening"))
                return HttpNotFound();

            if (!screenDate.HasValue)
                screenDate = Clock.Today;

            using (var context = new DbModel.SavingsContext())
            {
                var activeCustomerIds = FetchAllActiveCustomerIds(context);

                //Figure out which customers need screening
                var activeResult = GetActiveCustomerIdsAndAlreadyScreenedCount(context, screenDate.Value, activeCustomerIds: activeCustomerIds);
                var customerIdsToScreen = activeResult.ActiveCustomerIds;

                //Make sure those customers have been screened
                var customerClient = new CustomerClient();
                var screenResult = customerClient.ListScreenBatch(customerIdsToScreen, screenDate.Value);

                //Figure out which of these have conflicts and log it
                var result = customerClient.FetchCustomerKycStatusChanges(customerIdsToScreen, screenDate.Value);
                if (result.CustomerIdsWithChanges == null)
                    result.CustomerIdsWithChanges = new List<int>();

                var h = new DailyKycScreenHeader
                {
                    ChangedDate = Clock.Now,
                    ChangedById = CurrentUserId,
                    InformationMetaData = InformationMetadata,
                    NrOfCustomersScreened = result.TotalScreenedCount,
                    NrOfCustomersConflicted = result.CustomerIdsWithChanges.Count,
                    ResultModel = JsonConvert.SerializeObject(new
                    {
                        ScreenDate = screenDate,
                        ActiveCustomerIds = activeCustomerIds,
                        TotalScreenedCount = result.TotalScreenedCount,
                        ConflictedCustomerIds = result.CustomerIdsWithChanges.Count
                    }),
                    TransactionDate = Clock.Today
                };
                context.DailyKycScreenHeaders.Add(h);

                context.SaveChanges();

                if (result.CustomerIdsWithChanges.Any())
                {
                    SendSummaryEmail(screenDate, context, result);
                }

                return Json2(new
                {
                    Success = true
                });
            }
        }

        private static void SendSummaryEmail(DateTime? screenDate, DbModel.SavingsContext context, CustomerClient.FetchCustomerKycStatusChangesResult result)
        {
            var reportEmails = NEnv.KycScreenReportEmails;
            if (reportEmails == null)
                return;

            if (!EmailServiceFactory.HasEmailProvider)
                throw new Exception("Attempting to send kyc email without having an email provider configured. Either add and email provider or remove ntech.kycscreen.reportemail to indicate no emails should be sent");

            var conflictsInfo = context
                .SavingsAccountHeaders
                .Where(x => result.CustomerIdsWithChanges.Contains(x.MainCustomerId))
                .GroupBy(x => x.MainCustomerId)
                .Select(x => new
                {
                    CustomerId = x.Key,
                    SavingsAccountNr = x.OrderByDescending(y => y.CreatedByBusinessEventId).Select(y => y.SavingsAccountNr).FirstOrDefault()
                })
                .ToList();

            var b = new StringBuilder();
            foreach (var c in conflictsInfo)
            {
                b.AppendFormat($"- SavingsAccountNr: {c.SavingsAccountNr}").AppendLine();
            }

            //Send summary email
            var em = EmailServiceFactory.CreateEmailService();
            em.SendTemplateEmail(
                reportEmails,
                "savings-kyc-dailyscreen-hit",
                new Dictionary<string, string>
                {
                            { "screenDate", screenDate.Value.ToString("yyyy-MM-dd") },
                            { "context", b.ToString() }
                },
                "dailyKycScreenSavings");
        }

        private static ISet<int> FetchAllActiveCustomerIds(DbModel.SavingsContext context)
        {
            return new HashSet<int>(context
                .SavingsAccountHeaders
                .Where(x => x.Status == SavingsAccountStatusCode.Active.ToString())
                .Select(x => x.MainCustomerId)
                .ToList());
        }

        public static (ISet<int> ActiveCustomerIds, int ScreenedTodayCount) GetActiveCustomerIdsAndAlreadyScreenedCount(DbModel.SavingsContext context, DateTime screenDate, ISet<int> activeCustomerIds = null)
        {
            var customerIds = activeCustomerIds ?? FetchAllActiveCustomerIds(context);

            var customerClient = new CustomerClient();
            var statusChanges = customerClient.FetchCustomerKycStatusChanges(customerIds.ToHashSet(), screenDate);

            return (ActiveCustomerIds: customerIds, ScreenedTodayCount: statusChanges.TotalScreenedCount);
        }

        public class Filter
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }

        [HttpPost]
        [Route("Api/Kyc/GetFilesPage")]
        public ActionResult GetFilesPage(int pageSize, Filter filter = null, int pageNr = 0)
        {
            using (var context = new DbModel.SavingsContext())
            {
                var baseResult = context
                    .DailyKycScreenHeaders
                    .AsQueryable();

                if (filter?.FromDate != null)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd);
                }

                if (filter?.ToDate != null)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td);
                }

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.NrOfCustomersScreened,
                        x.NrOfCustomersConflicted,
                        UserId = x.ChangedById
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.NrOfCustomersScreened,
                        x.NrOfCustomersConflicted,
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString())
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