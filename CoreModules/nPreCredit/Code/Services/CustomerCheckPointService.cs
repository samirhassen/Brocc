using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;

namespace nPreCredit.Code.Services
{
    public static class CustomerCheckPointService
    {
        public static void MigrateToCustomerModule(
            PreCreditContextFactoryService preCreditContextFactoryService,
            EncryptionService encryptionService,
            ICustomerClient customerClient,
            NTechServiceRegistry serviceRegistry)
        {
            var keyValueStore = new KeyValueStore(KeyValueStoreKeySpaceCode.CustomerCheckPointMigrationStatusV1, new KeyValueStoreService(preCreditContextFactoryService));

            const string DoneMarkerName = "done";
            const string KeyName = "status";

            var status = keyValueStore.GetValue(KeyName);
            if (status == "done")
            {
                return;
            }

            //Ensure customer startup has run so we dont get migration issues
            var result = new ServiceClientSyncConverterLegacy().ToSync(() =>
                new HttpClient().GetAsync(serviceRegistry.Internal.ServiceUrl("nCustomer", "hb").ToString()));
            result.EnsureSuccessStatusCode();

            int? startAfterId = status == null ? new int?() : int.Parse(status);

            int[] allIds;
            using (var context = preCreditContextFactoryService.CreateExtendedConcrete())
            {
                var baseQuery = context.CustomerCheckpoints.AsQueryable();
                if (startAfterId.HasValue)
                {
                    baseQuery = baseQuery.Where(x => x.Id > startAfterId.Value);
                }

                allIds = baseQuery.Select(x => x.Id).OrderByDescending(x => x).ToArray();
            }

            foreach (var idGroup in allIds.SplitIntoGroupsOfN(200))
            {
                List<BulkInsertCheckpointsRequest.HistoricalCheckpoint> checkpointsToMove;

                using (var context = preCreditContextFactoryService.CreateExtendedConcrete())
                {
                    var checkpointsInGroup = context
                        .CustomerCheckpoints.Where(x => idGroup.Contains(x.Id))
                        .ToList();

                    var encryptedValues = checkpointsInGroup
                        .Where(x => x.IsReasonTextEncrypted)
                        .Select(x => new { x.ReasonText, x.Id })
                        .ToList();

                    var decryptedValue = encryptionService
                        .DecryptEncryptedValues(context, encryptedValues.Select(x => long.Parse(x.ReasonText)).ToArray());

                    checkpointsToMove = checkpointsInGroup.Select(x => new BulkInsertCheckpointsRequest.HistoricalCheckpoint
                    {
                        Codes = x.IsCheckpointActive
                            ? new List<string>()
                            : new List<string> { ApplicationCheckpointService.ApplicationCheckpointCode },
                        CustomerId = x.CustomerId,
                        IsCurrentState = x.IsCurrentState,
                        StateBy = x.StateBy,
                        StateDate = x.StateDate,
                        ReasonText = x.IsReasonTextEncrypted ? decryptedValue[long.Parse(x.ReasonText)] : x.ReasonText
                    }).ToList();
                }

                customerClient.BulkInsertCheckpoints(new BulkInsertCheckpointsRequest
                {
                    Checkpoints = checkpointsToMove
                });

                var maxId = idGroup.Max();
                keyValueStore.SetValue(KeyName, maxId.ToString());
            }

            keyValueStore.SetValue(KeyName, DoneMarkerName);
        }
    }
}