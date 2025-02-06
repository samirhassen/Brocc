using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;


namespace nCredit.DbModel.BusinessEvents
{    
    public class PaymentPlacementItem
    {
        [Required]
        public decimal AmountCurrent { get; set; }
        [Required]
        public string CreditNr { get; set; }
        /// <summary>
        /// Id that is basically a combination of CostTypeUniqueId + NotificationId + CreditNr
        /// </summary>
        [Required]
        public string ItemId { get; set; }
        [EnumCode(EnumType = typeof(PaymentPlacementItemCode))]
        [Required]
        public string ItemType { get; set; }
        [Required]
        public decimal AmountPlaced { get; set; }
        [Required]
        public decimal AmountWrittenOff { get; set; }        
        public int? NotificationId { get; set; }
        public DateTime? NotificationDueDate { get; set; }
        [Required]
        public string CostTypeUniqueId { get; set; }

        /// <summary>
        /// Used for CreateAndPlace types like RSE and not notified interest
        /// to indicate what the value would have been if we could fully pay everything.
        /// This is needed since placed = current is an invariant that must be maintained for those.
        /// Computed is used to support the ui when making manual changed to how payments are placed.
        /// </summary>
        public decimal? AmountCurrentComputed { get; set; }
        public bool HasAmountCurrentComputed { get; set; }

        public decimal GetCreditPlacedOrWrittenOffAmount(string creditNr)
        {
            if (CreditNr != creditNr)
                return 0m;

            if (ItemType == PaymentPlacementItemCode.MoveToUnplacedOrPlace.ToString())
                return AmountPlaced;
            else if (ItemType == PaymentPlacementItemCode.PlaceOrWriteoff.ToString())
                return AmountPlaced + AmountWrittenOff;
            else if (ItemType == PaymentPlacementItemCode.CreateAndPlace.ToString())
                return 0m;
            else
                throw new NotImplementedException();
        }

        public decimal GetNotificationPlacedOrWrittenOffAmunt(int notificationId)
        {
            if (NotificationId != notificationId)
                return 0m;

            if (ItemType == PaymentPlacementItemCode.MoveToUnplacedOrPlace.ToString())
                return AmountPlaced;
            else if (ItemType == PaymentPlacementItemCode.PlaceOrWriteoff.ToString())
                return AmountPlaced + AmountWrittenOff;
            else if (ItemType == PaymentPlacementItemCode.CreateAndPlace.ToString())
                return 0m;
            else
                throw new NotImplementedException();
        }

        public string GetInvalidReason(PaymentOrderService paymentOrderService)
        {
            if (AmountCurrent < 0 || AmountPlaced < 0 || AmountWrittenOff < 0)
                return "Negative amount";

            if (NotificationId.HasValue && NotificationId.Value <= 0)
                return "Invalid NotificationId";
            if (string.IsNullOrWhiteSpace(CostTypeUniqueId))
                return "Missing CostTypeUniqueId";

            var isValidUniqueId = paymentOrderService.IsValidUniqueId(CostTypeUniqueId) || CostTypeUniqueId == PaymentOrderItem.FromSwedishRse().GetUniqueId();
            if (!isValidUniqueId)
                return "Invalid CostTypeUniqueId";

            if(AmountCurrent - AmountPlaced - AmountWrittenOff < 0m)
                return "Current - Placed - WrittenOff < 0";

            return null;
        }
        
        public string MoveToUnplacedItemId { get; set; }
        public static string CreateItemId(string creditNr, string costTypeUniqueId, int? notificationId) => $"{creditNr}-{(notificationId.HasValue ? "N" : "L")}{notificationId?.ToString()}-{costTypeUniqueId}";
    }

    public enum PaymentPlacementItemCode
    {
        PlaceOrWriteoff,
        MoveToUnplacedOrPlace,
        CreateAndPlace
    }

}