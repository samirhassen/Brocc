using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Dynamic;

namespace nPreCredit.Code
{
    public static class PdfCreator
    {
        public static IDictionary<string, object> ToTemplateContext<T>(T m)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(m), new ExpandoObjectConverter()) as IDictionary<string, object>;
        }
    }
}