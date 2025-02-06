using nPreCredit.DbModel;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nPreCredit.Controllers.Api
{
    public partial class ApiUpdateDataWarehouseController
    {
        private void Merge_Dimension_CreditApplication()
        {
            Func<List<CreditApplicationDimensionModel>, PreCreditContext, List<ExpandoObject>> toDwItems = (items, context) =>
                {
                    var encryptedItems = items.SelectMany(x => x.FetchedItems).Where(x => x.IsEncrypted).ToList();
                    if (encryptedItems.Any())
                    {
                        var decryptedValues = EncryptionContext.Load(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray(), NEnv.EncryptionKeys.AsDictionary());
                        foreach (var item in encryptedItems)
                        {
                            item.Value = decryptedValues[long.Parse(item.Value)];
                        }
                    }
                    Func<IEnumerable<CreditApplicationDimensionModel.Item>, string, string, string> getAppItemValue = (i, g, n) =>
                        i.Where(y => y.GroupName == g && y.Name == n).Select(y => y.Value).SingleOrDefault();

                    return items.Select(item =>
                    {
                        int? applicant1Age = null;
                        bool? applicant1IsMale = null;
                        int? applicant2Age = null;
                        bool? applicant2IsMale = null;

                        ICivicRegNumber applicant1CivicRegNr;
                        if (NEnv.BaseCivicRegNumberParser.TryParse(getAppItemValue(item.FetchedItems, "applicant1", "civicRegNr"), out applicant1CivicRegNr))
                        {
                            applicant1Age = GetFullYearsBetween(applicant1CivicRegNr.BirthDate.Value, item.ApplicationDate.Date);
                            applicant1IsMale = applicant1CivicRegNr.IsMale;
                        }
                        if (item.NrOfApplicants > 1)
                        {
                            ICivicRegNumber applicant2CivicRegNr;
                            if (NEnv.BaseCivicRegNumberParser.TryParse(getAppItemValue(item.FetchedItems, "applicant2", "civicRegNr"), out applicant2CivicRegNr))
                            {
                                applicant2Age = GetFullYearsBetween(applicant2CivicRegNr.BirthDate.Value, item.ApplicationDate.Date);
                                applicant2IsMale = applicant2CivicRegNr.IsMale;
                            }
                        }
                        var e = new ExpandoObject();
                        dynamic ed = e;
                        ed.ApplicationNr = item.ApplicationNr;
                        ed.ProviderName = item.ProviderName;
                        ed.NrOfApplicants = item.NrOfApplicants;
                        ed.ApplicationDate = item.ApplicationDate.Date;
                        ed.ApplicationDateWithTime = item.ApplicationDate;
                        ed.Applicant1Age = applicant1Age;
                        ed.Applicant2Age = applicant2Age;
                        ed.Applicant1IsMale = applicant1IsMale;
                        ed.Applicant2IsMale = applicant2IsMale;
                        ed.Applicant1CustomerId = int.Parse(getAppItemValue(item.FetchedItems, "applicant1", "customerId"));
                        ed.Applicant2CustomerId = item.NrOfApplicants > 1 ? int.Parse(getAppItemValue(item.FetchedItems, "applicant2", "customerId")) : new int?();
                        return e;
                    }).ToList();
                };

            const string DimensionName = "CreditApplication";

            MergeDimension(
                c => ApplicationDimensionQuery(c),
                SystemItemCode.DwLatestMergedTimestamp_Dimension_CreditApplication,
                DimensionName,
                toDwItems,
                2000);
        }

        private class CreditApplicationDimensionModel : TimestampedItem
        {
            public DateTimeOffset ApplicationDate { get; internal set; }
            public string ApplicationNr { get; internal set; }
            public int NrOfApplicants { get; internal set; }
            public string ProviderName { get; internal set; }
            public IEnumerable<Item> FetchedItems { get; set; }
            public class Item
            {
                public string GroupName { get; set; }
                public string Name { get; set; }
                public string Value { get; set; }
                public bool IsEncrypted { get; set; }
            }
        }

        private static string[] ApplicationDimensionItemsNamesToFetch = new string[]
        {
            "civicRegNr", "customerId"
        };

        private IQueryable<CreditApplicationDimensionModel> ApplicationDimensionQuery(PreCreditContext context)
        {
            var cl = CreditApplicationTypeCode.companyLoan.ToString();
            return context.CreditApplicationHeaders.Where(x => !x.ArchivedDate.HasValue && x.ApplicationType != cl).Select(x => new CreditApplicationDimensionModel
            {
                ApplicationNr = x.ApplicationNr,
                ProviderName = x.ProviderName,
                NrOfApplicants = x.NrOfApplicants,
                ApplicationDate = x.ApplicationDate,
                FetchedItems = x.Items.Where(y => ApplicationDimensionItemsNamesToFetch.Contains(y.Name)).Select(y => new CreditApplicationDimensionModel.Item
                {
                    GroupName = y.GroupName,
                    IsEncrypted = y.IsEncrypted,
                    Name = y.Name,
                    Value = y.Value
                }),
                Timestamp = x.Timestamp
            });
        }
    }
}