using System;

namespace nCustomer.DbModel
{
    public class KycQuestionTemplate
    {
        public int Id { get; set; }
        public string RelationType { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTimeOffset? RemovedDate { get; set; }
        public int? RemovedByUserId { get; set; }
        public string ModelData { get; set; }
    }
}
