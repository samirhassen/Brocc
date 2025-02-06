using NTech.Core.Module.Shared.Database;
using System;

namespace nCustomer
{
    //CustomerCardConflict has been outphased as a concept
    //This class will remain for a while in case we need to backtrack
    //And then it will be removed.
    public class CustomerCardConflict : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Value { get; set; }
        public bool IsSensitive { get; set; }
        public bool IsEncrypted { get; set; }
        public Nullable<DateTimeOffset> ApprovedDate { get; set; }
        public Nullable<DateTimeOffset> DiscardedDate { get; set; }
    }
}