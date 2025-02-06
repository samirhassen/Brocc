using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace nCredit.DbModel.BusinessEvents
{
    public class PaymentPlacementSuggestionResponse
    {
        public MultiCreditPaymentPlacementInstruction Instruction { get; set; }
    }

    public class MultiCreditPaymentPlacementInstruction
    {
        [Required]
        public List<PaymentPlacementItem> NotificationPlacementItems { get; set; }
        [Required]
        public List<PaymentPlacementItem> NotNotifiedPlacementItems { get; set; }
        [Required]
        public decimal InitialPaymentAmount { get; set; }
        [Required]
        public decimal LeaveUnplacedAmount { get; set; }

        /// <summary>
        /// Deals with potential issues caused by roundtripping to the ui
        /// which has javascripts shitty floating point nrs
        /// </summary>
        public void RoundEverything()
        {
            decimal Round(decimal input) => Math.Round(input, 2);
            InitialPaymentAmount = Round(InitialPaymentAmount);
            LeaveUnplacedAmount = Round(LeaveUnplacedAmount);
            NotificationPlacementItems?.ForEach(x =>
            {
                x.AmountPlaced = Round(x.AmountPlaced);
                x.AmountWrittenOff = Round(x.AmountWrittenOff);
                x.AmountCurrent = Round(x.AmountCurrent);
            });
            NotNotifiedPlacementItems?.ForEach(x =>
            {
                x.AmountPlaced = Round(x.AmountPlaced);
                x.AmountWrittenOff = Round(x.AmountWrittenOff);
                x.AmountCurrent = Round(x.AmountCurrent);
            });
        }

        public decimal GetCreditPlacedOrWrittenOffAmount(string creditNr) => NotificationPlacementItems.Sum(x => x.GetCreditPlacedOrWrittenOffAmount(creditNr)) 
            + NotNotifiedPlacementItems.Sum(x => x.GetCreditPlacedOrWrittenOffAmount(creditNr));

        public bool IsSettledByPayment(ICreditPaymentPlacementModel credit)
        {
            return credit.GetBalance() - GetCreditPlacedOrWrittenOffAmount(credit.CreditNr) <= 0m;
        }

        public List<string> GetCreditNrs() =>
            NotificationPlacementItems.Concat(NotNotifiedPlacementItems).Select(x => x.CreditNr).DistinctPreservingOrder().ToList();

        public string GetInvalidReason(PaymentOrderService paymentOrderService)
        {
            if (NotificationPlacementItems == null || NotNotifiedPlacementItems == null)
                return "Missing NotificationPlacementItems or NotNotifiedPlacementItems";
            foreach(var item in NotificationPlacementItems.Concat(NotNotifiedPlacementItems))
            {
                var itemReason = item.GetInvalidReason(paymentOrderService);
                if (itemReason != null)
                    return itemReason;
            }
            return null;
        }
    }

}