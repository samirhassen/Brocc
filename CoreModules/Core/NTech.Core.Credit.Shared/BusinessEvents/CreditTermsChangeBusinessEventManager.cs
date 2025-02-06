using nCredit.Code.Services;
using nCredit.DbModel.Repository;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CreditTermsChangeBusinessEventManager : CreditTermsChangeCancelOnlyBusinessEventManager
    {
        private readonly LegalInterestCeilingService legalInterestCeilingService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly ILoggingService loggingService;
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly Func<string, AffiliateModel> getAffiliate;

        public CreditTermsChangeBusinessEventManager(INTechCurrentUserMetadata currentUser,
            LegalInterestCeilingService legalInterestCeilingService, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, INTechEmailServiceFactory emailServiceFactory,
            ICustomerClient customerClient, ILoggingService loggingService, INTechServiceRegistry serviceRegistry,
            Func<string, AffiliateModel> getAffiliate) : base(currentUser, clock, clientConfiguration, creditContextFactory, customerClient)
        {
            this.legalInterestCeilingService = legalInterestCeilingService;
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.emailServiceFactory = emailServiceFactory;
            this.loggingService = loggingService;
            this.serviceRegistry = serviceRegistry;
            this.getAffiliate = getAffiliate;
        }

        public class TermsChangeData
        {
            public decimal AnnuityAmount { get; set; }
            public decimal MonthlyAmount { get; set; }
            public decimal NotificationFee { get; set; }
            public int NrOfRemainingPayments { get; set; }
            public decimal MarginInterestRatePercent { get; set; }
            public decimal TotalInterestRatePercent { get; set; }
            public decimal EffectiveInterestRatePercent { get; set; }
            public decimal TotalPaidAmount { get; set; }

            public decimal OriginalAnnuityAmount { get; set; }
            public decimal OriginalMonthlyAmount { get; set; }
            public int? OriginalNrOfRemainingPayments { get; set; }
            public decimal OriginalMarginInterestRatePercent { get; set; }
            public decimal OriginalTotalInterestRatePercent { get; set; }
        }

        public class PendingChangeData : TermsChangeData
        {
            public int Id { get; set; }
            public DateTime SentDate { get; set; }
            public List<SignatureItem> Signatures { get; set; }

            public class SignatureItem
            {
                public int ApplicantNr { get; set; }
                public string UnsignedDocumentKey { get; set; }
                public string SignedDocumentKey { get; set; }
                public DateTime? SignatureDate { get; set; }
                public string SignatureUrl { get; set; }
            }
        }

        public bool TryFetchPendingTermsChangeData(int id, bool allowInvalidOriginalTerms, out PendingChangeData d, out string failedMessage)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var h = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new
                    {
                        x.Id,
                        Items = x.Items.Select(y => new
                        {
                            y.Name,
                            y.Value,
                            y.ApplicantNr,
                            TransactionDate = y.CreatedByEvent.TransactionDate
                        }),
                        SentDate = x.CreatedByEvent.TransactionDate,
                        CreditNr = x.CreditNr,
                        NrOfApplicants = x.Credit.NrOfApplicants
                    }).Single(x => x.Id == id);

                var newMarginInterestRatePercent = decimal.Parse(h
                    .Items
                    .Single(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.NewMarginInterestRatePercent.ToString())
                    .Value, NumberStyles.Number, CultureInfo.InvariantCulture);
                var newRepaymentTimeInMonths = int.Parse(h
                    .Items
                    .Single(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.NewRepaymentTimeInMonths.ToString())
                    .Value);

                if (!TryComputeTermsChangeData(h.CreditNr, newRepaymentTimeInMonths, newMarginInterestRatePercent, allowInvalidOriginalTerms, out d, out failedMessage))
                {
                    return false;
                }

                var signedDocuments = h
                    .Items
                    .Where(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString())
                    .Select(x => new
                    {
                        ApplicantNr = x.ApplicantNr.Value,
                        DocumentKey = x.Value,
                        TransactionDate = x.TransactionDate
                    })
                    .ToDictionary(x => x.ApplicantNr);

                var signatures = h
                    .Items
                    .Where(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.UnsignedAgreementDocumentArchiveKey.ToString())
                    .Select(x => new PendingChangeData.SignatureItem
                    {
                        ApplicantNr = x.ApplicantNr.Value,
                        UnsignedDocumentKey = x.Value,
                        SignedDocumentKey = signedDocuments.ContainsKey(x.ApplicantNr.Value)
                            ? signedDocuments[x.ApplicantNr.Value].DocumentKey
                            : null,
                        SignatureDate = signedDocuments.ContainsKey(x.ApplicantNr.Value)
                            ? new DateTime?(signedDocuments[x.ApplicantNr.Value].TransactionDate)
                            : null,
                        SignatureUrl = null
                    })
                    .OrderBy(x => x.ApplicantNr)
                    .ToList();

                d.Id = h.Id;
                d.SentDate = h.SentDate;
                d.Signatures = signatures;

                foreach (var signature in signatures.Where(y => !y.SignatureDate.HasValue))
                {
                    try
                    {
                        var sessionKey = h.Items.Single(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureSessionKey.ToString() && x.ApplicantNr == signature.ApplicantNr).Value;
                        var session = customerClient.GetElectronicIdSignatureSession(sessionKey, false)?.Session;
                        signature.SignatureUrl = session?.GetActiveSignatureUrlBySignerNr()?.Opt(1);
                    }
                    catch (Exception ex)
                    {
                        loggingService.Warning(ex, $"Could not get signature url for term changes on credit {h.CreditNr}");
                    }
                }
                return true;
            }
        }

        public bool TryComputeTermsChangeData<T>(string creditNr, int newRepaymentTimeInMonths, decimal newMarginInterestRatePercent, bool allowInvalidOriginalTerms, out T termsChangeData, out string failedMessage) where T : TermsChangeData, new()
        {
            newMarginInterestRatePercent = Math.Round(newMarginInterestRatePercent, 2);

            if (newRepaymentTimeInMonths <= 0 || newMarginInterestRatePercent <= 0m)
            {
                failedMessage = "Missing or invalid newRepaymentTimeInMonths and/or newMarginInterestRatePercent";
                termsChangeData = null;
                return false;
            }

            if (IsRejectedMarginInterestRate(newMarginInterestRatePercent))
            {
                failedMessage = "Margin interest rate outside the allowed range.";
                termsChangeData = null;
                return false;
            }

            HistoricalCreditModel model;
            using (var context = creditContextFactory.CreateContext())
            {
                model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, envSettings.IsMortgageLoansEnabled);
            }
            if (model.Status != CreditStatus.Normal.ToString())
            {
                failedMessage = $"Invalid status: {model.Status}";
                termsChangeData = null;
                return false;
            }

            var currentNotNotifiedCapitalBalance = model
                .Transactions
                .Where(x => x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                .Aggregate(0m, (a, b) => a + b.Amount);

            var am = model.AmortizationModel;
            if (model.AmortizationModel.AmortizationExceptionUntilDate.HasValue || model.AmortizationModel.AmortizationFreeUntilDate.HasValue)
                throw new Exception("Amortization freedom/exception not supported for term changes");

            var newTotalInterestRatePercent = model.ReferenceInterestRatePercent + legalInterestCeilingService.GetConstrainedMarginInterestRate(model.ReferenceInterestRatePercent, newMarginInterestRatePercent);
            var result = model.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital(annuityAmount => new
            {
                terms = PaymentPlanCalculation
                        .BeginCreateWithRepaymentTime(currentNotNotifiedCapitalBalance, newRepaymentTimeInMonths, newTotalInterestRatePercent, true, null, envSettings.CreditsUse360DayInterestYear)
                        .WithMonthlyFee(model.NotificationFee)
                        .EndCreate(),
                originalTerms = PaymentPlanCalculation
                        .BeginCreateWithAnnuity(
                            currentNotNotifiedCapitalBalance,
                            annuityAmount,
                            model.ReferenceInterestRatePercent + (model.MarginInterestRatePercent ?? 0m),
                            null,
                            envSettings.CreditsUse360DayInterestYear)
                        .WithMonthlyFee(model.NotificationFee)
                        .EndCreate()
            }, fixedMonthlyPaymentAmount =>
            {
                //This is not just added since we need the remaining number of months rather than the original
                throw new Exception("Need to support month count cap here for fixed months to work!");
            });

            var originalTerms = result.originalTerms;
            var terms = result.terms;

            string paymentPlanFailedMessage;
            int? originalNrOfRemainingPayments = null;
            if (!originalTerms.TryPrefetchPayments(out paymentPlanFailedMessage))
            {
                if (!allowInvalidOriginalTerms)
                    throw new Exception(paymentPlanFailedMessage);
            }
            else
            {
                originalNrOfRemainingPayments = originalTerms.Payments.Count;
            }

            termsChangeData = new T
            {
                AnnuityAmount = terms.AnnuityAmount,
                MonthlyAmount = terms.AnnuityAmount + model.NotificationFee,
                NotificationFee = model.NotificationFee,
                NrOfRemainingPayments = terms.Payments.Count,
                MarginInterestRatePercent = newMarginInterestRatePercent,
                TotalInterestRatePercent = newMarginInterestRatePercent + model.ReferenceInterestRatePercent,
                EffectiveInterestRatePercent = terms.EffectiveInterestRatePercent.Value,
                TotalPaidAmount = terms.TotalPaidAmount,

                OriginalAnnuityAmount = originalTerms.AnnuityAmount,
                OriginalMonthlyAmount = originalTerms.AnnuityAmount + model.NotificationFee,
                OriginalNrOfRemainingPayments = originalNrOfRemainingPayments,
                OriginalMarginInterestRatePercent = model.MarginInterestRatePercent ?? 0m,
                OriginalTotalInterestRatePercent = model.ReferenceInterestRatePercent + (model.MarginInterestRatePercent ?? 0m)
            };
            failedMessage = null;
            return true;
        }

        public (bool IsSuccess, string WarningMessage, CreditTermsChangeHeader TermChange) StartCreditTermsChange(
            string creditNr, int newRepaymentTimeInMonths, decimal newMarginInterestRatePercent,
            Func<IDocumentRenderer> createDocumentRenderer,
            Action<string, int> observeSignatureLinkAndApplicantNr = null)
        {
            string successUserWarningMessage = null;
            bool didEmailFail = false;

            TermsChangeData tc;

            if (IsRejectedMarginInterestRate(newMarginInterestRatePercent))
                return (IsSuccess: false, WarningMessage: "Margin interest rate outside the allowed range.", TermChange: null);

            if (!TryComputeTermsChangeData(creditNr, newRepaymentTimeInMonths, newMarginInterestRatePercent, true, out tc, out var failedMessage))
                return (IsSuccess: false, WarningMessage: failedMessage, TermChange: null);

            using (var context = creditContextFactory.CreateContext())
            {
                var evt = this.AddBusinessEvent(BusinessEventType.StartedCreditTermsChange, context);

                var localH = new CreditTermsChangeHeader
                {
                    CreatedByEvent = evt,
                    CreditNr = creditNr,
                    AutoExpireDate = Clock.Today.Date.AddDays(envSettings.NrOfDaysUntilCreditTermsChangeOfferExpires),
                    ChangedById = this.UserId,
                    ChangedDate = this.Clock.Now,
                    InformationMetaData = this.InformationMetadata
                };

                Action<int?, CreditTermsChangeItem.CreditTermsChangeItemCode, string> addItem = (applicantNr, name, value) =>
                {
                    context.AddCreditTermsChangeItems(new CreditTermsChangeItem
                    {
                        ApplicantNr = applicantNr,
                        CreatedByEvent = evt,
                        CreditTermsChange = localH,
                        ChangedById = this.UserId,
                        ChangedDate = this.Clock.Now,
                        InformationMetaData = this.InformationMetadata,
                        Name = name.ToString(),
                        Value = value
                    });
                };

                addItem(
                    null,
                    CreditTermsChangeItem.CreditTermsChangeItemCode.NewMarginInterestRatePercent,
                    tc.MarginInterestRatePercent.ToString(CultureInfo.InvariantCulture));

                addItem(
                    null,
                    CreditTermsChangeItem.CreditTermsChangeItemCode.NewRepaymentTimeInMonths,
                    tc.NrOfRemainingPayments.ToString(CultureInfo.InvariantCulture));

                var credit = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        CreditCustomers = x.CreditCustomers,
                        ProviderName = x.ProviderName
                    })
                    .Single();

                var customerInfos = this.LoadAgreementApplicantInfo(credit.CreditCustomers.ToDictionary(x => x.ApplicantNr, x => x.CustomerId));
                var attachmentArchiveKeys = new List<string>();
                using (var documentRenderer = createDocumentRenderer())
                {
                    foreach (var applicant in credit.CreditCustomers)
                    {
                        //Create agreement
                        var printContext = GetCreditChangeTermsAgreementContext(creditNr, credit.CreditCustomers.Count, applicant.ApplicantNr, customerInfos, credit.ProviderName, tc);

                        var archiveKey = documentRenderer.RenderDocumentToArchive("credit-agreement-changeterms", printContext, $"changeterms-{creditNr}-{applicant.ApplicantNr}-{Clock.Today.ToString("yyyy-MM-dd")}.pdf");
                        attachmentArchiveKeys.Add(archiveKey);
                        addItem(applicant.ApplicantNr, CreditTermsChangeItem.CreditTermsChangeItemCode.UnsignedAgreementDocumentArchiveKey, archiveKey);

                        //Create signature session
                        var customer = customerInfos[applicant.ApplicantNr];
                        var civicRegNr = customer.civicRegNr;

                        var callbackToken = Guid.NewGuid().ToString();
                        var serverToServerCallbackUrl =
                            serviceRegistry.InternalServiceUrl("nCredit", $"Api/Credit/ChangeTerms/SignaturePostback/{callbackToken}");
                        var (redirectAfterSuccessUrl, redirectAfterFailedUrl) = GetRedirectUrls();

                        var session = WithCreditNrOnExceptionR(() =>
                        {
                            return customerClient.CreateElectronicIdSignatureSession(new NTech.ElectronicSignatures.SingleDocumentSignatureRequestUnvalidated
                            {
                                DocumentToSignArchiveKey = archiveKey,
                                DocumentToSignFileName = $"change-terms-{creditNr}.pdf",
                                RedirectAfterSuccessUrl = redirectAfterSuccessUrl,
                                RedirectAfterFailedUrl = redirectAfterFailedUrl,
                                ServerToServerCallbackUrl = serverToServerCallbackUrl.ToString(),
                                SigningCustomers = new List<NTech.ElectronicSignatures.SingleDocumentSignatureRequestUnvalidated.SigningCustomer>
                            {
                                new NTech.ElectronicSignatures.SingleDocumentSignatureRequestUnvalidated.SigningCustomer
                                {
                                    SignerNr = 1,
                                    CivicRegNr = civicRegNr
                                }
                            },
                                CustomData = new Dictionary<string, string> { }
                            });
                        }, creditNr);
                        var signatureUrl = session.GetActiveSignatureUrlBySignerNr().Opt(1);
                        var sessionKey = session.Id;
                        var providerName = session.SignatureProviderName;

                        addItem(applicant.ApplicantNr, CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureCallbackToken, callbackToken);
                        addItem(applicant.ApplicantNr, CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureSessionKey, sessionKey);
                        addItem(applicant.ApplicantNr, CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureProviderName, session.SignatureProviderName);

                        observeSignatureLinkAndApplicantNr?.Invoke(signatureUrl, applicant.ApplicantNr);

                        if (emailServiceFactory.HasEmailProvider)
                        {
                            //Send email with link
                            var emailService = emailServiceFactory.CreateEmailService();
                            try
                            {
                                emailService.SendTemplateEmail(
                                    new List<string>() { customer.email },
                                    "credit-agreement-changeterms",
                                    new Dictionary<string, string> { { "link", signatureUrl } },
                                    evt.EventType);
                            }
                            catch (Exception ex)
                            {
                                loggingService.Error(ex, $"Change terms email failed for {creditNr}");
                                didEmailFail = true;
                            }
                        }
                    }
                }

                string deliveryText;// = emailServiceFactory.HasEmailProvider ? "agreements sent for signing" : "agreemeent signature links created";
                if (!emailServiceFactory.HasEmailProvider || didEmailFail)
                {
                    deliveryText = "agreement created but email could not be sent";
                    if (didEmailFail)
                        successUserWarningMessage = "Change terms - agreement created but email could not be sent.";
                }
                else
                    deliveryText = "agreement created and email sent";

                AddComment($"Change terms - {deliveryText}. Offered margin interest rate {tc.MarginInterestRatePercent.ToString(CommentFormattingCulture)} % and repayment time {tc.NrOfRemainingPayments.ToString(CommentFormattingCulture)} months. Offer expires {localH.AutoExpireDate.Value.ToString("d", CommentFormattingCulture)}", BusinessEventType.StartedCreditTermsChange, context, creditNr: creditNr, attachment: new CreditCommentAttachmentModel { archiveKeys = attachmentArchiveKeys }, evt: evt);

                //Cancel any current
                var allCurrent = context
                    .CreditTermsChangeHeadersQueryable
                    .Where(x => x.CreditNr == creditNr && !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue)
                    .ToList();
                foreach (var c in allCurrent)
                {
                    c.CancelledByEvent = evt;
                }

                context.AddCreditTermsChangeHeaders(localH);

                context.SaveChanges();

                return (IsSuccess: true, WarningMessage: successUserWarningMessage, TermChange: localH);
            }
        }

        public bool TryUpdateTermsChangeOnSignatureEvent(string signatureCallbackToken, string signatureEventName, string signatureErrorMessage, out string failedMessage)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var result = TryUpdateTermsChangeOnSignatureEventI(context, signatureCallbackToken, signatureEventName, signatureErrorMessage, out failedMessage);
                context.SaveChanges();
                return result;
            }
        }

        private bool TryUpdateTermsChangeOnSignatureEventI(ICreditContextExtended context, string signatureCallbackToken, string signatureEventName, string signatureErrorMessage, out string failedMessage)
        {
            var h = context
                .CreditTermsChangeHeadersQueryable
                .Select(x => new
                {
                    Header = x,
                    Items = x.Items,
                    Credit = x.Credit,
                    HitTokenItem = x
                        .Items
                        .Where(y => y.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureCallbackToken.ToString() && y.Value == signatureCallbackToken)
                        .FirstOrDefault()
                })
                .Where(x => x.HitTokenItem != null)
                .SingleOrDefault();

            if (h == null)
            {
                failedMessage = "No terms change exists with that token";
                return false;
            }

            if (signatureEventName == "Success" || signatureEventName == "Failed")
            {
                var failedEventType = BusinessEventType.AddedSignedAgreementToCreditTermsChange + "_Failed";
                if (h.Header.CancelledByEventId.HasValue)
                {
                    failedMessage = "This signing session has been cancelled";
                    AddComment($"Change terms - Signature callback for applicant {h.HitTokenItem.ApplicantNr} failed since the term change is cancelled.", failedEventType, h.Credit, context);
                    return false;
                }
                if (h.Header.CommitedByEventId.HasValue)
                {
                    failedMessage = "This signing session has been commited";
                    AddComment($"Change terms - Signature callback for applicant {h.HitTokenItem.ApplicantNr} failed since the term change is commited.", failedEventType, h.Credit, context);
                    return false;
                }
                var signatureSessionKeyItem = h.Items.Single(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignatureSessionKey.ToString() && x.ApplicantNr == h.HitTokenItem.ApplicantNr);


                var signatureSession = WithCreditNrOnExceptionR(() => customerClient.GetElectronicIdSignatureSession(signatureSessionKeyItem.Value, false), h.Header.CreditNr)?.Session;

                if (signatureSession == null)
                {
                    failedMessage = "Session does not exist";
                    AddComment($"Change terms - Sesion does not exist", failedEventType, h.Credit, context);
                    return false;
                }
                if (!signatureSession.HaveAllSigned())
                {
                    return TryCancelCreditTermsChange(
                        h.Header.Id,
                        false,
                        out failedMessage,
                        additionalReasonMessage: $" because applicant {h.HitTokenItem.ApplicantNr} failed to sign: {signatureSession?.ClosedMessage}");
                }
                if (signatureSession.SignedPdf == null)
                {
                    failedMessage = "Missing signed document";
                    AddComment($"Change terms - Signature callback for applicant {h.HitTokenItem.ApplicantNr} failed. Signed document url is missing.", failedEventType, h.Credit, context);
                    return false;
                }
                if (h.Items.Any(x => x.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString() && x.ApplicantNr == h.HitTokenItem.ApplicantNr))
                {
                    failedMessage = "Applicant already signed";
                    AddComment($"Change terms - Signature callback for applicant {h.HitTokenItem.ApplicantNr} failed. Applicant already signed.", failedEventType, h.Credit, context);
                    return false;
                }

                var documentFileName = $"changeterms-{h.Header.CreditNr}-{h.HitTokenItem.ApplicantNr}-{Clock.Today.ToString("yyyy-MM-dd")}-signed.pdf";

                var evt = AddBusinessEvent(BusinessEventType.AddedSignedAgreementToCreditTermsChange, context);

                context.AddCreditTermsChangeItems(new CreditTermsChangeItem
                {
                    ApplicantNr = h.HitTokenItem.ApplicantNr,
                    CreatedByEvent = evt,
                    CreditTermsChange = h.Header,
                    Name = CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString(),
                    Value = signatureSession.SignedPdf.ArchiveKey,
                    InformationMetaData = this.InformationMetadata,
                    ChangedById = this.UserId,
                    ChangedDate = this.Clock.Now,
                });

                AddComment(
                    $"Change terms - agreement signed by applicant {h.HitTokenItem.ApplicantNr}",
                    BusinessEventType.AddedSignedAgreementToCreditTermsChange, context,
                    creditNr: h.Credit.CreditNr,
                    attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(new List<string> { signatureSession.SignedPdf.ArchiveKey }));

                failedMessage = null;
                return true;
            }
            else
            {
                loggingService.Information($"Credit change terms event ignored '{signatureEventName}' '{signatureCallbackToken}'");
                failedMessage = null;
                return true;
            }
        }

        public void AutoCancelOldPendingTermChanges()
        {
            IList<int> idsToCancel;
            var today = Clock.Today.Date;
            using (var context = creditContextFactory.CreateContext())
            {
                idsToCancel = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new
                    {
                        Header = x,
                        x.Credit.NrOfApplicants,
                        NrOfSignatures = x
                            .Items
                            .Where(y => y.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString())
                            .Select(y => y.ApplicantNr)
                            .Distinct()
                            .Count()
                    })
                    .Where(x =>
                        !x.Header.CommitedByEventId.HasValue
                        && !x.Header.CancelledByEventId.HasValue
                        && (x.Header.AutoExpireDate.HasValue && x.Header.AutoExpireDate.Value <= today)
                        && x.NrOfApplicants != x.NrOfSignatures)
                    .Select(x => x.Header.Id)
                    .ToList();
            }

            foreach (var id in idsToCancel)
            {
                string failedMessage;
                if (!TryCancelCreditTermsChange(id, false, out failedMessage, additionalReasonMessage: " because the change has been pending for too long."))
                {
                    loggingService.Warning($"Change terms - AutoCancelOldUnfinisedPendingTermChanges failed for Id={id}, Message={failedMessage}");
                }
            }
        }

        public bool TryAcceptCreditTermsChange(int id, out string failedMessage)
        {
            PendingChangeData pd;
            if (!TryFetchPendingTermsChangeData(id, true, out pd, out failedMessage))
                return false;

            using (var context = creditContextFactory.CreateContext())
            {
                var h = context
                    .CreditTermsChangeHeadersQueryable
                    .Select(x => new
                    {
                        Header = x,
                        Credit = x.Credit
                    })
                    .Single(x => x.Header.Id == id);


                if (h.Credit.Status != CreditStatus.Normal.ToString())
                {
                    failedMessage = $"Invalid credit status: {h.Credit.Status}";
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.AcceptedCreditTermsChange, context);

                h.Header.CommitedByEvent = evt;

                var creditData = new PartialCreditModelRepository().NewQuery(Clock.Today)
                    .WithValues(DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate, DatedCreditValueCode.RequestedMarginInterestRate)
                    .Execute(context, x => x.Where(y => y.Credit.CreditNr == h.Credit.CreditNr))
                    .Single();

                var currentMarginInterestRate = creditData.GetValue(DatedCreditValueCode.MarginInterestRate).Value;
                var interestChange = this.legalInterestCeilingService.HandleMarginInterestRateChange(
                    creditData.GetValue(DatedCreditValueCode.ReferenceInterestRate) ?? 0m,
                    creditData.GetValue(DatedCreditValueCode.RequestedMarginInterestRate),
                    currentMarginInterestRate,
                    pd.MarginInterestRatePercent);

                var additionalCommentText = "";
                if (interestChange.NewMarginInterestRate.HasValue)
                {
                    AddDatedCreditValue(DatedCreditValueCode.MarginInterestRate.ToString(), interestChange.NewMarginInterestRate.Value, h.Credit, evt, context);
                    additionalCommentText += $", New Margin Interest={(interestChange.NewMarginInterestRate.Value / 100m).ToString("P", CommentFormattingCulture)}";
                }
                if (interestChange.NewRequestedMarginInterestRate.HasValue)
                {
                    AddDatedCreditValue(DatedCreditValueCode.RequestedMarginInterestRate.ToString(), interestChange.NewRequestedMarginInterestRate.Value, h.Credit, evt, context);
                    additionalCommentText += $", New Requested Margin Interest={(interestChange.NewRequestedMarginInterestRate.Value / 100m).ToString("P", CommentFormattingCulture)}";
                }

                AddComment($"Change terms - accepted. " + additionalCommentText, BusinessEventType.AcceptedCreditTermsChange, context, creditNr: h.Credit.CreditNr);
                AddDatedCreditValue(DatedCreditValueCode.AnnuityAmount.ToString(), pd.AnnuityAmount, h.Credit, evt, context);

                if (pd.Signatures != null)
                {
                    foreach (var signaure in pd.Signatures)
                    {
                        if (signaure?.SignedDocumentKey != null)
                            AddCreditDocument("ChangeTermsAgreement", signaure.ApplicantNr, signaure.SignedDocumentKey, context, credit: h.Credit);
                    }
                }


                context.SaveChanges();

                failedMessage = null;

                return true;
            }
        }

        public List<string> GetCreditNrsWithPendingTermChangesSignedByAllApplicants()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return context
                    .CreditTermsChangeHeadersQueryable
                    .Where(x => !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue)
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.Credit.NrOfApplicants,
                        NrOfSignatures = x
                            .Items
                            .Where(y => y.Name == CreditTermsChangeItem.CreditTermsChangeItemCode.SignedAgreementDocumentArchiveKey.ToString()).Select(y => y.ApplicantNr)
                            .Distinct()
                            .Count()
                    })
                    .Where(x => x.NrOfApplicants == x.NrOfSignatures)
                    .Select(x => x.CreditNr)
                    .ToList()
                    .Distinct()
                    .ToList();
            }
        }

        private class AgreementApplicantInfo
        {
            public string civicRegNr { get; set; }
            public string fullName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
        }

        private Dictionary<int, AgreementApplicantInfo> LoadAgreementApplicantInfo(Dictionary<int, int> customerIdByApplicantId)
        {
            var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(
                new HashSet<int>(customerIdByApplicantId.Values),
                "civicRegNr", "phone", "email", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry");

            var result = new Dictionary<int, AgreementApplicantInfo>();
            foreach (var c in customerIdByApplicantId)
            {
                var applicantNr = c.Key;
                var customerId = c.Value;

                var items = customerData[customerId];

                result[applicantNr] = new AgreementApplicantInfo
                {
                    civicRegNr = items.Opt("civicRegNr"),
                    fullName = ((items.Opt("firstName") ?? "") + " " + (items.Opt("lastName") ?? "")).Trim(),
                    streetAddress = items.Opt("addressStreet"),
                    areaAndZipcode = ((items.Opt("addressZipcode") ?? "") + " " + (items.Opt("addressCity") ?? "")).Trim(),
                    phone = items.Opt("phone"),
                    email = items.Opt("email")
                };
            }

            return result;
        }

        private Dictionary<string, object> GetCreditChangeTermsAgreementContext(
            string creditNr, int nrOfApplicants, int currentApplicantNr, IDictionary<int, AgreementApplicantInfo> applicantInfoByApplicantNr,
            string providerName, TermsChangeData tc)
        {
            var d = new Dictionary<string, object>();

            //NOTE: Applicant nr currently not used as both applicants are shown the same agreement

            d["agreementDate"] = this.Clock.Today.ToString("d", this.PrintFormattingCulture);
            d["loanNumber"] = creditNr;

            d["beforeChangeRepaymentTimeInMonths"] = tc.OriginalNrOfRemainingPayments?.ToString() ?? "-";
            d["repaymentTimeInMonths"] = tc.NrOfRemainingPayments.ToString();

            d["beforeChangeMonthlyPayment"] = tc.OriginalMonthlyAmount.ToString("C", this.PrintFormattingCulture);
            d["monthlyPayment"] = tc.MonthlyAmount.ToString("C", this.PrintFormattingCulture);

            d["beforeChangeMarginInterestRate"] = (tc.OriginalMarginInterestRatePercent / 100m).ToString("P", this.PrintFormattingCulture);
            d["marginInterestRate"] = (tc.MarginInterestRatePercent / 100m).ToString("P", this.PrintFormattingCulture);
            if (tc.MarginInterestRatePercent != tc.OriginalMarginInterestRatePercent)
                d["ismarginInterestRateChanged"] = true;

            d["referenceInterestRate"] = ((tc.TotalInterestRatePercent - tc.MarginInterestRatePercent) / 100m).ToString("P", this.PrintFormattingCulture);
            d["totalInterestRate"] = (tc.TotalInterestRatePercent / 100m).ToString("P", this.PrintFormattingCulture);
            d["effectiveInterestRate"] = (tc.EffectiveInterestRatePercent / 100m).ToString("P", this.PrintFormattingCulture);
            d["totalPaidAmount"] = tc.TotalPaidAmount.ToString("C", this.PrintFormattingCulture);
            d["notificationFee"] = tc.NotificationFee.ToString("C", this.PrintFormattingCulture);

            //NOTE: Both applicant1, applicant2 and applicants[] to support older templates
            foreach (var a in Enumerable.Range(1, nrOfApplicants))
            {
                d[$"applicant{a}"] = applicantInfoByApplicantNr[a];
            }
            d["applicants"] = Enumerable.Range(1, nrOfApplicants).Select(x => applicantInfoByApplicantNr[x]).ToList();

            var affiliate = getAffiliate(providerName);
            d["affiliate"] = affiliate.IsSelf ? null : new { displayToEnduserName = affiliate.DisplayToEnduserName };

            return d;
        }

        private bool IsRejectedMarginInterestRate(decimal? marginInterestRatePercent)
        {
            if (!marginInterestRatePercent.HasValue)
                return false; //Missing data is handled elsewhere

            var m = envSettings.MinAndMaxAllowedMarginInterestRate;
            if (m == null || (!m.Item1.HasValue && !m.Item2.HasValue))
                return false;

            if (m.Item1.HasValue && marginInterestRatePercent.Value < m.Item1.Value)
                return true;

            if (m.Item2.HasValue && marginInterestRatePercent.Value > m.Item2.Value)
                return true;

            return false;
        }

        private (string redirectAfterSuccessUrl, string redirectAfterFailedUrl) GetRedirectUrls()
        {
            bool isSignatureProviderSignicat2 = envSettings.EidSignatureProviderCode == SignatureProviderCode.signicat2;
            var redirectAfterSuccessUrl = isSignatureProviderSignicat2 ?
                serviceRegistry.ExternalServiceUrl("nCustomerPages", "signature-result-redirect/{localSessionId}/success").ToString()
                : serviceRegistry.ExternalServiceUrl("nCustomerPages", "signature-result/success").ToString();
            var redirectAfterFailedUrl = isSignatureProviderSignicat2 ?
                serviceRegistry.ExternalServiceUrl("nCustomerPages", "signature-result-redirect/{localSessionId}/failure").ToString()
                : serviceRegistry.ExternalServiceUrl("nCustomerPages", "signature-result/failure").ToString();

            return (redirectAfterSuccessUrl, redirectAfterFailedUrl);
        }
    }
}