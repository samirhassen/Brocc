using System;
using System.Linq;

namespace nCreditReport.DbModel
{
    public class SystemItemRepository
    {
        private int userId;
        private string informationMetadata;

        public SystemItemRepository(int userId, string informationMetadata)
        {
            this.userId = userId;
            this.informationMetadata = informationMetadata;
        }

        public void Set(SystemItemCode code, string value, CreditReportContext context)
        {
            context.SystemItems.Add(new SystemItem
            {
                ChangedById = this.userId,
                ChangedDate = DateTimeOffset.Now,
                InformationMetaData = this.informationMetadata,
                Key = code.ToString(),
                Value = value
            });
        }

        public string Get(SystemItemCode code, CreditReportContext context)
        {
            return context.SystemItems.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => x.Value).FirstOrDefault();
        }

        public void SetTimestamp(SystemItemCode code, byte[] timestamp, CreditReportContext context)
        {
            Set(code, Convert.ToBase64String(timestamp), context);
        }

        public byte[] GetTimestamp(SystemItemCode code, CreditReportContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return Convert.FromBase64String(result);
        }

        public DateTimeOffset? GetLatestChangeDate(SystemItemCode code, CreditReportContext context)
        {
            return context.SystemItems.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => (DateTimeOffset?)x.ChangedDate).FirstOrDefault();
        }
    }
}