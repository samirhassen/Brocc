using nCredit.Code;
using nCredit.DbModel.Model;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/CreditDataExport")]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiExportCreditDataController : NController
    {
        [Route("Run")]
        [HttpPost()]
        public ActionResult RunCreditDataExport(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.exportcreditdata",
                    () => RunCreditDataExportI(),
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private ActionResult RunCreditDataExportI()
        {
            List<string> errors = new List<string>();

            //Used by nScheduler
            var warnings = new List<string>();

            var w = Stopwatch.StartNew();
            try
            {
                var exportProfileName = NEnv.CreditDataExportProfileName;

                if (!string.IsNullOrWhiteSpace(exportProfileName))
                {
                    CreateAndPossiblyExportFile(exportProfileName);
                }

                else
                {
                    warnings.Add("CreditDataExport not exported: Export Profile not set.");
                }

            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreditDataExport crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
            finally
            {
                w.Stop();
            }

            NLog.Information("CreditDataExport finished TotalMilliseconds={totalMilliseconds}", w.ElapsedMilliseconds);

            errors?.ForEach(x => warnings.Add(x));

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
        }

        private OutgoingExportFileHeader CreateAndPossiblyExportFile(string exportProfileName)
        {
            var clock = NTech.ClockFactory.SharedInstance;
            var docClient = Service.DocumentClientHttpContext;
            var zipFileName = $"{clock.Now:yyyyMMddTHH}_CreditDataExport.zip";

            using (var context = new CreditContextExtended(this.GetCurrentUserMetadata(), clock))
            using (var tempDirectory = new TemporaryDirectory())
            {
                var exportZipFile = CreateExportZipFile(tempDirectory, zipFileName);
                var archiveKey = docClient.ArchiveStoreFile(exportZipFile, "application/zip", exportZipFile.Name);
                var exportFile = context.FillInfrastructureFields(new OutgoingExportFileHeader
                {
                    FileArchiveKey = archiveKey,
                    ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                    {
                        status = "NotExported"
                    }),
                    FileType = "CreditDataExport",
                    TransactionDate = clock.Now.UtcDateTime
                });

                context.SaveChanges();

                if (!string.IsNullOrWhiteSpace(exportProfileName))
                {
                    var exportResult = docClient.ExportArchiveFile(exportFile.FileArchiveKey, exportProfileName, null);
                    exportFile.ExportResultStatus = JsonConvert.SerializeObject(new OutgoingExportFileHeader.ExportResultStatusStandardModel
                    {
                        status = exportResult.IsSuccess ? "Success" : "Warning",
                        deliveryTimeInMs = exportResult.TimeInMs,
                        deliveredToProfileName = exportResult.SuccessProfileNames?[0],
                        deliveredToProfileNames = exportResult.SuccessProfileNames,
                        failedProfileNames = exportResult.FailedProfileNames
                    }, Formatting.None);

                    context.SaveChanges();
                }

                return exportFile;
            }
        }


        private FileInfo CreateExportZipFile(TemporaryDirectory temporaryDirectory, string zipFileName)
        {
            var exportZipFileDirectory = temporaryDirectory.GetRelativeTempDirectory("Export");
            CreateDocumentsAndPopulateExportZipFile(exportZipFileDirectory);

            return CreateZipFile(exportZipFileDirectory, temporaryDirectory.GetRelativeTempFile(zipFileName));
        }

        private void CreateDocumentsAndPopulateExportZipFile(InnerTemporaryDirectory exportZipFileDirectory)
        {
            /*
             * Not yet implemented
             * To be used to create files
             * Files are to populate the exportZipFile
             */
        }

        private FileInfo CreateZipFile(InnerTemporaryDirectory sourceDirectory, FileInfo targetFile)
        {
            var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fs.CreateZip(targetFile.FullName, sourceDirectory.FullName, true, null);

            return targetFile;
        }

    }
}




