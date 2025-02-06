using nCredit;
using nCredit.Code.Services;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services.SwedishMortgageLoans
{
    public class SwedishMortgageLoanCollateralReportService
    {
        private readonly CreditContextFactory creditContextFactory;

        public SwedishMortgageLoanCollateralReportService(CreditContextFactory creditContextFactory)
        {
            this.creditContextFactory = creditContextFactory;
        }

        public DocumentClientExcelRequest CreateReportRequest(SwedishMortgageLoanCollateralReportRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var collaterals = context.CollateralHeadersQueryable;
                if (request?.CreditNr != null)
                {
                    collaterals = collaterals.Where(x => x.Credits.Any(y => y.CreditNr == request.CreditNr));
                }
                var toDate = (request?.Date ?? context.CoreClock.Today).Date;

                var reportRowsPre = collaterals
                    .Select(x => new
                    {
                        Collateral = x,
                        Credits = x
                            .Credits
                            .Where(y => y.CreatedByEvent.TransactionDate <= toDate)
                            .Select(y => new
                            {
                                y.CreditNr,
                                CurrentStatus = y
                                    .DatedCreditStrings
                                    .Where(z => z.Name == DatedCreditStringCode.CreditStatus.ToString())
                                    .OrderByDescending(z => z.BusinessEventId)
                                    .Select(z => z.Value)
                                    .FirstOrDefault(),
                                CurrentCapitalDebt = y
                                    .Transactions
                                    .Where(z => z.AccountCode == TransactionAccountType.CapitalDebt.ToString() && z.TransactionDate <= toDate)
                                    .Sum(z => (decimal?)z.Amount)
                            })
                    })
                    .Select(x => new
                    {
                        CollateralId = x.Collateral.Id,
                        x.Credits,
                        CollateralItems = x.Collateral.Items.Where(y => y.CreatedByEvent.TransactionDate <= toDate)
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var originalCollateralSeMlAmortBasisKeyValueItemKey = x
                            .CollateralItems
                            //Ignores removed sinced an initial removed value is still the initial value
                            .Where(y => y.ItemName == "seMlAmortBasisKeyValueItemKey")
                            .OrderBy(y => y.Id)
                            .Select(y => y.StringValue)
                            .FirstOrDefault();

                        var currentCollateralItems = MortgageLoanCollateralService
                            .GetCurrentCollateralItems(x.CollateralItems)
                            .ToDictionary(y => y.Key, y => y.Value.StringValue);

                        return new
                        {
                            x.CollateralId,
                            ActiveCredits = x.Credits.Where(y => y.CurrentStatus == CreditStatus.Normal.ToString()).ToList(),
                            ActiveCreditCredit = x,
                            CurrentCollateralItems = currentCollateralItems,
                            OriginalCollateralSeMlAmortBasisKeyValueItemKey = originalCollateralSeMlAmortBasisKeyValueItemKey,
                            CurrentCollateralSeMlAmortBasisKeyValueItemKey = currentCollateralItems.Opt("seMlAmortBasisKeyValueItemKey"),
                        };
                    })
                    .Where(x => x.ActiveCredits.Count > 0)
                    .ToList();

                var allAmortBasisKeys = reportRowsPre
                    .Select(x => x.CurrentCollateralSeMlAmortBasisKeyValueItemKey)
                    .Concat(reportRowsPre.Select(y => y.OriginalCollateralSeMlAmortBasisKeyValueItemKey))
                    .Where(x => x != null)
                    .ToHashSetShared();

                var amortBasisDataByKey = context
                    .KeyValueItemsQueryable
                    .Where(y => allAmortBasisKeys.Contains(y.Key) && y.KeySpace == KeyValueStoreKeySpaceCode.SeMortgageLoanAmortzationBasisV1.ToString())
                    .Select(x => new { x.Key, x.Value })
                    .ToDictionary(x => x.Key, x => x.Value);

                var reportRows = reportRowsPre.Select(x =>
                {
                    var currentAmortizationBasisData = x.CurrentCollateralSeMlAmortBasisKeyValueItemKey == null
                        ? null
                        : amortBasisDataByKey.Opt(x.CurrentCollateralSeMlAmortBasisKeyValueItemKey);
                    var originalAmortizationBasisData = x.OriginalCollateralSeMlAmortBasisKeyValueItemKey == null
                        ? null
                        : amortBasisDataByKey.Opt(x.OriginalCollateralSeMlAmortBasisKeyValueItemKey);

                    var currentAmortizationBasis = SwedishMortgageLoanAmortizationBasisStorageModel.Parse(currentAmortizationBasisData);
                    var originalAmortizationBasis = SwedishMortgageLoanAmortizationBasisStorageModel.Parse(originalAmortizationBasisData);

                    return new
                    {
                        ActiveCreditNrs = x.ActiveCredits.Select(y => y.CreditNr).ToList(),
                        CollateralObjectTypeCode = x.CurrentCollateralItems.Opt("objectTypeCode"),
                        CollateralObjectId = x.CurrentCollateralItems.Opt("objectId"),
                        CollateralPropertyId = MortgageLoanCollateralService.GetPropertyId(x.CurrentCollateralItems, false),
                        CollateralSeTaxOfficeApartmentNr = x.CurrentCollateralItems.Opt("seTaxOfficeApartmentNr"),
                        CollateralSeBrfName = x.CurrentCollateralItems.Opt("seBrfName"),
                        CollateralSeBrfOrgNr = x.CurrentCollateralItems.Opt("seBrfOrgNr"),
                        CollateralObjectAddressZipcode = x.CurrentCollateralItems.Opt("objectAddressZipcode"),
                        CollateralObjectAddressMunicipality = x.CurrentCollateralItems.Opt("objectAddressMunicipality"),
                        OriginalObjectValue = originalAmortizationBasis?.Model?.ObjectValue,
                        OriginalObjectValueDate = originalAmortizationBasis?.Model?.ObjectValueDate,
                        CurrentObjectValue = currentAmortizationBasis?.Model?.ObjectValue,
                        CurrentObjectValueDate = currentAmortizationBasis?.Model?.ObjectValueDate,
                        CurrentCapitalDebt = x.ActiveCredits.Sum(y => y.CurrentCapitalDebt),
                        CurrentLtvFraction = currentAmortizationBasis?.Model?.LtvFraction,
                    };
                })
                .ToList();

                var sheet = new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"Collateral-{request.Date.Value.ToString("yyyy-MM-dd")}"
                };

                sheet.SetColumnsAndData(reportRows,
                    reportRows.Col(x => string.Join(", ", x.ActiveCreditNrs), ExcelType.Text, "Aktiva lån"),
                    reportRows.Col(x => x.CollateralObjectTypeCode == "seBrf" ? "Bostadsrätt" : "Fastighet", ExcelType.Text, "Typ av objekt"),
                    reportRows.Col(x => x.CollateralPropertyId, ExcelType.Text, "Objekt id"),
                    reportRows.Col(x => x.CollateralObjectId, ExcelType.Text, "Fastighetsbeteckning"),
                    reportRows.Col(x => x.CollateralSeTaxOfficeApartmentNr, ExcelType.Text, "Skv. lgh. nr"),
                    reportRows.Col(x => x.CollateralSeBrfName, ExcelType.Text, "Brf namn"),
                    reportRows.Col(x => x.CollateralSeBrfOrgNr, ExcelType.Text, "Brf orgnr"),
                    reportRows.Col(x => x.CollateralObjectAddressZipcode, ExcelType.Text, "Objekt postnr"),
                    reportRows.Col(x => x.CollateralObjectAddressMunicipality, ExcelType.Text, "Objekt kommun"),
                    reportRows.Col(x => x.OriginalObjectValue, ExcelType.Number, "Objekt ursp. värde"),
                    reportRows.Col(x => x.OriginalObjectValueDate, ExcelType.Date, "Objekt ursp. värde - datum"),
                    reportRows.Col(x => x.CurrentObjectValue, ExcelType.Number, "Objekt nuvarande värde"),
                    reportRows.Col(x => x.CurrentObjectValueDate, ExcelType.Date, "Objekt nuvarande värde - datum"),
                    reportRows.Col(x => x.CurrentCapitalDebt, ExcelType.Number, "Nuvarande kapitalskuld"),
                    reportRows.Col(x => x.CurrentLtvFraction, ExcelType.Percent, "Nuvarande LTV"));

                return new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[] { sheet }
                };
            }
        }
    }

    public class SwedishMortgageLoanCollateralReportRequest
    {
        public DateTime? Date { get; set; }
        public string CreditNr { get; set; }
    }
}
