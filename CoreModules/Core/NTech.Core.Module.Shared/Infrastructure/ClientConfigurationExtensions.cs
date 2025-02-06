namespace NTech.Core.Module.Shared.Infrastructure
{
    public static class ClientConfigurationExtensions
    {
        public static bool HasPerLoanDueDay(this IClientConfigurationCore source) => source.IsFeatureEnabled("ntech.feature.perloandueday");
    }
}
