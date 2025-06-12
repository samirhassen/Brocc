using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace nTest
{
    public class CreditDriverPreCreditClient
    {
        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nPreCredit"]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-Ntech-TimetravelTo", TimeMachine.SharedInstance.GetCurrentTime().ToString("o"));
            client.SetBearerToken(NEnv.AutomationBearerToken());
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        private class CreateApplicationResult
        {
            public string ApplicationNr { get; set; }
        }

        public string CreateApplication(string json)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsync("api/creditapplication/create", new StringContent(json, Encoding.UTF8, "application/json")).Result;
                r.EnsureSuccessStatusCode();
                return r.Content.ReadAsAsync<CreateApplicationResult>().Result?.ApplicationNr;
            }
        }

        private NHttp.NHttpCall Begin()
        {
            return NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), NEnv.AutomationBearerToken(), TimeSpan.FromSeconds(30));
        }

        public void FlagCustomersAsExternallyOnboarded(string applicationNr)
        {
            Begin()
                .PostJson("api/mortageloan/flag-customers-as-externally-onboarded", new { applicationNr = applicationNr })
                .EnsureSuccessStatusCode();
        }

        public void AddApplicationDocument(string applicationNr, string documentType, int? applicantNr, string dataUrl, string filename)
        {
            Begin()
                .PostJson("api/ApplicationDocuments/Add", new { applicationNr, documentType, applicantNr, dataUrl, filename })
                .EnsureSuccessStatusCode();
        }

        public string UpdateMortgageLoanDocumentCheckStatus(string applicationNr)
        {
            return Begin()
                .PostJson("api/ApplicationDocuments/UpdateMortgageLoanDocumentCheckStatus", new { applicationNr })
                .ParseJsonAsAnonymousType(new { DocumentCheckStatusAfter = "" })
                ?.DocumentCheckStatusAfter;
        }

        public void DoMortgageLoanKycScreen(string applicationNr, List<int> applicantNrs)
        {
            Begin()
                .PostJson("api/MortgageLoan/CustomerCheck/DoKycScreen", new { applicationNr, applicantNrs })
                .EnsureSuccessStatusCode();
        }

        public void ApproveMortgageLoanCustomerCheck(string applicationNr)
        {
            Begin()
                .PostJson("api/MortgageLoan/CustomerCheck/Approve", new { applicationNr })
                .EnsureSuccessStatusCode();
        }

        public void UpdateMortageApplicationDirectDebitStatus(string applicationNr, string newStatus, string bankAccountNr, int? bankAccountOwnerApplicantNr)
        {
            Begin().
                PostJson("api/MortgageLoan/DirectDebitCheck/UpdateStatus", new { applicationNr, newStatus, bankAccountNr, bankAccountOwnerApplicantNr })
                .EnsureSuccessStatusCode();
        }

        public void CreateMortgageLoan(string applicationNr)
        {
            Begin()
                .PostJson("api/mortageloan/create-loan", new { applicationNr })
                .EnsureSuccessStatusCode();
        }

        public void ScheduleOutgoingSettlementPayment(string applicationNr, decimal? interestDifferenceAmount, decimal? actualLoanAmount, DateTime? settlementDate)
        {
            Begin()
                .PostJson("api/mortageloan/schedule-outgoing-settlement-payment", new { applicationNr, interestDifferenceAmount, actualLoanAmount, settlementDate })
                .EnsureSuccessStatusCode();
        }

        public bool? AutomateFinalCreditCheck(string applicationNr)
        {
            return Begin()
                .PostJson("api/MortgageLoan/CreditCheck/AutomateFinal", new { applicationNr })
                .ParseJsonAsAnonymousType(new { isAccepted = (bool?)null })
                ?.isAccepted;
        }

        public void AutomateMortgageLoanAmortizationStep(string applicationNr, bool useNoneAsDefaultRule)
        {
            Begin()
                .PostJson("api/MortgageLoan/Amortization/AutomateBasedOnCurrentData", new { applicationNr, useNoneAsDefaultRule })
                .EnsureSuccessStatusCode();
        }

        public MortgageLoanApplicationFinalCreditCheckStatusModel FetchMortgageLoanFinalCreditCheckStatus(string applicationNr)
        {
            return Begin()
                .PostJson("api/MortgageLoan/CreditCheck/FetchFinalStatus", new { applicationNr })
                .ParseJsonAs<MortgageLoanApplicationFinalCreditCheckStatusModel>();
        }

        public MortgageLoanApplicationInitialCreditCheckStatusModel FetchMortgageLoanInitialCreditCheckStatus(string applicationNr)
        {
            return Begin()
                .PostJson("api/MortgageLoan/CreditCheck/FetchInitialStatus", new { applicationNr })
                .ParseJsonAs<MortgageLoanApplicationInitialCreditCheckStatusModel>();
        }

        public class MortgageLoanApplicationInitialCreditCheckStatusModel
        {
            public string CreditCheckStatus { get; set; }
            public string CustomerOfferStatus { get; set; }
            public bool IsViewDecisionPossible { get; set; }
            public string ViewCreditDecisionUrl { get; set; }
            public RejectedDecisionModel RejectedDecision { get; set; }
            public AcceptedDecisionModel AcceptedDecision { get; set; }
            public class RejectedDecisionModel
            {
                public string ScoringPass { get; set; }
                public List<RejectionReasonModel> RejectionReasons { get; set; }
            }
            public class AcceptedDecisionModel
            {
                public string ScoringPass { get; set; }
                public OfferModel Offer { get; set; }
            }
            public class OfferModel
            {
                public decimal? LoanAmount { get; set; }
                public decimal? MonthlyAmortizationAmount { get; set; }
                public decimal? NominalInterestRatePercent { get; set; }
                public decimal? InitialFeeAmount { get; set; }
                public decimal? MonthlyFeeAmount { get; set; }
            }
            public class RejectionReasonModel
            {
                public string DisplayName { get; set; }
                public string Name { get; set; }
            }
        }


        public void CreditCheckAutomatic(string applicationNr)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("api/creditapplication/creditcheck/automatic", new
                {
                    applicationNr = applicationNr,
                    followRejectRecommendation = true,
                    followAcceptRecommendation = true
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        public void AddIbanToApplication(string applicationNr, string iban)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("api/creditapplication/update", new
                {
                    applicationNr = applicationNr,
                    Items = new[]
                    {
                        new
                        {
                            Group = "application",
                            Name = "iban",
                            Value = iban
                        }
                    }
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        public void AddBankAccountNrToApplication(string applicationNr, string bankAccountNr)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("api/creditapplication/update", new
                {
                    applicationNr = applicationNr,
                    Items = new[]
                    {
                        new
                        {
                            Group = "application",
                            Name = "bankaccountnr",
                            Value = bankAccountNr
                        }
                    }
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        private class CreateAgreementPdfInArchiveResult
        {
            public string ArchiveKey { get; set; }
        }

        public string CreateAgreementPdfInArchive(string applicationNr)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("api/creditapplication/agreement/createpdf", new
                {
                    applicationNr = applicationNr,
                    archiveStoreFilename = "test-agreement.pdf"
                }).Result;
                r.EnsureSuccessStatusCode();
                return r.Content.ReadAsAsync<CreateAgreementPdfInArchiveResult>().Result?.ArchiveKey;
            }
        }

        public void AddSignedAgreement(string applicationNr, int applicantNr, string archiveKey)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("api/creditapplication/addsignedagreement", new
                {
                    applicationNr = applicationNr,
                    applicantNr = applicantNr,
                    archiveKey = archiveKey
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        public void SignalExternalFraudCheckWasDone(string applicationNr)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("api/creditapplication/fraudcheck/automatic", new
                {
                    applicationNr = applicationNr,
                    wasDoneExternally = true
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        public void ApproveApplication(string applicationNr)
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("CreditManagement/ApproveApplication", new
                {
                    applicationNr = applicationNr,
                    skipDwLiveUpdate = true
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        public void CreateCredits()
        {
            using (var c = CreateClient())
            {
                var r = c.PostAsJsonAsync("CreditDecision/CreateCredits", new
                {
                    approveAllPending = true
                }).Result;
                r.EnsureSuccessStatusCode();
            }
        }

        public ApplicationApplicantsModel FetchApplicants(string applicationNr)
        {
            return Begin()
                .PostJson("api/ApplicationInfo/FetchApplicants", new { applicationNr })
                .ParseJsonAs<ApplicationApplicantsModel>();
        }

        public class ApplicationApplicantsModel
        {
            public string ApplicationNr { get; set; }
            public int NrOfApplicants { get; set; }
            public Dictionary<int, int> CustomerIdByApplicantNr { get; set; }
        }

        public void AutomateValuationStep(string applicationNr)
        {
            var result = Begin()
                .PostJson("Api/MortgageLoan/Valuation/AutomateValution", new { applicationNr })
                .ParseJsonAs<Dictionary<string, string>>();

            Begin()
                .PostJson("Api/MortgageLoan/Valuation/AcceptUcbvValuation", new { applicationNr, valuationItems = result })
                .EnsureSuccessStatusCode();
        }

        public bool TryCreateUnsecuredLoanStandardApplication<T>(T request, out string applicationNr, out string errorMessage)
        {
            var result = Begin()
                .PostJson("/api/UnsecuredLoanStandard/Create-Application", request)
                .HandlingApiErrorWithHttpCode(
                    x => Tuple.Create(true, x.ParseJsonAsAnonymousType(new { ApplicationNr = "" })?.ApplicationNr),
                    (x, _) => Tuple.Create(false, x.ErrorCode + " - " + x.ErrorMessage));

            applicationNr = result.Item1 ? result.Item2 : null;
            errorMessage = result.Item1 ? null : result.Item2;
            return result.Item1;
        }

        public class MortgageLoanApplicationFinalCreditCheckStatusModel
        {
            public bool HasNonExpiredBindingOffer { get; set; }
            public bool IsNewCreditCheckPossible { get; set; }
            public string NewCreditCheckUrl { get; set; }
            public string UnsignedAgreementDocumentUrl { get; set; }
            public string UnsignedAgreementDocumentArchiveKey { get; set; }
            public string CreditCheckStatus { get; set; }
            public bool IsViewDecisionPossible { get; set; }
            public string ViewCreditDecisionUrl { get; set; }
            public RejectedDecisionModel RejectedDecision { get; set; }
            public AcceptedDecisionModel AcceptedDecision { get; set; }
            public class RejectedDecisionModel
            {
                public string ScoringPass { get; set; }
                public List<RejectionReasonModel> RejectionReasons { get; set; }
            }
            public class AcceptedDecisionModel
            {
                public string ScoringPass { get; set; }
                public OfferModel Offer { get; set; }
            }
            public class OfferModel
            {
                public decimal? LoanAmount { get; set; }
                public decimal? MonthlyAmortizationAmount { get; set; }
                public decimal? NominalInterestRatePercent { get; set; }
                public decimal? InitialFeeAmount { get; set; }
                public decimal? MonthlyFeeAmount { get; set; }
                public string BindingUntilDate { get; set; }

                public DateTime? GetBindingUntilDate()
                {
                    if (BindingUntilDate == null)
                        return null;
                    return DateTime.ParseExact(BindingUntilDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            public class RejectionReasonModel
            {
                public string DisplayName { get; set; }
                public string Name { get; set; }
            }
        }
    }
}