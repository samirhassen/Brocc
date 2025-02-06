namespace nCustomerPages.Code
{
    public class ApplicationDocumentResponse
    {
        public bool Exists { get; set; }
        public string DocumentType { get; set; }
        public string ArchiveKey { get; set; }
        public string Filename { get; set; }
    }
}