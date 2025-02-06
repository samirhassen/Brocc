using nCredit;
using nCredit.Code.Fileformats;
using nCredit.DbModel.BusinessEvents;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class PaymentFileImportService
    {
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICreditEnvSettings envSettings;
        private readonly ILoggingService loggingService;
        private readonly MultiCreditPlacePaymentBusinessEventManager paymentBusinessEventManager;
        private readonly CreditContextFactory contextFactory;
        private readonly PaymentAccountService paymentAccountService;

        public PaymentFileImportService(IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings, ILoggingService loggingService, 
            MultiCreditPlacePaymentBusinessEventManager paymentBusinessEventManager, CreditContextFactory contextFactory, PaymentAccountService paymentAccountService)
        {
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            this.loggingService = loggingService;
            this.paymentBusinessEventManager = paymentBusinessEventManager;
            this.contextFactory = contextFactory;
            this.paymentAccountService = paymentAccountService;
        }

        public PaymentFileImportResponse ImportFile(PaymentFileImportRequest request)
        {
            var parser = new IncomingPaymentFileParser(clientConfiguration, envSettings, loggingService);

            if (!parser.IsKnownFormat(request.FileFormatName))
                throw new NTechCoreWebserviceException($"Fileformat '{request.FileFormatName}' not supported") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (request.FileName == null)
                throw new NTechCoreWebserviceException($"Missing fileName") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (request.FileAsDataUrl == null)
                throw new NTechCoreWebserviceException($"Missing fileAsDataUrl") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            string mimetype;
            byte[] binaryData;
            
            if (!FileUtilities.TryParseDataUrl(request.FileAsDataUrl, out mimetype, out binaryData))
            {
                throw new NTechCoreWebserviceException($"Invalid file") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }

            try
            {
                string failedMessage;
                string placementMessage = null;

                if (!parser.TryParseWithOriginal(binaryData, request.FileName, request.FileFormatName, out var file, out var errorMessage))
                {
                    throw new NTechCoreWebserviceException($"Invalid '{request.FileFormatName}'-file: {errorMessage}") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                if (!paymentBusinessEventManager.TryImportFile(file, request.OverrideDuplicateCheck, request.OverrideIbanCheck, out failedMessage, out placementMessage))
                {
                    throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                return new PaymentFileImportResponse
                {
                    Message = placementMessage
                };
            }
            catch (NTechCoreWebserviceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, "Incoming payment file could not be imported");
                throw new NTechCoreWebserviceException("Internal server error") { IsUserFacing = true, ErrorHttpStatusCode = 500 };
            }
        }

 
        public PaymentFileFileDataResponse GetFileData(PaymentFileFileDataRequest request)
        {
            var parser = new IncomingPaymentFileParser(clientConfiguration, envSettings, loggingService);
            if (!parser.IsKnownFormat(request.FileFormatName))
                throw new NTechCoreWebserviceException($"Fileformat '{request.FileFormatName}' not supported") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (request.FileName == null)
                throw new NTechCoreWebserviceException($"Missing fileName") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (request.FileAsDataUrl == null)
                throw new NTechCoreWebserviceException($"Missing fileAsDataUrl") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            string mimetype;
            byte[] binaryData;

            if (!FileUtilities.TryParseDataUrl(request.FileAsDataUrl, out mimetype, out binaryData))
            {
                throw new NTechCoreWebserviceException($"Invalid file") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }

            IncomingPaymentFileWithOriginal file;

            try
            {
                if (!parser.MightBeAValidFile(binaryData, request.FileFormatName))
                {
                    throw new NTechCoreWebserviceException($"Invalid '{request.FileFormatName}'-file") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                if (!parser.TryParseWithOriginal(binaryData, request.FileName, request.FileFormatName, out file, out var errorMessage))
                {
                    throw new NTechCoreWebserviceException($"Invalid '{request.FileFormatName}'-file: {errorMessage}") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }
            }
            catch(NTechCoreWebserviceException)
            {
                throw;
            }
            catch(Exception ex)
            {
                loggingService.Warning(ex, $"GetFileData: file could not be parsed. {request.FileName}, {request.FileFormatName}");
                throw new NTechCoreWebserviceException($"Invalid '{request.FileFormatName}'-file") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }


            var currencies = file.Accounts.Select(x => x.Currency).Distinct();
            if (currencies.Count() > 1)
            {
                throw new NTechCoreWebserviceException($"File contains payments in multiple currencies which is not supported") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }

            if (currencies.Single() != this.clientConfiguration.Country.BaseCurrency)
            {
                throw new NTechCoreWebserviceException($"File contains non base currency payments which is not supported") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }

            var allPayments = file.Accounts.SelectMany(x => x.DateBatches).SelectMany(x => x.Payments);

            using (var context = contextFactory.CreateContext())
            {
                var includedBankAccountNrs = file.Accounts.Select(x => x.AccountNr.NormalizedValue).ToList();
                var expectedBankAccount = paymentAccountService.GetIncomingPaymentBankAccountNr();
                return new PaymentFileFileDataResponse
                {
                    HasBeenImported = context.IncomingPaymentFileHeadersQueryable.Any(x => x.ExternalId == file.ExternalId),
                    FileCreationDate = file.ExternalCreationDate,
                    ExternalId = file.ExternalId,
                    IncludedBankAccountNrs = string.Join(", ", includedBankAccountNrs),
                    ExpectedBankAccountNr = expectedBankAccount.FormatFor(null),
                    HasUnexpectedBankAccountNrs = includedBankAccountNrs.Any(x => x != expectedBankAccount.FormatFor(null)),
                    TotalPaymentCount = allPayments.Count(),
                    TotalPaymentSum = allPayments.Sum(x => (decimal?)x.Amount) ?? 0m
                };
            }
        }
         
    }

    public class PaymentFileFileDataRequest
    {
        [Required]
        public string FileFormatName { get; set; }
        [Required]
        public string FileName { get; set; }
        [Required]
        public string FileAsDataUrl { get; set; }
    }

    public class PaymentFileFileDataResponse
    {
        public bool HasBeenImported { get; internal set; }
        public DateTime FileCreationDate { get; internal set; }
        public string ExternalId { get; internal set; }
        public string IncludedBankAccountNrs { get; internal set; }
        public string ExpectedBankAccountNr { get; internal set; }
        public bool HasUnexpectedBankAccountNrs { get; internal set; }
        public int TotalPaymentCount { get; internal set; }
        public decimal TotalPaymentSum { get; internal set; }
    }


    public class PaymentFileImportRequest
    {
        [Required]
        public string FileFormatName { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FileAsDataUrl { get; set; }

        public bool? OverrideDuplicateCheck { get; set; }

        public bool? OverrideIbanCheck { get; set; }
    }

    public class PaymentFileImportResponse
    {
        public string Message { get; set; }
    }
}
