using nCredit.DbModel.BusinessEvents;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public interface ISnailMailLoanDeliveryService
    {
        OutgoingCreditNotificationDeliveryFileHeader DeliverLoans(List<string> errors, DateTime today, ICustomerPostalInfoRepository customerPostalInfoRepository, INTechCurrentUserMetadata user, List<string> onlyTheseCreditNrs = null);
    }
}