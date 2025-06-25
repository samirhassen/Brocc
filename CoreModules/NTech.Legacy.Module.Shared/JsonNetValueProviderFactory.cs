using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NTech.Services.Infrastructure
{
    public class JsonNetValueProviderFactory : ValueProviderFactory
    {
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            // first make sure we have a valid context
            if (controllerContext == null)
                throw new ArgumentNullException(nameof(controllerContext));

            // now make sure we are dealing with a json request
            if (!controllerContext.HttpContext.Request.ContentType.StartsWith("application/json",
                    StringComparison.OrdinalIgnoreCase))
                return null;

            // get a generic stream reader (get reader for the http stream)
            var streamReader = new StreamReader(controllerContext.HttpContext.Request.InputStream);
            // convert stream reader to a JSON Text Reader
            var jsonReader = new JsonTextReader(streamReader);
            // tell JSON to read
            if (!jsonReader.Read())
                return null;

            // make a new Json serializer
            var jsonSerializer = new JsonSerializer();
            // add the dyamic object converter to our serializer
            jsonSerializer.Converters.Add(new ExpandoObjectConverter());

            // use JSON.NET to deserialize object to a dynamic (expando) object
            object jsonObject;
            // if we start with a "[", treat this as an array
            if (jsonReader.TokenType == JsonToken.StartArray)
                jsonObject = jsonSerializer.Deserialize<List<ExpandoObject>>(jsonReader);
            else
                jsonObject = jsonSerializer.Deserialize<ExpandoObject>(jsonReader);

            // create a backing store to hold all properties for this deserialization
            var backingStore = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            // add all properties to this backing store
            AddToBackingStore(backingStore, jsonObject, string.Empty);
            // return the object in a dictionary value provider so the MVC understands it
            return new DictionaryValueProvider<object>(backingStore, CultureInfo.CurrentCulture);
        }

        private static void AddToBackingStore(Dictionary<string, object> backingStore, object value,
            string prefix = null)
        {
            prefix = prefix ?? string.Empty;
            switch (value)
            {
                case IDictionary<string, object> d:
                {
                    foreach (var entry in d)
                    {
                        AddToBackingStore(backingStore, entry.Value, MakePropertyKey(prefix, entry.Key));
                    }

                    return;
                }
                case IList l:
                {
                    for (var i = 0; i < l.Count; i++)
                    {
                        AddToBackingStore(backingStore, l[i], MakeArrayKey(prefix, i));
                    }

                    return;
                }
                default:
                    // primitive
                    backingStore[prefix] = value;
                    break;
            }
        }

        private static string MakeArrayKey(string prefix, int index)
        {
            return $"{prefix}[{index.ToString(CultureInfo.InvariantCulture)}]";
        }

        private static string MakePropertyKey(string prefix, string propertyName)
        {
            return string.IsNullOrEmpty(prefix) ? propertyName : prefix + "." + propertyName;
        }
    }
}