using NTech.Core.Customer.Shared.Settings.Form;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NTech.Core.Customer.Shared.Settings
{
    public partial class SettingsModel
    {
        public class FormDataModel
        {
            public List<FieldModel> Fields { get; set; }

            public class FieldModel
            {
                public string Name { get; set; }
                public string Type { get; set; }
                public string DisplayName { get; set; }
                public string DefaultValueSource { get; set; }
                public string DefaultValuePath { get; set; }
                public string StaticValue { get; set; }
                public int NrOfRows { get; set; }

                public bool IsInterestRateField() => Type == SettingsFormFieldTypeCode.InterestRate.ToString();
                public bool IsEnumField() => Type == SettingsFormFieldTypeCode.Enum.ToString();
                public bool IsPositiveIntegerField() => Type == SettingsFormFieldTypeCode.PositiveInteger.ToString();
                public bool IsUrlField() => Type == SettingsFormFieldTypeCode.Url.ToString();
                public bool IsHiddenTextField() => Type == SettingsFormFieldTypeCode.HiddenText.ToString();
                public bool IsTextAreaField() => Type == SettingsFormFieldTypeCode.TextArea.ToString();
                public bool IsTextField() => Type == SettingsFormFieldTypeCode.Text.ToString();

                public List<FieldEnumOptionModel> EnumOptions { get; set; }

                public bool HasClientConfigurationDefaultValue() => DefaultValueSource == SettingsDefaultValueSourceCode.ClientConfiguration.ToString();

                public bool HasStaticDefaultValue() => DefaultValueSource == SettingsDefaultValueSourceCode.StaticValue.ToString();
            }

            public class FieldEnumOptionModel
            {
                public string Code { get; set; }
                public string DisplayName { get; set; }
            }

            public FormDataModel AddInterestField(string name, string displayName, Func<FormDataModel, DefaultValue> defaultValue) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.InterestRate, defaultValue(this));

            public FormDataModel AddIntegerField(string name, string displayName, Func<FormDataModel, DefaultValue> defaultValue) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.PositiveInteger, defaultValue(this));

            public FormDataModel AddUrlField(string name, string displayName, Func<FormDataModel, DefaultValue> defaultValue) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.Url, defaultValue(this));

            public FormDataModel AddEnumField(string name, string displayName, Func<FormDataModel, DefaultValue> defaultValue, params Tuple<string, string>[] enumOptions) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.Enum, defaultValue(this), editMore: field =>
                {
                    field.EnumOptions = enumOptions.Select(x => new FieldEnumOptionModel
                    {
                        Code = x.Item1,
                        DisplayName = x.Item2
                    }).ToList();
                });

            public FormDataModel AddIsEnabledField(bool isEnabledByDefault, string overrideName = null, string overrideDisplayName = null) =>
                AddEnumField(overrideName ?? "isEnabled", overrideDisplayName ?? "Enabled", x => x.StaticDefaultValue(isEnabledByDefault ? "true" : "false"), Tuple.Create("true", "Yes"), Tuple.Create("false", "No"));

            public FormDataModel AddHiddenTextField(string name, string displayName, Func<FormDataModel, DefaultValue> defaultValue) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.HiddenText, defaultValue(this));

            public FormDataModel AddTextAreaField(string name, string displayName, int rows, Func<FormDataModel, DefaultValue> defaultValue) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.TextArea, defaultValue(this), editMore: field =>
                {
                    field.NrOfRows = rows;
                });

            public FormDataModel AddTextField(string name, string displayName, Func<FormDataModel, DefaultValue> defaultValue) =>
                AddField(name, displayName, SettingsFormFieldTypeCode.Text, defaultValue(this));

            public DefaultValue StaticDefaultValue(string value) => new DefaultValue { Source = SettingsDefaultValueSourceCode.StaticValue, Value = value };
            public DefaultValue StaticDefaultValue(decimal value) => new DefaultValue { Source = SettingsDefaultValueSourceCode.StaticValue, Value = value.ToString(CultureInfo.InvariantCulture) };

            public DefaultValue ClientConfigDefaultValue(string path) => new DefaultValue { Source = SettingsDefaultValueSourceCode.ClientConfiguration, Value = path };

            public class DefaultValue
            {
                public SettingsDefaultValueSourceCode Source { get; set; }
                public string Value { get; set; }
            }

            private FormDataModel AddField(string name, string displayName, SettingsFormFieldTypeCode typeCode, DefaultValue defaultValue, Action<FieldModel> editMore = null)
            {
                var field = new FieldModel
                {
                    Name = name,
                    Type = typeCode.ToString(),
                    DisplayName = displayName,
                    DefaultValueSource = defaultValue.Source.ToString(),
                    DefaultValuePath = defaultValue.Source == SettingsDefaultValueSourceCode.StaticValue ? null : defaultValue.Value,
                    StaticValue = defaultValue.Source == SettingsDefaultValueSourceCode.StaticValue ? defaultValue.Value : null,
                };
                editMore?.Invoke(field);
                Fields.Add(field);
                return this;
            }

            public string GetDisplayName(string fieldName) => Fields.Single(x => x.Name == fieldName).DisplayName;
        }
    }
}
