namespace nCustomer.Code.Services.Kyc
{
    public interface IKycScreeningProviderServiceFactory
    {
        bool DoesCurrentProviderSupportContactInfo();
        IKycScreeningProviderService CreateMultiCheckService();
    }
}