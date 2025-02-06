using nCredit.DomainModel;
using System;
using System.Collections.Generic;

namespace nCredit.DbModel.BusinessEvents
{
    public interface ICreditPaymentPlacementModel
    {
        decimal ComputeNotNotifiedInterestUntil(DateTime untilDate, out int nrOfInterestDays);
        decimal GetBalance();
        decimal? GetSettlementOfferSwedishRseEstimatedAmount();
        List<INotificationPaymentPlacementModel> GetOpenNotifications();
        decimal GetNotNotifiedCapitalBalance();
        string CreditNr { get; }
        CreditStatus GetCreditStatus();
        TResult UsingActualAnnuityOrFixedMonthlyCapital<TResult>(Func<decimal, TResult> onAnnuity, Func<decimal, TResult> onFixedMonthlyPayment);
        decimal? ActiveSettlementOfferAmount { get; }
        decimal ComputeInterestAmountIgnoringInterestFromDate(DateTime transactionDate, Tuple<DateTime, DateTime> dateInterval);
        CreditType CreditType { get; }
        void PlaceExtraAmortizationPayment(decimal amount);
        bool IsMainCredit { get; }
        decimal? RemainingActivePaymentPlanAmount { get; }
    }
}