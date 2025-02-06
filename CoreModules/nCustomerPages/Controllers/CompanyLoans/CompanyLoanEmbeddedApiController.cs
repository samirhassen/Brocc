using nCustomerPages;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("api/company-loans")]
    public class CompanyLoanEmbeddedApiController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsCompanyLoansEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        public class CreateApplicationRequestModel
        {
            [Required]
            public int? RequestedAmount { get; set; }

            [Required]
            public int? RequestedRepaymentTimeInMonths { get; set; }

            [Required]
            public string LoginSessionDataToken { get; set; }

            [Required]
            public CustomerModel Customer { get; set; }

            [Required]
            public ApplicantModel Applicant { get; set; }

            public Dictionary<string, string> AdditionalApplicationProperties { get; set; }

            public Dictionary<string, string> ExternalVariables { get; set; }

            public bool? IncludeNewLoginSessionTokenOnOffer { get; set; }

            public class CustomerModel
            {
                [Required]
                public string Orgnr { get; set; }

                public string CompanyName { get; set; }
                public string Email { get; set; }
                public string Phone { get; set; }
            }

            public class ApplicantModel
            {
                [Required]
                public string Email { get; set; }

                [Required]
                public string Phone { get; set; }

                public DateTime? BirthDate { get; set; }
            }
        }

        [HttpPost]
        [Route("create-application")]
        public ActionResult CreateApplication(CreateApplicationRequestModel request)
        {
            var validator = new NTechWebserviceRequestValidator();
            var validationErrors = validator.Validate(request ?? new CreateApplicationRequestModel());
            if (validationErrors.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing: " + string.Join(", ", validationErrors.Select(x => x.Path)));
            }

            var pc = new nCustomerPages.Code.PreCreditClient(() => NEnv.SystemUserBearerToken);

            if (!nCustomerPages.Controllers.SignicatController.TryConsumeLoginSessionDataToken(request.LoginSessionDataToken, pc, out var sessionData))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var result = pc.CreateCompanyLoanApplication(new nCustomerPages.Code.CreateCompanyLoanApplicationRequest
            {
                CustomerIpAddress = this.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress,
                ProviderName = "self",
                Applicant = new nCustomerPages.Code.CreateCompanyLoanApplicationRequest.ApplicantModel
                {
                    CivicRegNr = sessionData.UserInfo.CivicRegNr,
                    FirstName = sessionData.UserInfo.FirstName,
                    LastName = sessionData.UserInfo.LastName,
                    Phone = request.Applicant.Phone,
                    Email = request.Applicant.Email,
                    BirthDate = request.Applicant.BirthDate
                },
                AdditionalApplicationProperties = request.AdditionalApplicationProperties,
                RequestedAmount = request.RequestedAmount.Value,
                RequestedRepaymentTimeInMonths = request.RequestedRepaymentTimeInMonths.Value,
                Customer = new nCustomerPages.Code.CreateCompanyLoanApplicationRequest.CompanyModel
                {
                    Orgnr = request.Customer.Orgnr,
                    CompanyName = request.Customer.CompanyName,
                    Email = request.Customer.Email,
                    Phone = request.Customer.Phone
                },
                ExternalVariables = request.ExternalVariables,
                SkipHideFromManualUserLists = false,
                SkipInitialScoring = NEnv.SkipInitialCompanyLoanScoring
            });

            string newLoginSessionDataToken = null;
            if (result.Offer != null)
            {
                newLoginSessionDataToken = nCustomerPages.Controllers.SignicatController.CreateLoginSessionDataToken(pc, sessionData);
            }

            return new RawJsonActionResult
            {
                JsonData = JsonConvert.SerializeObject(new
                {
                    result.ApplicationNr,
                    result.DecisionStatus,
                    result.Offer,
                    LoginSessionDataToken = newLoginSessionDataToken
                    //NOTE: RejectionCodes intentionally left out. Dont leak that directly.
                })
            };
        }

        [HttpPost]
        [Route("validate-bankaccountnr")]
        public ActionResult ValidateBankAccountNr(string bankAccountNr, string bankAccountNrType)
        {
            Func<ActionResult> invalid = () => new RawJsonActionResult
            {
                JsonData = JsonConvert.SerializeObject(new
                {
                    IsValid = false
                })
            };

            var typeParsed = Enums.Parse<BankAccountNumberTypeCode>(bankAccountNrType);

            if (!typeParsed.HasValue)
                return invalid();

            var p = new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry);

            if (!p.TryParseBankAccount(bankAccountNr, typeParsed.Value, out var nrParsed))
                return invalid();

            var bse = nrParsed as NTech.Banking.BankAccounts.Se.BankAccountNumberSe;

            var r = new ValidateBankAccountNrResult
            {
                IsValid = true,
                AccountNrType = nrParsed.AccountType.ToString(),
                NormalizedNr = nrParsed.FormatFor(null),
                DisplayFormattedNr = nrParsed.FormatFor("display"),
                BankName = bse?.BankName,
                ClearingNrPart = bse?.ClearingNr,
                AccountNrPart = bse?.AccountNr
            };

            return new RawJsonActionResult
            {
                JsonData = JsonConvert.SerializeObject(r)
            };
        }

        private class ValidateBankAccountNrResult
        {
            public bool IsValid { get; set; }
            public string AccountNrType { get; set; }
            public string NormalizedNr { get; set; }
            public string DisplayFormattedNr { get; set; }
            public string BankName { get; set; }
            public string AccountNrPart { get; set; }
            public string ClearingNrPart { get; set; }
        }
    }
}