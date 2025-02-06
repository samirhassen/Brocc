using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.Repository
{
    public class CoreSystemItemRepository
    {
        private int userId;
        private string informationMetadata;

        public CoreSystemItemRepository(int userId, string informationMetadata)
        {
            this.userId = userId;
            this.informationMetadata = informationMetadata;
        }

        public CoreSystemItemRepository(INTechCurrentUserMetadata user)
        {
            this.userId = user.UserId;
            this.informationMetadata = user.InformationMetadata;
        }

        public void Set(SystemItemCode code, string value, ISystemItemCreditContext context)
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

        public string Get(SystemItemCode code, ISystemItemCreditContext context)
        {
            return context.SystemItemsQueryable.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => x.Value).FirstOrDefault();
        }

        public List<string> GetWithTakeN(SystemItemCode code, ISystemItemCreditContext context, int nrToTake)
        {
            return context.SystemItemsQueryable.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Take(nrToTake).Select(x => x.Value).ToList();
        }

        public DateTimeOffset? GetLatestChangeDate(SystemItemCode code, ISystemItemCreditContext context)
        {
            return context.SystemItemsQueryable.Where(x => x.Key == code.ToString()).OrderByDescending(x => x.Timestamp).Select(x => (DateTimeOffset?)x.ChangedDate).FirstOrDefault();
        }

        public void SetTimestamp(SystemItemCode code, byte[] timestamp, ISystemItemCreditContext context)
        {
            Set(code, Convert.ToBase64String(timestamp), context);
        }

        public byte[] GetTimestamp(SystemItemCode code, ISystemItemCreditContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return Convert.FromBase64String(result);
        }

        public void SetInt(SystemItemCode code, int value, ISystemItemCreditContext context)
        {
            Set(code, value.ToString(), context);
        }

        public void SetLong(SystemItemCode code, long value, ISystemItemCreditContext context)
        {
            Set(code, value.ToString(), context);
        }

        public int? GetInt(SystemItemCode code, ISystemItemCreditContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return int.Parse(result);
        }

        public long? GetLong(SystemItemCode code, ISystemItemCreditContext context)
        {
            var result = Get(code, context);
            if (result == null)
                return null;
            return long.Parse(result);
        }
    }
}