using System;
using System.IO;
using System.Web.Mvc;
using ICSharpCode.SharpZipLib.Zip;
using nSavings.Code;
using nSavings.Code.Riksgalden;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui
{
    [NTechAuthorizeSavingsHigh]
    public class RiksgaldenController : NController
    {
        [HttpGet]
        [Route("Ui/Riksgalden")]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = EncodeInitialData(new
            {
                firstFileUrlPattern = Url.Action("FirstFile", new { alsoEncryptAndSign = "BBBBB" }),
                secondFileUrlPattern = Url.Action("SecondFile",
                    new { alsoEncryptAndSign = "BBBBB", firstFileMaxBusinessEventId = "NNNNN" })
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
            var repo = new RiksgaldenDataRepository(NEnv.ClientCfg.Country.BaseCurrency, NEnv.BaseCivicRegNumberParser,
                NEnv.ClientCfg.Country.BaseCountry, Clock);
            var riksgaldenConfig = NEnv.RiksgaldenConfig;
            var data = repo.GetFirstFileDataSet();

            //Export
            var exporter = new RiksgaldenFileExporter(Clock.Now.Date, riksgaldenConfig.DepositorGuaranteeInstituteName,
                riksgaldenConfig.DepositorGuaranteeOrgnr);
            var now = Clock.Now.Date;
            var ms = new MemoryStream();

            using (var zip = new ZipOutputStream(ms))
            {
                AddFile("kund.txt", () => exporter.WriteCustomerFileToStream(data.Customers, zip));
                AddFile("konto.txt", () => exporter.WriteAccountsFileToStream(data.Accounts, zip));
                AddFile("kontofordelning.txt",
                    () => exporter.WriteAccountDistributionsFileToStream(data.AccountDistributions, zip));
                AddFile("transaktion.txt", () => exporter.WriteTransactionsFileToStream(data.Transactions, true, zip));
                if (NEnv.ClientCfg.Country.BaseCountry == "FI")
                {
                    AddFile("filial.txt",
                        () => exporter.WriteFinnishFilialFileToStream(data.FiFilialCustomers, data.FiFilialAccounts,
                            zip));
                }

                zip.IsStreamOwner = false;

                void AddFile(string filename, Action write)
                {
                    var newEntry = new ZipEntry(filename)
                    {
                        DateTime = now
                    };
                    zip.PutNextEntry(newEntry);
                    write();
                    zip.CloseEntry();
                }
            }

            ms.Position = 0;

            return new FileStreamResult(ms, "application/zip")
            {
                FileDownloadName =
                    $"Riksgalden_FirstFile_{now:yyyyMMddHHmmss}_LastEventId_{data.MaxBusinessEventId}.zip"
            };
        }

        [HttpGet]
        [Route("Api/Riksgalden/SecondFile")]
        public ActionResult SecondFile(int firstFileMaxBusinessEventId, bool alsoEncryptAndSign)
        {
            if (alsoEncryptAndSign)
                throw new NotImplementedException();

            //Fetch data
            var repo = new RiksgaldenDataRepository(NEnv.ClientCfg.Country.BaseCurrency, NEnv.BaseCivicRegNumberParser,
                NEnv.ClientCfg.Country.BaseCountry, Clock);
            var riksgaldenConfig = NEnv.RiksgaldenConfig;
            var data = repo.GetSecondFileDataSet(firstFileMaxBusinessEventId);

            //Export
            var exporter = new RiksgaldenFileExporter(Clock.Now.Date, riksgaldenConfig.DepositorGuaranteeInstituteName,
                riksgaldenConfig.DepositorGuaranteeOrgnr);
            var now = Clock.Now.Date;
            var ms = new MemoryStream();

            using (var zip = new ZipOutputStream(ms))
            {
                AddFile("transaktion.txt", () => exporter.WriteTransactionsFileToStream(data.Transactions, false, zip));

                zip.IsStreamOwner = false;

                void AddFile(string filename, Action write)
                {
                    var newEntry = new ZipEntry(filename)
                    {
                        DateTime = now
                    };
                    zip.PutNextEntry(newEntry);
                    write();
                    zip.CloseEntry();
                }
            }

            ms.Position = 0;

            return new FileStreamResult(ms, "application/zip")
            {
                FileDownloadName =
                    $"Riksgalden_SecondFile_{now:yyyyMMddHHmmss}_Events_{data.StartAfterBusinessEventId}_{data.MaxBusinessEventId}.zip"
            };
        }
    }
}