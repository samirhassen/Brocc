using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace nCustomerPages.Code
{
    public static class KycQuestions
    {
        public static Func<Tuple<string, string>, Tuple<string, string>> CreateSavingsAccountQuestionTranslator(XDocument kycQuestionsDocument, string language)
        {
            //This thing could potentinally be cached
            var questions = kycQuestionsDocument
                .Descendants()
                .Where(x => x.Name.LocalName == "q")
                .Select(x => new
                {
                    QuestionName = x.Descendants().Single(y => y.Name.LocalName == "qkey").Value?.Trim(),
                    QuestionTexts = x.Descendants().Where(y => y.Name.LocalName == "qtext")?.Select(y => new
                    {
                        Lang = y.Attribute("lang").Value,
                        Text = y.Value?.Trim()
                    }).ToList(),
                    Answers = x.Descendants().Where(y => y.Name.LocalName == "a").Select(y => new
                    {
                        Name = y.Descendants().Single(z => z.Name.LocalName == "akey").Value?.Trim(),
                        Texts = y.Descendants().Where(z => z.Name.LocalName == "atext")?.Select(z => new
                        {
                            Lang = z.Attribute("lang").Value,
                            Text = z.Value?.Trim()
                        }).ToList()
                    }).ToList()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.QuestionName))
                .ToList();

            return q =>
            {
                var r = questions
                    .Where(x => x.QuestionName == q.Item1)
                    .Select(x => new
                    {
                        QuestionText = x
                            .QuestionTexts
                            ?.Where(y => y.Lang == language)
                            ?.FirstOrDefault()
                            ?.Text,
                        AnswerText = x
                            .Answers
                            ?.Where(y => y.Name == q.Item2)
                            ?.FirstOrDefault()
                            ?.Texts
                            ?.Where(y => y.Lang == language)
                            ?.FirstOrDefault()
                            ?.Text
                    })
                    .FirstOrDefault();
                return Tuple.Create(r?.QuestionText ?? q.Item1, r?.AnswerText ?? q.Item2);
            };
        }

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