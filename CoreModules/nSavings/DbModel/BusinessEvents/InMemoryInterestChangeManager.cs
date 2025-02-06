using NTech;
using System;

namespace nSavings.DbModel.BusinessEvents
{
    public class InMemoryInterestChangeManager
    {
        private object lockObject = new object();
        private ChangeState currentChange = null;

        public class ChangeState
        {
            public string SavingsAccountTypeCode { get; set; }
            public string ChangeToken { get; set; }
            public decimal? OldInterestRatePercent { get; set; }
            public decimal NewInterestRatePercent { get; set; }
            public DateTime AllAccountsValidFromDate { get; set; }
            public DateTime? NewAccountsValidFromDate { get; set; }
            public int InitiatedByUserId { get; set; }
            public DateTimeOffset InitiatedDate { get; set; }

            public int? VerifiedByUserId { get; set; }
            public int? RejectedByUserId { get; set; }
            public DateTimeOffset? VerifiedOrRejectedDate { get; set; }
        }

        private void WipeCurrentIfOld(IClock clock)
        {
            if (currentChange != null && clock.Now.Subtract(currentChange.InitiatedDate) > TimeSpan.FromMinutes(15))
            {
                currentChange = null;
            }
        }

        public bool TryInitiateChange(IClock clock, string savingsAccountTypeCode, decimal? oldInterestRatePercent, decimal newInterestRatePercent, DateTime allAccountsValidFromDate, DateTime? newAccountsValidFromDate, int currentUserId, out string failedMessage, out ChangeState state)
        {
            lock (lockObject)
            {
                WipeCurrentIfOld(clock);
                if (currentChange != null)
                {
                    failedMessage = "There is already an active change";
                    state = null;
                    return false;
                }

                currentChange = new ChangeState
                {
                    SavingsAccountTypeCode = savingsAccountTypeCode,
                    AllAccountsValidFromDate = allAccountsValidFromDate,
                    NewAccountsValidFromDate = newAccountsValidFromDate,
                    ChangeToken = BusinessEventManagerBase.GenerateUniqueOperationKey(),
                    InitiatedByUserId = currentUserId,
                    InitiatedDate = clock.Now,
                    NewInterestRatePercent = newInterestRatePercent,
                    OldInterestRatePercent = oldInterestRatePercent
                };

                failedMessage = null;
                state = currentChange;
                return true;
            }
        }

        public ChangeState GetCurrentChangeState()
        {
            return this.currentChange;
        }

        public bool TryVerifyCurrentChange(IClock clock, int currentUserId, string changeToken, out string failedMessage, bool dontEnforceDuality = false)
        {
            return TryVerifyOrRejectCurrentChange(clock, currentUserId, changeToken, true, out failedMessage, dontEnforceDuality: dontEnforceDuality);
        }

        public bool TryRejectCurrentChange(IClock clock, int currentUserId, string changeToken, out string failedMessage, bool dontEnforceDuality = false)
        {
            return TryVerifyOrRejectCurrentChange(clock, currentUserId, changeToken, false, out failedMessage, dontEnforceDuality: dontEnforceDuality);
        }

        private bool TryCancelOrCarryOutCancelCurrentChange(IClock clock, int currentUserId, string changeToken, bool isCarriedOut, Func<ChangeState, Tuple<bool, string>> tryDoChange, out string failedMessage, bool dontEnforceDuality = false)
        {
            ChangeState finalState = null;

            lock (lockObject)
            {
                WipeCurrentIfOld(clock);
                if (currentChange == null)
                {
                    failedMessage = "There is no active change.";
                    return false;
                }
                if (currentChange.InitiatedByUserId != currentUserId && isCarriedOut)
                {
                    failedMessage = "You can only approve your own changes.";
                    return false;
                }
                if (currentChange.ChangeToken != changeToken)
                {
                    failedMessage = "The currently active change has been altered. Please refresh the page and make sure the new change is still ok.";
                    return false;
                }
                if (isCarriedOut && !currentChange.VerifiedByUserId.HasValue)
                {
                    failedMessage = "You cannot approve a change that has not been verified.";
                    return false;
                }
                finalState = currentChange;

                if (tryDoChange != null)
                {
                    var result = tryDoChange(finalState);
                    if (!result.Item1)
                    {
                        failedMessage = result.Item2;
                        return false;
                    }
                }

                currentChange = null;

                failedMessage = null;

                return true;
            }
        }

        public bool TryCarryOutCurrentChange(IClock clock, int currentUserId, string changeToken, Func<ChangeState, Tuple<bool, string>> tryDoChange, out string failedMessage, bool dontEnforceDuality = false)
        {
            return TryCancelOrCarryOutCancelCurrentChange(clock, currentUserId, changeToken, true, tryDoChange, out failedMessage, dontEnforceDuality: dontEnforceDuality);
        }

        public bool TryCancelCurrentChange(IClock clock, int currentUserId, string changeToken, out string failedMessage, bool dontEnforceDuality = false)
        {
            return TryCancelOrCarryOutCancelCurrentChange(clock, currentUserId, changeToken, false, null, out failedMessage, dontEnforceDuality: dontEnforceDuality);
        }

        private bool TryVerifyOrRejectCurrentChange(IClock clock, int currentUserId, string changeToken, bool isVerified, out string failedMessage, bool dontEnforceDuality = false)
        {
            lock (lockObject)
            {
                WipeCurrentIfOld(clock);
                if (currentChange == null)
                {
                    failedMessage = "There is no active change.";
                    return false;
                }
                if (currentChange.InitiatedByUserId == currentUserId && !dontEnforceDuality)
                {
                    failedMessage = "You cannot verify/reject your own change.";
                    return false;
                }
                if (currentChange.ChangeToken != changeToken)
                {
                    failedMessage = "The currently active change has been altered. Please refresh the page and make sure the new change is still ok.";
                    return false;
                }
                if (currentChange.VerifiedOrRejectedDate.HasValue)
                {
                    failedMessage = "The change has already been verified or rejected.";
                    return false;
                }
                currentChange.VerifiedOrRejectedDate = clock.Now;
                if (isVerified)
                    currentChange.VerifiedByUserId = currentUserId;
                else
                    currentChange.RejectedByUserId = currentUserId;

                failedMessage = null;
                return true;
            }
        }
    }
}
