using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.MortgageLoanStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class EditMortgageLoanStandardPropertyMethod : TypedWebserviceMethod<EditMortgageLoanStandardPropertyMethod.Request, EditMortgageLoanStandardPropertyMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Edit-Property";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var s = resolver.Resolve<IComplexApplicationListService>();

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
            var objectTypeCode = Enums.Parse<MortgageLoanStandardApplicationCreateRequest.ObjectTypeCodeValue>(request.ObjectTypeCode, ignoreCase: true);
            string SkipUnlessSeBrf(Func<string> value) => objectTypeCode == MortgageLoanStandardApplicationCreateRequest.ObjectTypeCodeValue.seBrf
                ? value()?.NormalizeNullOrWhitespace()
                : null;

            AddApplicationEdit("objectTypeCode", request.ObjectTypeCode);
            AddApplicationEdit("seBrfName", SkipUnlessSeBrf(() => request.SeBrfName));
            AddApplicationEdit("seBrfOrgNr", SkipUnlessSeBrf(() =>
                string.IsNullOrWhiteSpace(request.SeBrfOrgNr) ? null : NEnv.BaseOrganisationNumberParser.Parse(request.SeBrfOrgNr).NormalizedValue));
            AddApplicationEdit("seBrfApartmentNr", SkipUnlessSeBrf(() => request.SeBrfApartmentNr));
            AddApplicationEdit("seTaxOfficeApartmentNr", SkipUnlessSeBrf(() => request.SeTaxOfficeApartmentNr));
            AddApplicationEdit("objectLivingArea", request.ObjectLivingArea?.ToString());
            AddApplicationEdit("objectMonthlyFeeAmount", request.ObjectMonthlyFeeAmount?.ToString());
            AddApplicationEdit("objectOtherMonthlyCostsAmount", request.ObjectOtherMonthlyCostsAmount?.ToString());

            AddApplicationEdit("objectAddressStreet", request.ObjectAddressStreet);
            AddApplicationEdit("objectAddressZipcode", request.ObjectAddressZipcode);
            AddApplicationEdit("objectAddressCity", request.ObjectAddressCity);
            AddApplicationEdit("objectAddressMunicipality", request.ObjectAddressMunicipality);

            using (var context = new PreCreditContext())
            {
                complexListChanges.AddRange(GetMortgageLoansChanges(context, request));
            }

            s.ChangeList(complexListChanges);

            return new Response
            {

            };
        }

        private List<ComplexApplicationListOperation> GetMortgageLoansChanges(PreCreditContext context, Request request)
        {
            const string LoansListName = "MortgageLoansToSettle";
            var currentLoansItems = context
                .ComplexApplicationListItems
                .Where(x => x.ApplicationNr == request.ApplicationNr && x.ListName == LoansListName)
                .ToList();
            var newLoansItems = (request.MortgageLoansToSettle ?? new List<Request.MortgageLoanModel>()).Select(x =>
            {
                var d = new Dictionary<string, string>();

                if (x.CurrentDebtAmount.HasValue)
                    d["currentDebtAmount"] = x.CurrentDebtAmount.Value.ToString(CultureInfo.InvariantCulture);

                if (x.CurrentMonthlyAmortizationAmount.HasValue)
                    d["currentMonthlyAmortizationAmount"] = x.CurrentMonthlyAmortizationAmount.Value.ToString(CultureInfo.InvariantCulture);

                if (x.ShouldBeSettled.HasValue)
                    d["shouldBeSettled"] = x.ShouldBeSettled.Value ? "true" : "false";

                if (!string.IsNullOrWhiteSpace(x.BankName))
                    d["bankName"] = x.BankName.NormalizeNullOrWhitespace();

                if (x.InterestRatePercent.HasValue)
                    d["interestRatePercent"] = x.InterestRatePercent.Value.ToString(CultureInfo.InvariantCulture);

                if (!string.IsNullOrWhiteSpace(x.LoanNumber))
                    d["loanNumber"] = x.LoanNumber.NormalizeNullOrWhitespace();

                return d;
            }).ToList();
            return ComplexApplicationListService.SynchListTreatedAsArray(request.ApplicationNr, LoansListName, currentLoansItems, newLoansItems);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [EnumCode(EnumType = typeof(MortgageLoanStandardApplicationCreateRequest.ObjectTypeCodeValue))]
            public string ObjectTypeCode { get; set; }
            public string SeBrfName { get; set; }

            [OrgNr()]
            public string SeBrfOrgNr { get; set; }
            public string SeBrfApartmentNr { get; set; }
            public string SeTaxOfficeApartmentNr { get; set; }
            public int? ObjectLivingArea { get; set; }
            public int? ObjectMonthlyFeeAmount { get; set; }
            public int? ObjectOtherMonthlyCostsAmount { get; set; }
            public string ObjectAddressStreet { get; set; }
            public string ObjectAddressZipcode { get; set; }
            public string ObjectAddressCity { get; set; }
            public string ObjectAddressMunicipality { get; set; }

            public List<MortgageLoanModel> MortgageLoansToSettle { get; set; }

            public class MortgageLoanModel
            {
                public string BankName { get; set; }
                public int? CurrentDebtAmount { get; set; }
                public int? CurrentMonthlyAmortizationAmount { get; set; }
                public decimal? InterestRatePercent { get; set; }
                public bool? ShouldBeSettled { get; set; }
                public string LoanNumber { get; set; }
            }
        }

        public class Response
        {

        }
    }
}