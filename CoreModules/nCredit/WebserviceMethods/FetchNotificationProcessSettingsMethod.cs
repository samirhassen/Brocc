using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods
{
    public class FetchNotificationProcessSettingsMethod : TypedWebserviceMethod<FetchNotificationProcessSettingsMethod.Request, FetchNotificationProcessSettingsMethod.Response>
    {
        public override string Path => "Credit/Fetch-Notification-Process-Settings";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            var creditType = NEnv.IsCompanyLoansEnabled
                ? DomainModel.CreditType.CompanyLoan
                : NEnv.IsMortgageLoansEnabled
                    ? DomainModel.CreditType.MortgageLoan
                    : DomainModel.CreditType.UnsecuredLoan;

            var processSettings = NEnv.NotificationProcessSettings.GetByCreditType(creditType);

            return new Response
            {
                ReminderFeeAmount = processSettings.ReminderFeeAmount,
                NrOfFreeInitialReminders = processSettings.NrOfFreeInitialReminders
            };
        }

        public class Response
        {

            public decimal? ReminderFeeAmount { get; set; }
            public int NrOfFreeInitialReminders { get; set; }
        }

        public class Request
        {

        }
    }
}