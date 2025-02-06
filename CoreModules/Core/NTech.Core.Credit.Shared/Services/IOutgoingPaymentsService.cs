using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public interface IOutgoingPaymentsService
    {
        List<FetchPaymentsOutgoingPaymentModel> FetchPayments(int outgoingPaymentFileHeaderId);
    }
}