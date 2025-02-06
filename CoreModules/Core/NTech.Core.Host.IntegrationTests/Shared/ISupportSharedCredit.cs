using Moq;
using nCredit;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests
{
    public interface ISupportSharedCredit
    {
        ICreditEnvSettings CreditEnvSettings { get; }
        CreditContextFactory CreateCreditContextFactory();
        CreditType CreditType { get; }
        NotificationProcessSettings NotificationProcessSettings { get; }
    }

    public static class ISupportSharedCreditExtensions
    {
        public static INotificationProcessSettingsFactory GetNotificationProcessSettingsFactory(this ISupportSharedCredit source)
        {
            var m = new Mock<INotificationProcessSettingsFactory>(MockBehavior.Strict);
            m.Setup(x => x.GetByCreditType(source.CreditType)).Returns(source.NotificationProcessSettings);
            return m.Object;
        }

        public static List<PaymentOrderItem> PaymentOrder<T>(this T source) where T : SupportShared, ISupportSharedCredit =>        
            source.GetRequiredService<PaymentOrderService>().GetPaymentOrderItems();

        public static TReturn WithCreditDb<TReturn>(this ISupportSharedCredit source,  Func<Credit.Database.CreditContextExtended, TReturn> f)
        {
            using(var context = (Credit.Database.CreditContextExtended)source.CreateCreditContextFactory().CreateContext())
            {
                return f(context);
            }
        }
    }

    public abstract class CreditSupportShared : SupportShared, ISupportSharedCredit
    {
        public abstract ICreditEnvSettings CreditEnvSettings { get; set; }

        public abstract CreditType CreditType { get; }

        public abstract NotificationProcessSettings NotificationProcessSettings { get; set; }

        public abstract CreditContextFactory CreateCreditContextFactory();        
    }
}
