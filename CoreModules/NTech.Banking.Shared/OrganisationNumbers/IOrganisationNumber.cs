namespace NTech.Banking.OrganisationNumbers
{
    public interface IOrganisationNumber
    {
        string NormalizedValue { get; }
        string CrossCountryStorageValue { get; }
        string Country { get; }
    }
}
