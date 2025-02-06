using System.Text;

namespace nCreditReport.Code
{
    public interface IDocumentClient
    {
        string ArchiveStore(byte[] fileData, string mimeType, string filename);
        string FetchRawString(string key, Encoding encoding);
    }
}