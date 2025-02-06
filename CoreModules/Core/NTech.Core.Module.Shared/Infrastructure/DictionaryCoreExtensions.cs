using System.Collections.Generic;

namespace System
{
    //TODO: This should be added to banking shared instead, we should remove DictionaryExtensions from NTech.Services.Infrastructure and we should rename these back to <X> instead of <X>Val
    public static class DictionaryExtensionsCore
    {
        public static TValue OptVal<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, bool removeAfter = false) where TValue : class
        {
            if (source == null || !source.ContainsKey(key))
                return null;

            if (!removeAfter)
            {
                return source[key];
            }
            else
            {
                var t = source[key];
                source.Remove(key);
                return t;
            }
        }

        public static TValue? OptValS<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : struct
        {
            if (source == null || !source.ContainsKey(key))
                return null;
            else
                return source[key];
        }

        public static TValue? OptValSDefaultValue<TKey, TValue>(this IDictionary<TKey, TValue?> source, TKey key) where TValue : struct
        {
            if (source == null || !source.ContainsKey(key))
                return new TValue?();
            else
                return source[key];
        }

        public static TValue ReqVal<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : class
        {
            if (source == null)
                return null;
            else if (!source.ContainsKey(key))
                throw new Exception($"Missing key {key.ToString()}");
            else
                return source[key];
        }

        public static TValue Ensure<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, Func<TValue> createValue)
        {
            if (source == null) return default(TValue);

            if (!source.ContainsKey(key))
                source[key] = createValue();

            return source[key];
        }
    }
}
