using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using NTech.Shared.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public class MockSignatureService : SignicatSignatureServiceBase
    {
        private readonly SignicatSettings settings;
        private readonly IDocumentDatabaseService documentDatabaseService;

        private const string LegacyMockDocumentStoreKeySpace = "MockDocumentStoreKeySpaceV1";
        private const string MockDocumentStoreKeySpace = "MockDocumentStoreKeySpaceV2";

        public MockSignatureService(SignicatSettings settings, IDocumentDatabaseService documentDatabaseService, IDocumentService documentService, INEnv env) : base(documentDatabaseService, documentService, env)
        {
            this.settings = settings;
            this.documentDatabaseService = documentDatabaseService;
        }

        private List<SignaturePdf> GetPdfs(string requestId)
        {
            return JsonConvert.DeserializeObject<List<SignaturePdf>>(documentDatabaseService.Get<string>(MockDocumentStoreKeySpace, requestId));
        }

        protected override Task<CreateSignatureResponse> CreateSignatureRequest(Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr, List<SignaturePdf> pdfs)
        {
            var r = new CreateSignatureResponse
            {
                RequestId = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(),
                SigningCustomersByApplicantNr = new Dictionary<int, SignatureSession.SigningCustomer>(),
                Documents = pdfs.Select(x => new CreateSignatureResponse.Document
                {
                    Pdf = x,
                    SdsCode = x.DocumentId
                }).ToList()
            };

            documentDatabaseService.Set(MockDocumentStoreKeySpace, r.RequestId, JsonConvert.SerializeObject(pdfs), TimeSpan.FromDays(8));

            foreach (var s in signingCustomersByApplicantNr)
            {
                r.SigningCustomersByApplicantNr[s.Value.ApplicantNr] = new SignatureSession.SigningCustomer
                {
                    ApplicantNr = s.Value.ApplicantNr,
                    SignicatTaskId = s.Value.ToString(),
                    SignicatSignatureUrl = NTechServiceRegistry.CreateUrl(this.settings.SelfExternalUrl, "mock-sign",
                                    Tuple.Create("request_id", r.RequestId), Tuple.Create("taskId", s.Value.ApplicantNr.ToString())).ToString(),
                    CivicRegNr = s.Value.CivicRegNr.NormalizedValue,
                    CivicRegNrCountry = s.Value.CivicRegNr.Country
                };
            }

            return Task.FromResult(r);
        }

        protected override Task<Dictionary<string, DocumentSignatureResult>> GetSignatureStatusAndSignedDocumentUris(SignatureSession session)
        {
            if (session.FormatVersionNr < SignatureSession.CurrentFormatVersionNr)
                throw new Exception("The mock provider does not support prior session versions");

            var pdfs = GetPdfs(session.SignicatRequestId);

            return Task.FromResult(session.SigningCustomersByApplicantNr.ToDictionary(x => x.Key.ToString(), _ => new DocumentSignatureResult{
                TaskStatus = "completed",
                SignedDocumentUriByDocumentId = pdfs.ToDictionary(x => x.DocumentId, x => x.DocumentId)
            }));          
        }

        protected override Task<byte[]> PackageAsPdf(string requestId, List<string> documentUris)
        {
            var pdfs = GetPdfs(requestId);

            var pdfsToBePackaged = pdfs.Where(x => documentUris.Contains(x.DocumentId)).ToList();

            if (documentUris.Count > 1)
                throw new Exception("The mock provider does not support pdf concatenation");

            return Task.FromResult(pdfsToBePackaged.Single().PdfBytes);
        }

        protected override Task<(bool isOk, List<ICivicRegNumber> signedByCivicRegNrs)> VerifyIsSignedByAtLeastThesePersons(SignatureSession session, List<string> resultUris)
        {
            var expectedCivicRegNumbers = session.SigningCustomersByApplicantNr.Values.Select(x => SignicatLoginMethodValidator.ParseCivicRegNr(x.CivicRegNrCountry, x.CivicRegNr)).ToList();
            return Task.FromResult((settings.AlwaysFailVerifyIsSignedByExactlyThesePersonsInMock ? false : true, expectedCivicRegNumbers));
        }
    }
}