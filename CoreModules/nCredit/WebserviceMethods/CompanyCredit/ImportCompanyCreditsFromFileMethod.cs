using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace nCredit.WebserviceMethods
{
    public class ImportCompanyCreditsFromFileMethod : TypedWebserviceMethod<ImportCompanyCreditsFromFileMethod.Request, ImportCompanyCreditsFromFileMethod.Response>
    {
        public override string Path => "CompanyCredit/ImportOrPreviewFile";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.IsImportMode.Value == request.IsPreviewMode.Value)
                return Error("Exactly one of IsImportMode or IsPreviewMode must be true");

            var user = requestContext.CurrentUserMetadata();
            var services = requestContext.Service();

            
            var pr = CompanyLoanExcelFileImportOperation.BeginWithDataUrl(request.FileName, request.ExcelFileAsDataUrl, user,
                CoreClock.SharedInstance, NEnv.ClientCfgCore, services.DocumentClientHttpContext, services.ContextFactory, services.CustomerClientHttpContext, NEnv.EnvSettings);

            //Parse errors, dont continue with interpretation
            if (pr.Errors.Any())
                return Respond(pr, null, null);

            //Interpret
            var preview = pr.CheckConsistenyAndGeneratePreview();

            if (pr.Errors.Any())
                return Respond(pr, null, null);

            if (request.IsPreviewMode.Value)
            {
                return Respond(pr, new Response.PreviewModel
                {
                    Loans = preview.Item2,
                    Persons = preview.Item3,
                    Summaries = preview.Item1.Select(x => new Response.PreviewModel.Item
                    {
                        Key = x.Item1,
                        Value = x.Item2
                    }).ToList()
                }, null);
            }

            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
            var mgr = new NewCreditBusinessEventManager(user, services.LegalInterestCeiling, services.CreditCustomerListService, services.GetEncryptionService(user),
                new CoreClock(), NEnv.ClientCfgCore, customerClient, services.CreateOcrPaymentReferenceGenerator(user), NEnv.EnvSettings, services.PaymentAccount,
                services.CustomCostType);
            var creditNrs = pr.ImportLoans(mgr);

            return Respond(pr, null, new Response.ImportModel
            {
                CreditNrs = creditNrs.Select(x => new Response.ImportModel.Item { ImportedCreditNr = x.Item1, NewCreditNr = x.Item2 }).ToList()
            });
        }

        private Response Respond(
            CompanyLoanExcelFileImportOperation p,
            Response.PreviewModel preview,
            Response.ImportModel import)
        {
            return new Response
            {
                Shared = new Response.SharedModel
                {
                    Errors = p.Errors,
                    Warnings = p.Warnings
                },
                Import = import,
                Preview = preview
            };
        }

        public class Request
        {
            [Required]
            public string ExcelFileAsDataUrl { get; set; }
            [Required]
            public string FileName { get; set; }
            [Required]
            public bool? IsPreviewMode { get; set; }
            [Required]
            public bool? IsImportMode { get; set; }

            public bool? IncludeRaw { get; set; }
        }

        public class Response
        {
            public SharedModel Shared { get; set; }
            public PreviewModel Preview { get; set; }
            public ImportModel Import { get; set; }

            public class SharedModel
            {
                public List<string> Errors { get; set; }
                public List<string> Warnings { get; set; }
            }

            public class ImportModel
            {
                public List<Item> CreditNrs { get; set; }
                public class Item
                {
                    public string ImportedCreditNr { get; set; }
                    public string NewCreditNr { get; set; }
                }
            }

            public class PreviewModel
            {
                public List<Item> Summaries { get; set; }
                public List<CompanyLoanExcelFileImportOperation.LoanModel> Loans { get; set; }
                public List<CompanyLoanExcelFileImportOperation.PersonModel> Persons { get; set; }
                public class Item
                {
                    public string Key { get; set; }
                    public string Value { get; set; }
                }
            }
        }
    }
}