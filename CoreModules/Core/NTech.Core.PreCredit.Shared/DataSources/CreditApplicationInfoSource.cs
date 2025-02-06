using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nPreCredit.Code.Datasources
{
    public class CreditApplicationInfoSource : IApplicationDataSource
    {
        public const string DataSourceNameShared = "CreditApplicationInfo";
        private readonly ApplicationInfoService applicationInfoService;

        public CreditApplicationInfoSource(ApplicationInfoService applicationInfoService)
        {
            this.applicationInfoService = applicationInfoService;
        }

        public string DataSourceName => DataSourceNameShared;

        private static Lazy<Dictionary<string, System.Reflection.PropertyInfo>> properties = new Lazy<Dictionary<string, System.Reflection.PropertyInfo>>(() =>
            typeof(ApplicationInfoModel).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).ToDictionary(x => x.Name, x => x)
        );

        public static List<string> ValidNames
        {
            get
            {
                return properties.Value.Keys.Concat(new[] { "CustomerIds" }).ToList();
            }
        }

        public bool IsSetDataSupported => false;

        public Dictionary<string, string> GetItems(string applicationNr, ISet<string> names, ApplicationDataSourceMissingItemStrategy missingItemStrategy, Action<string> observeMissingItems = null, Func<string, string> getDefaultValue = null, Action<string> observeChangedItems = null)
        {
            var result = new Dictionary<string, string>(names.Count);

            var infoNames = names.Intersect(properties.Value.Keys);

            if (infoNames.Any())
            {
                var info = this.applicationInfoService.GetApplicationInfo(applicationNr);
                if (info == null)
                    throw new NTechCoreWebserviceException("No such application");

                foreach (var name in infoNames)
                {
                    if (!properties.Value.ContainsKey(name))
                        throw new NTechCoreWebserviceException($"No such property '{name}'. Valid properties are: {string.Join(", ", ValidNames)}");
                    result[name] = GetValueAsString(properties.Value[name], info);
                }
            }

            var otherNames = names.Except(properties.Value.Keys);
            if (otherNames.Any())
            {
                var a = this.applicationInfoService.GetApplicationApplicants(applicationNr);
                if (a == null)
                    throw new NTechCoreWebserviceException("No such application");

                foreach (var name in otherNames)
                {
                    if (name == "CustomerIds")
                        result["CustomerIds"] = string.Join(",", Enumerable.Range(1, a.NrOfApplicants).Select(x => a.CustomerIdByApplicantNr[x].ToString()));
                    else
                        throw new NTechCoreWebserviceException($"No such property '{name}'. Valid properties are: {string.Join(", ", ValidNames)}");
                }
            }

            return result;
        }

        private static string GetValueAsString(System.Reflection.PropertyInfo p, object instance)
        {
            object value = p.GetValue(instance);
            var pt = p.PropertyType;
            if (pt.FullName == "System.String")
                return value as string;
            else if (pt.FullName == "System.Decimal")
                return (value as Decimal?)?.ToString(CultureInfo.InvariantCulture);
            else if (pt.FullName == "System.Int32")
                return (value as Int32?)?.ToString();
            else if (pt.FullName == "System.Int64")
                return (value as Int64?)?.ToString();
            else if (pt.FullName == "System.DateTime")
                return (value as DateTime?)?.ToString("yyyy-MM-dd");
            else if (pt.FullName == "System.DateTimeOffset")
                return (value as DateTimeOffset?)?.ToString("yyyy-MM-dd");
            else if (pt.FullName == "System.Boolean")
            {
                var v = (value as bool?);
                return v.HasValue ? (v.Value ? "true" : "false") : null;
            }
            else
                throw new Exception($"Unsupported type: {pt.FullName}");
        }

        public int? SetData(string applicationNr, string compoundItemName, bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue, INTechCurrentUserMetadata currentUser)
        {
            throw new NotImplementedException();
        }
    }
}