using System;
using System.Linq;

namespace Newtonsoft.Json.Linq
{
    internal static class JObjectExtensions
    {
        public static int? GetIntPropertyValue(this JObject source, string name, bool ignoreCase, bool mustExist = false)
        {
            var prop = source.Properties().Where(x => x.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).SingleOrDefault();
            if (prop == null)
            {
                if (mustExist) throw new Exception($"Property {name} is missing in file. ");

                return null;
            }
            if (prop.Value.Type == JTokenType.Integer)
                return prop.ToObject<int>();
            else
                return null;
        }
    }
}
