namespace nCreditReport.Code.PropertyValuation.UcBvSe
{
    public class ServiceResponse<T>
    {
        public string Felmeddelande { get; set; }
        public long? Felkod { get; set; }
        public string TransId { get; set; }

        public bool IsError()
        {
            return ((Felkod ?? 0L) != 0L) || !string.IsNullOrWhiteSpace(Felmeddelande);
        }

        public string GetErrorMessage()
        {
            return $"ucbv: {Felkod} - {Felmeddelande}";
        }

        public T Data { get; set; }
        public string RawFullJson { get; set; }
    }
}