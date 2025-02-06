using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class EditApplicationGeneralDataMethod : TypedWebserviceMethod<EditApplicationGeneralDataMethod.Request, EditApplicationGeneralDataMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Edit-Application-General-Data";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var listService = resolver.Resolve<IComplexApplicationListService>();

            var complexListChanges = new List<ComplexApplicationListOperation>();

            void AddApplicationEdit(string itemName, string newValue) => complexListChanges.Add(
              new ComplexApplicationListOperation
              {
                  ApplicationNr = request.ApplicationNr,
                  Nr = 1,
                  ListName = "Application",
                  IsDelete = string.IsNullOrWhiteSpace(newValue),
                  ItemName = itemName,
                  UniqueValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue
              });

            AddApplicationEdit("isPurchase", request.IsPurchase ? "true" : "false");
            AddApplicationEdit("ownSavingsAmount", request.OwnSavingsAmount?.ToString());
            AddApplicationEdit("objectPriceAmount", request.ObjectPriceAmount?.ToString());
            AddApplicationEdit("paidToCustomerAmount", request.AdditionalLoanAmount?.ToString());
            AddApplicationEdit("objectValueAmount", request.ObjectValueAmount?.ToString());

            listService.ChangeList(complexListChanges);

            return new Response();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            public bool IsPurchase { get; set; }
            public int? AdditionalLoanAmount { get; set; }
            public int? OwnSavingsAmount { get; set; }
            public int? ObjectPriceAmount { get; set; }
            public int? ObjectValueAmount { get; set; }

        }

        public class Response { }

    }

}