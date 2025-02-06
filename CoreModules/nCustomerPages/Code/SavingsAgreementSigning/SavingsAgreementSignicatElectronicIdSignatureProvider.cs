using nCustomerPages.Code.Clients;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomerPages.Code.ElectronicIdSignature
{
    public class SavingsAgreementSignicatElectronicIdSignatureProvider : ISavingsAgreementElectronicIdSignatureProvider
    {
        private readonly SavingsAgreementElectronicIdSignatureProviderFactory.GetExternalLink getExternalLink;
        private readonly ElectronicIdProviderCode signatureProviderCode;

        public SavingsAgreementSignicatElectronicIdSignatureProvider(SavingsAgreementElectronicIdSignatureProviderFactory.GetExternalLink getExternalLink, ElectronicIdProviderCode signatureProviderCode)
        {
            this.getExternalLink = getExternalLink;
            this.signatureProviderCode = signatureProviderCode;
        }

        public string StartSignatureSessionReturningSignatureUrl(string tempDataKey, byte[] pdfBytes, ICivicRegNumber civicRegNr, string documentDisplayName, string firstName, string lastName, string userLanguage)
        {
            if (signatureProviderCode == ElectronicIdProviderCode.Signicat2)
            {
                ICustomerClient customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                IDocumentClient docClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);

                var file = docClient.ArchiveStore(pdfBytes, "application/pdf", "SavingsAgreement.pdf");
                var returnPage = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "savings/{localSessionId}/standard-application-aftersign").ToString();
                var signingCustomer = new SingleDocumentSignatureRequestUnvalidated.SigningCustomer { SignerNr = 1, CivicRegNr = civicRegNr.NormalizedValue };

                var request = new SingleDocumentSignatureRequestUnvalidated
                {
                    DocumentToSignArchiveKey = file,
                    DocumentToSignFileName = "SavingsAgreeement.pdf",
                    RedirectAfterSuccessUrl = returnPage,
                    RedirectAfterFailedUrl = returnPage,
                    SigningCustomers = new List<SingleDocumentSignatureRequestUnvalidated.SigningCustomer> { signingCustomer },
                    CustomData = new Dictionary<string, string>
                        {
                            { "tempDataKey", tempDataKey }
                        },
                };

                var session = customerClient.CreateElectronicIdSignatureSession(request);
                return session.SigningCustomersBySignerNr.Select(x => x.Value).FirstOrDefault().SignatureUrl;
            }
            else if (signatureProviderCode == ElectronicIdProviderCode.Signicat)
            {
                var o = new SystemUserSignicatClient();
                var returnPage = getExternalLink(
                    "AfterSign",
                    "SavingsStandardApplication",
                    null);
                var session = o.StartSignatureSession(new SystemUserSignicatClient.StartSignatureSessionRequest
                {
                    RedirectAfterSuccessUrl = returnPage.ToString(),
                    RedirectAfterFailedUrl = returnPage.ToString(),
                    PdfDisplayFileName = documentDisplayName,
                    PdfBytesBase64 = Convert.ToBase64String(pdfBytes),
                    CustomData = new Dictionary<string, string>
                {
                    { "tempDataKey", tempDataKey }
                },
                    SigningCustomersByApplicantNr = new Dictionary<int, SystemUserSignicatClient.StartSignatureSessionRequest.Customer>
                {
                    {
                        1,
                        new SystemUserSignicatClient.StartSignatureSessionRequest.Customer
                        {
                            ApplicantNr = 1,
                            CivicRegNr = civicRegNr.NormalizedValue,
                            SignicatLoginMethod = GetLoginMethodByCountry(),
                            FirstName = firstName,
                            LastName = lastName,
                            UserLanguage = userLanguage
                        }
                    }
                }
                });
                return session.GetNextSignatureUrl();
            }
            else
            {
                throw new Exception($"Unsupported signature provider code '{signatureProviderCode}'");
            }
        }

        public SavingsAgreementElectronicIdSignatureResult HandleSignatureCallback(Func<string, string> getRequestParameter)
        {
            if (signatureProviderCode == ElectronicIdProviderCode.Signicat2)
            {
                var sessionId = getRequestParameter("localSessionId");

                ICustomerClient customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                var sessionTuple = customerClient.GetElectronicIdSignatureSession(sessionId, false);

                if (!sessionTuple.HasValue)
                {
                    Log.Warning("Savings application failed due to signature session missing or expired: " + sessionId);
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = false
                    };
                }

                var session = sessionTuple.Value.Session;
                var tempDataKey = session.CustomData.Opt("tempDataKey");
                var sc = new SystemUserSavingsClient();
                if (!sc.TryGetTemporarilyEncryptedData(tempDataKey, out string plainData))
                {
                    Log.Warning("Savings application failed due to signature session missing or expired (2): " + sessionId);
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = false
                    };
                }

                if (session.SignedPdf != null)
                {
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = true,
                        SignedAgreementArchiveKey = session.SignedPdf.ArchiveKey,
                        PlainData = plainData
                    };
                }
                else
                {
                    Log.Warning("Signature failed for session: " + sessionId);
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = false
                    };
                }
            }
            else if (signatureProviderCode == ElectronicIdProviderCode.Signicat)
            {
                var sessionId = getRequestParameter("sessionId");
                var c = new SystemUserSignicatClient();
                var session = c.GetSignatureSession(sessionId);

                if (session == null)
                {
                    Log.Warning("Savings application failed due to signature session missing or expired: " + sessionId);
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = false
                    };
                }

                var tempDataKey = session.CustomData.Opt("tempDataKey");
                var sc = new SystemUserSavingsClient();
                string plainData;
                if (!sc.TryGetTemporarilyEncryptedData(tempDataKey, out plainData))
                {
                    Log.Warning("Savings application failed due to signature session missing or expired (2): " + sessionId);
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = false
                    };
                }

                if (session.SessionStateCode == "SignaturesSuccessful")
                {
                    var dc = new SystemUserDocumentClient();
                    var signedDocument = c.GetDocument(session.SignedDocumentKey);
                    var archiveKey = dc.ArchiveStore(Convert.FromBase64String(signedDocument.DocumentDataBase64), signedDocument.DocumentMimeType, signedDocument.DocumentDownloadName ?? session.DocumentFileName ?? "SavingsAgreement.pdf");
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = true,
                        SignedAgreementArchiveKey = archiveKey,
                        PlainData = plainData
                    };
                }
                else
                {
                    Log.Warning("Signature failed for session: " + sessionId);
                    return new SavingsAgreementElectronicIdSignatureResult
                    {
                        Success = false
                    };
                }
            }
            else
            {
                throw new Exception($"Unsupported signature provider code '{signatureProviderCode}' for HandleSignatureCallback");
            }
        }

        private string GetLoginMethodByCountry()
        {
            var c = NEnv.ClientCfg.Country.BaseCountry;
            if (c == "SE")
                return "SwedishBankId";
            else if (c == "FI")
                return "FinnishTrustNetwork";
            else
                throw new NotImplementedException();
        }
    }
}
