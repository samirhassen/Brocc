using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using nSavings.Code;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui
{
    [NTechAuthorizeSavingsMiddle]
    public class AccountCreationRemarksController : NController
    {
        [HttpGet]
        [Route("Ui/AccountCreationRemarks")]
        public ActionResult Index()
        {
            var cc = new CustomerClient();

            using (var context = new DbModel.SavingsContext())
            {
                var frozenAccounts = context
                    .SavingsAccountHeaders
                    .Where(x => x.Status == SavingsAccountStatusCode.FrozenBeforeActive.ToString())
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        mainCustomerId = x.MainCustomerId,
                        savingsAccountNr = x.SavingsAccountNr,
                        creationDate = x.CreatedByEvent.TransactionDate,
                        reasonCodes = x.CreationRemarks.Select(y => y.RemarkCategoryCode).Distinct()
                    })
                    .ToList();

                ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(
                    JsonConvert.SerializeObject(new
                    {
                        fetchDetailsUrl = Url.Action("FetchAccountCreationRemarkDetails", "AccountCreationRemarks"),
                        resolveAccountCreationRemarksUrl =
                            Url.Action("ResolveAccountCreationRemarks", "AccountCreationRemarks"),
                        kycScreenUrl =
                            Url.Action("KycScreenForResolveAccountCreationRemarks", "AccountCreationRemarks"),
                        frozenAccounts
                    })));
                return View();
            }
        }

        [HttpPost]
        [Route("Api/FetchAccountCreationRemarkDetails")]
        public ActionResult FetchAccountCreationRemarkDetails(string savingsAccountNr)
        {
            var cc = new CustomerClient();

            bool IsFatca(string remarkCode) => (remarkCode ?? "").Equals(
                SavingsAccountCreationRemarkCode.UnknownTaxOrCitizenCountry.ToString(),
                StringComparison.OrdinalIgnoreCase);

            bool IsKycAttentionNeeded(string remarkCode) => (remarkCode ?? "").Equals(
                SavingsAccountCreationRemarkCode.KycAttentionNeeded.ToString(), StringComparison.OrdinalIgnoreCase);

            bool IsCustomerCheckpoint(string remarkCode) => (remarkCode ?? "").Equals(
                SavingsAccountCreationRemarkCode.CustomerCheckpoint.ToString(), StringComparison.OrdinalIgnoreCase);


            using (var context = new DbModel.SavingsContext())
            {
                var a = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        status = x.Status,
                        mainCustomerId = x.MainCustomerId,
                        savingsAccountNr = x.SavingsAccountNr,
                        creationDate = x.CreatedByEvent.TransactionDate,
                        reasons = x.CreationRemarks.Select(y => new
                        {
                            code = y.RemarkCategoryCode,
                            data = y.RemarkData
                        }),
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.savingsAccountNr,
                        x.creationDate,
                        x.mainCustomerId,
                        customerCardUrl = CustomerClient.GetCustomerCardUri(x.mainCustomerId).ToString(),
                        x.status,
                        reasons = x.reasons.Select(y =>
                        {
                            var data = y.data == null ? null : JsonConvert.DeserializeObject<RemarkData>(y.data);
                            return new
                            {
                                y.code,
                                customerId = data?.customerId,
                                savingsAccountNr = data?.savingsAccountNr,
                                customerContactInfoSourceWarningCode = data?.customerContactInfoSourceWarningCode,
                                customerContactInfoSourceWarningMessage = data?.customerContactInfoSourceWarningMessage
                            };
                        }).Select(y => new
                        {
                            y.code,
                            y.customerId,
                            customerCardUrl =
                                y.customerId.HasValue && !IsFatca(y.code) && !IsKycAttentionNeeded(y.code) &&
                                !IsCustomerCheckpoint(y.code)
                                    ? CustomerClient.GetCustomerCardUri(y.customerId.Value).ToString()
                                    : null,
                            customerFatcaCrsUri = y.customerId.HasValue && IsFatca(y.code)
                                ? CustomerClient.GetCustomerFatcaCrsUri(y.customerId.Value).ToString()
                                : null,
                            customerPepKycUrl = y.customerId.HasValue && IsKycAttentionNeeded(y.code)
                                ? CustomerClient.GetCustomerPepKycUrl(y.customerId.Value, null).ToString()
                                : null,
                            customerCheckpointUrl = y.customerId.HasValue && IsCustomerCheckpoint(y.code)
                                ? NEnv.ServiceRegistry.Internal.ServiceUrl("nBackoffice",
                                    $"s/customer-checkpoints/for-customer/{y.customerId.Value}").ToString()
                                : null,
                            y.savingsAccountNr,
                            savingsAccountUrl = y.savingsAccountNr != null
                                ? CreateLinkToSavingsAccountDetails(y.savingsAccountNr)
                                : null,
                            y.customerContactInfoSourceWarningCode,
                            y.customerContactInfoSourceWarningMessage
                        })
                    })
                    .Single();

                var customer = cc.GetCustomerCardItems(a.mainCustomerId, "firstName", "lastName", "email", "phone");
                var latestKycScreenResult = cc.FetchLatestKycScreenResult(a.mainCustomerId);

                return Json2(new
                {
                    account = a,
                    customer = customer,
                    latestKycScreenResult = latestKycScreenResult
                });
            }
        }

        private class RemarkData
        {
            public int? customerId { get; set; }
            public string savingsAccountNr { get; set; }
            public string customerContactInfoSourceWarningCode { get; set; }
            public string customerContactInfoSourceWarningMessage { get; set; }
        }

        [HttpPost]
        [Route("Api/ResolveAccountCreationRemarks")]
        public ActionResult ResolveAccountCreationRemarks(string savingsAccountNr, string resolutionAction)
        {
            var mgr = new ResolveAccountCreationRemarksBusinessEventManager(GetCurrentUserMetadata(),
                CoreClock.SharedInstance, Service.ContextFactory,
                NEnv.ClientCfgCore);

            return !mgr.TryResolve(savingsAccountNr, resolutionAction, out var failedMessage)
                ? new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage)
                : Json2(new { });
        }

        [HttpPost]
        [Route("Api/KycScreenForResolveAccountCreationRemarks")]
        public ActionResult KycScreenForResolveAccountCreationRemarks(int? customerId, bool? force)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }

            var cc = new CustomerClient();
            var result = cc.ListScreenBatch(new[] { customerId.Value }.ToHashSet(), Clock.Today);
            var failedReason = result.FailedToGetTrapetsDataItems?.FirstOrDefault()?.Reason;
            if (failedReason != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "KYC screen failed: " + failedReason);
            }

            var latestKycScreenResult = cc.FetchLatestKycScreenResult(customerId.Value);
            return Json2(new { latestKycScreenResult });
        }
    }
}