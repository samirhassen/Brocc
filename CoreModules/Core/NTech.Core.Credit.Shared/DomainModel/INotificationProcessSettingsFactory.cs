using nCredit.DomainModel;
using System;


namespace nCredit.DbModel.DomainModel
{
    public interface INotificationProcessSettingsFactory : IDisposable
    {
        NotificationProcessSettings GetByCreditType(CreditType creditType);
        NotificationProcessSettings GetByCreditType(string creditType);
    }
}