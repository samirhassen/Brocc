using System;

namespace nCustomer.DbModel
{
    public class CustomerRelation
    {
        public int CustomerId { get; set; }
        public string RelationType { get; set; }
        public string RelationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}