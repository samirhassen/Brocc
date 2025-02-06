using nCustomer.Code.Clients;
using nCustomer.DbModel;
using NTech;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services.EidSignatures.Signicat
{
    /// <summary>
    /// NOTE: This does not use ProviderSignatureService since it was created before that.
    ///       It's likely a good idea to try an merge this into using the same.
    /// </summary>
    public class SignicatSignatureService
    {
        private readonly ICombinedClock combinedClock;
        private readonly CustomerContextFactory contextFactory;

        private IClock clock => combinedClock;
        private ICoreClock CoreClock => combinedClock;

        public SignicatSignatureService(ICombinedClock clock, CustomerContextFactory contextFactory)
        {
            this.combinedClock = clock;
            this.contextFactory = contextFactory;
        }

        public static string ProviderNameShared = "signicat";
        public string ProviderName => ProviderNameShared;

        public CommonElectronicIdSignatureSession GetSession(string sessionIdOrCustomSearchTerm, string customSearchTermNameOrNullForSessionId,  ICustomerContextExtended context)
        {
            var client = new SignicatSigningClient();
            SignicatSigningClient.SignatureSession session;
            if (customSearchTermNameOrNullForSessionId.IsOneOfIgnoreCase("alternateKey"))
                session = client.GetSignatureSessionByAlternateKey(sessionIdOrCustomSearchTerm, true);
            else if (customSearchTermNameOrNullForSessionId == null)
                session = client.GetSignatureSession(sessionIdOrCustomSearchTerm);
            else
                return null;

            return CreateCommonSessionFromSignicatSession(session, context);
        }

        public CommonElectronicIdSignatureSession CreateSingleDocumentSignatureSession(SingleDocumentSignatureRequest request)
        {
            using (var context = contextFactory.CreateContext())
            {
                var signicatClient = new SignicatSigningClient();
                var signingCustomersByApplicantNr = new Dictionary<int, SignicatSigningClient.StartSignatureSessionRequestBase.Customer>();
                foreach (var signer in request.SigningCustomers)
                {
                    signingCustomersByApplicantNr[signer.SignerNr.Value] = new SignicatSigningClient.StartSignatureSessionRequestBase.Customer
                    {
                        ApplicantNr = signer.SignerNr.Value,
                        CivicRegNr = signer.CivicRegNr,
                        FirstName = signer.FirstName,
                        LastName = signer.LastName,
                        SignicatLoginMethod = signicatClient.GetElectronicIdLoginMethod()
                    };
                }

                var documentBytes = new DocumentClient().FetchRawWithFilename(request.DocumentToSignArchiveKey, out var _, out var __);

                var session = signicatClient.StartSingleDocumentSignatureSession(new SignicatSigningClient.StartSingleDocumentSignatureSessionRequest
                {
                    PdfBytesBase64 = Convert.ToBase64String(documentBytes),
                    PdfDisplayFileName = request.DocumentToSignFileName,
                    ServerToServerCallbackUrl = request.ServerToServerCallbackUrl,
                    SigningCustomersByApplicantNr = signingCustomersByApplicantNr,
                    CustomData = MergeDictionaries(request.CustomData, new Dictionary<string, string> { { "unsignedDocumentArchiveKey", request.DocumentToSignArchiveKey } }),
                    RedirectAfterFailedUrl = request.RedirectAfterFailedUrl,
                    RedirectAfterSuccessUrl = request.RedirectAfterSuccessUrl
                });

                return CreateCommonSessionFromSignicatSession(session, context);
            }
        }

        private CommonElectronicIdSignatureSession CreateCommonSessionFromSignicatSession(SignicatSigningClient.SignatureSession session, ICustomerContextExtended context)
        {
            if (session == null)
                return null;

            if (session.Documents.Count != 1)
                throw new NTechWebserviceMethodException("Signicat multi document session not supported")
                {
                    ErrorCode = "unsupportedSessionType",
                    ErrorHttpStatusCode = 400,
                    IsUserFacing = true
                };

            var unsignedDocument = session.Documents.Single();
            SignicatSigningClient.SignatureSession.SignedDocumentCombination signedDocument = null;
            var unsignedDocumentArchiveKey = session.CustomData.Opt("unsignedDocumentArchiveKey"); //Since this is stored in SDS which we dont want to depend on

            DateTime? closedDate = null;
            string closedMessage = null;
            if (session.SessionStateCode.IsOneOfIgnoreCase("SignaturesSuccessful"))
            {
                signedDocument = session.SignedDocumentCombinations.Single();
                closedDate = session.SigningCustomersByApplicantNr.Values.Select(x => (x.SignedDateUtc ?? DateTime.UtcNow).ToLocalTime()).Max();
            }
            else if (session.SessionStateCode.IsOneOfIgnoreCase("PendingAllSignatures", "PendingSomeSignatures"))
            {

            }
            else
            {
                //Broken, Failed,Cancelled
                closedDate = DateTime.Today;
                closedMessage = $"SessionStateCode={session.SessionStateCode}, SessionStateMessage={session.SessionStateMessage}";
            }

            var documentClient = new DocumentClient();
            var signicatClient = new SignicatSigningClient();

            return new CommonElectronicIdSignatureSession
            {
                Id = session.Id,
                RedirectAfterFailedUrl = session.RedirectAfterFailedUrl,
                RedirectAfterSuccessUrl = session.RedirectAfterSuccessUrl,
                ServerToServerCallbackUrl = null, //Hidden from the user but stored for signicat. We could expose this if we ever need it.
                UnsignedPdf = unsignedDocumentArchiveKey == null ? null : new CommonElectronicIdSignatureSession.PdfModel
                {
                    ArchiveKey = unsignedDocumentArchiveKey,
                    FileName = unsignedDocument.DocumentFileName
                },
                ClosedDate = closedDate,
                ClosedMessage = closedMessage,
                CustomData = session.CustomData,
                SignatureProviderName = "signicat",
                SignedPdf = signedDocument == null ? null : new CommonElectronicIdSignatureSession.PdfModel
                {
                    ArchiveKey = CacheSignicatPdfInArchiveLocally(session.Id, signedDocument.SignedDocumentKey, context, documentClient, signicatClient),
                    FileName = signedDocument.CombinationFileName
                },
                SigningCustomersBySignerNr = session.SigningCustomersByApplicantNr.ToDictionary(x => x.Key, x => new CommonElectronicIdSignatureSession.SigningCustomer
                {
                    SignerNr = x.Value.ApplicantNr,
                    FirstName = null,
                    LastName = null,
                    CivicRegNr = x.Value.CivicRegNr,
                    SignedDateUtc = x.Value.SignedDateUtc,
                    SignatureUrl = session.GetActiveSignatureUrlForApplicant(x.Value.ApplicantNr)
                })
            };
        }

        private string CacheSignicatPdfInArchiveLocally(string sessionId, string signicatDocumentId, ICustomerContextExtended context, DocumentClient documentClient, SignicatSigningClient signicatSigningClient)
        {
            const string KeySpace = "SignicatDocumentIdToArchiveKeyMapping";
            var key = $"{sessionId}#{signicatDocumentId}";
            var archiveKey = KeyValueStoreService.GetValueComposable(context, key, KeySpace);
            if (archiveKey != null)
                return archiveKey;

            var document = signicatSigningClient.GetDocument(signicatDocumentId);

            archiveKey = documentClient.ArchiveStore(Convert.FromBase64String(document.DocumentDataBase64), document.DocumentMimeType, document.DocumentDownloadName);

            KeyValueStoreService.SetValueComposable(context, key, KeySpace, archiveKey, context.CurrentUser, CoreClock);

            return archiveKey;
        }

        private static Dictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(params Dictionary<TKey, TValue>[] dicts)
        {
            if (dicts == null)
                return null;

            var result = new Dictionary<TKey, TValue>();
            foreach (var d in dicts.Where(x => x != null))
                foreach (var kvp in d)
                    result[kvp.Key] = kvp.Value;

            return result;
        }
    }
}