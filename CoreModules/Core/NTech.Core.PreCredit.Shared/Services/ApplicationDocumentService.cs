using Newtonsoft.Json;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationDocumentService : IApplicationDocumentService
    {
        private IDocumentClient documentClient;
        private readonly bool isMortageLoanEnabled;
        private readonly Action<PreCreditEventCode, string> publishEvent;
        private readonly IPreCreditContextFactoryService contextFactoryService;

        public ApplicationDocumentService(IDocumentClient documentClient, 
            bool isMortageLoanEnabled, Action<PreCreditEventCode, string> publishEvent, IPreCreditContextFactoryService contextFactoryService)
        {
            this.documentClient = documentClient;
            this.isMortageLoanEnabled = isMortageLoanEnabled;
            this.publishEvent = publishEvent;
            this.contextFactoryService = contextFactoryService;
        }

        private IPreCreditContextExtended createContext() => contextFactoryService.CreateExtended();

        public List<ApplicationDocumentModel> FetchForApplication(string applicationNr, List<string> documentTypes)
        {
            return FetchForApplication(applicationNr, documentTypes, new List<string> { CreditApplicationDocumentTypeCode.Freeform.ToString() }, false);
        }

        public List<ApplicationDocumentModel> FetchFreeformForApplication(string applicationNr)
        {
            return FetchForApplication(applicationNr, new List<string> { CreditApplicationDocumentTypeCode.Freeform.ToString() }, null, true);
        }

        private List<ApplicationDocumentModel> FetchForApplication(string applicationNr, List<string> includedDocumentTypes, List<string> excludedDocumentTypes, bool onlyApplicationLevelDocuments)
        {
            using (var context = createContext())
            {
                var pre = context
                    .CreditApplicationDocumentHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr && !x.RemovedByUserId.HasValue);
                if (includedDocumentTypes != null && includedDocumentTypes.Count > 0)
                    pre = pre.Where(x => includedDocumentTypes.Contains(x.DocumentType));
                if (excludedDocumentTypes != null && excludedDocumentTypes.Count > 0)
                    pre = pre.Where(x => !excludedDocumentTypes.Contains(x.DocumentType));
                if (onlyApplicationLevelDocuments)
                    pre = pre.Where(x => !x.ApplicantNr.HasValue && !x.CustomerId.HasValue && x.DocumentSubType == null);
                return pre
                    .OrderBy(x => x.Id)
                    .ToList()
                    .Select(ToDocumentModel)
                    .ToList();
            }
        }

        public ApplicationDocumentModel FetchSingle(string applicationNr, int documentId)
        {
            using (var context = createContext())
            {
                return ToDocumentModel(context
                    .CreditApplicationDocumentHeadersQueryable
                    .Single(x => x.ApplicationNr == applicationNr && x.Id == documentId && !x.RemovedByUserId.HasValue));
            }
        }

        public bool TryAddDocumentAndRemoveExisting(string applicationNr, string documentType, int? applicantNr, int? customerId, string documentSubType, string attachedFileAsDataUrl, string attachedFileName, int documentIdToRemove, out ApplicationDocumentModel addedDocument, out string failedMessage)
        {
            bool result = false;
            addedDocument = null;
            int? currentUserId;
            string informationMetadata;
            using (var context = createContext())
            {
                CreditApplicationDocumentHeader addedInternalDocument;
                if (TryAddDocumentI(context, applicationNr, documentType, applicantNr, customerId, documentSubType, attachedFileAsDataUrl, attachedFileName, out addedInternalDocument, out failedMessage))
                {
                    if (TryRemoveDocumentI(context, applicationNr, documentIdToRemove, out failedMessage))
                    {
                        context.SaveChanges();
                        addedDocument = ToDocumentModel(addedInternalDocument);
                        result = true;
                    }
                }
                currentUserId = context.CurrentUserId;
                informationMetadata = context.InformationMetadata;
            }
            if (result && addedDocument != null)
                AfterDocumentAdded(applicationNr, addedDocument, currentUserId, informationMetadata);
            return result;
        }

        public bool TryAddDocument(string applicationNr, string documentType, int? applicantNr, int? customerId, string documentSubType, string attachedFileAsDataUrl, string attachedFileName, out ApplicationDocumentModel addedDocument, out string failedMessage)
        {
            bool result = false;
            addedDocument = null;
            int? currentUserId;
            string informationMetadata;
            using (var context = createContext())
            {
                CreditApplicationDocumentHeader addedInternalDocument;
                if (TryAddDocumentI(context, applicationNr, documentType, applicantNr, customerId, documentSubType, attachedFileAsDataUrl, attachedFileName, out addedInternalDocument, out failedMessage))
                {
                    context.SaveChanges();
                    addedDocument = ToDocumentModel(addedInternalDocument);
                    result = true;
                }
                currentUserId = context.CurrentUserId;
                informationMetadata = context.InformationMetadata;
            }
            if (result && addedDocument != null)
                AfterDocumentAdded(applicationNr, addedDocument, currentUserId, informationMetadata);
            return result;
        }

        private void AfterDocumentAdded(string applicationNr, ApplicationDocumentModel addedDocument, int? currentUserId, string informationMetadata)
        {
            if (isMortageLoanEnabled && addedDocument.DocumentType == CreditApplicationDocumentTypeCode.SignedAgreement.ToString())
            {
                this.publishEvent?.Invoke(
                    PreCreditEventCode.MortgageLoanAddedSignedAgreement,
                    JsonConvert.SerializeObject(new { applicationNr, addedDocument, currentUserId, informationMetadata }));
            }
        }

        public void RemoveDocument(string applicationNr, int documentId)
        {
            var success = TryRemoveDocument(applicationNr, documentId, out var failedMessage);
            if (!success)
            {
                throw new Exception($"Could not remove document: {failedMessage}");
            }
        }

        public bool TryRemoveDocument(string applicationNr, int documentId, out string failedMessage)
        {
            using (var context = createContext())
            {
                if (TryRemoveDocumentI(context, applicationNr, documentId, out failedMessage))
                {
                    context.SaveChanges();
                    return true;
                }
                else
                    return false;
            }
        }

        public bool TryUpdateMortgageLoanDocumentCheckStatus(string applicationNr, out ApplicationDocumentCheckStatusUpdateResult successResult, out string failedMessage)
        {
            using (var context = createContext())
            {
                var a = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        MortgageLoanExtension = x.MortgageLoanExtension,
                        NrOfApplicants = x.NrOfApplicants,
                        Documents = x.Documents.Where(y => !y.RemovedByUserId.HasValue)
                    })
                    .SingleOrDefault();
                if (a == null)
                {
                    successResult = null;
                    failedMessage = "No such application exists";
                    return false;
                }
                if (a.MortgageLoanExtension == null)
                    throw new Exception("Wrong application type");

                bool areAllAttached = true;
                foreach (var d in RequiredSharedMortagageLoansDocumentTypes)
                {
                    if (!a.Documents.Any(x => x.DocumentType == d.ToString()))
                        areAllAttached = false;
                }
                foreach (var d in RequiredPerApplicantMortagageLoansDocumentTypes)
                {
                    for (var applicantNr = 1; applicantNr <= a.NrOfApplicants; applicantNr++)
                    {
                        if (!a.Documents.Any(x => x.DocumentType == d.ToString() && x.ApplicantNr == applicantNr))
                            areAllAttached = false;
                    }
                }

                var newStatus = (areAllAttached ? MortgageLoanDocumentCheckStatus.Accepted : MortgageLoanDocumentCheckStatus.Initial).ToString();
                var currentStatus = a.MortgageLoanExtension.DocumentCheckStatus ?? MortgageLoanDocumentCheckStatus.Initial.ToString();

                if (currentStatus != newStatus)
                {
                    context.CreateAndAddComment($"Document check status changed from {currentStatus} -> {newStatus}", "DocumentCheckStatusUpdate", applicationNr: applicationNr);
                    a.MortgageLoanExtension.DocumentCheckStatus = newStatus;
                    context.SaveChanges();

                    failedMessage = null;
                    successResult = new ApplicationDocumentCheckStatusUpdateResult
                    {
                        WasStatusChanged = true,
                        DocumentCheckStatusAfter = newStatus
                    };

                    return true;
                }
                else
                {
                    failedMessage = null;

                    successResult = new ApplicationDocumentCheckStatusUpdateResult
                    {
                        WasStatusChanged = false,
                        DocumentCheckStatusAfter = currentStatus
                    };

                    return true;
                }
            }
        }

        public static List<CreditApplicationDocumentTypeCode> RequiredSharedMortagageLoansDocumentTypes { get; } = new List<CreditApplicationDocumentTypeCode>
            {
                CreditApplicationDocumentTypeCode.MortgageLoanCustomerAmortizationPlan,
                CreditApplicationDocumentTypeCode.MortgageLoanDenuntiation,
                CreditApplicationDocumentTypeCode.SignedDirectDebitConsent,
                CreditApplicationDocumentTypeCode.MortgageLoanLagenhetsutdrag,
                CreditApplicationDocumentTypeCode.ProxyAuthorization
            };

        public static List<CreditApplicationDocumentTypeCode> RequiredPerApplicantMortagageLoansDocumentTypes { get; } = new List<CreditApplicationDocumentTypeCode>
            {
                CreditApplicationDocumentTypeCode.SignedAgreement,
                CreditApplicationDocumentTypeCode.ProofOfIdentity
            };

        private bool TryAddDocumentI(IPreCreditContextExtended context, string applicationNr, string documentType, int? applicantNr, int? customerId, string documentSubType, string attachedFileAsDataUrl, string attachedFileName, out CreditApplicationDocumentHeader addedDocument, out string failedMessage)
        {
            var dt = Enums.Parse<CreditApplicationDocumentTypeCode>(documentType, ignoreCase: true);
            if (!dt.HasValue)
            {
                addedDocument = null;
                failedMessage = "Invalid or missing documentType";
                return false;
            }

            string attachedFileArchiveDocumentKey = null;
            string mimeType = null;
            if (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
            {
                byte[] fileData;
                if (!FileUtilities.TryParseDataUrl(attachedFileAsDataUrl, out mimeType, out fileData))
                {
                    addedDocument = null;
                    failedMessage = "Invalid attached file";
                    return false;
                }                
                try
                {
                    attachedFileArchiveDocumentKey = documentClient.ArchiveStoreWithSource(fileData, mimeType, attachedFileName, $"CreditApplicationDocument_{documentType}", applicationNr);
                }
                catch (NTechCoreWebserviceException ex)
                {
                    if (ex?.ErrorCode == "fileTypeNotAllowed")
                    {
                        addedDocument = null;
                        failedMessage = ex.ErrorCode;
                        return false;
                    }
                    throw;
                }
            }

            addedDocument = context.CreateAndAddApplicationDocument(attachedFileArchiveDocumentKey, attachedFileName, dt.Value, applicationNr: applicationNr, applicantNr: applicantNr, customerId: customerId, documentSubType: documentSubType);

            failedMessage = null;
            return true;
        }

        private bool TryRemoveDocumentI(IPreCreditContextExtended context, string applicationNr, int documentId, out string failedMessage)
        {
            var document = context
                .CreditApplicationDocumentHeadersQueryable
                .SingleOrDefault(x => x.ApplicationNr == applicationNr && x.Id == documentId && !x.RemovedByUserId.HasValue);
            if (document == null)
            {
                failedMessage = "No such document exists";
                return false;
            }
            document.RemovedByUserId = context.CurrentUserId;
            document.RemovedDate = context.CoreClock.Now;

            failedMessage = null;
            return true;
        }

        private ApplicationDocumentModel ToDocumentModel(CreditApplicationDocumentHeader c)
        {
            return new ApplicationDocumentModel
            {
                DocumentId = c.Id,
                ApplicantNr = c.ApplicantNr,
                CustomerId = c.CustomerId,
                DocumentType = c.DocumentType,
                DocumentSubType = c.DocumentSubType,
                Filename = c.DocumentFileName,
                DocumentArchiveKey = c.DocumentArchiveKey,
                DocumentDate = c.AddedDate,
                VerifiedDate = c.VerifiedDate
            };
        }

        public bool IsApplicantDocumentAddedForAllApplicants(string applicationNr, string documentType)
        {
            using (var context = createContext())
            {
                var d = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.NrOfApplicants,
                        NrOfDocuments = x.Documents.Where(y => y.DocumentType == documentType && y.ApplicantNr.HasValue && !y.RemovedByUserId.HasValue).Select(y => y.ApplicantNr).Distinct().Count()
                    })
                   .SingleOrDefault();
                if (d == null)
                    return false;
                return d.NrOfDocuments >= d.NrOfApplicants;
            }
        }

        public bool TrySetDocumentVerified(string applicationNr, int documentId, bool isVerified, out ApplicationDocumentModel documentAfter)
        {
            using (var context = createContext())
            {
                var document = context.CreditApplicationDocumentHeadersQueryable.SingleOrDefault(x => x.ApplicationNr == applicationNr && x.Id == documentId && !x.RemovedByUserId.HasValue);

                if (document == null)
                {
                    documentAfter = null;
                    return false;
                }

                if (isVerified)
                {
                    document.VerifiedByUserId = context.CurrentUserId;
                    document.VerifiedDate = context.CoreClock.Now;
                }
                else
                {
                    document.VerifiedByUserId = null;
                    document.VerifiedDate = null;
                }

                context.SaveChanges();

                documentAfter = ToDocumentModel(document);
                return true;
            }
        }
    }

    public class ApplicationDocumentCheckStatusUpdateResult
    {
        public string DocumentCheckStatusAfter { get; set; }
        public bool WasStatusChanged { get; set; }
    }

    public class ApplicationDocumentModel
    {
        public int DocumentId { get; set; }
        public int? ApplicantNr { get; set; }
        public int? CustomerId { get; set; }
        public string DocumentType { get; set; }
        public string DocumentSubType { get; set; }
        public string Filename { get; set; }
        public string DocumentArchiveKey { get; set; }
        public DateTimeOffset? DocumentDate { get; set; }
        public DateTimeOffset? VerifiedDate { get; set; }
    }

    public interface IApplicationDocumentService
    {
        List<ApplicationDocumentModel> FetchForApplication(string applicationNr, List<string> documentTypes);

        ApplicationDocumentModel FetchSingle(string applicationNr, int documentId);

        bool TryAddDocument(string applicationNr, string documentType, int? applicantNr, int? customerId, string documentSubType, string attachedFileAsDataUrl, string attachedFileName, out ApplicationDocumentModel addedDocument, out string failedMessage);

        bool TryAddDocumentAndRemoveExisting(string applicationNr, string documentType, int? applicantNr, int? customerId, string documentSubType, string attachedFileAsDataUrl, string attachedFileName, int documentIdToRemove, out ApplicationDocumentModel addedDocument, out string failedMessage);

        void RemoveDocument(string applicationNr, int documentId);

        bool TryRemoveDocument(string applicationNr, int documentId, out string failedMessage);

        bool TryUpdateMortgageLoanDocumentCheckStatus(string applicationNr, out ApplicationDocumentCheckStatusUpdateResult successResult, out string failedMessage);

        bool IsApplicantDocumentAddedForAllApplicants(string applicationNr, string documentType);

        List<ApplicationDocumentModel> FetchFreeformForApplication(string applicationNr);
        bool TrySetDocumentVerified(string applicationNr, int documentId, bool isVerified, out ApplicationDocumentModel documentAfter);
    }
}