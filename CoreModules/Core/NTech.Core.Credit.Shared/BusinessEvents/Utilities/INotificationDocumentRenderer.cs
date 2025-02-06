using nCredit.DomainModel;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public interface INotificationDocumentRenderer
    {
        string RenderDocumentToArchive(CreditType creditType, bool isForCoNotification, IDictionary<string, object> context, string archiveFilename);
    }

    public interface INotificationDocumentBatchRenderer
    {
        T WithRenderer<T>(Func<INotificationDocumentRenderer, T> f);
    }
}