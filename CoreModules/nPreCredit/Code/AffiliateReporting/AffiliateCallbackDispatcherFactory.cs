using Autofac;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateCallbackDispatcherFactory : IAffiliateCallbackDispatcherFactory
    {
        private readonly ILifetimeScope scope;

        public AffiliateCallbackDispatcherFactory(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        public IAffiliateCallbackDispatcher GetDispatcher(string dispatcherName)
        {
            if (dispatcherName == Self.SelfDispatcher.DispatcherName)
                return scope.Resolve<Self.SelfDispatcher>();
            else if (dispatcherName == Telefinans.TelefinansDispatcher.DispatcherName)
                return scope.Resolve<Telefinans.TelefinansDispatcher>();
            else if (dispatcherName == Vertaaensin.VertaaensinDispatcher.DispatcherName)
                return scope.Resolve<Vertaaensin.VertaaensinDispatcher>();
            else if (dispatcherName == Sortter.SortterDispatcher.DispatcherName)
                return scope.Resolve<Sortter.SortterDispatcher>();
            else if (dispatcherName == Etua.EtuaDispatcher.DispatcherName)
                return scope.Resolve<Etua.EtuaDispatcher>();
            else if (dispatcherName == Eone.EoneDispatcher.DispatcherName)
                return scope.Resolve<Eone.EoneDispatcher>();
            else if (dispatcherName == Salus.SalusDispatcher.DispatcherName)
                return scope.Resolve<Salus.SalusDispatcher>();
            else if (dispatcherName == Lendo.LendoDispatcher.DispatcherName)
                return scope.Resolve<Lendo.LendoDispatcher>();
            else if (dispatcherName == Zmarta.ZmartaDispatcher.DispatcherName)
                return scope.Resolve<Zmarta.ZmartaDispatcher>();
            else if (dispatcherName == Standard.StandardDispatcher.DispatcherName)
                return scope.Resolve<Standard.StandardDispatcher>();
            else
                return null;
        }
    }
}
