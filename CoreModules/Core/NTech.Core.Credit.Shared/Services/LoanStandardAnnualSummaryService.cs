using Dapper;
using nCredit.DbModel.Model;
using nCredit.DomainModel;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.Code.Services
{
    public class LoanStandardAnnualSummaryService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IDocumentClient documentClient;
        private readonly ICustomerClient customerClient;
        private readonly Func<string, byte[]> loadPdfTemplate;
        private readonly ICreditEnvSettings envSettings;
        private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var s = new JsonSerializerSettings();
            s.NullValueHandling = NullValueHandling.Ignore;
            return s;
        });

        public LoanStandardAnnualSummaryService(CreditContextFactory creditContextFactory, IClientConfigurationCore clientConfiguration, IDocumentClient documentClient,
            ICustomerClient customerClient, Func<string, byte[]> loadPdfTemplate, ICreditEnvSettings envSettings)
        {
            this.creditContextFactory = creditContextFactory;
            this.clientConfiguration = clientConfiguration;
            this.documentClient = documentClient;
            this.customerClient = customerClient;
            this.loadPdfTemplate = loadPdfTemplate;
            this.envSettings = envSettings;
        }

        public static bool IsAnnualStatementFeatureEnabled(IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings) => clientConfiguration.Country.BaseCountry == "SE" && (envSettings.IsStandardUnsecuredLoansEnabled || envSettings.IsStandardMortgageLoansEnabled);

        public bool IsExportCreatedForYear(int year)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var yearString = year.ToString();
                return context.OutgoingExportFileHeaderQueryable.Any(x => x.FileType == CreditAnnualStatementHeader.ExportFileType && x.CustomData == yearString);
            }
        }

        public OutgoingExportFileHeader CreateAndPossiblyExportAnnualStatementsForYear(int year, string exportProfileName)
        {
            if (!IsAnnualStatementFeatureEnabled(clientConfiguration, envSettings))
                throw new Exception("Annual statements not enabled");

            EnsureCreditAnnualStatementHeadersForYear(year);

            using (var context = creditContextFactory.CreateContext())
            using (var tempDirectory = new TemporaryDirectory())
            {
                var statements = context
                    .CreditAnnualStatementHeadersQueryable
                    .Where(x => x.Year == year && !x.OutgoingExportFileHeaderId.HasValue)
                    .ToList()
                    .Select(x =>
                    (
                        Statement: x,
                        Custom: JsonConvert.DeserializeObject<AnnualStatementCustomDataModel>(x.CustomData)
                    )).ToList();

                var exportZipFile = CreateExportZipFile(year, statements, tempDirectory);

                var archiveKey = documentClient.ArchiveStoreFile(exportZipFile, "application/zip", exportZipFile.Name);

                var exportFile = context.FillInfrastructureFields(new OutgoingExportFileHeader
                {
                    FileArchiveKey = archiveKey,
                    ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                    {
                        status = "NotExported"
                    }),
                    FileType = CreditAnnualStatementHeader.ExportFileType,
                    CustomData = year.ToString(),
                    TransactionDate = context.CoreClock.Today
                });
                foreach (var statement in statements)
                {
                    statement.Statement.OutgoingExportFile = exportFile;
                }

                context.SaveChanges();

                if (!string.IsNullOrWhiteSpace(exportProfileName))
                {
                    var exportResult = documentClient.TryExportArchiveFile(exportFile.FileArchiveKey, exportProfileName);
                    exportFile.ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                    {
                        status = exportResult.IsSuccess ? "Success" : "Warning",
                        deliveryTimeInMs = exportResult.TimeInMs,
                        deliveredToProfileName = exportResult.SuccessProfileNames?.First(),
                        deliveredToProfileNames = exportResult.SuccessProfileNames,
                        failedProfileNames = exportResult.FailedProfileNames
                    }, Formatting.None, NullIgnoringJsonSerializerSettings.Value);
                    context.SaveChanges();
                }

                return exportFile;
            }
        }

        public (int CurrentPageNr, int TotalNrOfPages, List<(DateTime TransactionDate, string FileArchiveKey, int StatementCount, int UserId, string ForYear, string ExportResultStatus)> Page) GetExportFile(int pageSize, int pageNr)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var baseResult = context
                    .OutgoingExportFileHeaderQueryable
                    .Where(x => x.FileType == CreditAnnualStatementHeader.ExportFileType);

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Id)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x =>
                    (
                        TransactionDate: x.TransactionDate,
                        FileArchiveKey: x.FileArchiveKey,
                        StatementCount: x.AnnualStatements.Count(),
                        UserId: x.ChangedById,
                        ForYear: x.CustomData,
                        ExportResultStatus: x.ExportResultStatus
                    ))
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return (CurrentPageNr: pageNr, TotalNrOfPages: nrOfPages, Page: currentPage);
            }
        }

        private FileInfo CreateExportZipFile(int year, List<(CreditAnnualStatementHeader Statement, AnnualStatementCustomDataModel Custom)> statements, TemporaryDirectory temporaryDirectory)
        {
            var documentsDirectory = CreateDocuments(statements, temporaryDirectory);
            var reportData = CreateExcelReport(year, statements);

            /*
             * Creates a zip file with two files inside:
             * Documents.zip - contains all the pdfs and the metadata file
             * AnnualStatements.xlsx - excel report for manual reporting to skatteverket
             */
            var exportFileDirectory = temporaryDirectory.GetRelativeTempDirectory("Export");
            CreateZipFile(documentsDirectory, exportFileDirectory.GetRelativeTempFile("Documents.zip"));
            File.WriteAllBytes(exportFileDirectory.GetRelativeTempFile($"AnnualStatements.xlsx").FullName, reportData.ToArray());
            return CreateZipFile(exportFileDirectory, temporaryDirectory.GetRelativeTempFile($"AnnualStatements-{year}.zip"));
        }

        /// <summary>
        /// Create a zip file with all the statement pdfs and a metadata file.
        /// The intent is that this can be delivered to a print partner for sending out to customers, hence
        /// the metadata file which can be used for zip code optimization or for adding extra address pages when needed.
        /// </summary>
        private InnerTemporaryDirectory CreateDocuments(List<(CreditAnnualStatementHeader Statement, AnnualStatementCustomDataModel Custom)> statements, TemporaryDirectory temporaryDirectory)
        {
            var country = clientConfiguration.Country.BaseCountry;

            var documentsDirectory = temporaryDirectory.GetRelativeTempDirectory("Documents");

            var metadataRoot = new XElement("AnnualStatements");
            var metadata = new XDocument(metadataRoot);
            foreach (var statement in statements)
            {
                var custom = statement.Custom;
                var fetchResult = documentClient.TryFetchRaw(statement.Statement.StatementDocumentArchiveKey);
                if (!fetchResult.IsSuccess)
                    throw new NTechCoreWebserviceException($"Missing archive document: {statement.Statement.StatementDocumentArchiveKey}");

                var pdfData = fetchResult.FileData;
                File.WriteAllBytes(documentsDirectory.GetRelativeTempFile(fetchResult.FileName).FullName, pdfData);
                metadataRoot.Add(new XElement("AnnualStatement",
                    new XElement("CreditNr", custom.CreditNr),
                    new XElement("CustomerId", custom.CustomerId),
                    new XElement("ForYear", custom.StatementYear),
                    new XElement("Name", custom.CustomerFullName),
                    new XElement("Street", custom.CustomerStreetAddress),
                    new XElement("City", custom.CustomerCity),
                    new XElement("Zip", custom.CustomerZipCode),
                    new XElement("Country", country),
                    new XElement("PdfFileName", fetchResult.FileName)));
            }
            metadata.Save(documentsDirectory.GetRelativeTempFile("documents_metadata.xml").FullName);
            return documentsDirectory;

        }

        private FileInfo CreateZipFile(InnerTemporaryDirectory sourceDirectory, FileInfo targetFile)
        {
            var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fs.CreateZip(targetFile.FullName, sourceDirectory.FullName, true, null);
            return targetFile;
        }

        private MemoryStream CreateExcelReport(int year, List<(CreditAnnualStatementHeader Statement, AnnualStatementCustomDataModel Custom)> statements)
        {
            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"KU25 - {year}"
                    },
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = "Details"
                    },
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = "Customers"
                    }
                }
            };

            var ku25Items = statements
                .Select(x => new
                {
                    x.Custom.SpecificationNr,
                    x.Custom.CustomerCivicRegNr,
                    x.Custom.CustomerFullName,
                    x.Custom.StatementYear,
                    x.Custom.CustomerZipCode,
                    x.Custom.CustomerStreetAddress,
                    x.Custom.CustomerCity,
                    x.Custom.PerCustomerF540Amount,
                    PerCustomerF541Amount = x.Custom.PerCustomerF540Amount == x.Custom.PerCustomerF541Amount ? new decimal?() : x.Custom.PerCustomerF541Amount,
                    PerCustomerF543Amount = x.Custom.PerCustomerF543Amount == 0m ? new decimal?() : x.Custom.PerCustomerF543Amount
                })
                .OrderBy(x => x.SpecificationNr)
                .ThenBy(x => x.CustomerCivicRegNr)
                .ToList();
            excelRequest.Sheets[0].SetColumnsAndData(ku25Items,
                ku25Items.Col(x => x.SpecificationNr, ExcelType.Number, "F570 - Specification Nr", isNumericId: true),
                ku25Items.Col(x => x.CustomerCivicRegNr, ExcelType.Text, "F215 - Civic nr"),
                ku25Items.Col(x => x.CustomerFullName, ExcelType.Text, "Full Name"),
                ku25Items.Col(x => x.CustomerStreetAddress, ExcelType.Text, "Street"),
                ku25Items.Col(x => x.CustomerZipCode, ExcelType.Text, "Zip code"),
                ku25Items.Col(x => x.CustomerCity, ExcelType.Text, "City"),
                ku25Items.Col(x => x.PerCustomerF540Amount, ExcelType.Number, "F540 - Interest paid excluding prepaid"),
                ku25Items.Col(x => x.PerCustomerF541Amount, ExcelType.Number, "F541 - Interest paid including prepaid"),
                ku25Items.Col(x => x.PerCustomerF543Amount, ExcelType.Number, "F543 - Interest compensation paid"));

            var details = statements
                .Select(x => x.Custom)
                .SelectMany(x => (x.Notifications ?? new List<AnnualSummaryNotificationWithInterestPaid>()).Select(y => new
                {
                    Notification = y,
                    Statement = x
                }))
                .OrderBy(x => x.Statement.SpecificationNr)
                .ThenBy(x => x.Statement.CustomerId)
                .ThenBy(x => x.Notification.DueDate)
                .Select(x => new
                {
                    x.Statement.CreditNr,
                    x.Statement.SpecificationNr,
                    x.Notification.DueDate,
                    x.Statement.CustomerId,
                    x.Notification.NrOfCustomers,
                    x.Notification.NotificationDate,
                    x.Notification.NotificationDaysTotal,
                    x.Notification.NotificationDaysThisYear,
                    x.Notification.InterestPaidDuringYear,
                    x.Notification.TotalF540Amount,
                    x.Notification.TotalF541Amount,
                    x.Notification.PerCustomerF540Amount,
                    x.Notification.PerCustomerF541Amount
                })
                .ToList();
            excelRequest.Sheets[1].SetColumnsAndData(details,
                details.Col(x => x.CreditNr, ExcelType.Text, "CreditNr"),
                details.Col(x => x.SpecificationNr, ExcelType.Number, "SpecificationNrF570", isNumericId: true),
                details.Col(x => x.DueDate, ExcelType.Date, "DueDate"),
                details.Col(x => x.CustomerId, ExcelType.Number, "CustomerId", isNumericId: true),
                details.Col(x => x.NrOfCustomers, ExcelType.Number, "NrOfCustomers", nrOfDecimals: 0),
                details.Col(x => x.NotificationDate, ExcelType.Date, "NotificationDate"),
                details.Col(x => x.NotificationDaysTotal, ExcelType.Number, "NotificationDaysTotal", nrOfDecimals: 0),
                details.Col(x => x.NotificationDaysThisYear, ExcelType.Number, "NotificationDaysThisYear", nrOfDecimals: 0),
                details.Col(x => x.InterestPaidDuringYear, ExcelType.Number, "InterestPaidDuringYear"),
                details.Col(x => x.TotalF540Amount, ExcelType.Number, "TotalF540Amount"),
                details.Col(x => x.TotalF541Amount, ExcelType.Number, "TotalF541Amount"),
                details.Col(x => x.PerCustomerF540Amount, ExcelType.Number, "PerCustomerF540Amount"),
                details.Col(x => x.PerCustomerF541Amount, ExcelType.Number, "PerCustomerF541Amount"));

            var customers = statements
                .Select(x => x.Custom)
                .GroupBy(x => x.CustomerId)
                .Select(x => x.First())
                .OrderBy(x => x.CustomerId)
                .ToList();

            excelRequest.Sheets[2].SetColumnsAndData(customers,
                customers.Col(x => x.CustomerId, ExcelType.Number, "CustomerId", isNumericId: true),
                customers.Col(x => x.CustomerCivicRegNr, ExcelType.Text, "Civic nr"),
                customers.Col(x => x.CustomerFullName, ExcelType.Text, "Full name"),
                customers.Col(x => x.CustomerStreetAddress, ExcelType.Text, "Street"),
                customers.Col(x => x.CustomerZipCode, ExcelType.Text, "Zip code"),
                customers.Col(x => x.CustomerCity, ExcelType.Text, "City"));

            var result = documentClient.CreateXlsx(excelRequest);
            result.Position = 0;
            return result;
        }

        /// <summary>
        /// The reason for splitting this up into batches and saving between is that pdf generation is inherently slow and brittle.
        /// If the job blows up the batching ensures that we dont have to start over from scratch if the job gets restarted again.
        /// It can blow up both for performance reasons and because some random data point which has been assumed to be present is suddenly not.
        /// Doing it this way rests strongly on the assumption that this can only be run for past years though as we could otherwise
        /// have data changing after saved so if that assumption is relaxed the batching this way needs to go away.
        /// </summary>
        private void EnsureCreditAnnualStatementHeadersForYear(int year)
        {
            string[] creditNrs;
            DateTime today;

            using (var context = creditContextFactory.CreateContext())
            {
                if (year >= context.CoreClock.Today.Year)
                    throw new NTechCoreWebserviceException("Cannot only be used for passed years")
                    {
                        ErrorCode = "yearMustBePassed",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };

                var firstDayOfNextYear = new DateTime(year + 1, 1, 1);
                var firstDayOfThisYear = new DateTime(year, 1, 1);
                creditNrs = context
                    .CreditHeadersQueryable
                    .Select(x => new
                    {
                        Credit = x,
                        ClosedDate = x
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.Value != CreditStatus.Normal.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (DateTime?)y.TransactionDate)
                            .FirstOrDefault()
                    })
                    .Where(x => x.Credit.CreatedByEvent.TransactionDate < firstDayOfNextYear
                        && (x.ClosedDate == null || x.ClosedDate >= firstDayOfThisYear)
                        && !x.Credit.AnnualStatements.Any(y => y.Year == year))
                    .Select(x => x.Credit.CreditNr)
                    .ToArray();

                today = context.CoreClock.Today;
            }

            var lastDateOfYear = new DateTime(year, 12, 31);

            foreach (var creditNrGroup in creditNrs.SplitIntoGroupsOfN(50))
            {
                using (var context = creditContextFactory.CreateContext())
                {
                    var creditNrGroupList = creditNrGroup.ToList();
                    var notificationsByCreditNr = GetNotificationsWithPaidInterestByCreditNr(context, year, creditNrGroupList);
                    var paidNotNotifiedInterestByCreditNr = GetNotNotifiedPaidInterestByCreditNr(context, year, creditNrGroupList);

                    var allCredits = context
                        .CreditHeadersQueryable
                        .Where(x => creditNrGroup.Contains(x.CreditNr))
                        .Select(x => new
                        {
                            x.CreditNr,
                            EndOfYearCapitalDebt = x
                                .Transactions
                                .Where(y => y.TransactionDate <= lastDateOfYear && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                .Sum(y => (decimal?)y.Amount) ?? 0m,
                            CustomerIds = x.CreditCustomers.Select(y => y.CustomerId)
                        })
                        .ToList();

                    var allCustomerIds = allCredits.SelectMany(x => x.CustomerIds).ToHashSetShared();
                    var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(allCustomerIds, "civicRegNr", "addressZipcode", "addressCity", "addressStreet", "lastName", "firstName");
                    var statementsByCreditNr = new Dictionary<string, AnnualStatementCustomDataModel>();

                    var documentPrefix = GetDocumentTemplatePrefix(envSettings.ClientCreditType);
                    var archiveBatchId = documentClient.BatchRenderBegin(loadPdfTemplate($"{documentPrefix}-annual-statement"));
                    foreach (var credit in allCredits)
                    {
                        var creditNr = credit.CreditNr;
                        var customerIds = credit.CustomerIds.ToList();
                        var notNotifiedInterest = paidNotNotifiedInterestByCreditNr.Opt(creditNr);
                        var totalPaidNotNotifiedInterest = (notNotifiedInterest?.PaidNotNotifiedInterestAmount + notNotifiedInterest?.PaidSwedishRseInterestAmount) ?? 0m;
                        var perCustomerPaidNotNotifiedInterest = Math.Round(totalPaidNotNotifiedInterest / ((decimal)customerIds.Count), 2);
                        var swedishRseAmount = notNotifiedInterest?.PaidSwedishRseInterestAmount ?? 0m;
                        var mortgageLoanPropertyIdByCreditNr = GetMortgageLoanPropertyIdByCreditNr(context, allCredits.Select(x => x.CreditNr).ToHashSetShared());

                        foreach (var customerId in customerIds)
                        {
                            var customer = customerData.Opt(customerId);
                            var customerNotifications = (notificationsByCreditNr.Opt(creditNr) ?? new List<AnnualSummaryNotificationWithInterestPaid>())
                                .Where(x => x.CustomerId == customerId).ToList();

                            var customDataModel = new AnnualStatementCustomDataModel
                            {
                                CustomerId = customerId,
                                CreditNr = creditNr,
                                StatementYear = year,
                                /*
                                 * The numeric part of credit nr. The justification is that we interpret the documentation as
                                 * this being used when needing to correct data after exporting to the tax office.
                                 * They say that this + civic regnr should point out a unique item and we export
                                 * one credit per person per year so credit nr + civic regnr should be unique within a year.
                                 */
                                SpecificationNr = int.Parse(new string(creditNr.Where(x => Char.IsDigit(x)).ToArray())),
                                CustomerCivicRegNr = customer.Opt("civicRegNr"),
                                CustomerStreetAddress = customer.Opt("addressStreet"),
                                CustomerFullName = $"{customer.Opt("firstName")} {customer.Opt("lastName")}".NormalizeNullOrWhitespace(),
                                CustomerCity = customer.Opt("addressCity"),
                                CustomerZipCode = customer.Opt("addressZipcode"),
                                PerCustomerF540Amount = customerNotifications.Sum(x => x.PerCustomerF540Amount) + perCustomerPaidNotNotifiedInterest,
                                PerCustomerF541Amount = customerNotifications.Sum(x => x.PerCustomerF541Amount) + perCustomerPaidNotNotifiedInterest,
                                TotalF540Amount = customerNotifications.Sum(x => x.TotalF540Amount) + totalPaidNotNotifiedInterest,
                                TotalF541Amount = customerNotifications.Sum(x => x.TotalF541Amount) + totalPaidNotNotifiedInterest,
                                PerCustomerF543Amount = Math.Round(swedishRseAmount / ((decimal)customerIds.Count), 2),
                                TotalF543Amount = swedishRseAmount,
                                EndOfYearCapitalDebt = credit.EndOfYearCapitalDebt,
                                Notifications = customerNotifications
                            };
                            var pdfArchiveKey = documentClient.BatchRenderDocumentToArchive(archiveBatchId, $"CreditAnnualStatement_{year}_{creditNr}_{customerId}.pdf", new Dictionary<string, object>
                            {
                                { "creditNr", customDataModel.CreditNr },
                                { "statementForYear", customDataModel.StatementYear.ToString() },
                                { "printDate", today.ToString("yyyyMMdd") },
                                { "customerCivicRegNr", customDataModel.CustomerCivicRegNr },
                                { "customerFullName", customDataModel.CustomerFullName },
                                { "customerStreetAddress", customDataModel.CustomerStreetAddress },
                                { "customerZipCodeAndArea", $"{customDataModel.CustomerZipCode} {customDataModel.CustomerCity}".NormalizeNullOrWhitespace() },
                                { "lastDateOfYear", new DateTime(customDataModel.StatementYear, 12, 31).ToString("yyyy-MM-dd") },
                                { "endOfYearCapitalDebt", customDataModel.EndOfYearCapitalDebt.ToString("C", PrintFormattingCulture) },
                                { "totalPaidInterestDuringYear", customDataModel.TotalF541Amount.ToString("C", PrintFormattingCulture) },
                                { "customerPaidInterestDuringYear", customDataModel.PerCustomerF541Amount.ToString("C", PrintFormattingCulture) },
                                { "totalPaidRseInterestDuringYear", customDataModel.TotalF543Amount.ToString("C", PrintFormattingCulture) },
                                { "totalPaidNonRseInterestDuringYear",(customDataModel.TotalF541Amount - customDataModel.TotalF543Amount).ToString("C", PrintFormattingCulture) },
                                { "mortgageLoanPropertyId", mortgageLoanPropertyIdByCreditNr?.Opt(customDataModel.CreditNr) }
                            });

                            context.AddCreditAnnualStatementHeaders(context.FillInfrastructureFields(new CreditAnnualStatementHeader
                            {
                                CreditNr = customDataModel.CreditNr,
                                CustomerId = customDataModel.CustomerId,
                                Year = customDataModel.StatementYear,
                                StatementDocumentArchiveKey = pdfArchiveKey,
                                CustomData = JsonConvert.SerializeObject(customDataModel)
                            }));
                        }
                    }
                    documentClient.BatchRenderEnd(archiveBatchId);

                    context.SaveChanges();
                }
            }
        }

        private Dictionary<string, string> GetMortgageLoanPropertyIdByCreditNr(ICreditContextExtended context, HashSet<string> creditNrs)
        {
            if (clientConfiguration.Country.BaseCountry != "SE" || envSettings.ClientCreditType != CreditType.MortgageLoan)
                return new Dictionary<string, string>();
            return MortgageLoanCollateralService.GetPropertyIdByCreditNr(context, creditNrs, false);
        }

        private Dictionary<string, List<AnnualSummaryNotificationWithInterestPaid>> GetNotificationsWithPaidInterestByCreditNr(INTechDbContext context, int forYear, List<string> creditNrs)
        {
            return context.GetConnection().Query<AnnualSummaryNotificationWithInterestPaid>(@"with
NotificationExtendedPre1
as
(
	select	n.CreditNr,
			n.NotificationDate,
			n.DueDate,
			(select max(cast(s.[Value] as date)) from DatedCreditString s where s.CreditNr = n.CreditNr and s.[Name]='NextInterestFromDate' and cast(s.[Value] as date) < n.DueDate) as InterestFromDate,
			isnull((select -sum(t.Amount) from AccountTransaction t where t.CreditNotificationId = n.Id and t.AccountCode = 'InterestDebt' and t.IncomingPaymentId is not null and t.WriteoffId is null and year(t.BookKeepingDate) = @forYear), 0) as InterestPaidDuringYear,
			(select count(*) from CreditCustomer c where c.CreditNr = n.CreditNr) as NrOfCustomers
	from	CreditNotificationHeader n
),
NotificationExtendedPre2
as
(
	select	n.*,
			(datediff(d, n.InterestFromDate, n.DueDate) + 1) as NotificationDaysTotal,
			case 
				when year(n.NotificationDate) <> year(n.DueDate) and year(n.NotificationDate) = @forYear 
				then datediff(d, DATEADD(yy, DATEDIFF(yy, 0, n.DueDate), 0), n.DueDate)
				else 0
			end as NotificationDaysNextYear,
			case 
				when year(n.NotificationDate) <> year(n.DueDate) and year(n.NotificationDate) = @forYear 
				then datediff(d, n.InterestFromDate, n.DueDate) - datediff(d, DATEADD(yy, DATEDIFF(yy, 0, n.DueDate), 0), n.DueDate) + 1
				else datediff(d, n.InterestFromDate, n.DueDate) + 1
			end as NotificationDaysThisYear
	from	NotificationExtendedPre1 n
	where	n.InterestPaidDuringYear > 0
),
NotificationExtended
as
(
	select	n.*,
	case 
		when n.NotificationDaysNextYear > 0
		then (cast(n.NotificationDaysThisYear as money) / cast(n.NotificationDaysTotal as money)) * n.InterestPaidDuringYear
		else InterestPaidDuringYear
	end as TotalF540Amount,
	n.InterestPaidDuringYear as TotalF541Amount
	from	NotificationExtendedPre2 n
)
select	e.CreditNr,
		e.DueDate,
		c.CustomerId,
		e.NrOfCustomers,
		e.NotificationDate,		
		e.NotificationDaysTotal,
		e.NotificationDaysThisYear,
		e.InterestPaidDuringYear,
		e.TotalF540Amount,
		e.TotalF541Amount,
		e.TotalF540Amount / e.NrOfCustomers as PerCustomerF540Amount,
		e.TotalF541Amount / e.NrOfCustomers as PerCustomerF541Amount
from	NotificationExtended e
join	CreditCustomer c on c.CreditNr = e.CreditNr
where   e.CreditNr in @creditNrs", param: new
            {
                creditNrs,
                forYear
            })
            .ToList()
            .GroupBy(x => x.CreditNr)
            .ToDictionary(x => x.Key, x => x.ToList());
        }

        private Dictionary<string, NotNotifiedPerCredit> GetNotNotifiedPaidInterestByCreditNr(INTechDbContext context, int forYear, List<string> creditNrs)
        {
            return context.GetConnection().Query<NotNotifiedPerCredit>(@"select	t.CreditNr,
		-sum(case when t.AccountCode = 'InterestDebt' then t.Amount else 0 end) as PaidNotNotifiedInterestAmount,
        -sum(case when t.AccountCode = 'SwedishRseDebt' then t.Amount else 0 end) as PaidSwedishRseInterestAmount
from	AccountTransaction t
where	year(t.TransactionDate) = @forYear
and		t.AccountCode in('SwedishRseDebt', 'InterestDebt')
and		t.IncomingPaymentId is not null
and		t.WriteoffId is null
and		t.CreditNotificationId is null
and		t.CreditNr in @creditNrs
group by t.CreditNr", param: new
            {
                forYear,
                creditNrs
            })
            .ToDictionary(x => x.CreditNr, x => x);
        }

        private class NotNotifiedPerCredit
        {
            public string CreditNr { get; set; }
            public decimal PaidNotNotifiedInterestAmount { get; set; }
            public decimal PaidSwedishRseInterestAmount { get; set; }
        }

        public class AnnualStatementCustomDataModel
        {
            public string CreditNr { get; set; }
            public int CustomerId { get; set; }
            public int SpecificationNr { get; set; }
            public int StatementYear { get; set; }
            public string CustomerCivicRegNr { get; set; }
            public string CustomerFullName { get; set; }
            public string CustomerStreetAddress { get; set; }
            public string CustomerZipCode { get; set; }
            public string CustomerCity { get; set; }
            public decimal EndOfYearCapitalDebt { get; set; }
            public decimal PerCustomerF540Amount { get; set; }
            public decimal PerCustomerF541Amount { get; set; }
            public decimal TotalF540Amount { get; set; }
            public decimal TotalF541Amount { get; set; }
            public List<AnnualSummaryNotificationWithInterestPaid> Notifications { get; set; }
            public decimal PerCustomerF543Amount { get; set; }
            public decimal TotalF543Amount { get; set; }
        }

        public class AnnualSummaryNotificationWithInterestPaid
        {
            public string CreditNr { get; set; }
            public int CustomerId { get; set; }
            public int NrOfCustomers { get; set; }
            public int NotificationDaysTotal { get; set; }
            public int NotificationDaysThisYear { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime NotificationDate { get; set; }
            public decimal InterestPaidDuringYear { get; set; }
            public decimal TotalF540Amount { get; set; }
            public decimal TotalF541Amount { get; set; }
            public decimal PerCustomerF540Amount { get; set; }
            public decimal PerCustomerF541Amount { get; set; }
        }

        private CultureInfo printFormattingCulture;

        protected CultureInfo PrintFormattingCulture
        {
            get
            {
                if (printFormattingCulture == null)
                {
                    printFormattingCulture = NTechCoreFormatting.GetPrintFormattingCulture(clientConfiguration.Country.BaseFormattingCulture);
                }
                return printFormattingCulture;
            }
        }

        private string GetDocumentTemplatePrefix(CreditType creditType)
        {
            switch (creditType)
            {
                case CreditType.UnsecuredLoan: return "credit";
                case CreditType.CompanyLoan: return "companyloan";
                case CreditType.MortgageLoan: return "mortgageloan";
                default:
                    throw new NotImplementedException();
            }
        }

    }
}