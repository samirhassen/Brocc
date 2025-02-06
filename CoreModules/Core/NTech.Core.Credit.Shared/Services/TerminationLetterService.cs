using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace nCredit.Code.Services
{
    public class TerminationLetterService
    {
        private readonly Func<IDocumentRenderer> createDocumentRenderer;
        private readonly NewCreditTerminationLettersBusinessEventManager terminationLetterManager;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ILoggingService loggingService;
        private readonly IDocumentClient documentClient;

        public TerminationLetterService(Func<IDocumentRenderer> createDocumentRenderer, NewCreditTerminationLettersBusinessEventManager terminationLetterManager,
            INotificationProcessSettingsFactory notificationProcessSettingsFactory, ICustomerClient customerClient,
            IClientConfigurationCore clientConfiguration, ILoggingService loggingService, IDocumentClient documentClient)
        {
            this.createDocumentRenderer = createDocumentRenderer;
            this.terminationLetterManager = terminationLetterManager;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
            this.loggingService = loggingService;
            this.documentClient = documentClient;
        }

        public CreateTerminationLettersResult CreateTerminationLetters(bool createTerminationLetters, bool createDeliveryFile, List<string> terminateTheseCreditNrs, CreditType creditType)
        {
            string GetDocumentPrefix()
            {
                switch (creditType)
                {
                    case CreditType.UnsecuredLoan: return "credit";
                    case CreditType.MortgageLoan: return "mortgageloan";
                    case CreditType.CompanyLoan: return "companyloan";
                    default: throw new NotImplementedException();
                }
            }

            using (var renderer = createDocumentRenderer())
            {
                Func<IDictionary<string, object>, string, bool, string> renderToArchive = (printContext, filename, isCoNotified) =>
                {
                    var templateName = $"{GetDocumentPrefix()}-{(isCoNotified ? "co-" : "")}terminationletter";
                    return renderer.RenderDocumentToArchive(templateName, printContext, filename);
                };
                    

                var p = notificationProcessSettingsFactory.GetByCreditType(creditType);

                List<string> errors = new List<string>();
                var deliveryFileCreated = false;
                var w = Stopwatch.StartNew();
                HashSet<string> creditNrsWithLettersCreated = new HashSet<string>();

                var customerPostalInfoRepository = new CustomerPostalInfoRepository(p.AllowMissingCustomerAddress, customerClient, clientConfiguration);

                if (createTerminationLetters)
                {
                    if (terminateTheseCreditNrs != null)
                    {
                        creditNrsWithLettersCreated = terminationLetterManager.CreateTerminationLettersForSpecificCreditNrs(renderToArchive, terminateTheseCreditNrs.ToArray(), customerPostalInfoRepository, creditType);
                    }
                    else
                    {
                        creditNrsWithLettersCreated = terminationLetterManager.CreateTerminationLettersForEligibleCredits(renderToArchive, customerPostalInfoRepository, creditType);
                    }
                }

                if (createDeliveryFile)
                {
                    var result = terminationLetterManager.CreateDeliveryExport(errors, documentClient, customerPostalInfoRepository, creditType);
                    if (result != null)
                        deliveryFileCreated = true;
                }

                foreach (var error in errors)
                {
                    loggingService.Warning($"CreateTerminationLetters: {error}");
                }


                loggingService.Information($"CreateTerminationLetters finished, TotalMilliseconds={w.ElapsedMilliseconds}");

                var warnings = new List<string>();
                errors?.ForEach(x => warnings.Add(x));
                if (creditNrsWithLettersCreated.Count == 0 && !deliveryFileCreated)
                    warnings.Add("No termination letters created or delivered");

                return new CreateTerminationLettersResult { Errors = errors, TotalMilliseconds = w.ElapsedMilliseconds, Warnings = warnings, CreditNrsWithLettersCreated = creditNrsWithLettersCreated };
            }
        }
    }

    public class CreateTerminationLettersResult
    {
        public List<string> Errors { get; set; }
        public long TotalMilliseconds { get; set; }
        public List<string> Warnings { get; set; }
        public HashSet<string> CreditNrsWithLettersCreated { get; set; }
    }
}