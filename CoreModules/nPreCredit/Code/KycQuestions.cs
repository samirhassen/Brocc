using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace nPreCredit.Code
{
    public class KycQuestions
    {
        //TODO: Cache
        public static dynamic FetchJsonResource(string kycQuestions)
        {
            var d = XDocuments.Parse(kycQuestions);

            var questions = d.Descendants().Where(x => x.Name.LocalName == "q");

            var e = new ExpandoObject() as IDictionary<string, object>;

            foreach (var q in questions)
            {
                var key = q.Descendants().Single(x => x.Name.LocalName == "qkey").Value?.Trim();

                var to = new ExpandoObject() as IDictionary<string, object>;

                foreach (var t in q.Descendants().Where(x => x.Name.LocalName == "qtext"))
                {
                    to[t.Attribute("lang").Value] = t.Value?.Trim();
                }

                //Answers
                var aos = new List<object>();
                foreach (var a in q.Descendants().Where(x => x.Name.LocalName == "a"))
                {
                    var ao = new ExpandoObject() as IDictionary<string, object>;

                    ao["key"] = a.Descendants().Single(x => x.Name.LocalName == "akey").Value;
                    foreach (var atext in a.Descendants().Where(x => x.Name.LocalName == "atext"))
                    {
                        ao[atext.Attribute("lang").Value] = atext.Value?.Trim();
                    }
                    aos.Add(ao);
                }

                //Extra resources
                var ros = new ExpandoObject() as IDictionary<string, object>;
                foreach (var r in q.Descendants().Where(x => x.Name.LocalName == "r"))
                {
                    var ro = new ExpandoObject() as IDictionary<string, object>;
                    var rkey = r.Descendants().Single(x => x.Name.LocalName == "rkey").Value;
                    ro["key"] = rkey;
                    foreach (var rtext in r.Descendants().Where(x => x.Name.LocalName == "rtext"))
                    {
                        ro[rtext.Attribute("lang").Value] = rtext.Value?.Trim();
                    }
                    ros[rkey] = ro;
                }

                e[key] = new
                {
                    key = key,
                    text = to,
                    answers = aos,
                    resources = ros
                };
            }

            return e;
        }
    }
}