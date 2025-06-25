using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using nSavings.DbModel;
using NTech;
using NTech.Core.Savings.Shared.DbModel;
using Serilog;

namespace nSavings.Code.Services;

public class FatcaExportService(IClock clock, Func<string, string> getUserDisplayNameByUserId, UrlHelper urlHelper)
    : IFatcaExportService
{
    public const string FileType = "FatcaExport";

    public void CreateFatcaFileToStream(DateTime forYearDate, Stream target, Action<int> observeNrOfAccounts = null)
    {
        var c = new CustomerClient();

        var customerIds = c.FetchFatcaCustomerIds();
        var startOfReportingYearDate = new DateTime(forYearDate.Date.Year, 1, 1);
        var reportingDate = new DateTime(forYearDate.Date.Year, 12, 31);

        var fatcaRequest = new CustomerClient.CreateFatcaExportFileRequest
        {
            ReportingDate = new DateTime(forYearDate.Date.Year, 12, 31),
            ExportDate = clock.Now.DateTime,
            Accounts = []
        };

        using (var context = new SavingsContext())
        {
            fatcaRequest.Accounts = context
                .SavingsAccountHeaders
                .Where(x => customerIds.Contains(x.MainCustomerId) &&
                            x.CreatedByEvent.TransactionDate <= reportingDate)
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.MainCustomerId,
                    StatusItem = x.DatedStrings.Where(y => y.TransactionDate <= reportingDate)
                        .OrderByDescending(y => y.BusinessEventId).Select(y => new { y.Value, y.TransactionDate })
                        .FirstOrDefault(),
                    EndOfYearBalance =
                        x.Transactions
                            .Where(y => y.AccountCode == nameof(LedgerAccountTypeCode.Capital) &&
                                        y.TransactionDate <= reportingDate).Sum(y => (decimal?)y.Amount) ?? 0m,
                    CapitalizationsDuringYear = x
                        .SavingsAccountInterestCapitalizations
                        .Where(y => y.ToDate >= startOfReportingYearDate && y.ToDate <= reportingDate)
                        .Select(y => new
                        {
                            IsCloseEvent =
                                y.CreatedByEvent.EventType == nameof(BusinessEventType.AccountClosure),
                            InterestAmount = (x
                                .Transactions
                                .Where(z => z.BusinessEventId == y.CreatedByBusinessEventId &&
                                            z.AccountCode == nameof(LedgerAccountTypeCode.CapitalizedInterest))
                                .Sum(z => (decimal?)z.Amount) ?? 0m),
                            CapitalAmount = (x
                                .Transactions
                                .Where(z => z.BusinessEventId == y.CreatedByBusinessEventId &&
                                            z.AccountCode == nameof(LedgerAccountTypeCode.Capital))
                                .Sum(z => (decimal?)z.Amount) ?? 0m),
                            WithheldCapitalizedInterestTax = (x
                                .Transactions
                                .Where(z => z.BusinessEventId == y.CreatedByBusinessEventId && z.AccountCode ==
                                    nameof(LedgerAccountTypeCode.WithheldCapitalizedInterestTax))
                                .Sum(z => (decimal?)z.Amount) ?? 0m)
                        }),
                })
                .ToList()
                .Select(x =>
                {
                    if (x.StatusItem.Value == "Closed" && x.StatusItem.TransactionDate < startOfReportingYearDate)
                        return null;

                    decimal accountBalance;
                    decimal accountInterest;

                    if (x.CapitalizationsDuringYear.Any(y => y.IsCloseEvent))
                    {
                        var closeEvent = x.CapitalizationsDuringYear.First(y => y.IsCloseEvent);
                        accountBalance = -closeEvent.CapitalAmount;
                        accountInterest = closeEvent.InterestAmount;
                    }
                    else
                    {
                        accountBalance = x.EndOfYearBalance;
                        accountInterest = x.CapitalizationsDuringYear.Sum(y => y.InterestAmount);
                    }

                    return new CustomerClient.FatcaExportFileRequestAccount
                    {
                        CustomerId = x.MainCustomerId,
                        IsClosed = x.StatusItem?.Value == "Closed",
                        AccountNumber = x.SavingsAccountNr,
                        AccountBalance = accountBalance,
                        AccountInterest = accountInterest
                    };
                })
                .Where(x => x != null)
                .ToList();
        }

        observeNrOfAccounts?.Invoke(fatcaRequest.Accounts.Count);

        c.CreateFatcaExportFile(fatcaRequest, target);
    }

    public OutgoingExportFileHeader CreateAndStoreAndExportFatcaExportFile(DateTime forYearDate,
        string exportProfileName, int userId, string informationMetadata,
        Action<OutgoingExportFileHeader.StandardExportResultStatusModel> observeExportResult = null)
    {
        var ms = new MemoryStream();
        var nrOfAccounts = 0;
        CreateFatcaFileToStream(forYearDate, ms, observeNrOfAccounts: x => nrOfAccounts = x);
        ms.Position = 0;

        var documentClient = new DocumentClient();
        var archiveKey =
            documentClient.ArchiveStore(ms.ToArray(), "application/xml", $"Fatca-{forYearDate.Year}.xml");

        using var context = new SavingsContext();
        var f = new OutgoingExportFileHeader
        {
            TransactionDate = clock.Today,
            FileArchiveKey = archiveKey,
            ChangedById = userId,
            InformationMetaData = informationMetadata,
            ChangedDate = clock.Now,
            FileType = FileType,
            CustomData = JsonConvert.SerializeObject(new FatcaExportCustomDataModel
            {
                ForYearDate = new DateTime(forYearDate.Year, 12, 31),
                NrOfAccounts = nrOfAccounts
            }),
            ExportResultStatus = null
        };
        context.OutgoingExportFileHeaders.Add(f);

        context.SaveChanges();

        var exportResult = Export(archiveKey, exportProfileName);

        f.ExportResultStatus = JsonConvert.SerializeObject(exportResult);

        observeExportResult?.Invoke(exportResult);

        context.SaveChanges();

        return f;
    }

    private class FatcaExportCustomDataModel
    {
        public DateTime ForYearDate { get; set; }
        public int NrOfAccounts { get; set; }
    }

    public List<FatcaExportFileModel> GetFatcaExportFiles(Tuple<int, int> pageSizeAndNr = null)
    {
        using var context = new SavingsContext();
        var pre = context
            .OutgoingExportFileHeaders
            .Where(x => x.FileType == FileType)
            .OrderByDescending(x => x.Id)
            .AsQueryable();
        if (pageSizeAndNr != null)
        {
            pre = pre.Skip(pageSizeAndNr.Item1 * pageSizeAndNr.Item2).Take(pageSizeAndNr.Item1);
        }

        var result = pre.Select(x => new
            {
                x.Id,
                x.FileArchiveKey,
                x.ChangedById,
                x.TransactionDate,
                x.ExportResultStatus,
                x.CustomData
            })
            .ToList()
            .Select(x =>
            {
                var customData = JsonConvert.DeserializeObject<FatcaExportCustomDataModel>(x.CustomData);
                return new FatcaExportFileModel
                {
                    Id = x.Id,
                    TransactionDate = x.TransactionDate,
                    ArchiveKey = x.FileArchiveKey,
                    ExportResult = x.ExportResultStatus == null
                        ? null
                        : JsonConvert
                            .DeserializeObject<OutgoingExportFileHeader.StandardExportResultStatusModel>(
                                x.ExportResultStatus),
                    ForYearDate = customData.ForYearDate,
                    NrOfAccounts = customData.NrOfAccounts,
                    UserId = x.ChangedById,
                    UserDisplayName = getUserDisplayNameByUserId(x.ChangedById.ToString()),
                    ArchiveDocumentUrl = x.FileArchiveKey == null
                        ? null
                        : urlHelper.Action("ArchiveDocument", "ApiArchiveDocument",
                            new { key = x.FileArchiveKey, setFileDownloadName = true })
                };
            })
            .ToList();

        return result;
    }

    private static OutgoingExportFileHeader.StandardExportResultStatusModel Export(string archiveKey,
        string exportProfileName)
    {
        var r = new OutgoingExportFileHeader.StandardExportResultStatusModel
        {
            Status = nameof(OutgoingExportFileHeader.StandardExportResultStatusModel.StatusCode.NoExportProfile),
            FailedProfileNames = [],
            SuccessProfileNames = [],
            Errors = [],
            Warnings = []
        };

        if (string.IsNullOrWhiteSpace(exportProfileName))
            return r;

        var documentClient = new DocumentClient();
        try
        {
            var isSuccess = documentClient.TryExportArchiveFile(archiveKey, exportProfileName,
                out var successProfileNames, out var failedProfileNames, out var timeInMs);

            if (failedProfileNames is { Count: > 0 })
                r.Warnings.Add("Failed profiles: " + string.Join(", ", failedProfileNames));

            r.SuccessProfileNames = successProfileNames;
            r.FailedProfileNames = failedProfileNames;
            r.TimeInMs = timeInMs;
        }
        catch (Exception ex)
        {
            NLog.Error(ex, $"Export profile '{exportProfileName}' crashed");
            r.Errors.Add("Export profile '{exportProfileName}' crashed: " + ex.Message);
        }

        r.Status = r.Errors.Any()
            ? nameof(OutgoingExportFileHeader.StandardExportResultStatusModel.StatusCode.Error)
            : ((r.Warnings.Any() || r.FailedProfileNames.Any())
                ? nameof(OutgoingExportFileHeader.StandardExportResultStatusModel.StatusCode.Warning)
                : nameof(OutgoingExportFileHeader.StandardExportResultStatusModel.StatusCode.Ok));

        return r;
    }
}

public interface IFatcaExportService
{
    void CreateFatcaFileToStream(DateTime forYearDate, Stream target, Action<int> observeNrOfAccounts = null);

    OutgoingExportFileHeader CreateAndStoreAndExportFatcaExportFile(DateTime forYearDate, string exportProfileName,
        int userId, string informationMetadata,
        Action<OutgoingExportFileHeader.StandardExportResultStatusModel> observeExportResult = null);

    List<FatcaExportFileModel> GetFatcaExportFiles(Tuple<int, int> pageSizeAndNr = null);
}

public class FatcaExportFileModel
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime ForYearDate { get; set; }
    public int NrOfAccounts { get; set; }
    public string ArchiveKey { get; set; }
    public string ArchiveDocumentUrl { get; set; }
    public int UserId { get; set; }
    public string UserDisplayName { get; set; }
    public OutgoingExportFileHeader.StandardExportResultStatusModel ExportResult { get; set; }
}