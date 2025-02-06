using NTech.Core.Module.Shared.Database;
using System;

namespace nCustomer.DbModel
{
    public class StoredCustomerQuestionSet : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime AnswerDate { get; set; }
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public string KeyValueStorageKeySpace { get; set; }
        public string KeyValueStorageKey { get; set; }
    }
}