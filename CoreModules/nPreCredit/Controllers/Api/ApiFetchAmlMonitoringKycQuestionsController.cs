using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("api")]
    public class ApiFetchAmlMonitoringKycQuestionsController : NController
    {
        private class KycQuestionAnswerSet
        {
            public int CustomerId { get; set; }
            public string CivicRegNr { get; set; }
            public string CreditNr { get; set; }
            public DateTime AnswerDate { get; set; }
            public class Item
            {
                public string Q { get; set; }
                public string A { get; set; }
            }
            public List<Item> QuestionAnswerCodes { get; set; }
        }

        [Route("FetchAmlMonitoringKycQuestions")]
        [HttpPost]
        public ActionResult FetchAmlMonitoringKycQuestions(string latestSeenTimestamp, List<string> questionNames, List<int> customerIds)
        {
            if (questionNames == null || questionNames.Count == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing or empty questionNames");

            var result = new List<KycQuestionAnswerSet>();
            using (var context = new PreCreditContext())
            {
                var latestTimestampItemIds = new List<int>();
                foreach (var customerIdsGroup in SplitIntoGroupsOfN((customerIds ?? new List<int>()).Select(x => x.ToString()).ToArray(), 500))
                {
                    var cids = new HashSet<string>(customerIdsGroup);

                    var apps = context
                        .CreditApplicationHeaders
                        .AsNoTracking()
                        .Where(x =>
                            x.IsFinalDecisionMade
                            && x.Items.Any(y => y.Name == "customerId" && cids.Contains(y.Value))
                            && x.Items.Any(y => y.GroupName == "application" && y.Name == "creditnr")
                            && x.Items.Any(y => y.GroupName.StartsWith("question") && questionNames.Contains(y.Name)))
                        .Select(x => x.ApplicationNr);

                    var items = context
                        .CreditApplicationItems
                        .Where(x => apps.Contains(x.ApplicationNr) && x.GroupName.StartsWith("question") && questionNames.Contains(x.Name));

                    List<string> applicationNrs;
                    if (!string.IsNullOrWhiteSpace(latestSeenTimestamp))
                    {
                        var ts = Convert.FromBase64String(latestSeenTimestamp);
                        applicationNrs = items.Where(x => BinaryComparer.Compare(x.Timestamp, ts) > 0).Select(x => x.ApplicationNr).Distinct().ToList();
                    }
                    else
                    {
                        applicationNrs = items.Select(x => x.ApplicationNr).Distinct().ToList();
                    }

                    var query = context
                        .CreditApplicationHeaders
                        .AsNoTracking()
                        .Where(x => applicationNrs.Contains(x.ApplicationNr))
                        .Select(x => new
                        {
                            x.NrOfApplicants,
                            CustomersIds = x
                                .Items
                                .Where(y => y.GroupName.StartsWith("applicant") && y.Name == "customerId")
                                .Select(y => new
                                {
                                    y.GroupName,
                                    CustomerId = y.Value
                                }),
                            CivicRegNrs = x
                                .Items
                                .Where(y => y.GroupName.StartsWith("applicant") && y.Name == "civicRegNr")
                                .Select(y => new
                                {
                                    y.GroupName,
                                    y.IsEncrypted,
                                    y.Value
                                }),
                            CreditNr = x
                                .Items
                                .Where(y => y.GroupName == "application" && y.Name == "creditnr")
                                .Select(y => y.Value)
                                .FirstOrDefault(),
                            QuestionsAndAnswers = x
                                .Items
                                .Where(y => y.GroupName.StartsWith("question") && questionNames.Contains(y.Name))
                                .Select(y => new
                                {
                                    y.Id,
                                    y.GroupName,
                                    y.ChangedDate,
                                    Question = y.Name,
                                    Answer = y.Value
                                }),
                        })
                        .Select(x => new
                        {
                            x.QuestionsAndAnswers,
                            x.NrOfApplicants,
                            x.CustomersIds,
                            x.CreditNr,
                            x.CivicRegNrs
                        });

                    var newMaxTsItemId = items.OrderByDescending(x => x.Timestamp).Select(x => (int?)x.Id).FirstOrDefault();

                    if (newMaxTsItemId.HasValue)
                    {
                        latestTimestampItemIds.Add(newMaxTsItemId.Value);
                    }

                    var queryResult = query.ToList();

                    var encryptedItemIds = queryResult.SelectMany(x => x.CivicRegNrs.Where(y => y.IsEncrypted)).Select(y => long.Parse(y.Value)).ToArray();
                    var decryptedCivicRegNrs = EncryptionContext.Load(context, encryptedItemIds, NEnv.EncryptionKeys.AsDictionary());

                    foreach (var app in queryResult)
                    {
                        for (var applicantNr = 1; applicantNr <= app.NrOfApplicants; ++applicantNr)
                        {
                            var customerId = app.CustomersIds.Single(x => x.GroupName == $"applicant{applicantNr}").CustomerId;
                            var civicRegNrItem = app.CivicRegNrs.Single(x => x.GroupName == $"applicant{applicantNr}");
                            if (cids.Contains(customerId))
                            {
                                var answerDate = app
                                    .QuestionsAndAnswers
                                    .OrderByDescending(x => x.ChangedDate)
                                    .Select(x => (DateTimeOffset?)x.ChangedDate)
                                    .FirstOrDefault();
                                var applicantAnswers = app
                                    .QuestionsAndAnswers
                                    .Where(x => x.GroupName == $"question{applicantNr}")
                                    .Select(x => new KycQuestionAnswerSet.Item { Q = x.Question, A = x.Answer })
                                    .ToList();
                                if (answerDate.HasValue && applicantAnswers.Count > 0)
                                {
                                    result.Add(new KycQuestionAnswerSet
                                    {
                                        CreditNr = app.CreditNr,
                                        AnswerDate = answerDate.Value.Date,
                                        CustomerId = int.Parse(customerId),
                                        CivicRegNr = civicRegNrItem.IsEncrypted ? decryptedCivicRegNrs[long.Parse(civicRegNrItem.Value)] : civicRegNrItem.Value,
                                        QuestionAnswerCodes = applicantAnswers
                                    });
                                }
                            }
                        }
                    }
                }

                string newLatestSeenTimestamp;
                if (latestTimestampItemIds.Count > 0)
                {
                    newLatestSeenTimestamp = Convert.ToBase64String(context.CreditApplicationItems.Where(x => latestTimestampItemIds.Contains(x.Id)).OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).First());
                }
                else
                    newLatestSeenTimestamp = null;

                return Json2(new { items = result, newLatestSeenTimestamp = newLatestSeenTimestamp });
            }
        }

        private static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }
    }
}