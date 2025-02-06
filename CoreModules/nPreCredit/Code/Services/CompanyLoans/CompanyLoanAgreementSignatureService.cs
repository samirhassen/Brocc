using nPreCredit.Code.Clients;
using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure.Email;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanAgreementSignatureService : ICompanyLoanAgreementSignatureService
    {
        private readonly ICompanyLoanWorkflowService companyLoanWorkflowService;
        private readonly ICustomerClient customerClient;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;
        private readonly ICreditApplicationCustomerListService creditApplicationCustomerListService;
        private readonly INTechEmailService emailService;
        private readonly IDocumentClient documentClient;
        private readonly IServiceRegistryUrlService serviceRegistryUrlService;
        private readonly ILockedAgreementService lockedAgreementService;
        private readonly IApplicationCommentServiceComposable applicationCommentService;
        private readonly DocumentDatabase<CompanyLoanSignatureSessionModel> signatureSessionDatabase;

        public CompanyLoanAgreementSignatureService(ICompanyLoanWorkflowService companyLoanWorkflowService, ICustomerClient customerClient, INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock, ICreditApplicationCustomerListService creditApplicationCustomerListService, IKeyValueStoreService keyValueStoreService, INTechEmailService emailService, IDocumentClient documentClient, IServiceRegistryUrlService serviceRegistryUrlService, ILockedAgreementService lockedAgreementService, IApplicationCommentServiceComposable applicationCommentService)
        {
            this.companyLoanWorkflowService = companyLoanWorkflowService;
            this.customerClient = customerClient;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
            this.creditApplicationCustomerListService = creditApplicationCustomerListService;
            this.emailService = emailService;
            this.documentClient = documentClient;
            this.serviceRegistryUrlService = serviceRegistryUrlService;
            this.lockedAgreementService = lockedAgreementService;
            this.applicationCommentService = applicationCommentService;
            this.signatureSessionDatabase = new DocumentDatabase<CompanyLoanSignatureSessionModel>(KeyValueStoreKeySpaceCode.CompanyLoanSignatureSessionV1, keyValueStoreService);
        }

        /// <summary>
        /// This is intented to basically revert OnSignatureEvent having been run for all applicants successfully
        /// </summary>
        public void CancelSignedAgreementStep(ApplicationInfoModel applicationInfo)
        {
            if (!applicationInfo.IsActive)
                throw new NTechWebserviceMethodException("Application is not active") { ErrorCode = "applicationNotActive", IsUserFacing = true };

            var signAgreementStep = companyLoanWorkflowService.Model.FindStepByCustomData(x => x?.IsSignAgreement == "yes", new { IsSignAgreement = "" });
            if (!companyLoanWorkflowService.IsStepStatusAccepted(signAgreementStep.Name, applicationInfo.ListNames))
                throw new NTechWebserviceMethodException("Agreement step not accepted") { ErrorCode = "agreementStepNotAccepted", IsUserFacing = true };

            var nextStep = companyLoanWorkflowService.Model.FindNextStep(signAgreementStep.Name);
            if (companyLoanWorkflowService.GetListName(nextStep.Name, companyLoanWorkflowService.InitialStatusName) != companyLoanWorkflowService.GetEarliestInitialListName(applicationInfo.ListNames))
                throw new NTechWebserviceMethodException("Agreement is not the last accepted step") { ErrorCode = "notLastAcceptedStep", IsUserFacing = true };


            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var application = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationInfo.ApplicationNr);
                companyLoanWorkflowService.ChangeStepStatusComposable(context, signAgreementStep.Name, companyLoanWorkflowService.InitialStatusName, application: application);
                application.AgreementStatus = "Initial";
                var signedAgreementKeyItem = application.Items.SingleOrDefault(x => x.GroupName == "application" && x.Name == "signed_initial_agreement_key");
                CommentAttachment commentAttachment = null;
                if (signedAgreementKeyItem != null)
                {
                    //Keep a copy of the document around in case they do this by accident. They can then recover by just downloading it and using attach even if was
                    //signed by e-id
                    context.CreditApplicationItems.Remove(signedAgreementKeyItem);
                    documentClient.FetchRawWithFilename(signedAgreementKeyItem.Value, out var mimeType, out var filename);
                    commentAttachment = CommentAttachment.CreateFileFromArchiveKey(signedAgreementKeyItem.Value, mimeType, filename);
                }

                if (!applicationCommentService.TryAddCommentComposable(applicationInfo.ApplicationNr, $"Cancelled agreement signature step", "SignedAgreementCancelled", commentAttachment, out var failedMessage, context))
                    throw new Exception(failedMessage);

                //This will cause the signature session to respawn the same way it does when this state is initially made active
                signatureSessionDatabase.RemoveComposable(context, applicationInfo.ApplicationNr);

                context.SaveChanges();
            }
        }

        public CompanyLoanSignatureSessionModel OnSignatureEvent(ApplicationInfoModel applicationInfo, string directUploadDocumentArchiveKey = null, Action<bool> observeAgreementAccepted = null)
        {
            var existingSession = signatureSessionDatabase.Get(applicationInfo.ApplicationNr);

            if (existingSession == null)
                return existingSession;

            if (existingSession.HaveAllSignersSigned())
                return existingSession;

            if (directUploadDocumentArchiveKey != null)
            {
                existingSession = signatureSessionDatabase.UpdateOnlyConcurrent(applicationInfo.ApplicationNr, x =>
                {
                    foreach (var s in x.Static.Signers)
                    {
                        x.State.MergeSignedDate(s.CustomerId, clock.Now.DateTime);
                    }
                    x.State.SignedDocumentArchiveKey = directUploadDocumentArchiveKey;
                    return x;
                });
            }
            else
            {
                var signicatSignatureClient = SignicatSigningClientFactory.CreateClient();
                var signicatSession = signicatSignatureClient.GetSignatureSession(existingSession.State.SignatureSessionId);
                var anyNewSignatures = existingSession.Static.Signers.Any(x => !existingSession.HasSigned(x) && signicatSession.HasSigned(x.SignicatSessionApplicantNr.Value));
                if (anyNewSignatures)
                {
                    existingSession = signatureSessionDatabase.UpdateOnlyConcurrent(applicationInfo.ApplicationNr, x =>
                    {
                        foreach (var s in x.Static.Signers.Where(y => !x.HasSigned(y) && signicatSession.HasSigned(y.SignicatSessionApplicantNr.Value)))
                        {
                            x.State.MergeSignedDate(s.CustomerId, clock.Now.DateTime);
                        }

                        if (x.HaveAllSignersSigned() && string.IsNullOrWhiteSpace(x.State.SignedDocumentArchiveKey))
                        {
                            var documentUrl = signicatSession.GetSignedDocumentUrl(new ServiceRegistryLegacy(NEnv.ServiceRegistry));
                            var archiveKey = this.documentClient.ArchiveStore(documentUrl, $"Signed-Agreement-{applicationInfo.ApplicationNr}.pdf");
                            x.State.SignedDocumentArchiveKey = archiveKey;
                        }

                        return x;
                    });
                }
            }

            if (existingSession.HaveAllSignersSigned() && applicationInfo.AgreementStatus != "Accepted")
            {
                using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
                {
                    context.DoUsingTransaction(() =>
                    {
                        var h = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationInfo.ApplicationNr);
                        var evt = context.CreateAndAddEvent(CreditApplicationEventCode.CompanyLoanAgreementAccepted, creditApplicationHeader: h);
                        var signAgreementStep = companyLoanWorkflowService.Model.FindStepByCustomData(x => x?.IsSignAgreement == "yes", new { IsSignAgreement = "" });
                        companyLoanWorkflowService.ChangeStepStatusComposable(context, signAgreementStep.Name, "Accepted", application: h, evt: evt);
                        h.AgreementStatus = "Accepted";
                        context.CreateAndAddComment("Agreement signed", evt.EventType, creditApplicationHeader: h);
                        context.AddOrUpdateCreditApplicationItems(h, new List<PreCreditContextExtended.CreditApplicationItemModel>
                        {
                            new PreCreditContextExtended.CreditApplicationItemModel
                            {
                                GroupName = "application",
                                Name = "signed_initial_agreement_key",
                                Value = existingSession.State.SignedDocumentArchiveKey
                            }
                        }, evt.EventType);
                        context.SaveChanges();
                    });
                }

                observeAgreementAccepted?.Invoke(true);
            }

            return existingSession;
        }

        public CompanyLoanSignatureSessionModel CreateOrGetSignatureModel(ApplicationInfoModel applicationInfo,
            string overrideTemplateName = null, bool disableTemplateCache = false,
            bool refreshSignatureSessionIfNeeded = false, bool disableCheckForNewSignatures = false,
            Func<CompanyLoanSignatureSessionModel, bool, CompanyLoanSignatureSessionModel> intercept = null)
        {
            if (NEnv.SignatureProvider != SignatureProviderCode.signicat)
                throw new Exception("Signature provider must be signicat");

            const string CurrentVersion = "20190903.1";

            var existingSession = signatureSessionDatabase.Get(applicationInfo.ApplicationNr);

            var now = clock.Now.DateTime;

            if (existingSession != null)
            {
                var wasSignitcatSessionCreated = false;
                if (refreshSignatureSessionIfNeeded && existingSession.State.SignatureSessionExpirationDateUtc < DateTime.UtcNow)
                {
                    var customerIds = existingSession.Static.Signers.Select(x => x.CustomerId).ToHashSet();
                    var cd = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds, "civicRegNr", "lastName");

                    var pdfBytes = documentClient.FetchRawWithFilename(existingSession.Static.UnsignedDocumentArchiveKey, out var _, out var __);
                    var signicatSessionId = CreateNewSignicatSession(existingSession, () => pdfBytes, (x, y) => cd[x].Opt(y));
                    existingSession = signatureSessionDatabase.UpdateOnlyConcurrent(applicationInfo.ApplicationNr, x =>
                    {
                        x.State.SignatureSessionId = signicatSessionId;
                        x.State.SignatureSessionExpirationDateUtc = DateTime.UtcNow.AddDays(3);
                        return x;
                    });
                    wasSignitcatSessionCreated = true;
                }

                return intercept == null ? existingSession : intercept(existingSession, wasSignitcatSessionCreated);
            }
            else
            {
                EnsureStepsBeforeComplete(applicationInfo);

                //Borgensmännen och angivna Authorized signatory
                const string CollateralListName = "companyLoanCollateral";
                const string SignatoryListName = "companyLoanAuthorizedSignatory";
                var collateralCustomerIds = creditApplicationCustomerListService.GetMemberCustomerIds(applicationInfo.ApplicationNr, CollateralListName);
                var authorizedSignatoryCustomerIds = creditApplicationCustomerListService.GetMemberCustomerIds(applicationInfo.ApplicationNr, SignatoryListName);
                var customerIds = collateralCustomerIds
                    .Union(authorizedSignatoryCustomerIds)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var nameAndBirthdateByCustomerId = GetPersonFirstNameAndBirthDatesByCustomerIds(customerIds.ToHashSet(), customerClient, "lastName");

                var signers =
                    customerIds
                    .Select((customerId, index) =>
                    {
                        return new
                        {
                            ApplicantNr = index,
                            CustomerId = customerId,
                            FirstName = nameAndBirthdateByCustomerId[customerId].FirstName,
                            BirthDate = nameAndBirthdateByCustomerId[customerId].BirthDate
                        };
                    })
                    .ToList();

                var lockedAgreement = lockedAgreementService.GetLockedAgreement(applicationInfo.ApplicationNr);
                if (lockedAgreement == null)
                    throw new NTechWebserviceMethodException("Locked agreement missing") { IsUserFacing = true, ErrorCode = "missingLockedAgreement" };
                if (!lockedAgreement.ApprovedByUserId.HasValue)
                    throw new NTechWebserviceMethodException("Locked agreement is not approved") { IsUserFacing = true, ErrorCode = "lockedAgreementNotApproved" };

                var pdfBytes = this.documentClient.FetchRawWithFilename(lockedAgreement.UnsignedAgreementArchiveKey, out var contentType, out var filename);

                var model = new CompanyLoanSignatureSessionModel
                {
                    Static = new CompanyLoanSignatureSessionModel.StaticModel
                    {
                        UnsignedDocumentArchiveKey = lockedAgreement.UnsignedAgreementArchiveKey,
                        ApplicationNr = applicationInfo.ApplicationNr,
                        AlternateSignatureSessionId = GetSignatureSessionAlternateKey(applicationInfo.ApplicationNr),
                        Version = CurrentVersion,
                        Signers = signers.Select(x => new CompanyLoanSignatureSessionModel.SignerModel
                        {
                            CustomerId = x.CustomerId,
                            SignicatSessionApplicantNr = x.ApplicantNr,
                            FirstName = x.FirstName,
                            BirthDate = x.BirthDate,
                            ListMemberships =
                                (collateralCustomerIds.Contains(x.CustomerId) ? new List<string> { CollateralListName } : new List<string>())
                                .Union(authorizedSignatoryCustomerIds.Contains(x.CustomerId) ? new List<string> { SignatoryListName } : new List<string>())
                                .ToList()
                        }).ToList()
                    },
                    State = new CompanyLoanSignatureSessionModel.StateModel
                    {
                        LatestSentDateByCustomerId = new Dictionary<int, DateTime>(),
                        SignatureSessionId = null
                    }
                };

                model.State.SignatureSessionId = CreateNewSignicatSession(model, () => pdfBytes, (x, y) => nameAndBirthdateByCustomerId[x].GetExtraPropertyValue(y));
                model.State.SignatureSessionExpirationDateUtc = DateTime.UtcNow.AddDays(3);
                signatureSessionDatabase.InsertOnlyConcurrent(applicationInfo.ApplicationNr, model);

                return intercept == null ? model : intercept(model, true);
            }
        }

        private string CreateNewSignicatSession(CompanyLoanSignatureSessionModel model, Func<byte[]> getPdf, Func<int, string, string> getCustomerItem)
        {
            var signicatSignatureClient = SignicatSigningClientFactory.CreateClient();
            var session = signicatSignatureClient.StartSingleDocumentSignatureSession(new Clients.SignicatSigningClient.StartSingleDocumentSignatureSessionRequest
            {
                AlternateSessionKey = model.Static.AlternateSignatureSessionId,
                PdfBytesBase64 = Convert.ToBase64String(getPdf()),
                PdfDisplayFileName = "Agreement.pdf",
                ServerToServerCallbackUrl = Controllers.Api.ApiSignaturePostbackController.GetCallbackUrl().ToString(),
                RedirectAfterFailedUrl = serviceRegistryUrlService.ServiceRegistry
                    .ExternalServiceUrl("nCustomerPages", "a/#/signature/failed").ToString(),
                RedirectAfterSuccessUrl = serviceRegistryUrlService.ServiceRegistry
                    .ExternalServiceUrl("nCustomerPages", "a/#/signature/success").ToString(),
                SigningCustomersByApplicantNr = model.Static.Signers.ToDictionary(x => x.SignicatSessionApplicantNr.Value, x => new Clients.SignicatSigningClient.StartSingleDocumentSignatureSessionRequest.Customer
                {
                    ApplicantNr = x.SignicatSessionApplicantNr.Value,
                    CivicRegNr = getCustomerItem(x.CustomerId, "civicRegNr"),
                    FirstName = x.FirstName,
                    LastName = getCustomerItem(x.CustomerId, "lastName"),
                    SignicatLoginMethod = signicatSignatureClient.GetElectronicIdLoginMethod()
                }),
                CustomData = new Dictionary<string, string>
                {
                    { "ApplicationNr", model.Static.ApplicationNr },
                    { "SignatureSessionType", Controllers.Api.ApiSignaturePostbackController.SignatureSessionTypeCode.CompanyLoanInitialAgreementSignatureV1.ToString() }
                }
            });

            return session.Id;
        }

        public CompanyLoanSignatureSessionModel SendAgreementSignatureEmails(ApplicationInfoModel applicationInfo, ISet<int> onlyForTheseCustomerIds = null)
        {
            var model = this.CreateOrGetSignatureModel(applicationInfo);

            var signicatSignatureClient = SignicatSigningClientFactory.CreateClient();
            var signicatSession = signicatSignatureClient.GetSignatureSession(model.State.SignatureSessionId);

            var failedEmailSignerCustomerIds = new List<int>();
            var okEmailSignerCustomerIds = new List<int>();

            var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(model.Static.Signers.Select(x => x.CustomerId).ToHashSet(), "email");

            CompanyLoanSignatureSessionModel latestModel = model;
            foreach (var signer in model.Static.Signers.Where(x => onlyForTheseCustomerIds == null || onlyForTheseCustomerIds.Contains(x.CustomerId)))
            {
                try
                {
                    var email = customerData.Opt(signer.CustomerId).Opt("email");
                    if (string.IsNullOrWhiteSpace(email))
                        throw new Exception("Missing email");
                    var mines = new Dictionary<string, string>
                    {
                        { "link", signicatSession.GetActiveSignatureUrlForApplicant(signer.SignicatSessionApplicantNr.Value) }
                    };
                    emailService.SendTemplateEmail(new List<string> { email }, "companyloan-agreement-signature", mines, applicationInfo.ApplicationNr);
                    okEmailSignerCustomerIds.Add(signer.CustomerId);
                    latestModel = signatureSessionDatabase.UpdateOnlyConcurrent(applicationInfo.ApplicationNr, x =>
                    {
                        x.State.MergeLatestSentDate(signer.CustomerId, clock.Now.DateTime);
                        return x;
                    });
                }
                catch (Exception ex)
                {
                    NLog.Warning(ex, $"CompanyLoanAgreementService.CreateOrGetSignatureSession: {applicationInfo.ApplicationNr}");
                    failedEmailSignerCustomerIds.Add(signer.CustomerId);
                }
            }

            if (okEmailSignerCustomerIds.Any())
            {
                using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
                {
                    context.CreateAndAddComment($"Agreement signature link emailed to {okEmailSignerCustomerIds.Count} people.", "SentAgreementLink", applicationNr: applicationInfo.ApplicationNr);
                    context.SaveChanges();
                }
            }

            if (failedEmailSignerCustomerIds.Any())
            {
                using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
                {
                    context.CreateAndAddComment($"Failed to send the agreement signature link email to: " + string.Join(", ", failedEmailSignerCustomerIds.Select(x =>
                    {
                        var signer = model.Static.Signers.Single(y => y.CustomerId == x);
                        return $"{signer.FirstName}, {signer.BirthDate?.ToString("yyyy-MM-dd")}";
                    })), "FailedToSendAgreementLink", applicationNr: applicationInfo.ApplicationNr);
                    context.SaveChanges();
                }
            }

            return latestModel;
        }

        private string GetSignatureSessionAlternateKey(string applicationNr)
        {
            return $"CompanyLoanSignature_{applicationNr}";
        }

        public bool CancelAnyActiveSignatureSessionByApplicationNr(string applicationNr)
        {
            var wasCancelled = false;
            var session = signatureSessionDatabase.Get(applicationNr);
            if (session != null)
            {
                wasCancelled = true;
                signatureSessionDatabase.Remove(applicationNr);
                var signicatSignatureClient = SignicatSigningClientFactory.CreateClient();
                var alternateKey = GetSignatureSessionAlternateKey(applicationNr);
                signicatSignatureClient.TryCancelSignatureSessionByAlternateKey(alternateKey, out var _);
            }
            return wasCancelled;
        }

        private void EnsureStepsBeforeComplete(ApplicationInfoModel applicationInfo)
        {
            string lastIncompleteStepName = "Unknown";
            if (!companyLoanWorkflowService.AreAllStepsBeforeComplete("CompanyLoanAgreement", applicationInfo.ListNames, debugContext: applicationInfo.ApplicationNr, observeLastIncompleteStep: s => lastIncompleteStepName = s))
                throw new NTechWebserviceMethodException($"Print context not available until all steps up to CompanyLoanAgreement are accepted. Last incomplete: {lastIncompleteStepName}")
                {
                    ErrorCode = "wrongApplicationStatus",
                    ErrorHttpStatusCode = 400,
                    IsUserFacing = true
                };
        }

        public static Dictionary<int, (string FirstName, DateTime? BirthDate, Func<string, string> GetExtraPropertyValue)> GetPersonFirstNameAndBirthDatesByCustomerIds(ISet<int> customerIds, ICustomerClient customerClient, params string[] extraPropertyNames)
        {
            var cd = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds.ToHashSet(),
                new[] { "civicRegNr", "firstName", "birthDate" }.Concat(extraPropertyNames ?? Enumerable.Empty<string>()).Distinct().ToArray());

            return
                customerIds
                .ToDictionary(x => x, x =>
                {
                    var c = cd.Opt(x);
                    var birthDateRaw = c.Opt("birthDate");
                    DateOnly birthDate = null;

                    if (!string.IsNullOrWhiteSpace(birthDateRaw))
                        birthDate = Dates.ParseDateOnlyExactOrNull(birthDateRaw, "yyyy-MM-dd");
                    if (birthDate == null)
                    {
                        NEnv.BaseCivicRegNumberParser.Parse(c["civicRegNr"]);
                        var civicRegNrRaw = c.Opt("civicRegNr");
                        if (civicRegNrRaw != null)
                            birthDate = DateOnly.Create(NEnv.BaseCivicRegNumberParser.Parse(civicRegNrRaw).BirthDate);
                    }

                    Func<string, string> getExtraPropertyValue = name => cd.Opt(x)?.Opt(name);

                    return (FirstName: c.Opt("firstName"), BirthDate: birthDate?.ToDate(), GetExtraPropertyValue: getExtraPropertyValue);
                });
        }
    }

    public interface ICompanyLoanAgreementSignatureService
    {
        CompanyLoanSignatureSessionModel SendAgreementSignatureEmails(ApplicationInfoModel applicationInfo, ISet<int> onlyForTheseCustomerIds = null);

        CompanyLoanSignatureSessionModel CreateOrGetSignatureModel(
            ApplicationInfoModel applicationInfo,
            string overrideTemplateName = null, bool disableTemplateCache = false,
            bool refreshSignatureSessionIfNeeded = false, bool disableCheckForNewSignatures = false,
            Func<CompanyLoanSignatureSessionModel, bool, CompanyLoanSignatureSessionModel> intercept = null);

        CompanyLoanSignatureSessionModel OnSignatureEvent(ApplicationInfoModel applicationInfo, string directUploadDocumentArchiveKey = null, Action<bool> observeAgreementAccepted = null);

        bool CancelAnyActiveSignatureSessionByApplicationNr(string applicationNr);
        void CancelSignedAgreementStep(ApplicationInfoModel applicationInfo);
    }
}