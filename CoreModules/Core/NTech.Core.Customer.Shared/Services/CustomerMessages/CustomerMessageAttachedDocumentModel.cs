namespace nCustomer.Code.Services
{
    public class CustomerMessageAttachedDocumentModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ArchiveKey { get; set; }
        public string ContentTypeMimetype { get; set; }
    }
}