namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static TValue GetWithDefault<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key) where TValue : class, new()
        {
            return source.ContainsKey(key) ? source[key] : new TValue();
        }
    }
}
