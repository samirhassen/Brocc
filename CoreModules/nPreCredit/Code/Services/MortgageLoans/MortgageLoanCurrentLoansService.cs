using System;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanCurrentLoansService : IMortgageLoanCurrentLoansService
    {
        private readonly KeyValueStore currentLoansStore;
        public MortgageLoanCurrentLoansService(IKeyValueStoreService keyValueStoreService)
        {
            this.currentLoansStore = new KeyValueStore(KeyValueStoreKeySpaceCode.MortgageLoanCurrentLoansV1, keyValueStoreService);
        }

        public string FetchJson(string applicationNr)
        {
            if (applicationNr == null)
                throw new ArgumentNullException("applicationNr", "model cannot be null");
            return currentLoansStore.GetValue(applicationNr);
        }

        public void StoreJson(string applicationNr, string model)
        {
            if (applicationNr == null)
                throw new ArgumentNullException("applicationNr", "model cannot be null");
            if (model == null)
                throw new ArgumentNullException("model", "model cannot be null");
            currentLoansStore.SetValue(applicationNr, model);
        }
    }

    public interface IMortgageLoanCurrentLoansService
    {
        void StoreJson(string applicationNr, string model);
        string FetchJson(string applicationNr);
    }
}