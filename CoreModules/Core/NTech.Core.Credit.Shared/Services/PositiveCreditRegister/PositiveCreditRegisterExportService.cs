using nCredit;
using nCredit.DbModel.Repository;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.ApiClient;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister
{
    public class PositiveCreditRegisterExportService
    {
        private readonly ICoreClock clock;
        private readonly ICreditEnvSettings envSettings;
        private readonly IServiceClientSyncConverter syncConverter;
        private readonly ILoggingService loggingService;
        private readonly PositiveCreditRegisterNewLoansService newLoansService;
        private readonly PositiveCreditRegisterLoanRepaymentsService loanRepaymentsService;
        private readonly PositiveCreditRegisterLoanChangesService loanChangesService;
        private readonly PositiveCreditRegisterDelayedRepaymentsService delayedRepaymentsService;
        private readonly PositiveCreditRegisterTerminatedLoansService terminatedLoansService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly PcrTransformService transformService;

        private PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;
        private readonly Lazy<PcrApiClient> apiClient;

        private PcrApiClient ApiClient => apiClient.Value;

        public PositiveCreditRegisterExportService(ICoreClock clock, CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, IServiceClientSyncConverter syncConverter, IClientConfigurationCore clientConfig,
            ICustomerClient customerClient, PaymentOrderService paymentOrderService, ILoggingService loggingService, INTechEnvironment environment)
        {
            this.clock = clock;
            this.envSettings = envSettings;
            this.syncConverter = syncConverter;
            this.loggingService = loggingService;
            this.creditContextFactory = creditContextFactory;
            this.transformService = new PcrTransformService(customerClient, environment, envSettings);
            this.newLoansService = new PositiveCreditRegisterNewLoansService(creditContextFactory, envSettings, clientConfig, transformService);
            this.loanRepaymentsService = new PositiveCreditRegisterLoanRepaymentsService(clock, creditContextFactory, envSettings, clientConfig, transformService);
            this.loanChangesService = new PositiveCreditRegisterLoanChangesService(creditContextFactory, envSettings, transformService);
            this.delayedRepaymentsService = new PositiveCreditRegisterDelayedRepaymentsService(creditContextFactory, envSettings, clientConfig, paymentOrderService, transformService);
            this.terminatedLoansService = new PositiveCreditRegisterTerminatedLoansService(creditContextFactory, envSettings, clientConfig, transformService);
            this.apiClient = new Lazy<PcrApiClient>(() =>
            {
                var settings = envSettings.PositiveCreditRegisterSettings;
                if (settings.IsMock)
                    return new MockPcrApiClient(envSettings, clock, syncConverter);
                else
                    return new RealPcrApiClient(envSettings, clock, syncConverter);
            });
        }

        public (int SuccessCount, List<string> Warnings) ExportAllBatches(DateTime? fromDate = null, DateTime? toDate = null)
        {
            int successCount = 0;
            var warnings = new List<string>();

            using (var context = creditContextFactory.CreateContext())
            {
                var repo = new CoreSystemItemRepository(context.CurrentUser);

                //Snapshot of full day
                //Default: every day since last run
                List<DateTime> daySnapshots = GetDaysToRun(context, repo, fromDate, toDate);

                foreach (var daySnapshot in daySnapshots)
                {
                    var newLoansExport = newLoansService.GetBatchNewLoans(daySnapshot);
                    var updateLoansExport = loanChangesService.GetBatchLoanChanges(daySnapshot);
                    var repaymentsExport = loanRepaymentsService.GetBatchLoanRepayments(daySnapshot);
                    var delayedRepamentsExport = delayedRepaymentsService.GetBatchDelayedRepayments(daySnapshot);
                    var terminatedLoansExport = terminatedLoansService.GetBatchTerminatedLoans(daySnapshot);

                    if (newLoansExport.Loans.Any())
                    {
                        var exportNewLoans = ApiClient.SendBatch(newLoansExport, BatchType.NewLoans, Settings.AddLoansEndpointUrl, context, repo);

                        if (exportNewLoans.responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += newLoansExport.Loans.Count;
                            warnings.AddRange(exportNewLoans.Warnings);
                        }
                    }

                    if (updateLoansExport.Loans.Any())
                    {
                        var exportUpdateLoans = ApiClient.SendBatch(updateLoansExport, BatchType.LoanChanges, Settings.ChangeLoansEndpointUrl, context, repo);

                        if (exportUpdateLoans.responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += updateLoansExport.Loans.Count;
                            warnings.AddRange(exportUpdateLoans.Warnings);
                        }
                    }

                    if (repaymentsExport.Repayments.Any())
                    {
                        var exportRepayments = ApiClient.SendBatch(repaymentsExport, BatchType.LoanRepayments, Settings.RepaymentsEndpointUrl, context, repo);

                        if (exportRepayments.responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += repaymentsExport.Repayments.Count;
                            warnings.AddRange(exportRepayments.Warnings);
                        }
                    }

                    if (delayedRepamentsExport.DelayedRepayments.Any())
                    {
                        var exportDelayedRepayments = ApiClient.SendBatch(delayedRepamentsExport, BatchType.DelayedPayments, Settings.DelayedRepaymentsEndpointUrl, context, repo);

                        if (exportDelayedRepayments.responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += delayedRepamentsExport.DelayedRepayments.Count;
                            warnings.AddRange(exportDelayedRepayments.Warnings);
                        }
                    }

                    if (terminatedLoansExport.LoanTerminations.Any())
                    {
                        var exportTerminatedLoans = ApiClient.SendBatch(terminatedLoansExport, BatchType.LoanTerminations, Settings.TerminatedLoansEndpointUrl, context, repo);

                        if (exportTerminatedLoans.responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += terminatedLoansExport.LoanTerminations.Count;
                            warnings.AddRange(exportTerminatedLoans.Warnings);
                        }
                    }
                }

                SetExportRunTimeStamp(context, repo);

                context.SaveChanges();

                return (successCount, warnings);
            }
        }

        private List<DateTime> GetDaysToRun(ICreditContextExtended context, CoreSystemItemRepository repo, DateTime? fromDate = null, DateTime? toDate = null)
        {
            //Returns a list of days to run
            //With request date params or default
            //Default=every day since from last export run to yesterday
            List<DateTime> GenerateDateList(DateTime startDate, DateTime endDate)
            {
                List<DateTime> dateList = new List<DateTime>();
                while (startDate <= endDate)
                {
                    dateList.Add(startDate);
                    startDate = startDate.AddDays(1);
                }
                return dateList;
            }

            if (fromDate.HasValue && toDate.HasValue)
            {
                if (fromDate >= clock.Today || toDate >= clock.Today)
                {
                    throw new Exception();
                }

                return GenerateDateList(fromDate.Value.Date, toDate.Value);
            }
            else
            {
                var lastExportRunOrDefault = GetLastExportRunTimeStamp(context, repo);
                return GenerateDateList(lastExportRunOrDefault.Date, clock.Today.AddDays(-1));
            }
        }


        public (int SuccessCount, List<string> Warnings) GetAllBatchStatusReportingFailedBatches(INTechEmailServiceFactory emailServiceFactory)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var repo = new CoreSystemItemRepository(context.CurrentUser);

                int successCount = 0;
                var warnings = new List<string>();

                var failedBatches = new List<(string BatchReference, BatchType Type, string BatchStatusCode)>();

                foreach (var type in new List<BatchType> { BatchType.NewLoans, BatchType.LoanChanges, BatchType.LoanRepayments, BatchType.DelayedPayments, BatchType.LoanTerminations })
                {
                    string batchReference = ApiClient.GetBatchReference(type, context, repo);

                    if (batchReference == null)
                    {
                        continue;
                    }

                    var result = ApiClient.CheckBatchStatus(batchReference, context, repo);
                    successCount += 1;
                    warnings.AddRange(result.Warnings);
                    if (!result.BatchStatus.IsFinishedSuccess)
                    {
                        failedBatches.Add((BatchReference: batchReference, Type: type, BatchStatusCode: result.BatchStatus.BatchStatusCode));
                    }
                }


                if (failedBatches.Count > 0)
                {
                    foreach (var failedBatch in failedBatches)
                        warnings.Add($"Batch {failedBatch.BatchStatusCode} failed with code {failedBatch.BatchStatusCode}");
                }

                context.SaveChanges();

                SendFailedBatchesEmail(failedBatches, emailServiceFactory);

                return (successCount, warnings);
            }
        }

        private void SendFailedBatchesEmail(List<(string BatchReference, BatchType Type, string BatchStatusCode)> failedBatches, INTechEmailServiceFactory emailServiceFactory)
        {
            if (failedBatches.Count == 0)
                return;

            if (Settings.BatchFailedReportEmail == null || !emailServiceFactory.HasEmailProvider)
                return;

            try
            {
                var emailService = emailServiceFactory.CreateEmailService();
                const string MessageSubjectTemplate = "PCR batches failed {{FailedDate}}";
                const string MessageBodyTemplate =
    @"**PCR Batches failed {{FailedDate}}**


{{#FailedBatches}}
- Batch {{BatchReference}} of type {{BatchType}} failed with code {{BatchStatusCode}}
{{/FailedBatches}}

";

                emailService.SendRawEmail(new List<string> { Settings.BatchFailedReportEmail }, MessageSubjectTemplate, MessageBodyTemplate,
                    new Dictionary<string, object>
                    {
                        ["FailedDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
                        ["FailedBatches"] = failedBatches.Select(x => new { x.BatchReference, BatchType = x.Type.ToString(), x.BatchStatusCode }).ToList()
                    }, "PCR Check status");
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, "PCR failed batches email failed");
            }
        }

        private void SetExportRunTimeStamp(ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            byte[] timestamp = BitConverter.GetBytes(clock.Now.DateTime.ToBinary());
            repo.SetTimestamp(SystemItemCode.PositiveCreditRegisterExport_ExportRun, timestamp, context);
        }

        private DateTime GetLastExportRunTimeStamp(ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            var timestampBytes = repo.GetTimestamp(SystemItemCode.PositiveCreditRegisterExport_ExportRun, context);

            if (timestampBytes != null && timestampBytes.Length == 8)
            {
                long timestampValue = BitConverter.ToInt64(timestampBytes, 0);
                return DateTime.FromBinary(timestampValue);
            }
            else
            {
                // Handle the first run
                return clock.Today.AddDays(-1);
            }
        }

        public (List<string> ExportLogs, List<string> StatusLogs) GetSystemLogs(int nrOfLogs)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var repo = new CoreSystemItemRepository(context.CurrentUser);

                var statusLogs = repo.GetWithTakeN(SystemItemCode.PositiveCreditRegisterExport_CheckBatchStatus, context, nrOfLogs).ToList();
                var exportLogs = new[]
                    {
                        SystemItemCode.PositiveCreditRegisterExport_NewLoans,
                        SystemItemCode.PositiveCreditRegisterExport_LoanChanges,
                        SystemItemCode.PositiveCreditRegisterExport_LoanRepayments,
                        SystemItemCode.PositiveCreditRegisterExport_DelayedRepayments,
                        SystemItemCode.PositiveCreditRegisterExport_TerminatedLoans,
                    }
                    .SelectMany(code => repo.GetWithTakeN(code, context, nrOfLogs))
                    .Take(nrOfLogs)
                    .ToList();

                return (exportLogs, statusLogs);
            }
        }

        public string FetchRawGetLoanResponse(string creditNr) => ApiClient.FetchRawGetLoanResponse(transformService.TransformLoanNr(creditNr));

        public (int SuccessCount, string RawResponse) ExportFirstTime(DateTime fromDate, DateTime toDate)
        {
            int successCount = 0;
            string rawResponse = "";

            using (var context = creditContextFactory.CreateContext())
            {
                var repo = new CoreSystemItemRepository(context.CurrentUser);

                //Trying this to see if it works as expected
                //TODO: cleanup and comments to understand this setting..
                if (Settings.ForceFirstTimeExportToTriggerLoanChanges)
                {
                    var loanChangesReport = loanChangesService.GetCorrectionBatchWithCurrentData(toDate); 

                    if (loanChangesReport.Loans.Any())
                    {
                        var (responseMessage, Warnings) = ApiClient.SendBatch(loanChangesReport, BatchType.LoanChanges, Settings.ChangeLoansEndpointUrl, context, repo);

                        if (responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += loanChangesReport.Loans.Count;
                        }

                        rawResponse += $"IsSuccess: {responseMessage.IsSuccessStatusCode}, StatusCode: {responseMessage.StatusCode}. Content: ";
                        rawResponse += syncConverter.ToSync(() => responseMessage.Content.ReadAsStringAsync());

                    }

                    SetExportRunTimeStamp(context, repo);
                    context.SaveChanges();
                    return (successCount, rawResponse);
                }

                else 
                {
                    var newLoansExport = newLoansService.GetBatchNewLoans(null, isFirstTimeExport: true, fromDate, toDate);

                    if (newLoansExport.Loans.Any())
                    {
                        var (responseMessage, Warnings) = ApiClient.SendBatch(newLoansExport, BatchType.NewLoans, Settings.AddLoansEndpointUrl, context, repo);

                        if (responseMessage.StatusCode == HttpStatusCode.Accepted)
                        {
                            successCount += newLoansExport.Loans.Count;
                        }

                        rawResponse += $"IsSuccess: {responseMessage.IsSuccessStatusCode}, StatusCode: {responseMessage.StatusCode}. Content: ";
                        rawResponse += syncConverter.ToSync(() => responseMessage.Content.ReadAsStringAsync());

                    }

                    SetExportRunTimeStamp(context, repo);
                    context.SaveChanges();
                    return (successCount, rawResponse);
                }
            }
        }

        public static Action<HttpRequestMessage> ObserveSendBatch { get; set; } = null;

        public enum BatchType
        {
            NewLoans,
            LoanChanges,
            LoanRepayments,
            DelayedPayments,
            LoanTerminations,
            CheckBatchStatus
        }
    }
}
