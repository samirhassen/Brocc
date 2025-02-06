using System;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Services
{
    public interface ICustomerSearchSourceService
    {
        ISet<int> FindCustomers(string searchQuery);
        List<CustomerSearchEntity> GetCustomerEntities(int customerId);
    }

    public class CustomerSearchEntity
    {
        /// <summary>
        /// nSavings = Savings accounts, nCredit = Credits/Loans, nPreCredit = Credit applications
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Source dependent entity type
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// ApplicationNr, SavingsAccountNr, CreditNr or similar
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Source dependent status text
        /// </summary>
        public string StatusDisplayText { get; set; }

        /// <summary>
        /// Source dependent status code
        /// </summary>       
        public string StatusCode { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Roles the customer has on this entity.
        /// Can be things like Applicant, Authorized signatory, Customer
        /// </summary>
        public List<CustomerSearchEntityCustomer> Customers { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? CurrentBalance { get; set; }
        public string GroupId { get; set; }
        public string GroupType { get; set; }

    }

    public class CustomerSearchEntityCustomer
    {
        public int CustomerId { get; set; }
        public List<string> Roles { get; set; }
    }
}
