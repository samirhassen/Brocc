namespace nCredit.Code
{
    public interface IOcrPaymentReferenceGenerator
    {
        IOcrNumber GenerateNew();
    }

    public interface IOcrNumber
    {
        string Country { get; }
        string NormalForm { get; }
        string DisplayForm { get; }
    }
}
