using NTech;
using System;
using System.Linq;

namespace nPreCredit.DbModel.Repository
{
    public class SystemItemRepository : BaseRepository
    {
        private int userId;
        private string informationMetadata;
        private IClock clock;

        public SystemItemRepository(int userId, string informationMetadata, IClock clock)
        {
            this.userId = userId;
            this.informationMetadata = informationMetadata;
            this.clock = clock;
        }

        public void Set(SystemItemCode code, string value, PreCreditContext context)
        {
            context.SystemItems.Add(new SystemItem
            {
                ChangedById = this.userId,
                ChangedDate = clock.Now,
                InformationMetaData = this.informationMetadata,
                Key = code.ToString(),
                Value = value
            });
        }

        public string Get(SystemItemCode code, PreCreditContext context)
        {
            return context.SystemItems.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => x.Value).FirstOrDefault();
        }

        public void SetTimestamp(SystemItemCode code, byte[] timestamp, PreCreditContext context)
        {
            Set(code, Convert.ToBase64String(timestamp), context);
        }

        public byte[] GetTimestamp(SystemItemCode code, PreCreditContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return Convert.FromBase64String(result);
        }

        public DateTimeOffset? GetLatestChangeDate(SystemItemCode code, PreCreditContext context)
        {
            return context.SystemItems.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => (DateTimeOffset?)x.ChangedDate).FirstOrDefault();
        }

        public void SetInt(SystemItemCode code, int? nr, PreCreditContext context)
        {
            if (nr.HasValue)
                Set(code, nr.Value.ToString(), context);
        }

        public int? GetInt(SystemItemCode code, PreCreditContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return int.Parse(result);
        }
    }
}