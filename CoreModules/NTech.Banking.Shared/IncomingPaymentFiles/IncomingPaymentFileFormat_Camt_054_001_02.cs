using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NTech.Banking.IncomingPaymentFiles
{
    public class IncomingPaymentFileFormat_Camt_054_001_02 : IIncomingPaymentFileFormat
    {
        private readonly Action<Exception> logException;
        private readonly bool skipOutgoingPayments;

        public IncomingPaymentFileFormat_Camt_054_001_02(Action<Exception> logException, bool skipOutgoingPayments = false)
        {
            this.logException = logException;
            this.skipOutgoingPayments = skipOutgoingPayments;
        }

        public class ExtendedData
        {
            public int NrOfSkippedOutgoingPayments { get; set; }
            public decimal AmountOfSkippedOutgoingPayments { get; set; }
        }

        public bool TryParseExtended<TIncomingPaymentFile>(byte[] filedata, out TIncomingPaymentFile paymentFile, out string errorMessage, out ExtendedData extendedData) where TIncomingPaymentFile : IncomingPaymentFile, new()
        {
            try
            {
                errorMessage = null;
                var r = Parse<TIncomingPaymentFile>(filedata);
                paymentFile = r.Item1;
                extendedData = r.Item2;
                return true;
            }
            catch (Exception ex)
            {
                logException?.Invoke(ex);
                errorMessage = ex.Message;
                paymentFile = null;
                extendedData = null;
                return false;
            }
        }

        public bool TryParse<TIncomingPaymentFile>(byte[] filedata, out TIncomingPaymentFile paymentFile, out string errorMessage) where TIncomingPaymentFile : IncomingPaymentFile, new()
        {
            ExtendedData _;
            return TryParseExtended(filedata, out paymentFile, out errorMessage, out _);
        }

        public bool IsSupportedInCountry(string countryIsoCode)
        {
            return countryIsoCode == "FI";
        }

        public string FileFormatName
        {
            get
            {
                return "camt.054.001.02";
            }
        }

        public bool MightBeAValidFile(byte[] filedata)
        {
            try
            {
                var d = XDocuments.Load(new MemoryStream(filedata));
                return d.Descendants().Any(x => x.Name.LocalName == "BkToCstmrDbtCdtNtfctn");
            }
            catch
            {
                return false;
            }
        }

        private static T? NParse<T>(string s, Func<string, T> parse) where T : struct
        {
            if (s == null)
                return null;
            else
                return parse(s);
        }

        private Tuple<TIncomingPaymentFile, ExtendedData> Parse<TIncomingPaymentFile>(byte[] fileData) where TIncomingPaymentFile : IncomingPaymentFile, new()
        {
            var d = XDocuments.Load(new MemoryStream(fileData));

            var file = new TIncomingPaymentFile();
            file.Format = FileFormatName;
            ExtendedData extendedData = new ExtendedData
            {
                AmountOfSkippedOutgoingPayments = 0,
                NrOfSkippedOutgoingPayments = 0
            };

            var ns = XNamespace.Get("urn:iso:std:iso:20022:tech:xsd:camt.054.001.02");
            var documentElement = d.Descendants().Where(x => x.Name == ns + "BkToCstmrDbtCdtNtfctn");

            var groupHeaderElement = documentElement.Descendants().Where(x => x.Name.LocalName == "GrpHdr");

            file.ExternalId = groupHeaderElement.Descendants().Single(x => x.Name.LocalName == "MsgId").Value;
            file.ExternalCreationDate = DateTime.ParseExact(groupHeaderElement.Descendants().Single(x => x.Name.LocalName == "CreDtTm").Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

            //One for each account
            var accountElements = documentElement.Descendants().Where(x => x.Name.LocalName == "Ntfctn");
            file.Accounts = accountElements.Select(accountElement =>
            {
                var a = new IncomingPaymentFile.Account();
                a.AccountNr = new IncomingPaymentFile.BankAccountNr(IBANFi.Parse(SingleChild(accountElement, "Acct", "Id", "IBAN").Value));
                a.Currency = SingleChild(accountElement, "Acct", "Ccy").Value;

                var totalNrOfEntries = NParse(OptionalSingleChild(accountElement, "TxsSummry", "TtlNtries", "NbOfNtries")?.Value, int.Parse) ?? 0;
                var expectedTotalAmount = NParse(OptionalSingleChild(accountElement, "TxsSummry", "TtlNtries", "TtlNetNtryAmt")?.Value, x => decimal.Parse(x, CultureInfo.InvariantCulture)) ?? 0m;
                var totalAmountType = OptionalSingleChild(accountElement, "TxsSummry", "TtlNtries", "CdtDbtInd")?.Value;
                if (totalAmountType == "DBIT") //Other value is CRDT
                    expectedTotalAmount = -expectedTotalAmount;

                var incomingPaymentCount = NParse(OptionalSingleChild(accountElement, "TxsSummry", "TtlCdtNtries", "NbOfNtries")?.Value, int.Parse) ?? 0;
                var incomingPaymentAmount = NParse(OptionalSingleChild(accountElement, "TxsSummry", "TtlCdtNtries", "Sum")?.Value, x => decimal.Parse(x, CultureInfo.InvariantCulture)) ?? 0m;

                var outgingPaymentCount = NParse(OptionalSingleChild(accountElement, "TxsSummry", "TtlDbtNtries", "NbOfNtries")?.Value, int.Parse) ?? 0;
                var outgingPaymentAmount = NParse(OptionalSingleChild(accountElement, "TxsSummry", "TtlDbtNtries", "Sum")?.Value, x => decimal.Parse(x, CultureInfo.InvariantCulture)) ?? 0m;

                if (skipOutgoingPayments)
                {
                    expectedTotalAmount = incomingPaymentAmount;
                    extendedData.AmountOfSkippedOutgoingPayments += outgingPaymentAmount;
                }
                else if (outgingPaymentAmount != 0m || outgingPaymentCount != 0)
                    throw new Exception($"Incoming Payment file {file.ExternalId} has debit entries which are not supported.");

                var expectedPaymentCount = 0;

                var typedDataBatches = accountElement.Elements().Where(x => x.Name.LocalName == "Ntry").Select(x => 
                {
                    var domain = SingleChild(x, "BkTxCd", "Domn", "Cd")?.Value?.ToUpperInvariant()?.Trim();
                    return new
                    {
                        IsPayment = domain == "PMNT",
                        Domain = domain,
                        Element = x
                    };
                });

                a.DateBatches = typedDataBatches.Where(x => x.IsPayment).Select(x => x.Element).Select(dateElement =>
                {
                    var dateTotalAmountType = SingleChild(dateElement, "CdtDbtInd").Value;
                    var isOutgoingPaymentEntry = dateTotalAmountType == "DBIT";

                    var sectionPaymentCount = int.Parse(SingleChild(dateElement, "NtryDtls", "Btch", "NbOfTxs").Value);
                    if (skipOutgoingPayments && isOutgoingPaymentEntry)
                    {
                        extendedData.NrOfSkippedOutgoingPayments += sectionPaymentCount;
                        return null;
                    }

                    var b = new IncomingPaymentFile.AccountDateBatch();

                    var dateMultiplier = 1.0m;
                    if (isOutgoingPaymentEntry) //Other value is CRDT
                        dateMultiplier = -1.0m;
                    var dateTotalAmount = dateMultiplier * decimal.Parse(SingleChild(dateElement, "Amt").Value, CultureInfo.InvariantCulture);

                    var dateStatus = SingleChild(dateElement, "Sts").Value;
                    if (dateStatus != "BOOK")
                        throw new Exception(dateStatus);

                    b.BookKeepingDate = DateTime.ParseExact(SingleChild(dateElement, "BookgDt", "Dt").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    expectedPaymentCount += sectionPaymentCount;

                    var paymentsElement = SingleChild(dateElement, "NtryDtls");

                    b.Payments = paymentsElement.Elements().Where(x => x.Name.LocalName == "TxDtls").Select(paymentElement =>
                    {
                        var p = new IncomingPaymentFile.AccountDateBatchPayment();

                        p.ExternalId = OptionalSingleChild(paymentElement, "Refs", "TxId")?.Value;

                        var transactionAmountElement = SingleChild(paymentElement, "AmtDtls", "TxAmt", "Amt");
                        p.Amount = dateMultiplier * decimal.Parse(transactionAmountElement.Value, CultureInfo.InvariantCulture);

                        var transactionCurrency = transactionAmountElement.Attributes().Where(x => x.Name.LocalName == "Ccy").Single().Value;
                        if (transactionCurrency != a.Currency)
                            throw new Exception($"Transaction currency differs from account currency for transaction: {p.ExternalId}");

                        var customerInfo = OptionalSingleChild(paymentElement, "RltdPties", "Dbtr");

                        p.CustomerName = OptionalSingleChild(paymentElement, "RltdPties", "Dbtr", "Nm")?.Value;

                        var postalAddressElement = OptionalSingleChild(paymentElement, "RltdPties", "Dbtr", "PstlAdr");
                        if (postalAddressElement != null)
                        {
                            p.CustomerAddressCountry = OptionalSingleChild(postalAddressElement, "Ctry")?.Value;
                            p.CustomerAddressStreetName = OptionalSingleChild(postalAddressElement, "StrtNm")?.Value;

                            p.CustomerAddressBuildingNumber = OptionalSingleChild(postalAddressElement, "BldgNb")?.Value;
                            p.CustomerAddressPostalCode = OptionalSingleChild(postalAddressElement, "PstCd")?.Value;
                            p.CustomerAddressTownName = OptionalSingleChild(postalAddressElement, "TwnNm")?.Value;

                            //NOTE: Documentation says this is 0 or 1 but in the sample file from danske bank it repeats
                            p.CustomerAddressLines = postalAddressElement.Elements().Where(x => x.Name.LocalName == "AdrLine").Select(x => x.Value).ToList();
                        }

                        var referenceType = SingleChild(paymentElement, "RmtInf", "Strd", "CdtrRefInf", "Tp", "CdOrPrtry", "Cd").Value;
                        if (referenceType != "SCOR")
                            throw new Exception($"Expected payment with reference type SCOR but instead got {referenceType} on transaction {p.ExternalId}");

                        p.OcrReference = SingleChild(paymentElement, "RmtInf", "Strd", "CdtrRefInf", "Ref").Value?.TrimStart('0');

                        return p;
                    }).ToList();

                    return b;
                }).Where(x => x != null).ToList();                

                a.NonPaymentTransactions = typedDataBatches.Where(x => !x.IsPayment).SelectMany(x =>
                {
                    var dateElement = x.Element;
                    var description = $"Domain={x.Domain}, Family={OptionalSingleChild(dateElement, "BkTxCd", "Domn", "Fmly", "Cd")?.Value}, SubFamily={OptionalSingleChild(dateElement, "BkTxCd", "Domn", "Fmly", "SubFmlyCd")?.Value}";

                    var countRaw = OptionalSingleChild(dateElement, "NtryDtls", "Btch", "NbOfTxs")?.Value;
                    var count = string.IsNullOrWhiteSpace(countRaw) ? 1 : int.Parse(countRaw);

                    var dateTotalAmountType = SingleChild(dateElement, "CdtDbtInd").Value;
                    var isOutgoingPaymentEntry = dateTotalAmountType == "DBIT";

                     var dateMultiplier = 1.0m;
                    if (isOutgoingPaymentEntry) //Other value is CRDT
                        dateMultiplier = -1.0m;
                    var dateTotalAmount = dateMultiplier * decimal.Parse(SingleChild(dateElement, "Amt").Value, CultureInfo.InvariantCulture);

                    return new [] {
                        new IncomingPaymentFile.NonPaymentTransaction 
                        { 
                            Amount = dateTotalAmount,
                            Count = count,
                            Description = description,
                            ExternalId = OptionalSingleChild(dateElement, "AcctSvcrRef")?.Value
                        } 
                    };
                }).ToList();

                var parsedPaymentCount = a.DateBatches.SelectMany(x => x.Payments).Count();
                if (expectedPaymentCount != parsedPaymentCount)
                {
                    throw new Exception($"The file metadata specifies that is should have {expectedPaymentCount} payments but {parsedPaymentCount} were found");
                }

                var parsedTotalAmount = 
                    a.DateBatches.SelectMany(x => x.Payments).Sum(x => x.Amount)
                    + a.NonPaymentTransactions.Sum(x => x.Amount);
                if (expectedTotalAmount != parsedTotalAmount )
                {
                    throw new Exception($"The file metadata specifies that it should have {expectedTotalAmount.ToString(CultureInfo.InvariantCulture)} total payment sum but {parsedTotalAmount.ToString(CultureInfo.InvariantCulture)} was found");
                }

                return a;
            }).ToList();            

            return Tuple.Create(file, extendedData);
        }

        private static XElement SingleChild(XElement parent, params string[] names)
        {
            var p = parent;
            foreach (var n in names)
            {
                p = p.Elements().Single(x => x.Name.LocalName == n);
            }
            return p;
        }

        private static XElement OptionalSingleChild(XElement parent, params string[] names)
        {
            var p = parent;
            foreach (var n in names)
            {
                p = p.Elements().SingleOrDefault(x => x.Name.LocalName == n);
                if (p == null)
                    return null;
            }
            return p;
        }
    }
}