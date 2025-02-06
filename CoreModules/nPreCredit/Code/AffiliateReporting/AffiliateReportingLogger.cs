using nPreCredit.DbModel;
using System;
using System.Linq;
using System.Text;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateReportingLogger : IAffiliateReportingLogger
    {
        public void Log(long incomingApplicationEventId, string providerName, HandleEventResult result)
        {
            using (var dbContext = new PreCreditContext())
            {
                var now = DateTime.Now;
                var evt = dbContext.AffiliateReportingEvents.Single(x => x.Id == incomingApplicationEventId && x.ProviderName == providerName);

                evt.ProcessedDate = now;
                evt.ProcessedStatus = result.Status.ToString();
                if (result.WaitUntilNextAttempt.HasValue)
                    evt.WaitUntilDate = now.Add(result.WaitUntilNextAttempt.Value);

                dbContext.AffiliateReportingLogItems.Add(new AffiliateReportingLogItem
                {
                    ProviderName = providerName,
                    LogDate = now,
                    DeleteAfterDate = evt.DeleteAfterDate,
                    MessageText = ClipRight(result?.Message, 1024),
                    ProcessedStatus = result.Status.ToString(),
                    IncomingApplicationEventId = incomingApplicationEventId,
                    ThrottlingContext = result.ThrottlingCountAndContext?.Item2,
                    ThrottlingCount = result.ThrottlingCountAndContext?.Item1,
                    ExceptionText = ClipRight(result.Exception != null ? FormatException(result.Exception) : null, 1024),
                    OutgoingRequestBody = result.OutgoingRequestBody,
                    OutgoingResponseBody = ClipRight(result.OutgoingResponseBody, 10 * 1024) //Just to guard against crazy responses
                });

                dbContext.SaveChanges();
            }
        }

        private static string ClipRight(string s, int maxLength)
        {
            if (s == null)
                return s;
            return s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        private static string FormatException(Exception ex)
        {
            var b = new StringBuilder();
            var guard = 0;
            while (ex != null && guard++ < 10)
            {
                b.AppendLine(ex.GetType().Name);
                b.AppendLine(ex.Message);
                b.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            return b.ToString();
        }
    }
}