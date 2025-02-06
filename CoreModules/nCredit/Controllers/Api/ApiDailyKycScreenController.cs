using nCredit.Code.Email;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace nCredit.Controllers
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

            var kycProvider = this.Service.Kyc;

            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                ISet<int> activeCustomerIds;
                if (NEnv.IsCompanyLoansEnabled)
                    activeCustomerIds = kycProvider.CompanyLoanFetchAllActiveCustomerIds(context);
                else
                    activeCustomerIds = kycProvider.FetchAllActiveCustomerIds(context);

                //Make sure those customers have been screened
                kycProvider.ListScreenBatch(activeCustomerIds, screenDate.Value);

                //Figure out which of these have conflicts and log it
                var result = kycProvider.FetchCustomerKycStatusChanges(activeCustomerIds, screenDate.Value);
                if (result.ConflictedCustomerIds == null)
                    result.ConflictedCustomerIds = new List<int>();

                var h = new DailyKycScreenHeader
                {
                    ChangedDate = Clock.Now,
                    ChangedById = CurrentUserId,
                    InformationMetaData = InformationMetadata,
                    NrOfCustomersScreened = result.TotalScreenedCount,
                    NrOfCustomersConflicted = result.ConflictedCustomerIds.Count,
                    ResultModel = JsonConvert.SerializeObject(new
                    {
                        ScreenDate = screenDate,
                        ActiveCustomerIds = activeCustomerIds,
                        TotalScreenedCount = result.TotalScreenedCount,
                        ConflictedCustomerIds = result.ConflictedCustomerIds
                    }),
                    TransactionDate = Clock.Today
                };
                context.DailyKycScreenHeaders.Add(h);

                context.SaveChanges();

                if (result.ConflictedCustomerIds.Any())
                {
                    SendSummaryEmail(screenDate, context, result);
                }

                return Json2(new
                {
                    Success = true
                });
            }
        }

        private static void SendSummaryEmail(DateTime? screenDate, CreditContext context, Code.Services.FetchCustomerKycStatusChangesResult result)
        {
            var reportEmails = NEnv.KycScreenReportEmails;
            if (reportEmails == null)
                return;

            if (!EmailServiceFactory.HasEmailProvider)
                throw new Exception("Attempting to send kyc email without having an email provider configured. Either add and email provider or remove ntech.kycscreen.reportemail to indicate no emails should be sent");

            var conflictsInfo = context
                .CreditCustomers
                .Where(x => result.ConflictedCustomerIds.Contains(x.CustomerId))
                .GroupBy(x => x.CustomerId)
                .Select(x => new
                {
                    CustomerId = x.Key,
                    CreditNr = x.OrderByDescending(y => y.Timestamp).Select(y => y.CreditNr).FirstOrDefault()
                })
                .ToList();

            var b = new StringBuilder();
            foreach (var c in conflictsInfo)
            {
                b.AppendFormat($"- CreditNr: {c.CreditNr}").AppendLine();
            }

            if (NEnv.IsCompanyLoansEnabled)
            {
                var conflictsInfoBenOwners = context
                    .CreditCustomerListMembers
                    .Where(x => result.ConflictedCustomerIds.Contains(x.CustomerId) && x.ListName == "companyLoanBeneficialOwner")
                    .GroupBy(x => x.CustomerId)
                    .Select(x => new
                    {
                        CustomerId = x.Key,
                        CreditNr = x.OrderByDescending(y => y.Timestamp).Select(y => y.CreditNr).FirstOrDefault()
                    })
                    .ToList();

                foreach (var c in conflictsInfoBenOwners)
                {
                    b.AppendFormat($"- CreditNr: {c.CreditNr}").AppendLine();
                }
            }
            //Send summary email
            var em = EmailServiceFactory.CreateEmailService();
            em.SendTemplateEmail(
                reportEmails,
                "credit-kyc-dailyscreen-hit",
                new Dictionary<string, string>
                {
                            { "screenDate", screenDate.Value.ToString("yyyy-MM-dd") },
                            { "context", b.ToString() }
                },
                "dailyKycScreen");
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
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .DailyKycScreenHeaders
                    .AsQueryable();

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd);
                }

                if (filter != null && filter.ToDate.HasValue)
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
                    Page = currentPage.ToList(),
                    Filter = filter
                });
            }
        }
    }
}