using System;
using System.Linq;

namespace Newtonsoft.Json.Linq
{
    public static class JObjectExtensions
    {
        public static void RemoveJsonProperty(this JObject source, string name, bool ignoreCase)
        {
            foreach (var d in source.Children().Where(x => x.Type == JTokenType.Property).ToList())
            {
                var prop = d as JProperty;
                if (prop.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    prop.Remove();
            }
        }

        public static void AddOrReplaceJsonProperty(this JObject source, string name, JValue value, bool ignoreCase)
        {
            RemoveJsonProperty(source, name, ignoreCase);
            source.Add(name, value);
        }

        public static string GetStringPropertyValue(this JObject source, string name, bool ignoreCase)
        {
            var p = source.Properties().Where(x => x.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).SingleOrDefault();
            if (p == null)
                return null;
            if (p.Value.Type == JTokenType.String)
                return (p.Value as JValue).Value as string;
            else
                return null;
        }

        public static bool? GetBooleanPropertyValue(this JObject source, string name, bool ignoreCase)
        {
            var p = source.Properties().Where(x => x.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).SingleOrDefault();
            if (p == null)
                return null;
            if (p.Value.Type == JTokenType.Boolean)
                return (bool)(p.Value as JValue).Value;
            else
                return null;
        }

        public static decimal? GetDecimalPropertyValue(this JObject source, string name, bool ignoreCase, bool mustExist = false)
        {
            var prop = source.Properties().Where(x => x.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).SingleOrDefault();
            if (prop == null)
            {
                if (mustExist) throw new Exception($"Property {name} is missing in file. ");

                return null;
            }
            if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
                return prop.ToObject<decimal>();
            else
                return null;
        }

        public static JArray GetArray(this JObject source, string name, bool ignoreCase)
        {
            var p = source.Properties().Where(x => x.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).SingleOrDefault();
            if (p == null)
                return null;
            if (p.Value.Type == JTokenType.Array)
                return (p.Value as JArray);
            else
                return null;
        }

        public static decimal? GetDecimalPropertyValueByPath(this JObject source, params string[] path)
        {
            var result = source.SelectToken($"$.{string.Join(".", path)}") as JValue;
            if (result == null || !(result.Type == JTokenType.Float || result.Type == JTokenType.Integer))
                return null;
            return result.ToObject<decimal>();
        }
    }
}
