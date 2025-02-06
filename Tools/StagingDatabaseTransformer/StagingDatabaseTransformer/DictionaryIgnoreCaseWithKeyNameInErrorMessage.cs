using System;
using System.Collections.Generic;
using System.Collections;

namespace StagingDatabaseTransformer
{
    public class DictionaryIgnoreCaseWithKeyNameInErrorMessage : IDictionary<string, string>
    {
        private IDictionary<string, string> backingStore = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public string this[string key]
        {
            get
            {
                try
                {
                    return backingStore[key];
                }
                catch (Exception ex)
                {
                    throw new Exception($"Key={key}", ex);
                }
            }
            set => backingStore[key] = value;
        }

        public ICollection<string> Keys => backingStore.Keys;

        public ICollection<string> Values => backingStore.Values;

        public int Count => backingStore.Count;

        public bool IsReadOnly => backingStore.IsReadOnly;

        public void Add(string key, string value)
        {
            backingStore.Add(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            backingStore.Add(item);
        }

        public void Clear()
        {
            backingStore.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return backingStore.Contains(item);            
        }

        public bool ContainsKey(string key)
        {
            return backingStore.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return backingStore.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return backingStore.Remove(key);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return backingStore.Remove(item);
        }

        public bool TryGetValue(string key, out string value)
        {
            return backingStore.TryGetValue(key, out value);
        }

        public string Opt(string key)
        {
            if(TryGetValue(key, out var value))
                return value;
            else
                return null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable b = backingStore;
            return b.GetEnumerator();
        }
    }
}
