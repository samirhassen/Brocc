using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class FraudModelService : IFraudModelService
    {
        private readonly IHttpContextUrlService urlService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;

        public FraudModelService(IHttpContextUrlService urlService, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository)
        {
            this.urlService = urlService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
        }

        public FraudControlModel GetFraudControlModel(string applicationNr)
        {
            var appModel = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId", "birthDate" });

            var fraudControlModel = new FraudControlModel();
            using (var context = new PreCreditContext())
            {
                var app = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        FraudControls = x.FraudControls.Where(y => y.IsCurrentData),
                        x.FraudCheckStatus,
                        x.AgreementStatus,
                        x.WaitingForAdditionalInformationDate,
                        x.IsActive,
                        x.IsPartiallyApproved
                    })
                    .Single();

                fraudControlModel.fraudCheckStatus = app.FraudCheckStatus;
                fraudControlModel.agreementStatus = app.AgreementStatus;
                fraudControlModel.isApplicationWaitingForAdditionalInformation = app.WaitingForAdditionalInformationDate.HasValue;
                fraudControlModel.isApplicationPartiallyApproved = app.IsPartiallyApproved;
                fraudControlModel.isApplicationActive = app.IsActive;

                foreach (var applicantNr in Enumerable.Range(1, appModel.NrOfApplicants))
                {
                    var fraudControl = app
                        .FraudControls
                        .SingleOrDefault(x => x.ApplicantNr == applicantNr);
                    var status = fraudControl?.Status ?? FraudCheckStatusCode.Initial;

                    string GetBackTarget(bool isNew, bool continueExisting)
                    {
                        return NTechNavigationTarget.CreateCrossModuleNavigationTargetCode(
                            $"UnsecuredLoanFraudCheck{(isNew ? "New" : "View")}",
                            new Dictionary<string, string>
                            {
                                { "applicationNr", applicationNr },
                                { "continueExisting", continueExisting ? "True" : "False" },
                                { "applicantNr", applicantNr.ToString() },
                            });
                    }
                    var a = new FraudControlModel.Applicant
                    {
                        status = status,
                        newUrl = urlService.ActionStrict("FraudCheckNew", "CreditManagement", new { applicationNr, backTarget = GetBackTarget(true, false), applicantNr = applicantNr, continueExisting = false }),
                        continueUrl = urlService.ActionStrict("FraudCheckNew", "CreditManagement", new { applicationNr, backTarget = GetBackTarget(true, true), applicantNr = applicantNr, continueExisting = true }),
                        viewUrl = urlService.ActionStrict("FraudCheckView", "CreditManagement", new { applicationNr, backTarget = GetBackTarget(false, false), applicantNr = applicantNr }),
                    };
                    if (applicantNr == 1)
                        fraudControlModel.applicant1 = a;
                    else if (applicantNr == 2)
                        fraudControlModel.applicant2 = a;
                }
            }
            return fraudControlModel;
        }
    }

    public class FraudControlModel
    {
        public Applicant applicant1 { get; set; }
        public Applicant applicant2 { get; set; }
        public string fraudCheckStatus { get; set; }
        public string agreementStatus { get; set; }
        public bool isApplicationWaitingForAdditionalInformation { get; set; }
        public bool isApplicationPartiallyApproved { get; set; }
        public bool isApplicationActive { get; set; }

        public class Applicant
        {
            public string status { get; set; }
            public string newUrl { get; set; }
            public string continueUrl { get; set; }
            public string viewUrl { get; set; }
        }
    }

    public interface IFraudModelService
    {
        FraudControlModel GetFraudControlModel(string applicationNr);
    }
}