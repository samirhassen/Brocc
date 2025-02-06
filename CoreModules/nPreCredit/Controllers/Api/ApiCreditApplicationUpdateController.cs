using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public class ApiCreditApplicationUpdateController : NController
    {
        public class CreditApplicationUpdateRequest
        {
            public string ApplicationNr { get; set; }
            public Item[] Items { get; set; }
            public class Item
            {
                public string Group { get; set; }
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        private static Lazy<ISet<string>> AllowedApplicationFields = new Lazy<ISet<string>>(() => new HashSet<string>
            {
                NEnv.ClientCfg.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban"
            });

        private static ISet<string> AllowedDocumentFields = new HashSet<string>
            {
                "signed_initial_agreement_key", "signed_initial_agreement_date"
            };

        [Route("creditapplication/update")]
        [HttpPost]
        public ActionResult UpdateApplication(CreditApplicationUpdateRequest request)
        {
            try
            {
                List<string> errors = new List<string>();

                var u = this.User?.Identity as System.Security.Claims.ClaimsIdentity;

                if (u?.FindFirst("ntech.isprovider")?.Value == "true")
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Providers are not allowed access to this function");
                }

                if (request == null || request.Items == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing Items");

                var items = request.Items;

                var actualItems = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>();

                foreach (var item in items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Group) || string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Value))
                    {
                        errors.Add("Invalid item");
                    }
                    else if (item.Group.StartsWith("document") && AllowedDocumentFields.Contains(item.Name))
                    {
                        actualItems.Add(new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = item.Group,
                            Name = item.Name,
                            Value = item.Value,
                            IsSensitive = false
                        });
                    }
                    else if (item.Group == "application" && AllowedApplicationFields.Value.Contains(item.Name))
                    {
                        actualItems.Add(new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = item.Group,
                            Name = item.Name,
                            Value = item.Value,
                            IsSensitive = false
                        });
                    }
                    else
                    {
                        errors.Add($"Invalid item {item.Group}.{item.Name}");
                    }
                }

                if (errors.Count > 0)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(", ", errors));
                }

                var repo = DependancyInjection.Services.Resolve<UpdateCreditApplicationRepository>();

                repo.UpdateApplication(request.ApplicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                {
                    StepName = "UpdateApplication",
                    InformationMetadata = InformationMetadata,
                    UpdatedByUserId = CurrentUserId,
                    Items = actualItems
                });

                if (errors.Count > 0)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(", ", errors));
                }

                UpdateAgreementStatus(request.ApplicationNr);
                UpdateCustomerCheckStatus(request.ApplicationNr);

                return Json(new { });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to update application");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}