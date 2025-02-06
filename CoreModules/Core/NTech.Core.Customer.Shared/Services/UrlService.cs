using NTech.Core.Module;
using System;

namespace nCustomer.Code.Services
{
    public class UrlService : IUrlService
    {
        private readonly INTechServiceRegistry serviceRegistry;

        public UrlService(INTechServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry;
        }

        public string ArchiveDocumentUrl(string archiveKey, bool setFilename)
        {
            return serviceRegistry.
                InternalServiceUrl("nCustomer", "Api/ArchiveDocument/Show",
                    Tuple.Create("key", archiveKey),
                    Tuple.Create("setFilename", setFilename.ToString()))
                .ToString();
        }

        public Uri GetCustomerRelationUrlOrNull(string relationType, string relationId)
        {
            if (relationType == "Credit_UnsecuredLoan" && serviceRegistry.ContainsService("nCredit"))
            {
                return serviceRegistry.InternalServiceUrl("nCredit", "Ui/Credit", Tuple.Create("creditNr", relationId));
            }
            else if (relationType == "Credit_MortgageLoan" && serviceRegistry.ContainsService("nCredit"))
            {
                return serviceRegistry.InternalServiceUrl("nCredit", "Ui/Credit", Tuple.Create("creditNr", relationId));
            }
            else if (relationType == "SavingsAccount_StandardAccount" && serviceRegistry.ContainsService("nSavings"))
            {
                return serviceRegistry.InternalServiceUrl("nSavings", $"Ui/SavingsAccount#!/Details/{relationId}");
            }
            else
                return null;
        }
    }

    public interface IUrlService
    {
        string ArchiveDocumentUrl(string archiveKey, bool setFilename);
        Uri GetCustomerRelationUrlOrNull(string relationType, string relationId);
    }
}