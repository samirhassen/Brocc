using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchDataSourceItemsMethod : TypedWebserviceMethod<FetchDataSourceItemsMethod.Request, FetchDataSourceItemsMethod.Response>
    {
        public override string Path => "Application/FetchDataSourceItems";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.ApplicationNr);
            });

            var resolver = requestContext.Resolver();
            var s = resolver.Resolve<ApplicationDataSourceService>();

            var missingItems = new Dictionary<string, List<string>>();
            var changedItems = new Dictionary<string, List<string>>();

            ApplicationDataSourceServiceRequest[] requests = request.Requests.Select(x =>
                {
                    var localDataSourceName = x.DataSourceName;
                    return new ApplicationDataSourceServiceRequest
                    {
                        DataSourceName = x.DataSourceName,
                        Names = new HashSet<string>(x.Names),
                        MissingItemStrategy = x.ErrorIfMissing.GetValueOrDefault()
                        ? Code.Datasources.ApplicationDataSourceMissingItemStrategy.ThrowException : (x.ReplaceIfMissing.GetValueOrDefault()
                            ? Code.Datasources.ApplicationDataSourceMissingItemStrategy.UseDefaultValue : Code.Datasources.ApplicationDataSourceMissingItemStrategy.Skip),
                        ObserveMissingItems = name =>
                        {
                            if (!missingItems.ContainsKey(localDataSourceName))
                                missingItems.Add(localDataSourceName, new List<string>());
                            missingItems[localDataSourceName].Add(name);
                        },
                        ObserveChangedItems = !x.IncludeIsChanged.GetValueOrDefault() ? (Action<string>)null : name =>
                            {
                                if (!changedItems.ContainsKey(localDataSourceName))
                                    changedItems.Add(localDataSourceName, new List<string>());
                                changedItems[localDataSourceName].Add(name);
                            },
                        GetDefaultValue = ConstructGetDefaultValue(x)
                    };
                }).ToArray();

            var result = s.GetData(request.ApplicationNr, requests);

            //NOTE: We start from Requests for both sources and items to preserve the callers order in case they use that instead of the names to extract results. This is a bad idea since missing data is skipped but we can at least
            //      help them make it work in the replace case.
            Func<DataSourceRequest, List<DataSourceItem>> getItems = x =>
            {
                var dr = result[x.DataSourceName];
                var items = new List<DataSourceItem>();
                items.AddRange(x
                    .Names
                    .Select(y => dr.ContainsKey(y) ? new DataSourceItem { Name = y, Value = result[x.DataSourceName][y] } : null).Where(y => y != null)
                    .ToList());

                if (items.Count < dr.Count)
                {
                    //For items we also include extra items so call patterns like names = ['*'] will work to get everything
                    foreach (var v in dr.Where(y => !x.Names.Contains(y.Key)))
                    {
                        items.Add(new DataSourceItem { Name = v.Key, Value = v.Value });
                    }
                }

                return items;
            };

            var editorService = new Lazy<ICreditApplicationCustomEditableFieldsService>(() => resolver.Resolve<ICreditApplicationCustomEditableFieldsService>());
            return new Response
            {
                Results = request
                    .Requests
                    .Select(x =>
                    {
                        var items = getItems(x);
                        if (x.IncludeEditorModel.GetValueOrDefault())
                        {
                            foreach (var i in items)
                                i.EditorModel = editorService.Value.GetFieldModel(x.DataSourceName, i.Name);
                        }
                        return new DataSourceResult
                        {
                            DataSourceName = x.DataSourceName,
                            MissingNames = missingItems.ContainsKey(x.DataSourceName) ? missingItems[x.DataSourceName] : new List<string>(),
                            Items = items,
                            ChangedNames = x.IncludeIsChanged.GetValueOrDefault()
                            ? (changedItems.ContainsKey(x.DataSourceName) ? changedItems[x.DataSourceName] : new List<string>())
                            : null
                        };
                    })
                    .ToList()
            };
        }

        private Func<string, string> ConstructGetDefaultValue(DataSourceRequest request)
        {
            if (!request.ReplaceIfMissing.GetValueOrDefault())
                return null;
            return _ => request.MissingItemReplacementValue ?? "null";
        }

        public class Response
        {
            public List<DataSourceResult> Results { get; set; }
        }

        public class DataSourceResult
        {
            public string DataSourceName { get; set; }
            public List<DataSourceItem> Items { get; set; }
            public List<string> MissingNames { get; set; }
            public List<string> ChangedNames { get; set; }
        }

        public class DataSourceItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public CreditApplicationCustomEditableFieldsModel.FieldModel EditorModel { get; set; }
        }

        public class Request
        {
            public string ApplicationNr { get; set; }

            public List<DataSourceRequest> Requests { get; set; }
        }

        public class DataSourceRequest
        {
            public string DataSourceName { get; set; }

            public List<string> Names { get; set; }

            /// <summary>
            /// Default is the string 'null'
            /// </summary>
            public string MissingItemReplacementValue { get; set; }

            /// <summary>
            /// Return an error if any item is missing. Default is to skip the value.
            /// </summary>
            public bool? ErrorIfMissing { get; set; }

            /// <summary>
            /// Replace the value if missing (default replacement is the string 'null'). Default is to skip the value.
            /// </summary>
            public bool? ReplaceIfMissing { get; set; }

            /// <summary>
            /// Includes the list (ChangedNames) of names of items that have been user edited after being added
            /// </summary>
            public bool? IncludeIsChanged { get; set; }

            /// <summary>
            /// Include the customer editor model from ClientResources/CreditApplicationCustomEditableFields.json
            /// </summary>
            public bool? IncludeEditorModel { get; set; }
        }
    }
}