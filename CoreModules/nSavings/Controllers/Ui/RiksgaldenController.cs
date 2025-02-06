using ICSharpCode.SharpZipLib.Zip;
using nSavings.Code.Riksgalden;
using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechAuthorizeSavingsHigh]
    public class RiksgaldenController : NController
    {
        [HttpGet]
        [Route("Ui/Riksgalden")]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                firstFileUrlPattern = Url.Action("FirstFile", new { alsoEncryptAndSign = "BBBBB" }),
                secondFileUrlPattern = Url.Action("SecondFile", new { alsoEncryptAndSign = "BBBBB", firstFileMaxBusinessEventId = "NNNNN" })
            });
            return View();
        }

        [HttpGet]
        [Route("Api/Riksgalden/FirstFile")]
        public ActionResult FirstFile(bool alsoEncryptAndSign)
        {
            if (alsoEncryptAndSign)
                throw new NotImplementedException();

            //Fetch data
            var repo = new RiksgaldenDataRepository(NEnv.ClientCfg.Country.BaseCurrency, NEnv.BaseCivicRegNumberParser, NEnv.ClientCfg.Country.BaseCountry, Clock);
            var riksgaldenConfig = NEnv.RiksgaldenConfig;
            var data = repo.GetFirstFileDataSet();

            //Export
            var exporter = new RiksgaldenFileExporter(Clock.Now.Date, riksgaldenConfig.DepositorGuaranteeInstituteName, riksgaldenConfig.DepositorGuaranteeOrgnr);
            var now = Clock.Now.Date;
            var ms = new MemoryStream();

            using (var zip = new ZipOutputStream(ms))
            {
                Action<string, Action> addFile = (filename, write) =>
                {
                    var newEntry = new ZipEntry(filename);
                    newEntry.DateTime = now;
                    zip.PutNextEntry(newEntry);
                    write();
                    zip.CloseEntry();
                };

                addFile("kund.txt", () => exporter.WriteCustomerFileToStream(data.Customers, zip));
                addFile("konto.txt", () => exporter.WriteAccountsFileToStream(data.Accounts, zip));
                addFile("kontofordelning.txt", () => exporter.WriteAccountDistributionsFileToStream(data.AccountDistributions, zip));
                addFile("transaktion.txt", () => exporter.WriteTransactionsFileToStream(data.Transactions, true, zip));
                if (NEnv.ClientCfg.Country.BaseCountry == "FI")
                {
                    addFile("filial.txt", () => exporter.WriteFinnishFilialFileToStream(data.FiFilialCustomers, data.FiFilialAccounts, zip));
                }
                zip.IsStreamOwner = false;
            }

            ms.Position = 0;

            return new FileStreamResult(ms, "application/zip") { FileDownloadName = $"Riksgalden_FirstFile_{now.ToString("yyyyMMddHHmmss")}_LastEventId_{data.MaxBusinessEventId}.zip" };
        }

        [HttpGet]
        [Route("Api/Riksgalden/SecondFile")]
        public ActionResult SecondFile(int firstFileMaxBusinessEventId, bool alsoEncryptAndSign)
        {
            if (alsoEncryptAndSign)
                throw new NotImplementedException();

            //Fetch data
            var repo = new RiksgaldenDataRepository(NEnv.ClientCfg.Country.BaseCurrency, NEnv.BaseCivicRegNumberParser, NEnv.ClientCfg.Country.BaseCountry, Clock);
            var riksgaldenConfig = NEnv.RiksgaldenConfig;
            var data = repo.GetSecondFileDataSet(firstFileMaxBusinessEventId);

            //Export
            var exporter = new RiksgaldenFileExporter(Clock.Now.Date, riksgaldenConfig.DepositorGuaranteeInstituteName, riksgaldenConfig.DepositorGuaranteeOrgnr);
            var now = Clock.Now.Date;
            var ms = new MemoryStream();

            using (var zip = new ZipOutputStream(ms))
            {
                Action<string, Action> addFile = (filename, write) =>
                {
                    var newEntry = new ZipEntry(filename);
                    newEntry.DateTime = now;
                    zip.PutNextEntry(newEntry);
                    write();
                    zip.CloseEntry();
                };

                addFile("transaktion.txt", () => exporter.WriteTransactionsFileToStream(data.Transactions, false, zip));

                zip.IsStreamOwner = false;
            }

            ms.Position = 0;

            return new FileStreamResult(ms, "application/zip") { FileDownloadName = $"Riksgalden_SecondFile_{now.ToString("yyyyMMddHHmmss")}_Events_{data.StartAfterBusinessEventId}_{data.MaxBusinessEventId}.zip" };
        }
    }
}