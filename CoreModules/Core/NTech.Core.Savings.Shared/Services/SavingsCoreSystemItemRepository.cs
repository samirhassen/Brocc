using System;
using System.Linq;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;

namespace NTech.Core.Savings.Shared.Services
{
    public class SavingsCoreSystemItemRepository
    {
        private int userId;
        private string informationMetadata;

        public SavingsCoreSystemItemRepository(int userId, string informationMetadata)
        {
            this.userId = userId;
            this.informationMetadata = informationMetadata;
        }

        public SavingsCoreSystemItemRepository(INTechCurrentUserMetadata user)
        {
            this.userId = user.UserId;
            this.informationMetadata = user.InformationMetadata;
        }

        public void Set(SystemItemCode code, string value, ISystemItemSavingsContext context)
        {
            context.AddSystemItems(new SystemItem
            {
                ChangedById = userId,
                ChangedDate = DateTimeOffset.Now,
                InformationMetaData = informationMetadata,
                Key = code.ToString(),
                Value = value
            });
        }

        public string Get(SystemItemCode code, ISystemItemSavingsContext context)
        {
            return context.SystemItemsQueryable.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => x.Value).FirstOrDefault();
        }

        public DateTimeOffset? GetLatestChangeDate(SystemItemCode code, ISystemItemSavingsContext context)
        {
            return context.SystemItemsQueryable.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => (DateTimeOffset?)x.ChangedDate).FirstOrDefault();
        }

        public void SetTimestamp(SystemItemCode code, byte[] timestamp, ISystemItemSavingsContext context)
        {
            Set(code, Convert.ToBase64String(timestamp), context);
        }

        public byte[] GetTimestamp(SystemItemCode code, ISystemItemSavingsContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return Convert.FromBase64String(result);
        }

        public void SetInt(SystemItemCode code, int value, ISystemItemSavingsContext context)
        {
            Set(code, value.ToString(), context);
        }

        public void SetLong(SystemItemCode code, long value, ISystemItemSavingsContext context)
        {
            Set(code, value.ToString(), context);
        }

        public int? GetInt(SystemItemCode code, ISystemItemSavingsContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return int.Parse(result);
        }

        public long? GetLong(SystemItemCode code, ISystemItemSavingsContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return long.Parse(result);
        }
    }
}