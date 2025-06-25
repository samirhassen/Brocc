using Newtonsoft.Json;
using nPreCredit.Code.Datasources;
using NTech;
using NTech.Banking.BankAccounts.Fi;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace nPreCredit.Code.Services
{
    public class CreditApplicationCustomEditableFieldsService : ICreditApplicationCustomEditableFieldsService
    {
        private readonly Lazy<CreditApplicationCustomEditableFieldsModel> model;
        private readonly Lazy<Dictionary<string, Dictionary<string, CreditApplicationCustomEditableFieldsModel.FieldModel>>> complexApplicationListModels;


        private static CreditApplicationCustomEditableFieldsModel LoadModel(Func<FileInfo> getModelFile)
        {
            var f = getModelFile();
            return f.Exists
                ? JsonConvert.DeserializeObject<CreditApplicationCustomEditableFieldsModel>(File.ReadAllText(f.FullName))
                : new CreditApplicationCustomEditableFieldsModel();
        }

        public CreditApplicationCustomEditableFieldsService(Lazy<int> maxNrOfApplicants) : this(new CreditApplicationCustomEditableFieldsModel(), maxNrOfApplicants)
        {

        }

        public CreditApplicationCustomEditableFieldsService(Func<FileInfo> getModelFile, Lazy<int> maxNrOfApplicants) : this(LoadModel(getModelFile), maxNrOfApplicants)
        {

        }

        public CreditApplicationCustomEditableFieldsService(CreditApplicationCustomEditableFieldsModel m, Lazy<int> maxNrOfApplicants)
        {
            var localModel = new Lazy<CreditApplicationCustomEditableFieldsModel>(() =>
            {
                m.CustomFields = m.CustomFields ?? new List<CreditApplicationCustomEditableFieldsModel.FieldModel>();
                if (m.CustomFields.Any(x => x.ItemName.Contains("{{nrOfApplicants}}")))
                {
                    var fields = new List<CreditApplicationCustomEditableFieldsModel.FieldModel>();
                    foreach (var cf in m.CustomFields)
                    {
                        if (cf.ItemName.Contains("{{nrOfApplicants}}"))
                        {
                            for (var applicantNr = 1; applicantNr <= maxNrOfApplicants.Value; applicantNr++)
                            {
                                var cfc = Clone(cf);
                                cfc.ItemName = cf.ItemName.Replace("{{nrOfApplicants}}", applicantNr.ToString());
                                fields.Add(cfc);
                            }
                        }
                        else
                        {
                            fields.Add(cf);
                        }
                        m.CustomFields = fields;
                    }
                }
                foreach (var cf in m.CustomFields.Where(x => x.DataSourceName == ComplexApplicationListDataSource.DataSourceNameShared))
                {
                    cf.ItemName = cf.ItemName?.Replace("{{nr}}", "*");
                }
                return m;
            });
            this.model = localModel;
            this.complexApplicationListModels = new Lazy<Dictionary<string, Dictionary<string, CreditApplicationCustomEditableFieldsModel.FieldModel>>>(() =>
            {
                var dataSourceFields = localModel.Value.CustomFields.Where(x => x.DataSourceName == ComplexApplicationListDataSource.DataSourceNameShared).ToList();
                var d = new Dictionary<string, Dictionary<string, CreditApplicationCustomEditableFieldsModel.FieldModel>>();
                foreach (var c in dataSourceFields)
                {
                    if (!ComplexApplicationListDataSource.TryParseCompoundName(c.ItemName, out var listNameFilter, out var nrFilter, out var isRepeatableFilter, out var itemNameFilter, out var isFullySpecifiedItem))
                    {
                        throw new Exception($"Invalid Custom field specification: {c}");
                    }
                    if (listNameFilter == null || itemNameFilter == null)
                    {
                        throw new Exception($"Invalid Custom field specification: {c}");
                    }
                    if (!d.ContainsKey(listNameFilter))
                        d[listNameFilter] = new Dictionary<string, CreditApplicationCustomEditableFieldsModel.FieldModel>();
                    d[listNameFilter][itemNameFilter] = c;
                }
                return d;
            });
        }

        private static T Clone<T>(T item) where T : class, new()
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
        }

        public ISet<string> GetCustomizedItemNames(string dataSourceName)
        {
            return model
                .Value
                .CustomFields
                .Where(x => x.DataSourceName.Equals(dataSourceName, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.ItemName)
                .ToHashSetShared();
        }

        public static CreditApplicationCustomEditableFieldsModel.FieldModel CreateComplexApplicationListFieldModel(string compoundName,
            string itemName,
            string listName,
            bool isRepeatable,
            string dataType = "string",
            string editorType = "text",
            string labelText = null,
            Tuple<List<string>, List<string>> dropdownOptionsAndTexts = null,
            bool isRemovable = false)
        {
            return new CreditApplicationCustomEditableFieldsModel.FieldModel
            {
                DataSourceName = "ComplexApplicationList",
                ItemName = compoundName,
                DataType = dataType,
                EditorType = editorType,
                LabelText = labelText ?? $"ComplexApplicationList.{compoundName}",
                DropdownRawOptions = dropdownOptionsAndTexts == null ? null : dropdownOptionsAndTexts.Item1,
                DropdownRawDisplayTexts = dropdownOptionsAndTexts == null ? null : dropdownOptionsAndTexts.Item2,
                IsRemovable = isRemovable,
                CustomData = new Dictionary<string, string>
                    {
                        { "listName",  listName },
                        { "itemName", itemName },
                        { "isRepeatable", isRepeatable ? "true" : "false"}
                    }
            };
        }

        public CreditApplicationCustomEditableFieldsModel.FieldModel GetFieldModel(string dataSourceName, string itemName)
        {
            if (dataSourceName == CreditApplicationItemDataSource.DataSourceNameShared)
            {
                var m = model.Value.CustomFields
                    .SingleOrDefault(x => x.DataSourceName.Equals(dataSourceName, StringComparison.OrdinalIgnoreCase) && x.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));

                return new CreditApplicationCustomEditableFieldsModel.FieldModel
                {
                    DataSourceName = dataSourceName,
                    ItemName = itemName,
                    DataType = m?.DataType ?? "string",
                    EditorType = m?.EditorType ?? "text",
                    LabelText = m?.LabelText ?? $"{dataSourceName}.{itemName}",
                    DropdownRawOptions = m?.DropdownRawOptions,
                    DropdownRawDisplayTexts = m?.DropdownRawDisplayTexts,
                    IsRemovable = m?.IsRemovable ?? false,
                    IsRequired = m?.IsRequired,
                    IsReadonly = m?.IsReadonly,
                    Translations = m?.Translations
                };
            }
            else if (dataSourceName == BankAccountTypeAndNrCreditApplicationItemDataSource.BankAccountTypeAndNrName)
            {
                return new CreditApplicationCustomEditableFieldsModel.FieldModel
                {
                    DataSourceName = dataSourceName,
                    ItemName = itemName,
                    DataType = "bankaccountnr",
                    EditorType = "bankaccountnr",
                    LabelText = "Bank account nr",
                    DropdownRawOptions = null,
                    DropdownRawDisplayTexts = null,
                    IsRemovable = false
                };
            }
            else if (dataSourceName == ComplexApplicationListDataSource.DataSourceNameShared)
            {
                if (!ComplexApplicationListDataSource.TryParseCompoundName(itemName, out var localListName, out _, out var inputIsRepeatable, out var localItemName, out var ___))
                    throw new Exception($"Invalid itemName: {itemName}");

                CreditApplicationCustomEditableFieldsModel.FieldModel m = null;
                bool? isModelRepeatable = null;
                if (localListName == null || localItemName == null)
                    throw new Exception("listName and itemName must be specified");

                m = complexApplicationListModels.Value.Opt(localListName).Opt(localItemName);
                if (m != null)
                    ComplexApplicationListDataSource.TryParseCompoundName(m.ItemName, out var i1, out var i2, out isModelRepeatable, out var i3, out var i4);

                return CreateComplexApplicationListFieldModel(itemName, localItemName, localListName, (inputIsRepeatable ?? isModelRepeatable ?? false),
                        editorType: m?.EditorType ?? "text",
                        dataType: m?.DataType ?? "string",
                        labelText: m?.LabelText ?? $"{dataSourceName}.{itemName}",
                        isRemovable: m?.IsRemovable ?? false,
                        dropdownOptionsAndTexts: m?.DropdownRawOptions != null && m?.DropdownRawDisplayTexts != null ? Tuple.Create(m?.DropdownRawOptions, m?.DropdownRawDisplayTexts) : null
                    );
            }
            else
                throw new NTechCoreWebserviceException("Invalid datasource")
                {
                    ErrorCode = "invalidDataSource",
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };
        }
    }

    public interface ICreditApplicationCustomEditableFieldsService
    {
        CreditApplicationCustomEditableFieldsModel.FieldModel GetFieldModel(string dataSourceName, string itemName);

        ISet<string> GetCustomizedItemNames(string dataSourceName);
    }

    public class CreditApplicationCustomEditableFieldsModel
    {
        public List<FieldModel> CustomFields { get; set; }

        public class FieldModel
        {
            public string DataSourceName { get; set; }
            public string ItemName { get; set; }
            public string EditorType { get; set; }
            public string DataType { get; set; }
            public string LabelText { get; set; }
            public List<string> DropdownRawOptions { get; set; }
            public List<string> DropdownRawDisplayTexts { get; set; }
            public Dictionary<string, Dictionary<string, string>> Translations { get; set; }
            public bool? IsRemovable { get; set; }
            public bool? IsReadonly { get; set; }
            public bool? IsRequired { get; set; }
            public Dictionary<string, string> CustomData { get; set; }

            public string FormatValueForDisplay(string value, CultureInfo formattingCulture)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                var dt = (DataType ?? "string").ToLowerInvariant();
                if (dt == "string")
                {
                    if (EditorType == "dropdownRaw" && DropdownRawOptions != null)
                    {
                        var i = DropdownRawOptions.IndexOf(value);
                        if (i >= 0 && DropdownRawDisplayTexts.Count > i)
                            return DropdownRawDisplayTexts[i];
                    }
                    return value;
                }
                else if (dt == "positiveInt")
                {
                    return int.Parse(value).ToString(formattingCulture);
                }
                else if (dt == "positiveDecimal")
                {
                    var v = decimal.Parse(value, CultureInfo.InvariantCulture);
                    var i = value.IndexOf(".");
                    if (i >= 0)
                    {
                        return v.ToString($"N{value.Length - i - 1}");
                    }
                    else
                    {
                        return v.ToString("N2", formattingCulture);
                    }
                }
                else if (dt == "month")
                {
                    return Dates.ParseDateTimeExactOrNull(value, "yyyy-MM-dd")?.ToString("yyyy-MM-dd", formattingCulture) ?? value;
                }
                else if (dt == "localDateAndTime")
                {
                    return Dates.ParseDateTimeExactOrNull(value, "yyyy-MM-ddTHH:mm")?.ToString("g", formattingCulture) ?? value;
                }
                else if (dt == "ibanfi")
                {
                    return IBANFi.Parse(value).FormatFor("display");
                }
                else
                {
                    return value;
                }
            }
        }
    }
}