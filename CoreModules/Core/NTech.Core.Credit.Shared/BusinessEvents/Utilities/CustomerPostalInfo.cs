using System;

namespace nCredit.DbModel.BusinessEvents
{
    public abstract class SharedCustomerPostalInfo
    {
        public abstract string GetCustomerName();
        public int CustomerId { get; set; }
        public string StreetAddress { get; set; }
        public string PostArea { get; set; }
        public string ZipCode { get; set; }
        public string AddressCountry { get; set; }
        public bool IsCompany { get; set; }

        public T GetCompanyPropertyOrNull<T>(Func<CompanyCustomerPostalInfo, T> f)
        {
            var c = this as CompanyCustomerPostalInfo;
            if (c == null)
                return default(T);
            return f(c);
        }

        public T GetPersonPropertyOrNull<T>(Func<PersonCustomerPostalInfo, T> f)
        {
            var c = this as PersonCustomerPostalInfo;
            if (c == null)
                return default(T);
            return f(c);
        }
    }

    public class PersonCustomerPostalInfo : SharedCustomerPostalInfo
    {
        public string FullName { get; set; }

        public override string GetCustomerName()
        {
            return this.FullName;
        }
    }

    public class CompanyCustomerPostalInfo : SharedCustomerPostalInfo
    {
        public string CompanyName { get; set; }
        public override string GetCustomerName()
        {
            return this.CompanyName;
        }
    }
}