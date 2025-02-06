using nCredit;
using nCredit.DbModel.Repository;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.PositiveCreditRegisterExportService;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister
{
    internal abstract class PcrApiClient
    {
        protected readonly ICreditEnvSettings envSettings;
        protected readonly ICoreClock clock;
        protected readonly IServiceClientSyncConverter syncConverter;
        protected readonly PcrLoggingService httpLogger;

        protected PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;        

        public PcrApiClient(ICreditEnvSettings envSettings, ICoreClock clock, IServiceClientSyncConverter syncConverter)
        {
            this.envSettings = envSettings;
            this.clock = clock;
            this.syncConverter = syncConverter;
            httpLogger = new PcrLoggingService(envSettings, clock, syncConverter);
        }

        public abstract (HttpResponseMessage responseMessage, List<string> Warnings) SendBatch(object fields, BatchType batchType, string requestUrl, ICreditContextExtended context, CoreSystemItemRepository repo);

        public abstract (List<string> Warnings, PcrBatchCheckResult BatchStatus) CheckBatchStatus(string batchReference, ICreditContextExtended context, CoreSystemItemRepository repo);

        public abstract string FetchRawGetLoanResponse(string creditNr);

        protected void SetLog(BatchType type, string batchReference, ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            if (batchTypeToSystemItemCodeMap.TryGetValue(type, out SystemItemCode systemItemCode))
            {
                repo.Set(systemItemCode, batchReference, context);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected object CreateCheckBatchStatusFields(string batchReference) => new
        {
            targetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test,
            owner = new
            {
                idCodeType = IdCodeType.BusinessId,
                idCode = Settings.OwnerIdCode
            },
            batchReference
        };

        public string GetBatchReference(BatchType type, ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            if (batchTypeToSystemItemCodeMap.TryGetValue(type, out SystemItemCode systemItemCode))
            {
                return repo.Get(systemItemCode, context);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private readonly Dictionary<BatchType, SystemItemCode> batchTypeToSystemItemCodeMap = new Dictionary<BatchType, SystemItemCode>
            {
                { BatchType.NewLoans, SystemItemCode.PositiveCreditRegisterExport_NewLoans },
                { BatchType.LoanChanges, SystemItemCode.PositiveCreditRegisterExport_LoanChanges },
                { BatchType.LoanRepayments, SystemItemCode.PositiveCreditRegisterExport_LoanRepayments },
                { BatchType.DelayedPayments, SystemItemCode.PositiveCreditRegisterExport_DelayedRepayments },
                { BatchType.LoanTerminations, SystemItemCode.PositiveCreditRegisterExport_TerminatedLoans },
                { BatchType.CheckBatchStatus, SystemItemCode.PositiveCreditRegisterExport_CheckBatchStatus }
            };
    }
}
