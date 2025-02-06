using nCustomer.DbModel;
using System.Collections.Generic;

namespace nCustomer
{
    public class CustomerModel
    {
        public int CustomerId { get; set; }
        public CustomerProperty CivicRegNr { get; set; }
        public List<CustomerPropertyModel> Items { get; set; }

        public CustomerModel()
        {
            CivicRegNr = new CustomerProperty();
            Items = new List<CustomerPropertyModel>();
        }
    }
}