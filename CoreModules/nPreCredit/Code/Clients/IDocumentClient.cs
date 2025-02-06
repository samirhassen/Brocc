using System;
using System.Collections.Generic;
using System.IO;

namespace nPreCredit.Code
{

    public interface IDocumentClient
    {
        string ArchiveStore(byte[] fileData, string mimeType, string filename);
        string ArchiveStore(Uri urlToFile, string filename);
        byte[] FetchRawWithFilename(string key, out string contentType, out string filename);
        byte[] PdfRenderDirect(string templateName, IDictionary<string, object> context, bool disableTemplateCache = false);
        Stream CreateXlsx(DocumentClientExcelRequest request, TimeSpan? timeout = null);
    }
}