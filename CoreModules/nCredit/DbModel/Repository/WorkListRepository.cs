using nCredit.Code;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace nCredit.DbModel.Repository
{
    public class WorkListRepository
    {
        public WorkListRepository(
            int currentUserId,
            IClock clock,
            string informationMetadata, IDocumentClient documentClient)
        {
            this.currentUserId = currentUserId;
            this.clock = clock;
            this.informationMetadata = informationMetadata;
            this.documentClient = documentClient;
        }

        private int currentUserId;
        private IClock clock;
        private string informationMetadata;
        private readonly IDocumentClient documentClient;

        private T FillInfrastructureBaseItem<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = currentUserId;
            b.ChangedDate = clock.Now;
            b.InformationMetaData = informationMetadata;
            return b;
        }

        public static int? GetCustomDataVersion(string customData)
        {
            return JsonConvert.DeserializeAnonymousType(customData, new { version = (int?)null }).version;
        }

        public static WorkListCustomDataV1 ParseCustomDataV1(string customData)
        {
            var dataVersion = JsonConvert.DeserializeAnonymousType(customData, new { version = (int?)null }).version;
            if (dataVersion != 1)
                return null;

            return JsonConvert.DeserializeAnonymousType(customData, new
            {
                data = (WorkListCustomDataV1)null
            }).data;
        }

        public class WorkListCreateRequest
        {
            public string ListType { get; set; }
            public List<FilterItem> FilterItems { get; set; }
            public WorkListCustomDataV1 CustomData { get; set; }

            public class FilterItem
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public class WorkListCustomDataV1
        {
            public class DescriptorBase
            {
                public string Name { get; set; }
                public string DisplayName { get; set; }
            }

            public class PropertyDescriptor : DescriptorBase
            {
                public string DataTypeName { get; set; }
            }

            public class FilterDescriptor : DescriptorBase
            {

            }

            public List<PropertyDescriptor> PropertyDescriptors { get; set; }
            public List<FilterDescriptor> FilterDescriptors { get; set; }
        }

        public static Func<string, int> CreateOrderFunction<T>(List<T> descriptors) where T : WorkListCustomDataV1.DescriptorBase
        {
            if (descriptors == null)
                return _ => 0;

            var dd = descriptors.Select((x, i) => new { d = x, DisplayOrderNr = i }).ToList();

            var fallback = (dd
                .OrderByDescending(x => x.DisplayOrderNr)
                .Select(x => (int?)x.DisplayOrderNr)
                .FirstOrDefault()
                ?? (descriptors.Count)) + 1;
            var d = dd.ToDictionary(x => x.d.Name, x => x.DisplayOrderNr);

            return n => d.ContainsKey(n) ? d[n] : fallback;
        }

        public static Func<string, string> CreateDisplayNameFunction<T>(List<T> descriptors) where T : WorkListCustomDataV1.DescriptorBase
        {
            if (descriptors == null)
                return x => x;

            var displayNamesByName = descriptors?.ToDictionary(x => x.Name, x => x.DisplayName);
            return x => (displayNamesByName?.ContainsKey(x) ?? false) ? displayNamesByName[x] : x;
        }

        public int BeginCreate(WorkListCreateRequest request)
        {
            using (var context = new CreditContext())
            {
                var wl = new WorkListHeader
                {
                    CustomData = JsonConvert.SerializeObject(new { version = 1, data = request.CustomData }),
                    ListType = request.ListType,
                    IsUnderConstruction = true,
                    CreatedByUserId = currentUserId,
                    CreationDate = clock.Now.DateTime,
                };
                FillInfrastructureBaseItem(wl);

                foreach (var f in request.FilterItems)
                {
                    context.WorkListFilterItems.Add(new WorkListFilterItem
                    {
                        Name = f.Name,
                        Value = f.Value,
                        WorkList = wl
                    });
                }

                context.WorkListHeaders.Add(wl);

                context.SaveChanges();

                return wl.Id;
            }
        }

        public bool TryEndCreate(int id)
        {
            using (var context = new CreditContext())
            {
                var wl = context.WorkListHeaders.Single(x => x.Id == id);

                if (!wl.IsUnderConstruction)
                    return false;

                wl.IsUnderConstruction = false;

                context.SaveChanges();

                return true;
            }
        }

        public class ItemProperty
        {
            public string Name { get; set; }
            public string DataTypeName { get; set; }
            public bool IsEncrypted { get; set; }
            public string Value { get; set; }

            private ItemProperty()
            {

            }

            private static ItemProperty Create(string name, string dataTypeName, string value)
            {
                return new ItemProperty
                {
                    Name = name,
                    Value = value,
                    DataTypeName = dataTypeName
                };
            }

            public static ItemProperty Create(string name, string value)
            {
                return Create(name, typeof(string).Name, value);
            }

            public static ItemProperty Create(string name, decimal? value)
            {
                return Create(name, typeof(decimal).Name, value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null);
            }

            public static ItemProperty Create(string name, int? value)
            {
                return Create(name, typeof(int).Name, value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null);
            }

            public static ItemProperty Create(string name, DateTime? value)
            {
                return Create(name, typeof(int).Name, value.HasValue ? value.Value.ToString("o") : null);
            }

            public static Func<string, object> GetPropertyParser(string typeName)
            {
                if (typeName == typeof(int).Name)
                    return x => x == null ? new int?() : new int?(int.Parse(x));
                else if (typeName == typeof(decimal).Name)
                    return x => x == null ? new decimal?() : new decimal?(decimal.Parse(x, CultureInfo.InvariantCulture));
                else if (typeName == typeof(DateTime).Name)
                    return x => x == null ? new DateTime?() : DateTime.Parse(x, null, DateTimeStyles.RoundtripKind); //Assumes stored with ToString("o")
                else if (typeName == typeof(string).Name)
                    return x => x;
                else
                    return x => x;
            }
        }

        public class AddItemsToWorkListRequest
        {
            public int WorkListId { get; set; }

            public List<Item> Items { get; set; }

            public class Item
            {
                public string ItemId { get; set; }
                public List<ItemProperty> Properties { get; set; }
            }
        }

        public bool TryAddItems(AddItemsToWorkListRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return true; //Or false, kind of a philosophical question if the effect of adding nothing is that nothing happens is right or wrong.

            using (var context = new CreditContext())
            {
                var result = context
                    .WorkListHeaders
                    .Select(x => new
                    {
                        H = x,
                        MaxOrderNr = x
                            .Items
                            .OrderByDescending(y => y.OrderNr)
                            .Select(y => (int?)y.OrderNr)
                            .FirstOrDefault()
                    })
                    .Single(x => x.H.Id == request.WorkListId);

                var wl = result.H;

                if (!wl.IsUnderConstruction)
                    return false;

                var nextOrderNr = (result.MaxOrderNr ?? 0) + 1;

                foreach (var item in request.Items)
                {
                    if (item.Properties.Any(x => x.IsEncrypted))
                        throw new NotImplementedException(); //Implement using the standard EncryptedValue storage and store id as value. Make sure to fix all readers also though.

                    context.WorkListItems.Add(FillInfrastructureBaseItem(new WorkListItem
                    {
                        ItemId = item.ItemId,
                        OrderNr = nextOrderNr++,
                        WorkList = wl,
                        Properties = item.Properties.Select(x => new WorkListItemProperty
                        {
                            IsEncrypted = false,
                            Name = x.Name,
                            Value = x.Value
                        }).ToList()
                    }));
                }

                context.SaveChanges();
            }

            return true;
        }

        public string TryTakeWorkListItem(int workListHeaderId, int userId, Action concurrencyProblemCallback = null)
        {
            //NOTE: Unclear if you should be allowed to take for other users but probably if we want to automate this somewhere            
            using (var context = new CreditContext())
            {
                //Find an id that seems vacant
                //Try to take it using optimistic concurrency
                //On failure sleep between 1 and 20 ms and try again up to 5 times
                var random = new Lazy<Random>(() => new Random());
                bool tryAgain = true;
                bool isConcurrencyProblem = false;
                string resultItemId = null;
                var nrOfTries = 0;
                while (tryAgain)
                {
                    var candidateItemId = context
                        .WorkListItems
                        .Where(x => !x.WorkList.IsUnderConstruction
                            && !x.WorkList.ClosedByUserId.HasValue
                            && x.WorkListHeaderId == workListHeaderId
                            && !x.TakenByUserId.HasValue)
                        .OrderBy(x => x.OrderNr)
                        .Select(x => x.ItemId)
                        .FirstOrDefault();

                    if (candidateItemId == null)
                    {
                        tryAgain = false;
                    }
                    else
                    {
                        var updateCount = context.Database.ExecuteSqlCommand("update WorkListItem set TakenByUserId = @takenByUserId, TakenDate = @takenDate where TakenByUserId is null and WorkListHeaderId = @workListHeaderId and ItemId = @candidateItemId",
                            new SqlParameter("@takenByUserId", userId),
                            new SqlParameter("@takenDate", clock.Now.DateTime),
                            new SqlParameter("@workListHeaderId", workListHeaderId),
                            new SqlParameter("@candidateItemId", candidateItemId));

                        if (updateCount == 0)
                        {
                            if (++nrOfTries < 5)
                            {
                                Thread.Sleep(random.Value.Next(1, 21));
                            }
                            else
                            {
                                tryAgain = false;
                                isConcurrencyProblem = true;
                            }
                        }
                        else
                        {
                            resultItemId = candidateItemId;
                            tryAgain = false;
                        }
                    }
                }

                if (isConcurrencyProblem)
                    concurrencyProblemCallback?.Invoke();

                return resultItemId;
            }
        }

        public bool TryReplaceWorkListItem(int workListHeaderId, string itemId)
        {
            using (var context = new CreditContext())
            {
                var item = context
                    .WorkListItems
                    .Where(x => !x.WorkList.IsUnderConstruction
                            && !x.WorkList.ClosedByUserId.HasValue
                            && x.WorkListHeaderId == workListHeaderId
                            && x.TakenByUserId.HasValue
                            && x.ItemId == itemId)
                    .SingleOrDefault();

                bool wasReplaced = false;
                if (item != null)
                {
                    wasReplaced = true;
                    item.TakenByUserId = null;
                    item.TakenDate = null;
                    item.CompletedDate = null;

                    context.SaveChanges();
                }

                return wasReplaced;
            }
        }

        public bool TryCompleteWorkListItem(int workListHeaderId, string itemId)
        {
            using (var context = new CreditContext())
            {
                var item = context
                    .WorkListItems
                    .Where(x => !x.WorkList.IsUnderConstruction
                            && !x.WorkList.ClosedByUserId.HasValue
                            && x.WorkListHeaderId == workListHeaderId
                            && x.TakenByUserId.HasValue
                            && x.ItemId == itemId)
                    .SingleOrDefault();

                bool wasCompleted = false;
                if (item != null)
                {
                    wasCompleted = true;
                    item.CompletedDate = clock.Now.DateTime;
                    context.SaveChanges();
                }

                return wasCompleted;
            }
        }

        public bool TryCloseWorkList(int workListId)
        {
            using (var context = new CreditContext())
            {
                var h = context.WorkListHeaders.Where(x => x.Id == workListId).SingleOrDefault();
                if (h == null)
                    return false;
                if (h.ClosedByUserId.HasValue || h.IsUnderConstruction)
                    return false;

                h.ClosedByUserId = this.currentUserId;
                h.ClosedDate = this.clock.Now.DateTime;
                h.ChangedById = this.currentUserId;
                h.ChangedDate = this.clock.Now;

                context.SaveChanges();

                return true;
            }
        }

        public Stream CreateWorkListInitialStateAsXlsx(int workListId)
        {
            var d = documentClient;

            using (var context = new CreditContext())
            {
                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = "Filter"
                            },
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = "Items"
                            }
                    }
                };

                var result = context
                    .WorkListHeaders
                    .Where(x => x.Id == workListId && !x.IsUnderConstruction)
                    .Select(x => new
                    {
                        x.CustomData,
                        FilterItems = x.FilterItems.Select(y => new
                        {
                            y.Name,
                            y.Value
                        }),
                        Items = x
                            .Items
                            .OrderBy(y => y.OrderNr)
                            .Select(y => new
                            {
                                y.ItemId,
                                Properties = y.Properties.Select(z => new
                                {
                                    z.Name,
                                    Value = z.IsEncrypted ? "Encrypted" : z.Value
                                })
                            })
                    })
                    .Single();

                if (GetCustomDataVersion(result.CustomData) != 1)
                    throw new NotImplementedException();

                var customData = ParseCustomDataV1(result.CustomData);

                var filterSheet = request.Sheets[0];
                var filterOrderFn = CreateOrderFunction(customData.FilterDescriptors);
                var filterDisplayNameFn = CreateDisplayNameFunction(customData.FilterDescriptors);
                var filterItems = result.FilterItems.OrderBy(x => filterOrderFn(x.Name)).Select(x => new
                {
                    DisplayName = filterDisplayNameFn(x.Name),
                    Value = x.Value
                }).ToList();
                filterSheet.SetColumnsAndData(filterItems,
                    filterItems.Col(x => x.DisplayName, ExcelType.Text, "Name"),
                    filterItems.Col(x => x.Value, ExcelType.Text, "Value"));

                var dataSheet = request.Sheets[1];

                var dataItems = result
                    .Items
                    .Select(x => Tuple.Create(
                        x.ItemId,
                        x.Properties.ToDictionary(y => y.Name, y => y.Value)))
                    .ToList();
                var dataCols = CreateSheetDataColumnsFromItems(dataItems, customData);

                dataSheet.SetColumnsAndData(dataItems, dataCols.ToArray());

                return d.CreateXlsx(request);
            }
        }

        public Stream CreateWorkListResultAsXlsx(int workListId, Func<string, string> getUserDisplayNameByUserId)
        {
            var d = documentClient;

            using (var context = new CreditContext())
            {
                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = "Items"
                            }
                    }
                };

                var result = context
                    .WorkListHeaders
                    .Where(x => x.Id == workListId && !x.IsUnderConstruction)
                    .Select(x => new
                    {
                        x.CustomData,
                        Items = x
                            .Items
                            .OrderBy(y => y.OrderNr)
                            .Select(y => new
                            {
                                y.ItemId,
                                y.TakenByUserId,
                                y.TakenDate,
                                y.CompletedDate
                            })
                    })
                    .Single();

                if (GetCustomDataVersion(result.CustomData) != 1)
                    throw new NotImplementedException();


                var customData = ParseCustomDataV1(result.CustomData);

                var dataSheet = request.Sheets[0];

                var dataItems = result
                    .Items
                    .Select(x => new
                    {
                        x.ItemId,
                        TakenByUserDisplayName = x.TakenByUserId.HasValue ? getUserDisplayNameByUserId(x.TakenByUserId.Value.ToString()) : (string)null,
                        TakenDate = x.TakenDate,
                        CompletedDate = x.CompletedDate
                    })
                    .ToList();
                var itemDisplayName = customData?.PropertyDescriptors?.Where(x => x.Name == "ItemId")?.FirstOrDefault()?.DisplayName ?? "ItemId";
                dataSheet.SetColumnsAndData(dataItems,
                    dataItems.Col(x => x.ItemId, ExcelType.Text, itemDisplayName),
                    dataItems.Col(x => x.TakenByUserDisplayName, ExcelType.Text, "Taken by"),
                    dataItems.Col(x => x.TakenDate, ExcelType.Date, "Taken date", includeTime: true),
                    dataItems.Col(x => x.CompletedDate, ExcelType.Date, "Completed date", includeTime: true));

                return d.CreateXlsx(request);
            }
        }

        private List<Tuple<DocumentClientExcelRequest.Column, Func<Tuple<string, Dictionary<string, string>>, object>, Func<Tuple<string, Dictionary<string, string>>, DocumentClientExcelRequest.StyleData>>> CreateSheetDataColumnsFromItems(
            List<Tuple<string, Dictionary<string, string>>> dataItems,
            WorkListCustomDataV1 customData)
        {
            var dataOrderFn = CreateOrderFunction(customData.PropertyDescriptors);
            var dataDisplayNameFn = CreateDisplayNameFunction(customData.PropertyDescriptors);

            var orderedPropertyNames = customData
                .PropertyDescriptors
                .Select(x => x.Name)
                .ToList();

            var dataCols = DocumentClientExcelRequest.CreateDynamicColumnList(dataItems);
            var ps = customData.PropertyDescriptors.ToDictionary(x => x.Name);
            foreach (var propertyName in orderedPropertyNames)
            {
                var p = ps[propertyName];
                var parse = ItemProperty.GetPropertyParser(p.DataTypeName);
                var excelType = ExcelType.Text;
                var nrOfDecimals = new int?();
                if (p.DataTypeName == typeof(int).Name)
                {
                    excelType = ExcelType.Number;
                    nrOfDecimals = 0;
                }
                else if (p.DataTypeName == typeof(decimal).Name)
                {
                    excelType = ExcelType.Number;
                    nrOfDecimals = 2;
                }
                else if (p.DataTypeName == typeof(DateTime).Name)
                {
                    excelType = ExcelType.Date;
                    nrOfDecimals = new int?();
                }
                dataCols.Add(dataItems.Col(x => parse(propertyName == "ItemId" ? x.Item1 : x.Item2[propertyName]), excelType, dataDisplayNameFn(propertyName), nrOfDecimals: nrOfDecimals));
            }
            return dataCols;
        }
    }
}