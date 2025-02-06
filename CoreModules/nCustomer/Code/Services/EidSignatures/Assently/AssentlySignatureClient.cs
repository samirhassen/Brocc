using NTech.Core.Module.Shared.Clients;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static nCustomer.Services.EidSignatures.Assently.AssentlyDocument;

namespace nCustomer.Services.EidSignatures.Assently
{
    public class AssentlySignatureClient : AssentlySignatureClientBase
    {
        public AssentlySignatureClient(NTechSimpleSettings settings) : base(settings)
        {

        }

        private static readonly IServiceClientSyncConverter serviceClientSyncConverter = new ServiceClientSyncConverterLegacy();
        public TResult ToSync<TResult>(Func<Task<TResult>> action) => serviceClientSyncConverter.ToSync(action);
        
        public async Task<CaseModel> GetCaseAsync(Dictionary<int, CommonElectronicIdSignatureSession.SigningCustomer> signingCustomersBySignerNr, byte[] fileData, string fileName, Uri callbackUri)
        {
            var token = await GetTokenAsync();
            var caseId = await CreateCaseAsync(token, caseName: fileName);
            var sendDocument =  await SendDocumentAsync(token, signingCustomersBySignerNr, fileData, fileName, caseId);
            var isCallbackUriSet = await SetCallbackAsync(token, caseId, callbackUri);
            var isCaseSent = await SendCaseForSigningAsync(token, caseId);
            var signatureStatus = await GetSignatureStatusBySignerNrAsync(caseId, token);

            return new CaseModel
            {
                CaseId = caseId,
                SignatureStatus = signatureStatus
            }; 
        }

        public async Task<string> GetTokenAsync()
        {
            var token = await GetCredentialTokenAsync();
            return token;
        }

        public async Task<string> CreateCaseAsync(string token, string caseName)
        {
            var caseId = await CreateCaseIdAsync(token, caseName);
            return caseId;
        }

        public async Task<Dictionary<int, PartySignatureStatus>> GetSignatureStatusBySignerNrAsync(string caseId, string token)
        {
            var signatureUrl = await GetPartySignatureStatusAsync(caseId, token);
            return signatureUrl;
        }

        public async Task<bool> SetCallbackAsync(string token, string caseId, Uri callbackUrl)
        {
            var isCallbackSet = await SetCallbackUriAsync(token, caseId, callbackUrl);
            return isCallbackSet; 
        }

        public async Task<bool> SendCaseForSigningAsync(string token, string caseId)
        {
            var isCaseSent = await SendCaseAsync(token, caseId);
            return isCaseSent; 
        }

        public async Task<AssentlyDocument> SendDocumentAsync(string token, Dictionary<int, CommonElectronicIdSignatureSession.SigningCustomer> signingCustomersBySignerNr, byte[] pdfData, string pdfFileName, string caseId)
        {
            var content = CreateMultipartFormDataContent(
                file: (Data: pdfData, FileName: pdfFileName, Name: "File", ContentType: "multipart/form-data"));

            var rawDocument = await PostNewDocumentAsync(token, signingCustomersBySignerNr, content, caseId);
            return new AssentlyDocument(rawDocument);
        }

        public async Task<bool> CancelDocumentAsync(string caseId)
        {
            var recallCase = await RecallCaseAsync(caseId);
            return recallCase;
        }

        public async Task<(string FileName, byte[] FileData)> GetSignedDocumentAsync(string caseId)
        {
            return await GetSignedDocumentDataAsync(caseId);
        }
    }

    public class CaseModel
    {
        public string CaseId { get; set; }
        public Dictionary<int, PartySignatureStatus> SignatureStatus { get; set; }
    }
}