using nCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;
namespace nCredit.WebserviceMethods
{
    public class FetchCreditAnnualStatementExportFilesMethod : TypedWebserviceMethod<FetchCreditAnnualStatementExportFilesMethod.Request, FetchCreditAnnualStatementExportFilesMethod.Response>
    {
        public override string Path => "CreditAnnualStatements/Fetch-Export-Files";

        public override bool IsEnabled => LoanStandardAnnualSummaryService.IsAnnualStatementFeatureEnabled(NEnv.ClientCfgCore, NEnv.EnvSettings);
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("High");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Service();
            var s = resolver.LoanStandardAnnualSummary;
            var result = s.GetExportFile(request.PageSize ?? 50, request.PageNr ?? 0);
            var displayNameService = resolver.UserDisplayName;

            return new Response
            {
                CurrentPageNr = result.CurrentPageNr,
                TotalNrOfPages = result.TotalNrOfPages,
                Page = result.Page.Select(x => new Response.PageModel
                {
                    TransactionDate = x.TransactionDate,
                    StatementCount = x.StatementCount,
                    UserId = x.UserId,
                    FileArchiveKey = x.FileArchiveKey,
                    ArchiveDocumentUrl = NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Api/ArchiveDocument", 
                            Tuple.Create("key", x.FileArchiveKey),
                            Tuple.Create("setFileDownloadName", true.ToString())).ToString(),
                    UserDisplayName = displayNameService.GetUserDisplayNameByUserId(x.UserId.ToString()),
                    ForYear = int.Parse(x.ForYear),
                    ExportResultStatus = x.ExportResultStatus
                }).ToList()
            };
        }

        public class Request
        {
            public int? PageSize { get; set; }
            public int? PageNr { get; set; }
        }

        public class Response
        {
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public List<PageModel> Page { get; set; }

            public class PageModel
            {
                public DateTime TransactionDate { get; set; }
                public int StatementCount { get; set; }
                public int UserId { get; set; }
                public string UserDisplayName { get; set; }
                public string FileArchiveKey { get; set; }
                public string ArchiveDocumentUrl { get; set; }
                public int ForYear { get; set; }
                public string ExportResultStatus { get; set; }
            }
        }
    }
}