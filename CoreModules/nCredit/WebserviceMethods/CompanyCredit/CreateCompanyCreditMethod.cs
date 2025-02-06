using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Core;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCredit.WebserviceMethods
{
    public class CreateCompanyCreditMethod : TypedWebserviceMethod<CreateCompanyCreditMethod.Request, CreateCompanyCreditMethod.Response>
    {
        public override string Path => "CompanyCredit/Create";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var user = requestContext.CurrentUserMetadata();

            var services = requestContext.Service();
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
            var mgr = new NewCreditBusinessEventManager(user, services.LegalInterestCeiling, services.CreditCustomerListService,
                services.GetEncryptionService(user), new CoreClock(), NEnv.ClientCfgCore, customerClient, services.CreateOcrPaymentReferenceGenerator(user), NEnv.EnvSettings,
                services.PaymentAccount, services.CustomCostType);

            var r = ConvertRequestToInternal(request);

            using (var context = new CreditContextExtended(user, requestContext.Clock()))
            {
                var credit = context.UsingTransaction(() =>
                {
                    var model = new DomainModel.SharedDatedValueDomainModel(context);
                    var getReferenceInterest = new Lazy<decimal>(() => model.GetReferenceInterestRatePercent(requestContext.Clock().Today));

                    var creditCreated = mgr.CreateNewCredit(context, r, getReferenceInterest);
                    context.SaveChanges();
                    return creditCreated;
                });

                if (credit != null)
                {
                    services.CustomerRelationsMerge.MergeLoansToCustomerRelations(onlyTheseCreditNrs: new HashSet<string> { credit.CreditNr });
                }

                return new Response
                {
                    CreditNr = credit?.CreditNr
                };
            }
        }

        public static NewCreditRequest ConvertRequestToInternal(Request request)
        {
            return new NewCreditRequest
            {
                IsCompanyCredit = true,
                ApplicationNr = request.ApplicationNr,
                BankAccountNr = request.BankAccountNr,
                Iban = request.Iban,
                BankAccountNrType = request.BankAccountNrType,
                CapitalizedInitialFeeAmount = request.CapitalizedInitialFeeAmount,
                DrawnFromLoanAmountInitialFeeAmount = request.DrawnFromLoanAmountInitialFeeAmount,
                CreditAmount = request.CreditAmount,
                NrOfApplicants = 1,
                ProviderApplicationId = request.ProviderApplicationId,
                CampaignCode = request.CampaignCode,
                CreditNr = request.CreditNr,
                MarginInterestRatePercent = request.MarginInterestRatePercent.Value,
                NotificationFee = request.NotificationFee,
                ProviderName = request.ProviderName,
                SourceChannel = request.SourceChannel,
                AnnuityAmount = request.AnnuityAmount.Value,
                Applicants = new List<NewCreditRequest.Applicant>
                {
                    new NewCreditRequest.Applicant
                    {
                        ApplicantNr = 1,
                        CustomerId = request.CompanyCustomerId.Value
                    }
                },
                SharedAgreementPdfArchiveKey = request.AgreementPdfArchiveKey,
                CompanyLoanApplicantCustomerIds = request.CompanyLoanApplicantCustomerIds,
                CompanyLoanAuthorizedSignatoryCustomerIds = request.CompanyLoanAuthorizedSignatoryCustomerIds,
                CompanyLoanBeneficialOwnerCustomerIds = request.CompanyLoanBeneficialOwnerCustomerIds,
                CompanyLoanCollateralCustomerIds = request.CompanyLoanCollateralCustomerIds,
                ApplicationFreeformDocumentArchiveKeys = request.ApplicationFreeformDocumentArchiveKeys,
                SniKodSe = request.SniKodSe
            };
        }

        public class Request
        {
            [Required]
            public decimal? AnnuityAmount { get; set; }
            [Required]
            public string CreditNr { get; set; }
            public string Iban { get; set; }
            public string BankAccountNr { get; set; }
            public string BankAccountNrType { get; set; }
            [Required]
            public string ProviderName { get; set; }
            [Required]
            public decimal CreditAmount { get; set; }
            public decimal? CapitalizedInitialFeeAmount { get; set; }
            public decimal? DrawnFromLoanAmountInitialFeeAmount { get; set; }
            public decimal? NotificationFee { get; set; }
            [Required]
            public decimal? MarginInterestRatePercent { get; set; }
            public string AgreementPdfArchiveKey { get; set; }
            [Required]
            public int? CompanyCustomerId { get; set; }
            public string ProviderApplicationId { get; set; }
            public string ApplicationNr { get; set; }
            public string CampaignCode { get; set; }
            public string SourceChannel { get; set; }
            public List<int> CompanyLoanApplicantCustomerIds { get; set; }
            public List<int> CompanyLoanAuthorizedSignatoryCustomerIds { get; set; }
            public List<int> CompanyLoanBeneficialOwnerCustomerIds { get; set; }
            public List<int> CompanyLoanCollateralCustomerIds { get; set; }
            public List<string> ApplicationFreeformDocumentArchiveKeys { get; set; }
            public string SniKodSe { get; set; }
        }

        public class Response
        {
            public string CreditNr { get; set; }
        }
    }
}