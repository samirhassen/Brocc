using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.Code.Services
{
    public class ZippedPdfsLoanDeliveryService : ISnailMailLoanDeliveryService
    {
        private readonly IDocumentClient documentClient;
        private readonly CreditContextFactory contextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;

        public ZippedPdfsLoanDeliveryService(IDocumentClient documentClient, CreditContextFactory contextFactory, ICreditEnvSettings envSettings, IClientConfigurationCore clientConfiguration)
        {
            this.documentClient = documentClient;
            this.contextFactory = contextFactory;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
        }

        private T[] SkipNulls<T>(params T[] args) where T : class
        {
            return args.Where(x => x != null).ToArray();
        }

        public OutgoingCreditNotificationDeliveryFileHeader DeliverLoans(List<string> errors, DateTime today, ICustomerPostalInfoRepository customerPostalInfoRepository, INTechCurrentUserMetadata user, List<string> onlyTheseCreditNrs = null)
        {
            return CreateDeliveryExport(today, errors, customerPostalInfoRepository, user, onlyTheseCreditNrs: onlyTheseCreditNrs);
        }

        private IQueryable<CreditNotificationHeader> GetDeliverableNotifications(ICreditContextExtended context, DateTime month)
        {
            var q = context
                    .CreditNotificationHeadersQueryable
                    .Where(x =>
                        x.DueDate > month //Can be next month if using per loan due dates
                        && x.PdfArchiveKey != null //Child credits are not delivered
                        && !x.OutgoingCreditNotificationDeliveryFileHeaderId.HasValue);

            if (envSettings.IsDirectDebitPaymentsEnabled)
            {
                var code = DatedCreditStringCode.IsDirectDebitActive.ToString();
                q = q
                    .Select(x => new
                    {
                        H = x,
                        IsDirectDebitActive = x.Credit.DatedCreditStrings.Where(y => y.Name == code).OrderByDescending(y => y.BusinessEventId).Select(y => y.Value).FirstOrDefault()
                    })
                    .Where(x => x.IsDirectDebitActive == null || x.IsDirectDebitActive == "false")
                    .Select(x => x.H);
            }

            return q;
        }

        protected byte[] FetchRawDocument(string archiveKey)
        {
            var fetchResult = documentClient.TryFetchRaw(archiveKey);
            if (!fetchResult.IsSuccess)
                throw new Exception($"Missing document {archiveKey} in the archive");

            return fetchResult.FileData;
        }

        private OutgoingCreditNotificationDeliveryFileHeader CreateDeliveryExport(DateTime month, List<string> errors, ICustomerPostalInfoRepository customerPostalInfoRepository, INTechCurrentUserMetadata user, List<string> onlyTheseCreditNrs = null)
        {
            var c = documentClient;
            //Take all non delivied notifications for this month, not just the ones just created. This is to allow recovering from previous errors
            using (var context = contextFactory.CreateContext())
            {
                var pre = GetDeliverableNotifications(context, month);
                if (onlyTheseCreditNrs != null)
                    pre = pre.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));

                var ns = pre
                    .Select(x => new
                    {
                        h = x,
                        Applicant1CustomerId = x.Credit.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => (int?)y.CustomerId).FirstOrDefault(),
                        x.Credit.CreditType
                    })
                    .ToList();

                if (ns.Count == 0)
                    return null;

                customerPostalInfoRepository.PreFetchCustomerPostalInfo(new HashSet<int>(ns.Select(x => x.Applicant1CustomerId.Value)));

                var f = new OutgoingCreditNotificationDeliveryFileHeader
                {
                    ChangedById = user.UserId,
                    ExternalId = Guid.NewGuid().ToString(),
                    InformationMetaData = user.InformationMetadata,
                    TransactionDate = context.CoreClock.Today,
                    ChangedDate = context.CoreClock.Now
                };

                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var tempZipfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                Directory.CreateDirectory(tempFolder);
                List<CreditNotificationHeader> deliveredNotifications = new List<CreditNotificationHeader>();
                try
                {
                    List<XElement> meta = new List<XElement>();
                    foreach (var n in ns)
                    {
                        var header = n.h;
                        if (header.PdfArchiveKey == null)
                        {
                            errors.Add("Missing PdfArchiveKey for credit " + header.CreditNr);
                        }
                        else
                        {
                            var postalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(n.Applicant1CustomerId.Value);
                            var pdfBytes = FetchRawDocument(header.PdfArchiveKey);
                            var fileName = $"creditnotification_{header.CreditNr}_{header.DueDate.ToString("yyyy-MM-dd")}_1.pdf";
                            System.IO.File.WriteAllBytes(Path.Combine(tempFolder, fileName), pdfBytes);
                            meta.Add(new XElement("CreditNotification", SkipNulls(
                                new XElement("CreditNr", header.CreditNr),
                                new XElement("Name", postalInfo.GetCustomerName()),
                                new XElement("Street", postalInfo.StreetAddress),
                                new XElement("City", postalInfo.PostArea),
                                new XElement("Zip", postalInfo.ZipCode),
                                new XElement("Country", postalInfo.AddressCountry ?? clientConfiguration.Country.BaseCountry),
                                new XElement("PdfFileName", fileName))));
                            deliveredNotifications.Add(header);
                        }
                    }

                    XDocument metaDoc = new XDocument(new XElement("CreditNotifications",
                        new XAttribute("creationDate", context.CoreClock.Now.ToString("o")),
                        new XAttribute("deliveryId", f.ExternalId),
                        meta));

                    metaDoc.Save(Path.Combine(tempFolder, "creditnotification_metadata.xml"));

                    var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();

                    fs.CreateZip(tempZipfile, tempFolder, true, null);

                    var filename = $"creditnotification_{month.ToString("yyyy-MM")}_{f.ExternalId}.zip";
                    f.FileArchiveKey = documentClient.ArchiveStoreFile(
                        new FileInfo(tempZipfile),
                        "application/zip",
                        filename);

                    foreach(var notification in deliveredNotifications)
                        notification.DeliveryFile = f;

                    context.SaveChanges();

                    if (envSettings.OutgoingCreditNotificationDeliveryFolder != null)
                    {
                        var targetFolder = envSettings.OutgoingCreditNotificationDeliveryFolder;
                        targetFolder.Create();
                        System.IO.File.Copy(tempZipfile, Path.Combine(targetFolder.FullName, filename));
                    }

                    return f;
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempFolder, true);
                        if (System.IO.File.Exists(tempZipfile)) System.IO.File.Delete(tempZipfile);
                    }
                    catch { /* ignored*/ }
                }
            }
        }
    }
}