using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code
{
    public static class Translations
    {
        public static dynamic FetchTranslation(string lang)
        {
            var translation = new BalanziaApplicationTranslation();
            Func<Dictionary<string, string>, dynamic> tableToAngularTranslateResource = t =>
            {
                dynamic exp = new ExpandoObject();
                var d = (IDictionary<string, object>)exp;
                foreach (var r in t)
                {
                    d[r.Key] = r.Value;
                }
                return exp;
            };

            var table = translation.GetTranslationTable("Translation-BalanziaApplication.xml");

            if (table.ContainsKey(lang))
            {
                return tableToAngularTranslateResource(table[lang]);
            }
            else
            {
                return null;
            }
        }
    }
}