using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public interface IDocumentRenderer : IDisposable
    {
        string RenderDocumentToArchive(string templateName, IDictionary<string, object> context, string archiveFilename);
    }
}