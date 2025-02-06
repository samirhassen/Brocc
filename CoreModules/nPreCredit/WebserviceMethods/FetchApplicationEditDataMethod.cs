using Newtonsoft.Json;
using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchApplicationEditDataMethod : TypedWebserviceMethod<FetchApplicationEditDataMethod.Request, FetchApplicationEditDataMethod.Response>
    {
        public override string Path => "Application/Edit/FetchItemData";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var currentValue = resolver.Resolve<ApplicationDataSourceService>().GetData(
                request.ApplicationNr,
                new ApplicationDataSourceServiceRequest
                {
                    DataSourceName = request.DataSourceName,
                    MissingItemStrategy = ApplicationDataSourceMissingItemStrategy.UseDefaultValue,
                    Names = new HashSet<string> { request.ItemName },
                    GetDefaultValue = _ => request.DefaultValueIfMissing
                })[request.DataSourceName][request.ItemName];

            var r = new Response
            {
                ApplicationNr = request.ApplicationNr,
                ItemValue = currentValue
            };

            if (request.IncludeEdits.GetValueOrDefault())
            {
                using (var context = new PreCreditContext())
                {
                    if (request.DataSourceName == CreditApplicationItemDataSource.DataSourceNameShared)
                    {
                        var groupAndItemName = CreditApplicationItemDataSource.GroupNameAndItemNameFromName(request.ItemName, request.ApplicationNr);
                        var groupName = groupAndItemName.Item1;
                        var itemName = groupAndItemName.Item2;
                        r.HistoricalChanges = GetApplicationItemHistory(request.ApplicationNr, groupName, itemName, currentValue, context);
                    }
                    else if (request.DataSourceName == BankAccountTypeAndNrCreditApplicationItemDataSource.DataSourceNameShared)
                    {
                        r.HistoricalChanges = GetBankAccountTypeAndNrHistory(request, currentValue, context);
                    }
                }
            }

            r.EditModel = resolver.Resolve<ICreditApplicationCustomEditableFieldsService>().GetFieldModel(request.DataSourceName, request.ItemName);

            return r;
        }

        private static List<Response.HistoricalChangeModel> GetBankAccountTypeAndNrHistory(Request request, string currentValue, PreCreditContext context)
        {
            var currentValues = BankAccountTypeAndNrCreditApplicationItemDataSource.SeparateCombinedValues(currentValue);
            var typeItems = GetApplicationItemHistory(request.ApplicationNr, "application", "bankAccountNrType", currentValues.Item1, context);
            var nrItems = GetApplicationItemHistory(request.ApplicationNr, "application", "bankAccountNr", currentValues.Item2, context);

            return typeItems
                .Select(x => new { i = x, t = true })
                .Concat(nrItems.Select(y => new { i = y, t = false }))
                .Where(x => x.i.EventId.HasValue)
                .GroupBy(y => y.i.EventId.Value)
                .OrderByDescending(y => y.Key)
                .Select(x =>
                {
                    var typeItem = x.Single(y => y.t);
                    var nrItem = x.Single(y => !y.t);
                    nrItem.i.FromValue = BankAccountTypeAndNrCreditApplicationItemDataSource.CombineValues(typeItem.i.FromValue, nrItem.i.FromValue);
                    nrItem.i.ToValue = BankAccountTypeAndNrCreditApplicationItemDataSource.CombineValues(typeItem.i.ToValue, nrItem.i.ToValue);
                    return nrItem.i;
                })
                .ToList();
        }

        private static List<Response.HistoricalChangeModel> GetApplicationItemHistory(string applicationNr, string groupName, string itemName, string currentValue, PreCreditContext context)
        {
            var h = new List<Response.HistoricalChangeModel>();

            var edits = context
                .CreditApplicationChangeLogItems
                .Where(x => x.GroupName == groupName && x.Name == itemName && x.ApplicationNr == applicationNr)
                .Select(x => new
                {
                    x.Id,
                    x.OldValue,
                    x.TransactionType,
                    x.ChangedDate,
                    x.ChangedById,
                    x.EditEventId
                })
                .ToList();

            foreach (var e in edits.OrderByDescending(x => x.Id))
            {
                h.Add(new Response.HistoricalChangeModel
                {
                    Date = e.ChangedDate.DateTime,
                    UserId = e.ChangedById,
                    TransactionType = e.TransactionType,
                    ToValue = currentValue,
                    FromValue = e.OldValue,
                    EventId = e.EditEventId
                });
                currentValue = e.OldValue;
            }

            return h;
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string DataSourceName { get; set; }
            [Required]
            public string ItemName { get; set; }
            [Required]
            public string DefaultValueIfMissing { get; set; }
            public bool? IncludeEdits { get; set; }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public string ItemValue { get; set; }
            public CreditApplicationCustomEditableFieldsModel.FieldModel EditModel { get; set; }
            public List<HistoricalChangeModel> HistoricalChanges { get; set; }

            public class HistoricalChangeModel
            {
                public int ChangeId { get; set; }
                public string FromValue { get; set; }
                public string ToValue { get; set; }
                public DateTime Date { get; set; }
                public int UserId { get; set; }
                public string TransactionType { get; set; }
                public int? EventId { get; set; }
            }
        }
    }
}