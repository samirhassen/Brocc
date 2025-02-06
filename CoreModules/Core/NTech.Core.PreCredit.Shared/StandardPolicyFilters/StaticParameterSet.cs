using Newtonsoft.Json;
using System.Collections.Generic;

namespace nPreCredit.Code.StandardPolicyFilters
{
    /// <summary>
    /// Static parameters for a single scoring rule.
    /// Be aware that - unlike scoring variables - these are only unique within a specific scoring rule so dont share these.
    /// </summary>
    public class StaticParameterSet
    {
        public Dictionary<string, string> StoredValues { get; set; }

        private string GetStoredValue(string name) => StoredValues == null
            ? null
            : (StoredValues.TryGetValue(name, out var value) ? value : null);

        public string GetString(string name)
        {
            var value = GetStoredValue(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new PolicyFilterException($"Missing static parameter {name}")
                {
                    IsMissingStaticParameter = true,
                    MissingVariableOrParameterName = name
                };
            return value;
        }

        public int GetInt(string name) => int.Parse(GetString(name));
        public decimal GetDecimal(string name) => decimal.Parse(GetString(name), System.Globalization.CultureInfo.InvariantCulture);

        public List<string> GetStringList(string name) => JsonConvert.DeserializeObject<List<string>>(GetString(name));

        public StaticParameterSet SetString(string name, string value)
        {
            if (StoredValues == null)
                StoredValues = new Dictionary<string, string>();
            StoredValues[name] = value;
            return this;
        }

        public StaticParameterSet SetInt(string name, int value) =>
            SetString(name, value.ToString());

        public StaticParameterSet SetDecimal(string name, decimal value) =>
            SetString(name, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public StaticParameterSet SetPercent(string name, decimal value) =>
            SetDecimal(name, value);

        public StaticParameterSet SetStringList(string name, List<string> value) =>
            SetString(name, JsonConvert.SerializeObject(value));

        public static StaticParameterSet CreateFromStoredValues(Dictionary<string, string> storedValues) =>
            new StaticParameterSet { StoredValues = storedValues ?? new Dictionary<string, string>() };

        public static StaticParameterSet CreateEmpty() => new StaticParameterSet { StoredValues = new Dictionary<string, string>() };
    }
}