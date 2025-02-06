using nCreditReport.Code;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCreditReport.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    public class ApiFindCreditReportsByReasonController : NController
    {
        [HttpPost()]
        [Route("Api/CreditReports/FindByReason")]
        public ActionResult FindByReason(string reasonType, string reasonData, bool? findCompanyReports)
        {
            if (string.IsNullOrWhiteSpace(reasonType) || string.IsNullOrWhiteSpace(reasonData))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing reasonType or reasonData");

            using (var context = new CreditReportContext())
            {
                var query = context
                    .CreditApplicationHeaders
                    .Where(x =>
                        x.SearchTerms.Any(y => y.Name == "reasonType" && y.Value == reasonType)
                        && x.SearchTerms.Any(y => y.Name == "reasonData" && y.Value == reasonData));

                var reports = FindFromQuery(context, query, findCompanyReports.GetValueOrDefault())
                    .OrderByDescending(x => x.Id)
                    .ToList();

                return Json2(new { CreditReports = reports });
            }
        }

        public static List<FindForCustomerCreditReportModel> FindFromQuery(CreditReportContext context, IQueryable<CreditReportHeader> query, bool findCompanyReports)
        {
            var reports = query.Select(x => new
            {
                Id = x.Id,
                RequestDate = x.RequestDate,
                CreditReportProviderName = x.CreditReportProviderName,
                CustomerId = (int)x.CustomerId,
                HasReason = x.SearchTerms.Any(y => y.Name == "reasonType" || y.Name == "reasonData"),
                PreviewItems = x
                    .EncryptedItems
                    .Where(y => y.Name == "htmlReportArchiveKey" || y.Name == "xmlReportArchiveKey" || y.Name == "pdfReportArchiveKey")
                    .Select(y => new
                    {
                        x.EncryptionKeyName,
                        y.Id,
                        y.Name
                    }),
                IsInjectedTestReport = x.SearchTerms.Any(y =>y.Name == "isInjectedTestReport" && y.Value == "true")
            }).ToList();

            var enc = NEnv.EncryptionKeys;
            var creditReportRepository = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());
            var encryptionKeysAndIds = reports.SelectMany(x => x.PreviewItems.Select(y => Tuple.Create(y.EncryptionKeyName, y.Id))).ToList();
            var encryptedItemValueById = creditReportRepository.BulkFetchCreditReports(encryptionKeysAndIds, context);

            var result = new List<FindForCustomerCreditReportModel>();
            foreach (var providerReports in reports.GroupBy(x => x.CreditReportProviderName))
            {
                var providerName = providerReports.Key;
                var provider = findCompanyReports
                    ? (BaseCreditReportService)CompanyProviderFactory.Create(providerName, null)
                    : PersonProviderFactory.Create(providerName);

                foreach (var creditReport in providerReports)
                {
                    var resultReport = new FindForCustomerCreditReportModel
                    {
                        Id = creditReport.Id,
                        CreditReportProviderName = creditReport.CreditReportProviderName,
                        CustomerId = creditReport.CustomerId,
                        RequestDate = creditReport.RequestDate,
                        HasReason = creditReport.HasReason,
                    };
                    foreach (var previewItem in creditReport.PreviewItems)
                    {
                        if (previewItem.Name == "htmlReportArchiveKey")
                            resultReport.HtmlPreviewArchiveKey = encryptedItemValueById.Opt(previewItem.Id);
                        else if (previewItem.Name == "xmlReportArchiveKey")
                            resultReport.RawXmlArchiveKey = encryptedItemValueById.Opt(previewItem.Id);
                        else if (previewItem.Name == "pdfReportArchiveKey")
                            resultReport.PdfPreviewArchiveKey = encryptedItemValueById.Opt(previewItem.Id);
                    }
                    resultReport.HasTableValuesPreview = provider.CanFetchTabledValues() || reports.Any(x => x.Id == resultReport.Id && x.IsInjectedTestReport);
                    result.Add(resultReport);
                }
            }

            return result;
        }
    }

    public class FindForCustomerCreditReportModel
    {
        public int Id { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public string CreditReportProviderName { get; set; }
        public int CustomerId { get; set; }
        public bool HasReason { get; set; }
        public bool HasTableValuesPreview { get; set; }
        public string HtmlPreviewArchiveKey { get; set; }
        public string PdfPreviewArchiveKey { get; set; }
        public string RawXmlArchiveKey { get; set; }
    }
}
