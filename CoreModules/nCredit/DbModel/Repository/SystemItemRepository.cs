using System;

namespace nCredit.DbModel.Repository
{
    public class SystemItemRepository
    {
        private readonly CoreSystemItemRepository repository;

        public SystemItemRepository(int userId, string informationMetadata)
        {
            this.repository = new CoreSystemItemRepository(userId, informationMetadata);
        }

        public void Set(SystemItemCode code, string value, CreditContext context) => repository.Set(code, value, context);

        public string Get(SystemItemCode code, CreditContext context) => repository.Get(code, context);

        public DateTimeOffset? GetLatestChangeDate(SystemItemCode code, CreditContext context) => repository.GetLatestChangeDate(code, context);

        public void SetTimestamp(SystemItemCode code, byte[] timestamp, CreditContext context) => repository.SetTimestamp(code, timestamp, context);

        public byte[] GetTimestamp(SystemItemCode code, CreditContext context) => repository.GetTimestamp(code, context);

        public void SetInt(SystemItemCode code, int value, CreditContext context) => repository.SetInt(code, value, context);

        public void SetLong(SystemItemCode code, long value, CreditContext context) => repository.SetLong(code, value, context);

        public int? GetInt(SystemItemCode code, CreditContext context) => repository.GetInt(code, context);

        public long? GetLong(SystemItemCode code, CreditContext context) => repository.GetLong(code, context);
    }
}