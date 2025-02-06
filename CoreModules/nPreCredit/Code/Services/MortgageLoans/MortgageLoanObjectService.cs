namespace nPreCredit.Code.Services
{
    public class MortgageLoanObjectService : IMortgageLoanObjectService
    {
        private readonly KeyValueStore loanObjectStore;

        public MortgageLoanObjectService(IKeyValueStoreService keyValueStoreService)
        {
            this.loanObjectStore = new KeyValueStore(KeyValueStoreKeySpaceCode.MortgageLoanObjectV1, keyValueStoreService);
        }

        public string GetObjectJson(string applicationNr)
        {
            return loanObjectStore.GetValue(applicationNr);
        }

        public void SetObjectJson(string applicationNr, string model)
        {
            loanObjectStore.SetValue(applicationNr, model);
        }
    }

    public interface IMortgageLoanObjectService
    {
        string GetObjectJson(string applicationNr);

        void SetObjectJson(string applicationNr, string model);
    }
}