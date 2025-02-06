using nPreCredit.Code;
using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services;
using NTech;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.OutgoingPaymentFiles;
using NTech.Banking.Shared.BankAccounts;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class CreateMortgageLoanDualSettlementPaymentsFileMethod : TypedWebserviceMethod<CreateMortgageLoanDualSettlementPaymentsFileMethod.Request, CreateMortgageLoanDualSettlementPaymentsFileMethod.Response>
    {
        public override string Path => "MortgageLoan/Create-DualSettlementPaymentsFile";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var infoService = r.Resolve<ApplicationInfoService>();

            var ai = infoService.GetApplicationInfo(request.ApplicationNr);
            if (ai == null)
                return Error("No such application", errorCode: "noSuchApplication");

            var workFlowService = r.Resolve<IMortgageLoanWorkflowService>();

            var currentListName = workFlowService.GetCurrentListName(ai.ListNames);
            if (!workFlowService.TryDecomposeListName(currentListName, out var names))
            {
                throw new Exception("Invalid application. Current listname is broken.");
            }
            var currentStepName = names.Item1;

            var settlementStep = workFlowService.Model.FindStepByCustomData(x => x?.IsSettlement == "yes", new { IsSettlement = "" });

            if (settlementStep == null)
                throw new Exception("There needs to be a step in the workflow with CustomData item IsSettlement = \"yes\"");

            var isSettlementStep = currentStepName == settlementStep.Name;
            if (!isSettlementStep || !ai.IsActive || ai.IsFinalDecisionMade || !ai.HasLockedAgreement)
                return Error("No on the settlement step", errorCode: "wrongStatus");

            var dataSourceService = r.Resolve<ApplicationDataSourceService>();

            var extraItems = dataSourceService.NewSimpleRequest(CreditApplicationItemDataSource.DataSourceNameShared,
                "application.outgoingPaymentFileStatus");

            ApplicationDataSourceResult dataItems = null;
            if (!TryGetPaymentsPlus(request.ApplicationNr, dataSourceService, out var payments, out var errorMessage, extras: extraItems, observeResult: x => dataItems = x))
            {
                return Error(errorMessage, errorCode: "incorrectPayments", httpStatusCode: 400);
            }

            var outgoingPaymentFileStatus = dataItems.Item(CreditApplicationItemDataSource.DataSourceNameShared, "application.outgoingPaymentFileStatus").StringValue.Optional;

            if (outgoingPaymentFileStatus != "initialized")
            {
                return Error("Settlement status is not 'initialized'", errorCode: "wrongStatus");
            }

            var settings = NEnv.OutgoingPaymentFilesDanskeBankSettings;
            if (settings.Req("fileformat") != "pain.001.001.03")
                throw new Exception("Fileformat not supported: " + settings.Req("fileformat"));

            var paymentFileInfo = new OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile
            {
                CurrencyCode = "EUR",
                ExecutionDate = GetExecutionDate(requestContext.Clock()),
                SenderCompanyId = settings.Req("sendingcompanyid"),
                SenderCompanyName = settings.Req("sendingcompanyname"),
                SendingBankBic = settings.Req("sendingbankbic"),
                SendingBankName = settings.Req("sendingbankname"),
                Groups = new List<OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile.PaymentGroup>
                {
                    new OutgoingPaymentFileFormat_Pain_001_001_3.PaymentFile.PaymentGroup
                    {
                        FromIban = IBANFi.Parse(settings.Req("fromiban")),
                        Payments = payments.Select(x => new OutgoingPaymentFileFormat_Pain_001_001_3.Payment
                        {
                            Amount = x.PaymentAmount,
                            CustomerName = x.TargetBankName,
                            Message = x.MessageToReceiver,
                            ToIban = x.TargetBankAccount as IBAN,
                            IsUrgentPayment = x.IsUrgentPayment,
                            PaymentReference = x.PaymentReference
                        }).ToList()
                    }
                }
            };

            var builder = new OutgoingPaymentFileFormat_Pain_001_001_3(NEnv.IsProduction);
            builder.PopulateIds(paymentFileInfo);
            var fileBytes = builder.CreateFileAsBytes(paymentFileInfo, requestContext.Clock().Now);

            var archiveKey = r.Resolve<IDocumentClient>().ArchiveStore(
                fileBytes,
                "application/xml",
                $"OutgoingMortgageLoanSettlementPayments-{request.ApplicationNr}-{requestContext.Clock().Now.ToString("yyyy-MM-dd")}-{paymentFileInfo.PaymentFileId}.xml");

            var c = r.Resolve<IPartialCreditApplicationModelService>();
            c.Update(request.ApplicationNr, requestContext.CurrentUserMetadata(), currentStepName, applicationItems: new List<PartialCreditApplicationModelService.ApplicationUpdateItem>
            {
                new PartialCreditApplicationModelService.ApplicationUpdateItem { Name = "outgoingPaymentFileStatus", Value = "pending" },
                new PartialCreditApplicationModelService.ApplicationUpdateItem { Name = "outgoingPaymentFileArchiveKey", Value = archiveKey },
                new PartialCreditApplicationModelService.ApplicationUpdateItem { Name = "outgoingPaymentFileCreationDate", Value = requestContext.Clock().Now.ToString("o") },
            });

            return new Response();
        }

        /// <summary>
        /// Today for production, in the future in test to prevent disasters if importing it in prod ... you will at least have a month to stop the payment
        /// </summary>
        /// <param name="clock"></param>
        /// <returns></returns>
        private DateTime GetExecutionDate(NTech.IClock clock)
        {
            return NEnv.IsProduction ? clock.Today : clock.Today.AddMonths(1);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public string PaymentFileArchiveKey { get; set; }
        }

        public class DualLoanOutgoingPaymentModel
        {
            public bool IsMain { get; internal set; }
            public int RowNr { get; internal set; }
            public decimal PaymentAmount { get; set; }
            public string TargetBankName { get; set; }
            public string MessageToReceiver { get; set; }
            public IBankAccountNumber TargetBankAccount { get; set; }
            public string PaymentReference { get; set; }
            public bool IsUrgentPayment { get; set; }
        }

        public static bool TryGetPaymentsPlus(string applicationNr,
            ApplicationDataSourceService dataSourceService,
            out List<DualLoanOutgoingPaymentModel> payments,
            out string errorMessage,
            Dictionary<string, HashSet<string>> extras = null,
            Action<ApplicationDataSourceResult> observeResult = null)
        {
            if (NEnv.ClientCfg.Country.BaseCountry != "FI")
                throw new Exception("To support other countries than FI add handling of the different kinds of bank accounts");

            var requestItems = dataSourceService.AppendToSimpleRequest(
                extras,
                ComplexApplicationListDataSource.DataSourceNameShared,
                Enumerables.Array("MainSettlementPayments", "ChildSettlementPayments")
                        .SelectMany(listName => Enumerables.Array("exists", "paymentAmount", "targetBankName", "messageToReceiver", "targetAccountIban", "paymentReference", "isExpressPayment").Select(itemName =>
                            $"{listName}#*#u#{itemName}")).ToArray());

            var dataItems = dataSourceService.GetDataSimple(applicationNr, requestItems);
            observeResult?.Invoke(dataItems);

            var paymentsPre = dataItems
                .ItemNames(ComplexApplicationListDataSource.DataSourceNameShared)
                .Select(compoundName =>
                {
                    var n = ComplexApplicationListDataSource.ParseFullySpecifiedCompoundName(compoundName);
                    var value = dataItems.Item(ComplexApplicationListDataSource.DataSourceNameShared, compoundName).StringValue.Optional;
                    return new
                    {
                        n.ListName,
                        n.RowNr,
                        n.ItemName,
                        Value = value
                    };
                })
                .Where(x => x.ListName.IsOneOf("MainSettlementPayments", "ChildSettlementPayments") && x.Value != null)
                .GroupBy(x => new { x.ListName, x.RowNr })
                .Select(x =>
                {
                    return new
                    {
                        ListName = x.Key.ListName,
                        RowNr = x.Key.RowNr,
                        Exists = x.FirstOrDefault(y => y.ItemName == "exists")?.Value == "true",
                        PaymentAmountRaw = x.FirstOrDefault(y => y.ItemName == "paymentAmount")?.Value,
                        TargetBankNameRaw = x.FirstOrDefault(y => y.ItemName == "targetBankName")?.Value,
                        MessageToReceiverRaw = x.FirstOrDefault(y => y.ItemName == "messageToReceiver")?.Value,
                        TargetAccountIbanRaw = x.FirstOrDefault(y => y.ItemName == "targetAccountIban")?.Value,
                        PaymentReference = x.FirstOrDefault(y => y.ItemName == "paymentReference")?.Value,
                        IsUrgentPayment = x.FirstOrDefault(y => y.ItemName == "isExpressPayment")?.Value == "true"
                    };
                })
                .Where(x => x.Exists)
                .Select(x =>
                {
                    var isError = false;
                    string errorCode = null;
                    decimal? paymentAmount = null;
                    string targetBankName = null;
                    string messageToReceiver = null;
                    IBAN targetAccountIban = null;
                    string paymentReference = null;
                    var isUrgentPayment = false;

                    void Error(string error)
                    {
                        isError = true; errorCode = error;
                    }

                    if (string.IsNullOrWhiteSpace(x.PaymentAmountRaw))
                    {
                        Error("missing amount");
                    }
                    else
                    {
                        paymentAmount = Numbers.ParseDecimalOrNull(x.PaymentAmountRaw);
                        if (!paymentAmount.HasValue || paymentAmount <= 0m)
                        {
                            Error("invalid amount");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(x.TargetBankNameRaw))
                    {
                        Error("missing bank name");
                    }
                    else
                        targetBankName = x.TargetBankNameRaw.Trim();

                    if (string.IsNullOrWhiteSpace(x.MessageToReceiverRaw) && string.IsNullOrWhiteSpace(x.PaymentReference))
                    {
                        Error("missing either one of 'Message' or 'Reference'");
                    }

                    if (!string.IsNullOrWhiteSpace(x.MessageToReceiverRaw))
                        messageToReceiver = x.MessageToReceiverRaw.Trim();

                    if (!string.IsNullOrWhiteSpace(x.MessageToReceiverRaw) && !string.IsNullOrWhiteSpace(x.PaymentReference))
                    {
                        Error("only one of 'Message' or 'Reference' can be used");
                    }

                    if (string.IsNullOrWhiteSpace(x.TargetAccountIbanRaw) || !IBAN.TryParse(x.TargetAccountIbanRaw, out var b))
                    {
                        Error("missing bank account");
                    }
                    else
                        targetAccountIban = b;

                    paymentReference = x.PaymentReference;
                    isUrgentPayment = x.IsUrgentPayment;

                    return new
                    {
                        IsMain = x.ListName == "MainSettlementPayments",
                        x.ListName,
                        x.RowNr,
                        IsError = isError,
                        ErrorCode = errorCode,
                        PaymentAmount = isError ? new decimal?() : paymentAmount.Value,
                        TargetBankName = targetBankName,
                        MessageToReceiver = messageToReceiver,
                        TargetAccountIban = targetAccountIban,
                        PaymentReference = paymentReference,
                        IsUrgentPayment = isUrgentPayment
                    };
                })
                .ToList();

            var errors = paymentsPre.Where(x => x.IsError).ToList();
            if (errors.Count > 0)
            {
                var e = errors.First();
                errorMessage = $"Incorrect payments found. The first one is on {(e.IsMain ? "Mortgage" : "Other")} loan nr {e.RowNr}. Error: {e.ErrorCode}"; ;
                payments = null;
                return false;
            }

            errorMessage = null;
            payments = paymentsPre.Where(x => !x.IsError).Select(x => new DualLoanOutgoingPaymentModel
            {
                IsMain = x.IsMain,
                RowNr = x.RowNr,
                PaymentAmount = x.PaymentAmount.Value,
                TargetBankName = x.TargetBankName,
                MessageToReceiver = x.MessageToReceiver,
                TargetBankAccount = x.TargetAccountIban,
                PaymentReference = x.PaymentReference,
                IsUrgentPayment = x.IsUrgentPayment,

            }).ToList();
            return true;
        }
    }
}