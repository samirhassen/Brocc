using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class AddPropertyValuationMethod : TypedWebserviceMethod<AddPropertyValuationMethod.Request, AddPropertyValuationMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Add-Property-Valuation";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            const string ListName = "MortgageObjectValuation";

            string sourceRawDataArchiveKey = null;
            if (request.UcBvRawJsonData != null)
            {
                var documentClient = new nDocumentClient();

                sourceRawDataArchiveKey = documentClient.ArchiveStore(Encoding.UTF8.GetBytes(request.UcBvRawJsonData), "application/json", "RawValuationResponse.json");
            }

            return NTechPerServiceExclusiveLock.RunWithExclusiveLock($"{ListName}.{request.ApplicationNr}", () =>
            {
                using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
                {
                    var nextNr = (context
                        .ComplexApplicationListItems
                        .Where(x => x.ListName == ListName)
                        .Max(x => (int?)x.Nr) ?? 0) + 1;

                    var values = new Dictionary<string, string>
                    {
                        { "CreationDate", context.Clock.Now.ToString("o") },
                        { "SourceType", request.SourceType },
                        { "UcBvObjectId", request.UcBvObjectId },
                        { "UcBvTransId", request.UcBvTransId },
                        { "ValuationAmount", request.ValuationAmount?.ToString() },
                        { "ValuationPdfArchiveKey", request.UcBvValuationPdfArchiveKey },
                        { "SeBrfArsredovisningPdfArchiveKey", request.SeBrfArsredovisningPdfArchiveKey },
                        { "SourceRawDataArchiveKey", sourceRawDataArchiveKey },
                        { "ApartmentArea", request.ApartmentArea?.ToString() },
                        { "SeTaxOfficeApartmentNr", request.SeTaxOfficeApartmentNr },
                        { "EntityName", request.EntityName },
                        { "InskrivningJsonArchiveKey", request.InskrivningJsonArchiveKey }
                    };

                    values = values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value);

                    ComplexApplicationListService.SetUniqueItems(request.ApplicationNr, ListName, nextNr, values, context);

                    context.SaveChanges();

                    return new Response
                    {
                        RowNumber = nextNr
                    };
                }
            },
            () =>
            {
                return Error("Attempt to save several valuations at once");
            });
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string UcBvObjectId { get; set; }
            [Required]
            public string UcBvTransId { get; set; }
            [Required]
            public int? ValuationAmount { get; set; }
            public string UcBvValuationPdfArchiveKey { get; set; }
            public string UcBvRawJsonData { get; set; }
            [Required]
            public string SourceType { get; set; }
            public int? ApartmentArea { get; set; }
            public string SeTaxOfficeApartmentNr { get; set; }
            public string EntityName { get; set; }
            public string SeBrfArsredovisningPdfArchiveKey { get; set; }
            public string InskrivningJsonArchiveKey { get; set; }
        }

        public class Response
        {
            public int RowNumber { get; set; }
        }
    }
}