using System;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.Services;

namespace nSavings.DbModel.Repository
{
    public class SystemItemRepository
    {
        private readonly SavingsCoreSystemItemRepository repo;

        public SystemItemRepository(int userId, string informationMetadata)
        {
            repo = new SavingsCoreSystemItemRepository(userId, informationMetadata);
        }

        public void Set(SystemItemCode code, string value, SavingsContext context) => repo.Set(code, value, context);
        public string Get(SystemItemCode code, SavingsContext context) => repo.Get(code, context);

        public DateTimeOffset? GetLatestChangeDate(SystemItemCode code, SavingsContext context) =>
            repo.GetLatestChangeDate(code, context);

        public void SetTimestamp(SystemItemCode code, byte[] timestamp, SavingsContext context) =>
            repo.SetTimestamp(code, timestamp, context);

        public byte[] GetTimestamp(SystemItemCode code, SavingsContext context) => repo.GetTimestamp(code, context);
        public void SetInt(SystemItemCode code, int value, SavingsContext context) => repo.SetInt(code, value, context);
        public int? GetInt(SystemItemCode code, SavingsContext context) => repo.GetInt(code, context);

        public void SetLong(SystemItemCode code, long value, SavingsContext context) =>
            repo.SetLong(code, value, context);

        public long? GetLong(SystemItemCode code, SavingsContext context) => repo.GetLong(code, context);
    }
}