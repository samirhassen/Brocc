using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class LockedAgreementService : ILockedAgreementService
    {
        private readonly DocumentDatabase<LockedAgreementModel> lockedAgreementDb;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;

        public LockedAgreementService(IKeyValueStoreService keyValueStoreService, INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
            this.lockedAgreementDb = new DocumentDatabase<LockedAgreementModel>(KeyValueStoreKeySpaceCode.LockedAgreementV1, keyValueStoreService);
        }

        public bool UnlockAgreement(string applicationNr)
        {
            var wasRemoved = false;
            lockedAgreementDb.Remove(applicationNr, observeWasRemoved: x => wasRemoved = x);
            return wasRemoved;
        }

        public LockedAgreementModel LockAgreement(string applicationNr, string unsignedAgreementArchiveKey, decimal loanAmount, int creditDecisionId)
        {
            Func<LockedAgreementModel> createNew = () => new LockedAgreementModel
            {
                UnsignedAgreementArchiveKey = unsignedAgreementArchiveKey,
                IsMultiAgreement = false,
                LoanAmount = loanAmount,
                CreditDecisionId = creditDecisionId,
                LockedByUserId = ntechCurrentUserMetadata.UserId,
                LockedDate = clock.Now.DateTime
            };
            return lockedAgreementDb.SetConcurrent(applicationNr, createNew, x => createNew());
        }

        public LockedAgreementModel LockAgreementMulti(string applicationNr, Dictionary<int, string> unsignedAgreementArchiveKeyByCustomerId, decimal loanAmount, int creditDecisionId)
        {
            Func<LockedAgreementModel> createNew = () => new LockedAgreementModel
            {
                UnsignedAgreementArchiveKey = null,
                LoanAmount = loanAmount,
                CreditDecisionId = creditDecisionId,
                LockedByUserId = ntechCurrentUserMetadata.UserId,
                LockedDate = clock.Now.DateTime,
                IsMultiAgreement = true,
                UnsignedAgreementArchiveKeyByCustomerId = unsignedAgreementArchiveKeyByCustomerId
            };
            return lockedAgreementDb.SetConcurrent(applicationNr, createNew, x => createNew());
        }

        public bool TryApprovedLockedAgreement(string applicationNr, bool requireDuality, out LockedAgreementModel lockedAgreement)
        {
            var isDeniedByDualityCheck = false;

            lockedAgreement = lockedAgreementDb.UpdateOnlyConcurrent(applicationNr, x =>
            {
                if (requireDuality && ntechCurrentUserMetadata.UserId == x.LockedByUserId)
                {
                    isDeniedByDualityCheck = true;
                    return x;
                }
                x.ApprovedByUserId = ntechCurrentUserMetadata.UserId;
                x.ApprovedDate = clock.Now.DateTime;
                return x;
            });

            return !isDeniedByDualityCheck;
        }

        public LockedAgreementModel GetLockedAgreement(string applicationNr)
        {
            return lockedAgreementDb.Get(applicationNr);
        }
    }

    public class LockedAgreementModel
    {
        public string UnsignedAgreementArchiveKey { get; set; }
        public decimal LoanAmount { get; set; }
        public int CreditDecisionId { get; set; }
        public int LockedByUserId { get; set; }
        public DateTime LockedDate { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public Dictionary<int, string> UnsignedAgreementArchiveKeyByCustomerId { get; set; }
        public bool IsMultiAgreement { get; set; }
    }

    public interface ILockedAgreementService
    {
        bool UnlockAgreement(string applicationNr);

        LockedAgreementModel LockAgreement(string applicationNr, string unsignedAgreementArchiveKey, decimal loanAmount, int creditDecisionId);

        LockedAgreementModel LockAgreementMulti(string applicationNr, Dictionary<int, string> unsignedAgreementArchiveKeyByCustomerId, decimal loanAmount, int creditDecisionId);

        bool TryApprovedLockedAgreement(string applicationNr, bool requireDuality, out LockedAgreementModel lockedAgreement);

        LockedAgreementModel GetLockedAgreement(string applicationNr);
    }
}