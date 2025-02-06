namespace nCustomer.DbModel
{
    public class CustomerMessageAttachedDocument
    {
        public int Id { get; set; }
        public int CustomerMessageId { get; set; }
        public CustomerMessage Message { get; set; }
        public string FileName { get; set; }
        public string ArchiveKey { get; set; }
        public string ContentTypeMimetype { get; set; }

    }
}