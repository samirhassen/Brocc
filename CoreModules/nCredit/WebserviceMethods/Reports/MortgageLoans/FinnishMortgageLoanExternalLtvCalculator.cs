using System;
using System.Globalization;

namespace nCredit.WebserviceMethods.Reports
{
    public class FinnishMortgageLoanExternalLtvCalculator
    {
        public class LtvDataModel
        {
            public decimal ObjectInternalMortgageLoanBalance { get; set; }
            public decimal ObjectHousingCompanyLoanBalance { get; set; }
            public decimal ObjectExternalValue { get; set; }
            public decimal OtherExternalValue { get; set; }
            public decimal OtherHousingCompanyLoansBalance { get; set; }
            public decimal OtherSecurityElsewhereAmount { get; set; }
            public decimal ObjectOtherLoans { get; set; }
        }

        public decimal CalculateLtv(LtvDataModel model, Action<string> observeFormula)
        {
            /*
ExternalLtv = ObjectMortgageLoans / (ObjectSecurity + OtherSecurites - ObjectOtherLoans)

ObjectMortgageLoans = ObjectInternalLoanBalance + ObjectHousingCompanyLoanBalance

ObjectSecurity = ObjectExternalValue + ObjectHousingCompanyLoanBalance

OtherSecurites = Min(OtherSecuritiesValue, ObjectMortgageLoans)

OtherSecuritiesValue = [summera över alla andra säkerheter] ExternalValue - HousingCompanyLoans - SecurityElsewhereAmount

ObjectOtherLoans = [summera över alla lån med objektet som säkerhet som är blankobolån] InternalLoanBalance

             */
            var objectMortgageLoans = (model.ObjectInternalMortgageLoanBalance + model.ObjectHousingCompanyLoanBalance);
            var objectSecurity = (model.ObjectExternalValue + model.ObjectHousingCompanyLoanBalance);
            var otherSecurites = Math.Min((model.OtherExternalValue - model.OtherHousingCompanyLoansBalance - model.OtherSecurityElsewhereAmount), model.ObjectInternalMortgageLoanBalance);

            if (observeFormula != null)
            {
                Func<decimal?, string> f = s => s?.ToString("F2", CultureInfo.InvariantCulture);
                observeFormula($"({f(model.ObjectInternalMortgageLoanBalance)} + {f(model.ObjectHousingCompanyLoanBalance)}) / (({f(model.ObjectExternalValue)} + {f(model.ObjectHousingCompanyLoanBalance)}) + Min({f(model.OtherExternalValue)} - {f(model.OtherHousingCompanyLoansBalance)} - {f(model.OtherSecurityElsewhereAmount)}, {f(model.ObjectInternalMortgageLoanBalance)}) - {f(model.ObjectOtherLoans)})");
            }

            return objectMortgageLoans / (objectSecurity + otherSecurites - model.ObjectOtherLoans);
        }
    }
}