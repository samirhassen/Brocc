using Newtonsoft.Json;
using nPreCredit.DbModel;
using NTech;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace nPreCredit.Code.Services
{
    public class WorkListService : IWorkListService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;
        private readonly IDocumentClient documentClient;
        private readonly PreCreditContextFactoryService contextService;

        public WorkListService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock, IDocumentClient documentClient, PreCreditContextFactoryService contextService)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
            this.documentClient = documentClient;
            this.contextService = contextService;
        }

        private T FillInfrastructureBaseItem<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = ntechCurrentUserMetadata.UserId;
            b.ChangedDate = clock.Now;
            b.InformationMetaData = ntechCurrentUserMetadata.InformationMetadata;
            return b;
        }

        private static int? GetCustomDataVersion(string customData)
        {
            return JsonConvert.DeserializeAnonymousType(customData, new { version = (int?)null }).version;
        }

        private static WorkListCustomDataV1 ParseCustomDataV1(string customData)
        {
            var dataVersion = JsonConvert.DeserializeAnonymousType(customData, new { version = (int?)null }).version;
            if (dataVersion != 1)
                return null;

            return JsonConvert.DeserializeAnonymousType(customData, new
            {
                data = (WorkListCustomDataV1)null
            }).data;
        }

        private class WorkListCreateRequest
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

        private class WorkListCustomDataV1
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

        private static Func<string, int> CreateOrderFunction<T>(List<T> descriptors) where T : WorkListCustomDataV1.DescriptorBase
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

        private static Func<string, string> CreateDisplayNameFunction<T>(List<T> descriptors) where T : WorkListCustomDataV1.DescriptorBase
        {
            if (descriptors == null)
                return x => x;

            var displayNamesByName = descriptors?.ToDictionary(x => x.Name, x => x.DisplayName);
            return x => (displayNamesByName?.ContainsKey(x) ?? false) ? displayNamesByName[x] : x;
        }

        private int BeginCreate(WorkListCreateRequest request)
        {
            using (var context = contextService.Create())
            {
                var wl = new WorkListHeader
                {
                    CustomData = JsonConvert.SerializeObject(new { version = 1, data = request.CustomData }),
                    ListType = request.ListType,
                    IsUnderConstruction = true,
                    CreatedByUserId = ntechCurrentUserMetadata.UserId,
                    CreationDate = clock.Now.DateTime,
                };
                FillInfrastructureBaseItem(wl);

                if (request.FilterItems != null)
                {
                    foreach (var f in request.FilterItems)
                    {
                        context.WorkListFilterItems.Add(new WorkListFilterItem
                        {
                            Name = f.Name,
                            Value = f.Value,
                            WorkList = wl
                        });
                    }
                }

                context.WorkListHeaders.Add(wl);

                context.SaveChanges();

                return wl.Id;
            }
        }

        private bool TryEndCreate(int id)
        {
            using (var context = contextService.Create())
            {
                var wl = context.WorkListHeaders.Single(x => x.Id == id);

                if (!wl.IsUnderConstruction)
                    return false;

                wl.IsUnderConstruction = false;

                context.SaveChanges();

                return true;
            }
        }

        private class ItemProperty
        {
            public string Name { get; set; }
            public string DataTypeName { get; set; }
            public bool IsEncrypted { get; set; }
            public string Value { get; set; }

            private ItemProperty()
            {

            }

            public static ItemProperty Create(string name, string dataTypeName, string value)
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
                return Create(name, typeof(DateTime).Name, value.HasValue ? value.Value.ToString("o") : null);
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

        private class AddItemsToWorkListRequest
        {
            public int WorkListId { get; set; }

            public List<Item> Items { get; set; }

            public class Item
            {
                public string ItemId { get; set; }
                public List<ItemProperty> Properties { get; set; }
            }
        }

        private bool TryAddItems(AddItemsToWorkListRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return true; //Or false, kind of a philosophical question if the effect of adding nothing is that nothing happens is right or wrong.

            using (var context = contextService.Create())
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
                            DataTypeName = x.DataTypeName,
                            Value = x.Value
                        }).ToList()
                    }));
                }

                context.SaveChanges();
            }

            return true;
        }

        private (string Name, string Value, string DataTypeName) CreateProperty(ItemProperty p)
        {
            return (p.Name, p.Value, p.DataTypeName);
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

        private bool TryCloseWorkList(int workListId)
        {
            using (var context = contextService.Create())
            {
                var h = context.WorkListHeaders.Where(x => x.Id == workListId).SingleOrDefault();
                if (h == null)
                    return false;
                if (h.ClosedByUserId.HasValue || h.IsUnderConstruction)
                    return false;

                h.ClosedByUserId = this.ntechCurrentUserMetadata.UserId;
                h.ClosedDate = this.clock.Now.DateTime;
                h.ChangedById = this.ntechCurrentUserMetadata.UserId;
                h.ChangedDate = this.clock.Now;

                context.SaveChanges();

                return true;
            }
        }

        private IQueryable<WorkListStatusModel> GetWorkListStatusModels(IPreCreditContext context, int userId)
        {
            return context
                .WorkListHeaders
                .Select(x => new WorkListStatusModel
                {
                    WorkListHeaderId = x.Id,
                    IsUnderConstruction = x.IsUnderConstruction,
                    CreationDate = x.CreationDate,
                    CreatedByUserId = x.CreatedByUserId,
                    ClosedByUserId = x.ClosedByUserId,
                    ClosedDate = x.ClosedDate,
                    TotalCount = x.Items.Count(),
                    TakenCount = x.Items.Count(y => y.TakenByUserId.HasValue && !y.CompletedDate.HasValue),
                    CompletedCount = x.Items.Count(y => y.TakenByUserId.HasValue && y.CompletedDate.HasValue),
                    TakeOrCompletedByCurrentUserCount = x.Items.Count(y => y.TakenByUserId == userId),
                    CurrentUserActiveItemId = x
                        .Items
                        .Where(y => y.TakenByUserId == userId && !y.CompletedDate.HasValue)
                        .Select(y => y.ItemId)
                        .FirstOrDefault(),
                    IsTakePossible = !x.ClosedByUserId.HasValue && x.Items.Any(y => !y.TakenByUserId.HasValue)
                });
        }

        public bool TryCompleteWorkListItem(int workListHeaderId, string itemId)
        {
            using (var context = contextService.Create())
            {
                var item = context
                    .WorkListItems
                    .SingleOrDefault(x => !x.WorkList.IsUnderConstruction
                                          && !x.WorkList.ClosedByUserId.HasValue
                                          && x.WorkListHeaderId == workListHeaderId
                                          && x.TakenByUserId.HasValue
                                          && x.ItemId == itemId);

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

        public bool TryReplaceWorkListItem(int workListHeaderId, string itemId)
        {
            using (var context = contextService.Create())
            {
                var item = context
                    .WorkListItems
                    .SingleOrDefault(x => !x.WorkList.IsUnderConstruction
                                && !x.WorkList.ClosedByUserId.HasValue
                                && x.WorkListHeaderId == workListHeaderId
                                && x.TakenByUserId.HasValue
                                && x.ItemId == itemId);

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

        public string TryTakeWorkListItem(int workListHeaderId, int userId)
        {
            //NOTE: Unclear if you should be allowed to take for other users but probably if we want to automate this somewhere            
            using (var context = contextService.Create())
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
                        var updateCount = context.ExecuteDatabaseSqlCommand("update WorkListItem set TakenByUserId = @takenByUserId, TakenDate = @takenDate where TakenByUserId is null and WorkListHeaderId = @workListHeaderId and ItemId = @candidateItemId",
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
                    throw new NTechWebserviceMethodException(
                        "Failed to take an item after several tries even though the lists seems non empty")
                    {
                        ErrorCode = "tryTakeConcurrencyError",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };

                return resultItemId;
            }
        }

        public bool TryCloseWorkList(int workListHeaderId, int userId)
        {
            using (var context = contextService.Create())
            {
                var wl = context.WorkListHeaders.SingleOrDefault(x => x.Id == workListHeaderId);
                if (wl == null)
                    return false;
                if (wl.ClosedByUserId.HasValue)
                    return false;
                wl.ClosedByUserId = userId;
                wl.ClosedDate = clock.Now.DateTime;
                context.SaveChanges();
                return true;
            }
        }

        public Stream CreateWorkListInitialStateAsXlsx(int workListId)
        {
            using (var context = contextService.Create())
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
                    .SingleOrDefault();

                if (result == null)
                    throw new NTechWebserviceMethodException(
                        "Worklist does not exist or is under construction")
                    {
                        ErrorCode = "doesNotExistOrUnderConstruction",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };

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

                return documentClient.CreateXlsx(request);
            }
        }

        public Stream CreateWorkListResultAsXlsx(int workListId, Func<string, string> getUserDisplayNameByUserId)
        {
            using (var context = contextService.Create())
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
                    .SingleOrDefault();

                if (result == null)
                    throw new NTechWebserviceMethodException(
                        "Worklist does not exist or is under construction")
                    {
                        ErrorCode = "doesNotExistOrUnderConstruction",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };

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

                return documentClient.CreateXlsx(request);
            }
        }

        public (int? WorkListId, bool WasAdded, bool WasLeftUnderConstruction, bool WasCreated) CreateOrAddToWorkList(
            int? existingWorkListId, bool leaveUnderConstruction, string newListType, List<(string Name, string Value)> newListFilters,
            List<(string ItemId, List<(string Name, string Value, string DataTypeName)> Properties)> newItems)
        {

            List<(string ItemId, List<(string Name, string Value, string DataTypeName)> Properties)> EnsureItemIdHasProperty(List<(string ItemId, List<(string Name, string Value, string DataTypeName)> Properties)> items)
            {
                if (items == null || items.Count == 0)
                    return items;

                if (items.First().Properties.Any(x => x.Name == "ItemId"))
                    return items;

                return items
                    .Select(x =>
                    (
                        ItemId: x.ItemId,
                        Properties: (new[] { (Name: "ItemId", Value: x.ItemId, DataTypeName: "String") }).Concat(x.Properties).ToList()
                    ))
                    .ToList();
            }

            newItems = EnsureItemIdHasProperty(newItems);

            int workListId;
            bool wasCreated = false;
            if (existingWorkListId.HasValue)
            {
                using (var context = contextService.Create())
                {
                    var isUnderConstruction = context
                        .WorkListHeaders
                        .Where(x => x.Id == existingWorkListId.Value)
                        .Select(x => (bool?)x.IsUnderConstruction)
                        .SingleOrDefault();
                    if (!isUnderConstruction.HasValue)
                        throw new NTechWebserviceMethodException(
                            "No such worklist exists")
                        {
                            ErrorCode = "noSuchWorkListExists",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };

                    if (!isUnderConstruction.Value)
                        throw new NTechWebserviceMethodException(
                            "Worklist is not under construction")
                        {
                            ErrorCode = "notUnderConstruction",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                }
                workListId = existingWorkListId.Value;
            }
            else
            {
                if (newItems == null || newItems.Count == 0)
                {
                    //If this is relaxed make sure to enforce some other mechanism for getting the column metadata (PropertyDescriptors) into the list
                    throw new NTechWebserviceMethodException(
                        "A worklist must have at least one item")
                    {
                        ErrorCode = "mustHaveAtLeastOneItem",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };
                }

                if (string.IsNullOrWhiteSpace(newListType))
                    throw new NTechWebserviceMethodException(
                        "List type required when creating a new list")
                    {
                        ErrorCode = "missingListType",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };

                workListId = BeginCreate(new WorkListCreateRequest
                {
                    ListType = newListType,
                    FilterItems = newListFilters?.Select(x => new WorkListCreateRequest.FilterItem
                    {
                        Name = x.Name,
                        Value = x.Value
                    })?.ToList() ?? new List<WorkListCreateRequest.FilterItem>(),
                    CustomData = new WorkListCustomDataV1
                    {
                        FilterDescriptors = newListFilters?.Select(x => new WorkListCustomDataV1.FilterDescriptor
                        {
                            Name = x.Name,
                            DisplayName = x.Name
                        })?.ToList() ?? new List<WorkListCustomDataV1.FilterDescriptor>(),
                        PropertyDescriptors = newItems.First().Properties.Select(x => new WorkListCustomDataV1.PropertyDescriptor
                        {
                            Name = x.Name,
                            DisplayName = x.Name,
                            DataTypeName = x.DataTypeName
                        }).ToList()
                    }
                });
                wasCreated = true;
            }

            var wasAdded = TryAddItems(new AddItemsToWorkListRequest
            {
                WorkListId = workListId,
                Items = newItems.Select(x => new AddItemsToWorkListRequest.Item
                {
                    ItemId = x.ItemId,
                    Properties = x.Properties.Select(y => ItemProperty.Create(y.Name, y.DataTypeName, y.Value)).ToList()
                }).ToList()
            });

            bool wasLeftUnderConstruction = true;
            if (wasAdded && !leaveUnderConstruction)
            {
                wasLeftUnderConstruction = !TryEndCreate(workListId);
            }

            return (workListId, wasAdded, wasLeftUnderConstruction, wasCreated);
        }

        public (string Name, string Value, string DataTypeName) CreateProperty(string name, string value)
        {
            return CreateProperty(ItemProperty.Create(name, value));
        }

        public (string Name, string Value, string DataTypeName) CreateProperty(string name, decimal value)
        {
            return CreateProperty(ItemProperty.Create(name, value));
        }

        public (string Name, string Value, string DataTypeName) CreateProperty(string name, DateTime value)
        {
            return CreateProperty(ItemProperty.Create(name, value));
        }

        public List<WorkListStatusModel> GetActiveWorkListsWithUserState(string listType, int userId)
        {
            using (var context = contextService.Create())
            {
                return GetWorkListStatusModels(context, userId)
                    .Where(x => !x.IsUnderConstruction && !x.ClosedDate.HasValue)
                    .OrderByDescending(x => x.WorkListHeaderId)
                    .ToList();
            }
        }

        public WorkListStatusModel GetWorkListWithUserState(int workListId, int userId)
        {
            using (var context = contextService.Create())
            {
                return GetWorkListStatusModels(context, userId).SingleOrDefault(x => x.WorkListHeaderId == workListId);
            }
        }
    }

    public interface IWorkListService
    {
        (int? WorkListId, bool WasAdded, bool WasLeftUnderConstruction, bool WasCreated) CreateOrAddToWorkList(
            int? existingWorkListId, bool leaveUnderConstruction, string newListType, List<(string Name, string Value)> newListFilters,
            List<(string ItemId, List<(string Name, string Value, string DataTypeName)> Properties)> newItems);
        Stream CreateWorkListInitialStateAsXlsx(int workListId);
        Stream CreateWorkListResultAsXlsx(int workListId, Func<string, string> getUserDisplayNameByUserId);
        string TryTakeWorkListItem(int workListHeaderId, int userId);
        bool TryReplaceWorkListItem(int workListHeaderId, string itemId);
        bool TryCompleteWorkListItem(int workListHeaderId, string itemId);
        bool TryCloseWorkList(int workListHeaderId, int userId);
        (string Name, string Value, string DataTypeName) CreateProperty(string name, string value);
        (string Name, string Value, string DataTypeName) CreateProperty(string name, decimal value);
        (string Name, string Value, string DataTypeName) CreateProperty(string name, DateTime value);
        List<WorkListStatusModel> GetActiveWorkListsWithUserState(string listType, int userId);
        WorkListStatusModel GetWorkListWithUserState(int workListId, int userId);
    }

    public class WorkListStatusModel
    {
        public int WorkListHeaderId { get; set; }
        public int? ClosedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? ClosedDate { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public string CurrentUserActiveItemId { get; set; }
        public int TakenCount { get; set; }
        public int TakeOrCompletedByCurrentUserCount { get; set; }
        public bool IsTakePossible { get; set; }
        public bool IsUnderConstruction { get; set; }
    }
}