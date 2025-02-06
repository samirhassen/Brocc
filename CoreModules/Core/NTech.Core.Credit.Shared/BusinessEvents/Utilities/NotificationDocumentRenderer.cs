using nCredit.DomainModel;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public class NotificationDocumentRenderer : INotificationDocumentRenderer, INotificationDocumentBatchRenderer
    {
        private IDocumentRenderer documentRenderer;
        private readonly Func<IDocumentRenderer> createDocumentRenderer;

        public NotificationDocumentRenderer(Func<IDocumentRenderer> createDocumentRenderer)
        {
            this.createDocumentRenderer = createDocumentRenderer;
            this.documentRenderer = null;
        }

        private string GetTemplateName(CreditType creditType, bool isForCoNotification)
        {
            if (isForCoNotification && creditType != CreditType.MortgageLoan)
                throw new NotImplementedException();

            switch (creditType)
            {
                case CreditType.UnsecuredLoan: return "credit-notification";
                case CreditType.MortgageLoan: return $"mortgageloan-{(isForCoNotification ? "co-" : "")}notification";
                case CreditType.CompanyLoan: return "companyloan-notification";
                default:
                    throw new NotImplementedException();
            }
        }

        public string RenderDocumentToArchive(CreditType creditType, bool isForCoNotification, IDictionary<string, object> context, string archiveFilename)
        {
            if (this.documentRenderer == null)
                throw new Exception("Not rendering currently");

            var templateName = GetTemplateName(creditType, isForCoNotification);
            return documentRenderer.RenderDocumentToArchive(templateName, context, archiveFilename);
        }

        public T WithRenderer<T>(Func<INotificationDocumentRenderer, T> f)
        {
            using (this.documentRenderer = createDocumentRenderer())
            {
                return f(this);
            }
        }
    }
}