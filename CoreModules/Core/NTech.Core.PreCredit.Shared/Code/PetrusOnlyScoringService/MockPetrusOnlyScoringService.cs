using Newtonsoft.Json;
using nPreCredit;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared.Services.UlLegacy;
using nTest.RandomDataSource;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public class MockPetrusOnlyScoringService : IPetrusOnlyScoringService
    {
        private readonly IApplicationCommentService commentService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly IDocumentClient documentClient;

        public MockPetrusOnlyScoringService(IApplicationCommentService commentService, IPreCreditEnvSettings envSettings, IDocumentClient documentClient)
        {
            this.commentService = commentService;
            this.envSettings = envSettings;
            this.documentClient = documentClient;
        }

        public PetrusOnlyCreditCheckResponse NewCreditCheck(PetrusOnlyCreditCheckRequest request)
        {
            if (envSettings.IsProduction)
                throw new Exception("Not allowed in production");

            var webserviceRequest = PetrusOnlyRequestBuilder.CreateWebserviceRequest(request);

            var archiveKey = documentClient.ArchiveStore(Encoding.UTF8.GetBytes(webserviceRequest.ToString(Formatting.Indented)), "application/json", "PetrusRequest.json");

            if (!commentService.TryAddComment(request.DataContext.ApplicationNr, "Local petrus mock used. See attached file for what the request would have looked like.", "Petrus2MockRequest",
                CommentAttachment.CreateFileFromArchiveKey(archiveKey, "application/json", "PetrusRequest.json"), out var failedMessage))
                throw new Exception(failedMessage);

            var loanAmount = request.DataContext.Application.Application.Get("amount").DecimalValue.Required;
            var repaymentTimeInYears = request.DataContext.Application.Application.Get("repaymentTimeInYears").IntValue.Required;
            var carOrBoatLoanAmount = request.DataContext.Application.Applicant(1).Get("carOrBoatLoanAmount").DecimalValue.Optional;

            if (carOrBoatLoanAmount == 7)
            {
                throw RealPetrusOnlyScoringService.CreatePetrusErrorException("Testing petrus error");
            }

            PetrusOnlyCreditCheckResponse.OfferModel offer = null;
            string rejectionReason = null;
            if (ResultOverride.HasValue)
            {
                if (ResultOverride.Value.IsAccepted)
                    offer = ResultOverride.Value.AcceptedOffer;
            }
            else
            {
                var result = GetScoringResult(request.DataContext);

                if (result.OfferedInterestRate.HasValue)
                    offer = new PetrusOnlyCreditCheckResponse.OfferModel
                    {
                        Amount = loanAmount,
                        InitialFeeAmount = loanAmount > 2000m ? 100m : 0m,
                        NotificationFeeAmount = loanAmount > 2000m ? 5m : 0m,
                        RepaymentTimeInMonths = repaymentTimeInYears * 12,
                        MarginInterestRatePercent = result.OfferedInterestRate.Value
                    };
                else
                    rejectionReason = result.RejectionReason;
            }
            PetrusOnlyCreditCheckResponse.ApplicantModel mainApplicant = null;
            if (offer != null)
            {
                var civicNr = CivicRegNumberFi.Parse(request.DataContext.Application.Applicant(1).Get("civicRegNr").StringValue.Required);
                var testPerson = TestPersonGenerator.GetSharedInstance("FI").GenerateTestPerson(
                    /*
                    hashcode seed is just to make sure it gives the same name/adr for the same civic nr every time to make testing deterministic
                    could be any int that is the same across database resets for the same civicnr
                    */
                    new RandomnessSource(civicNr.NormalizedValue.GetHashCode()),
                    civicNr, true, DateTime.Now);
                mainApplicant = new PetrusOnlyCreditCheckResponse.ApplicantModel
                {
                    City = testPerson.Req("addressCity"),
                    FirstName = testPerson.Req("firstName"),
                    LastName = testPerson.Req("lastName"),
                    StreetAddress = testPerson.Req("addressStreet"),
                    ZipCode = testPerson.Req("addressZipcode")
                };
                
                if (carOrBoatLoanAmount == 4m)
                {
                    //Break the applicant, skip name
                    mainApplicant.FirstName = null;
                    mainApplicant.LastName = null;
                }
                else if (carOrBoatLoanAmount == 5)
                {
                    //Break the applicant, remove address
                    mainApplicant.City = null;
                    mainApplicant.StreetAddress = null;
                    mainApplicant.ZipCode = null;
                }
                else if (carOrBoatLoanAmount == 6)
                {
                    //Remove the main applicant entirely
                    mainApplicant = null;
                }
            }

            return new PetrusOnlyCreditCheckResponse
            {
                Accepted = offer != null,
                LoanApplicationId = request.DataContext.Application.Application.Get("petrusApplicationId").StringValue.Optional ?? $"T-{Guid.NewGuid()}",
                MainApplicant = mainApplicant,
                Offer = offer,
                RejectionReason = rejectionReason
            };
        }

        public XDocument GetPetrusLog(string applicationId) =>
            new XDocument(new XElement("PetrusFakeTestLogg", new XAttribute("applicationId", applicationId),
                new XElement("LogEntries",
                    Enumerable.Range(1, 50).Select(x => new XElement("LogEntry", $"Some random text about an action {x}")).ToArray())));

        private static (decimal? OfferedInterestRate, string RejectionReason) GetScoringResult(PetrusOnlyCreditCheckService.ScoringDataContext dataContext)
        {
            var civicRegNr = dataContext.Application.Applicant(1).Get("civicRegNr").StringValue.Required;

            string legalEntityNumberWithoutLetters = Regex.Replace(civicRegNr, @"[^0-9]", "");
            var lastNumberStr = legalEntityNumberWithoutLetters[legalEntityNumberWithoutLetters.Length - 1];

            if (!int.TryParse(lastNumberStr.ToString(), out int lastNumber))
            {
                throw new Exception($"Mock scoring error 2: could not parse last digit in string on application {dataContext.ApplicationNr}");
            }

            var creditCardAmount = dataContext.Application.Applicant(1).Get("creditCardAmount").DecimalValue.Optional ?? 0;
            var studentLoanAmount = dataContext.Application.Applicant(1).Get("studentLoanAmount").DecimalValue.Optional ?? 0;
            var isAccepted = (creditCardAmount > 0m || lastNumber != 0) && studentLoanAmount != 999m && studentLoanAmount != 998m && studentLoanAmount != 997m; //LegalEntityNumbers ending in 0 are randomly not accepted 
            if (isAccepted)
                return (OfferedInterestRate: GetOfferedInterestRate(creditCardAmount, lastNumber), RejectionReason: (string)null);
            else
            {
                string rejectionReason;
                if (studentLoanAmount == 999m)
                    rejectionReason = "paymentRemark";
                else if (studentLoanAmount == 998m)
                    rejectionReason = "score";
                else
                    rejectionReason = PetrusOnlyCreditCheckService.FallbackRejectionReason;
                return (OfferedInterestRate: new decimal?(), RejectionReason: rejectionReason);
            }                
        }

        private static decimal GetOfferedInterestRate(decimal creditCardAmount, int lastSsnNumber)
        {
            var rate = creditCardAmount > 0m ? creditCardAmount % 100m : lastSsnNumber + 6;
            if (rate < 7m)
                rate = rate + 7m;
            if (rate > 18m)
                return 18m;
            else
                return rate;
        }

        public static (bool IsAccepted, PetrusOnlyCreditCheckResponse.OfferModel AcceptedOffer)? ResultOverride { get; set; }        
    }
}