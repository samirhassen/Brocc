using Newtonsoft.Json;
using nGccCustomerApplication.Code;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace nGccCustomerApplication
{
    //TODO: Merge into PrecreditClient
    public class PreCreditClientWrapperDirectPart
    {
        private Tuple<bool, TResponse> Call<TResponse>(string uri, object input, string bearerToken = null) where TResponse : class
        {
            Func<Tuple<bool, TResponse>> fail = () =>
            {
                return Tuple.Create<bool, TResponse>(false, null);
            };
            try
            {
                if (bearerToken == null)
                {
                    bearerToken = NEnv.GetSelfCachingSystemUserBearerToken();
                }

                var client = new HttpClient();
                client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nPreCredit"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.SetBearerToken(bearerToken);
                var response = client.PostAsJsonAsync(uri, input).Result;
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<TResponse>(response.Content.ReadAsStringAsync().Result);

                    return Tuple.Create<bool, TResponse>(true, result);
                }
                else
                {
                    NLog.Warning("Failed {failedMessage}", response.ReasonPhrase);
                    return fail();
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error");
                return fail();
            }
        }

        private async Task<Tuple<bool, TResponse>> CallAsync<TResponse>(string uri, object input, string bearerToken = null) where TResponse : class
        {
            Func<Tuple<bool, TResponse>> fail = () =>
            {
                return Tuple.Create<bool, TResponse>(false, null);
            };
            try
            {
                if (bearerToken == null)
                {
                    bearerToken = NEnv.GetSelfCachingSystemUserBearerToken();
                }

                var client = new HttpClient();
                client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nPreCredit"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.SetBearerToken(bearerToken);
                var response = await client.PostAsJsonAsync(uri, input);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TResponse>(content);

                    return Tuple.Create<bool, TResponse>(true, result);
                }
                else
                {
                    NLog.Warning("Failed {failedMessage}", response.ReasonPhrase);
                    return fail();
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error");
                return fail();
            }
        }

        private class StateWrapper<T>
        {
            public T State { get; set; }
        }

        public class ApplicationState
        {
            public int NrOfApplicants { get; set; }
            public bool IsActive { get; set; }
            public string Token { get; set; }
            public string ApplicationNr { get; set; }
            public ActiveStateModel ActiveState { get; set; }
            public ClosedStateModel ClosedState { get; set; }
            public bool IsForcedBankAccountDataSharing { get; set; }
        }

        public class ClosedStateModel
        {
            public bool WasAccepted { get; set; }
        }

        public class ActiveStateModel
        {
            public bool IsWaitingForClient { get; set; }
            public bool IsAwaitingFinalApproval { get; set; }
            public bool ShouldChooseDocumentSource { get; set; }
            public bool IsWatingForDocumentUpload { get; set; }
            public bool IsWaitingForSharedAccountDataCallback { get; set; }
            public bool ShouldAnswerAdditionalQuestions { get; set; }
            public AdditionalQuestionsInitialDataModel AdditionalQuestionsData { get; set; }
            public bool ShouldAnswerExternalAdditionalQuestions { get; set; }
            public ExternalAdditionalQuestionsInitialDataModel ExternalAdditionalQuestionsData { get; set; }

            public DocumentUploadDataModel DocumentUploadData { get; set; }

            public DocumentSourceDataModel DocumentSourceData { get; set; }
            public bool ShouldSignAgreements { get; set; }
            public AgreementInitialModel AgreementsData { get; set; }
        }

        public class DocumentUploadDataModel
        {
            public Applicant Applicant1 { get; set; }
            public Applicant Applicant2 { get; set; }
            public class AttachedFile
            {
                public string Id { get; set; }
                public int ApplicantNr { get; set; }
                public string FileName { get; set; }
                public string MimeType { get; set; }
            }
            public class Applicant : ApplicantInitialModel
            {
                public string SharedAccountDataPdfPreviewArchiveKey { get; set; }
                public List<AttachedFile> AttachedFiles { get; set; }
            }
        }

        public class DocumentSourceDataModel
        {
            public bool HasApplicant1ChosenDataSource { get; set; }
        }

        public class AgreementInitialModel
        {
            public bool HasApplicant1SignedAgreement { get; set; }
            public bool HasApplicant2SignedAgreement { get; set; }
            public ApplicantInitialModel Applicant1 { get; set; }
            public ApplicantInitialModel Applicant2 { get; set; }
        }

        public class ApplicantInitialModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string CivicRegNr { get; set; }
            public string HasOtherTaxOrCitizenCountry { get; set; }
        }

        public class AdditionalQuestionsInitialDataModel
        {
            public bool IsAdditionalLoanOffer { get; set; }
            public ApplicantInitialModel Applicant1 { get; set; }
            public ApplicantInitialModel Applicant2 { get; set; }
        }

        public class ExternalAdditionalQuestionsInitialDataModel
        {
            public string RedirectUrl { get; set; }
        }

        private class IsSatAccountDataSharingEnabledWrapper
        {
            public bool IsSatAccountDataSharingEnabled { get; set; }
        }

        public ApplicationState GetApplicationState(string token)
        {
            var result = Call<StateWrapper<ApplicationState>>("api/creditapplication-wrapper-direct/fetch-application-state", new { token = token });

            /* //Mocking Object
            var result = new Tuple<bool, StateWrapper<ApplicationState>>(true,
            new StateWrapper<ApplicationState>
            {
                State = new ApplicationState
                {
                    NrOfApplicants = 1,
                    IsActive = true,
                    Token = token,
                    ApplicationNr = "CA176053",
                    ActiveState = new ActiveStateModel
                    {
                        IsWaitingForClient = false,
                        IsAwaitingFinalApproval = false,
                        ShouldChooseDocumentSource = false,
                        IsWatingForDocumentUpload = false,
                        IsWaitingForSharedAccountDataCallback = false,
                        ShouldAnswerAdditionalQuestions = true,
                        AdditionalQuestionsData = new AdditionalQuestionsInitialDataModel
                        {
                            IsAdditionalLoanOffer = true,
                            Applicant1 = new ApplicantInitialModel
                            {
                                FirstName = "Samir",
                                LastName = "Hassen",
                                CivicRegNr = "100380X598T",
                                HasOtherTaxOrCitizenCountry = "No"
                            }
                        },
                        ShouldAnswerExternalAdditionalQuestions = false,
                        ExternalAdditionalQuestionsData = null,
                        DocumentUploadData = new DocumentUploadDataModel
                        {
                            Applicant1 = new DocumentUploadDataModel.Applicant
                            {
                                FirstName = "Samir",
                                LastName = "Hassen",
                                CivicRegNr = "100380X598T",
                                //HasOtherTaxOrCitizenCountry = "No",
                                //SharedAccountDataPdfPreviewArchiveKey = "mockArchiveKey1",
                                //AttachedFiles = new List<DocumentUploadDataModel.AttachedFile>
                                //{
                                //    new DocumentUploadDataModel.AttachedFile
                                //    {
                                //        Id = "file1",
                                //        ApplicantNr = 1,
                                //        FileName = "file1.pdf",
                                //        MimeType = "application/pdf"
                                //    }
                                //}
                            }
                        },
                        DocumentSourceData = new DocumentSourceDataModel
                        {
                            HasApplicant1ChosenDataSource = false
                        },
                        ShouldSignAgreements = true,
                        //AgreementsData = new AgreementInitialModel
                        //{
                        //    HasApplicant1SignedAgreement = true,
                        //    Applicant1 = new ApplicantInitialModel
                        //    {
                        //        FirstName = "Samir",
                        //        LastName = "Hassen",
                        //        CivicRegNr = "100380X598T",
                        //        HasOtherTaxOrCitizenCountry = "No"
                        //    }
                        //}
                    },
                    //ClosedState = new ClosedStateModel
                    //{
                    //    WasAccepted = false
                    //},
                    ClosedState = null,
                    IsForcedBankAccountDataSharing = false
                }
            }
        );
            */


            var activeState = result?.Item2?.State?.ActiveState;
            HandleAttachedFiles(token, activeState);

            if (result.Item1)
                return result.Item2.State;
            else
                return null;
        }

        public async Task<ApplicationState> GetApplicationStateAsync(string token)
        {
            var result = await CallAsync<StateWrapper<ApplicationState>>("api/creditapplication-wrapper-direct/fetch-application-state", new
            {
                token = token
            });

            var activeState = result?.Item2?.State?.ActiveState;
            HandleAttachedFiles(token, activeState);

            if (result.Item1)
                return result.Item2.State;
            else
                return null;
        }
        
        public async Task<bool> GetIsSatAccountDataSharingEnabled(string token)
        {
            var result = await CallAsync<IsSatAccountDataSharingEnabledWrapper>("api/creditapplication-wrapper-direct/get-is-sat-account-data-sharing-enabled", new
            {
                token = token
            });

            if (result.Item1)
                return result.Item2.IsSatAccountDataSharingEnabled;
            else
                return false;
        }
        private void HandleAttachedFiles(string token, ActiveStateModel activeState)
        {
            if (!activeState.IsWatingForDocumentUpload)
            {
                return;
            }
            var result = DocumentCheckRepository.SharedInstance.GetFilesAndHasManualRemovals(token, true);
            var currentFiles = result.Item1;
            var hasManualRemovals = result.Item2;

            var needsReload = false;
            //Since we never do this if there are manual removals this will cause us to add the users shared document at most once
            //for each user but when they arrive doesnt matter.
            void AppendIfNotPresent(int applicantNr, DocumentUploadDataModel.Applicant applicant)
            {
                if (hasManualRemovals) //Never attach documents after the user has removed any
                    return;

                if (applicant == null) //No such applicant
                    return;

                var archiveKey = applicant.SharedAccountDataPdfPreviewArchiveKey;

                if (archiveKey == null) //No shared document
                    return;

                if (currentFiles.Any(x => x.ArchiveKey == archiveKey)) //Already present
                    return;

                DocumentCheckRepository.SharedInstance.AddExistingFile(token, applicantNr, applicant.SharedAccountDataPdfPreviewArchiveKey, false);
                needsReload = true;
            }

            var applicant1 = activeState?.DocumentUploadData?.Applicant1;
            var applicant2 = activeState?.DocumentUploadData?.Applicant2;

            var applicants = new List<Tuple<DocumentUploadDataModel.Applicant, int>>();
            if (applicant1 != null)
                applicants.Add(Tuple.Create(applicant1, 1));
            if (applicant2 != null)
                applicants.Add(Tuple.Create(applicant2, 2));

            foreach (var applicant in applicants)
            {
                AppendIfNotPresent(applicant.Item2, applicant.Item1);
            }

            if (needsReload)
            {
                currentFiles = DocumentCheckRepository.SharedInstance.GetFiles(token, true);
            }

            foreach (var a in applicants)
            {
                var applicant = a.Item1;
                var applicantNr = a.Item2;
                applicant.SharedAccountDataPdfPreviewArchiveKey = null;
                applicant.AttachedFiles = currentFiles.Where(x => x.ApplicantNr == applicantNr).Select(x => new DocumentUploadDataModel.AttachedFile
                {
                    ApplicantNr = x.ApplicantNr,
                    FileName = x.FileName,
                    Id = x.Id,
                    MimeType = x.MimeType
                }).ToList();
            }
        }

        private class UpdateSignatureStateResult
        {
            public bool IsFailed { get; set; }
        }

        public void UpdateSignatureState(string token, string signatureSessionKey, int? applicantNr, out bool isSessionFailed)
        {
            var result = Call<UpdateSignatureStateResult>("api/creditapplication-wrapper-direct/update-signature-state", new { token, signatureSessionKey, applicantNr });
            if (result.Item1)
            {
                isSessionFailed = result.Item2.IsFailed;
            }
            else
                isSessionFailed = false;
        }

        public class SignatureLinkResult
        {
            public string SignatureUrl { get; set; }
            public int? ApplicantNr { get; set; }
        }

        public void UpdateBankAccountDataShareData(string applicationNr, int applicantNr, string rawDataArchiveKey, string pdfPreviewArchiveKey)
        {
            Call<UpdateBankAccountDataShareDataResult>("api/BankAccountDataShare/Update-Data", new { applicationNr, applicantNr, rawDataArchiveKey, pdfPreviewArchiveKey });
        }

        private class UpdateBankAccountDataShareDataResult
        {

        }

        public SignatureLinkResult CreateApplicationSignatureLink(string token, int applicantNr)
        {
            var result = Call<SignatureLinkResult>("api/creditapplication-wrapper-direct/create-application-signature-link", new { token = token, applicantNr = applicantNr });
            if (result.Item1)
                return result.Item2;
            else
                return null;
        }

        public ApplicationState ApplyAdditionalQuestionAnswers(string token, PreCreditClient.AnswersModel answers, string userLanguage)
        {
            var result = Call<StateWrapper<ApplicationState>>("api/creditapplication-wrapper-direct/apply-additionalquestion-answers", new
            {
                token = token,
                answers = answers,
                userLanguage = userLanguage
            });
            if (result.Item1)
                return result.Item2.State;
            else
                return null;
        }

        public class DocumentCheckAttachRequest
        {
            public string token { get; set; }
            public List<File> Files { get; set; }
            public class File
            {
                public int ApplicantNr { get; set; }
                public string FileName { get; set; }
                public string MimeType { get; set; }
                public string ArchiveKey { get; set; }
            }
        }

        public class DocumentSourceRequest
        {
            public string Token { get; set; }
            public int ApplicantNr { get; set; }
            public string SourceCode { get; set; }
        }

        public ApplicationState AttachUserAddedDocumentCheckDocuments(DocumentCheckAttachRequest request)
        {
            var result = Call<StateWrapper<ApplicationState>>("api/creditapplication-wrapper-direct/attach-useradded-documentcheck-documents", request);
            if (result.Item1)
                return result.Item2.State;
            else
                return null;
        }

        public ApplicationState UpdateApplicationDocumentDataSourceState(DocumentSourceRequest request)
        {
            var result = Call<StateWrapper<ApplicationState>>("api/creditapplication-wrapper-direct/update-application-document-source-state", request);
            if (result.Item1)
            {
                var activeState = result?.Item2?.State?.ActiveState;
                HandleAttachedFiles(request.Token, activeState);
                return result?.Item2?.State;
            }
            else
                return null;
        }
    }
}
