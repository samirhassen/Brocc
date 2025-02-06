using nCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace nCredit.WebserviceMethods
{
    public class CreateCreditAnnualStatementsMethod : TypedWebserviceMethod<CreateCreditAnnualStatementsMethod.Request, CreateCreditAnnualStatementsMethod.Response>
    {
        public override string Path => "CreditAnnualStatements/Create-Yearly-Report";

        public override bool IsEnabled => LoanStandardAnnualSummaryService.IsAnnualStatementFeatureEnabled(NEnv.ClientCfgCore, NEnv.EnvSettings);
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("High");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var w = Stopwatch.StartNew();
            try
            {
                var clock = requestContext.Clock();
                var forYear = request.ForYear ?? (clock.Today.Year - 1);
                var s = requestContext.Service().LoanStandardAnnualSummary;

                var exportProfileName = string.IsNullOrWhiteSpace(request.ExportProfileName) ? NEnv.AnnualStatementsExportProfileName : request.ExportProfileName?.Trim();
                return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createannualstatements",
                  () =>
                  {
                      var exportFile = s.CreateAndPossiblyExportAnnualStatementsForYear(forYear, exportProfileName);
                      return new Response
                      {
                          ForYear = forYear,
                          TotalMilliseconds = w.ElapsedMilliseconds,
                          ExportFileArchiveKey = exportFile.FileArchiveKey
                      };
                  },
                  () => Error("Job is already running", httpStatusCode: 400, errorCode: "jobIsAlreadyRunning"));
            }
            catch (NTechWebserviceMethodException)
            {
                throw;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreditAnnualStatements/Create-Yearly-Report crashed");
                return new Response
                {
                    Errors = new List<string> { "CreditAnnualStatements/Create-Yearly-Report crashed: " + ex.Message },
                    TotalMilliseconds = w.ElapsedMilliseconds
                };
            }
            finally
            {
                w.Stop();
            }
        }

        public class Request
        {
            public int? ForYear { get; set; }

            public string ExportProfileName { get; set; }

            public Dictionary<string, string> SchedulerData { get; set; }
        }

        public class Response
        {
            public int ForYear { get; set; }
            public List<string> Errors { get; set; }
            public long TotalMilliseconds { get; set; }
            public List<string> Warnings { get; set; }
            public string ExportFileArchiveKey { get; set; }
        }
    }
}