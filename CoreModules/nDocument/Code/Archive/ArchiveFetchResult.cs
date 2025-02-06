using System.IO;

namespace nDocument.Code.Archive
{
    public class ArchiveFetchResult : ArchiveMetadataFetchResult
    {
        public Stream Content { get; set; }
    }
}