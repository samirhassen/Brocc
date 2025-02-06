using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class CreditDocumentsService : ICreditDocumentsService
    {
        private readonly IDocumentClient documentClient;
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly CreditContextFactory contextFactory;

        public CreditDocumentsService(IDocumentClient documentClient, INTechServiceRegistry serviceRegistry, CreditContextFactory contextFactory)
        {
            this.documentClient = documentClient;
            this.serviceRegistry = serviceRegistry;
            this.contextFactory = contextFactory;
        }

        public List<CreditDocumentModel> FetchCreditDocuments(string creditNr, bool fetchFilenames, bool includeExtraDocuments)
        {
            using (var context = contextFactory.CreateContext())
            {
                var creditDocuments = context
                    .CreditDocumentsQueryable
                    .Where(x => x.CreditNr == creditNr || x.Reminder.CreditNr == creditNr || x.TerminationLetter.CreditNr == creditNr)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.ChangedDate,
                        x.DocumentType,
                        x.ArchiveKey,
                        x.ApplicantNr,
                        x.CustomerId,
                        x.Id
                    })
                    .ToList();

                var ds = creditDocuments.Select(x =>
                    new
                    {
                        document = new CreditDocumentModel
                        {
                            DocumentId = x.Id,
                            ApplicantNr = x.ApplicantNr,
                            CustomerId = x.CustomerId,
                            CreationDate = x.ChangedDate,
                            DocumentType = x.DocumentType,
                            DownloadUrl = serviceRegistry.ExternalServiceUrl("nCredit", "Api/ArchiveDocument", Tuple.Create("key", x.ArchiveKey)).ToString(),
                            ArchiveKey = x.ArchiveKey
                        },
                        key = x.ArchiveKey
                    }
                ).ToList();


                var extraDocuments = LoadExtraDocuments(creditNr, includeExtraDocuments, context);
                var allDocuments = ds.Concat(extraDocuments.Select(x => new { document = x.Document, key = x.ArchiveKey })).ToList();

                if (fetchFilenames)
                {
                    var keys = ds.Select(x => x.key).ToHashSetShared();
                    var m = documentClient.FetchMetadataBulk(keys);
                    foreach (var d in allDocuments)
                    {
                        d.document.FileName = m.Opt(d.key)?.FileName;
                    }
                }

                return allDocuments.Select(x => x.document).OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.DocumentId).ToList();
            }
        }

        private IEnumerable<(CreditDocumentModel Document, string ArchiveKey)> LoadExtraDocuments(string creditNr, bool includeExtraDocuments, ICreditContextExtended context)
        {
            if (!includeExtraDocuments)
                yield break;

            const string ExtraDocumentPrefix = "Extra_";

            var credits = context
                .CreditHeadersQueryable
                .Where(x => x.CreditNr == creditNr)
                .Select(x => new
                {
                    x.CreditNr,
                    AnnualStatements = x.AnnualStatements.Select(y => new
                    {
                        y.ChangedDate,
                        ApplicantNr = x.CreditCustomers.Where(z => z.CustomerId == y.CustomerId).Select(z => z.ApplicantNr).FirstOrDefault(),
                        y.Year,
                        y.StatementDocumentArchiveKey,
                        y.CustomerId
                    })
                })
                .ToList();

            foreach (var credit in credits)
            {
                foreach (var annualStatement in credit.AnnualStatements)
                {
                    yield return (Document: new CreditDocumentModel
                    {
                        CustomerId = annualStatement.CustomerId,
                        ApplicantNr = annualStatement.ApplicantNr,
                        CreationDate = annualStatement.ChangedDate.Date,
                        DocumentId = 0,
                        DocumentType = $"{ExtraDocumentPrefix}AnnualStatement",
                        IsExtraDocument = true,
                        ExtraDocumentData = annualStatement.Year.ToString(),
                        DownloadUrl = serviceRegistry.ExternalServiceUrl("nCredit", "Api/ArchiveDocument", Tuple.Create("key", annualStatement.StatementDocumentArchiveKey)).ToString(),
                        ArchiveKey = annualStatement.StatementDocumentArchiveKey
                    }, ArchiveKey: annualStatement.StatementDocumentArchiveKey);
                }
            }

        }
    }

    public interface ICreditDocumentsService
    {
        List<CreditDocumentModel> FetchCreditDocuments(string creditNr, bool fetchFilenames, bool includeExtraDocuments);
    }

    public class CreditDocumentModel
    {
        public int DocumentId { get; set; }
        public string DocumentType { get; set; }
        public int? ApplicantNr { get; set; }
        public int? CustomerId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string DownloadUrl { get; set; }
        public string ArchiveKey { get; set; }
        public string FileName { get; set; }
        public bool IsExtraDocument { get; set; }
        public string ExtraDocumentData { get; set; }
    }
}