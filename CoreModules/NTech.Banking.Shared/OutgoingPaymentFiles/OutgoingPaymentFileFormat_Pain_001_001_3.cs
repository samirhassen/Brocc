using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NTech.Banking.Shared.BankAccounts;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Banking.OutgoingPaymentFiles
{
    /// <summary>
    /// Implemented from Danske Bank integration documents. 
    /// https://danskeci.com/-/media/pdf/danskeci-com/iso-20022-xml/pain,-d-,001,-d-,001,-d-,03.pdf
    /// </summary>
    public class OutgoingPaymentFileFormat_Pain_001_001_3
    {
        private static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

        private readonly bool isProduction;

        public OutgoingPaymentFileFormat_Pain_001_001_3(bool isProduction)
        {
            this.isProduction = isProduction;
        }

        //ISO Standard: ISO 20022

        public class PaymentFile
        {
            public string CurrencyCode { get; set; }
            public string PaymentFileId { get; set; }
            public string SenderCompanyName { get; set; }
            public string SenderCompanyId { get; set; }
            public string SendingBankBic { get; set; } //DABAFIHH
            public string SendingBankName { get; set; } //Danske Bank
            public DateTime ExecutionDate { get; set; }
            public List<PaymentGroup> Groups { get; set; }

            public class PaymentGroup
            {
                public string PaymentGroupId { get; set; }
                public IBANFi FromIban { get; set; }
                public List<Payment> Payments { get; set; }
            }
        }

        public class Payment
        {
            public IBAN ToIban { get; set; }
            public string Message { get; set; }
            public decimal Amount { get; set; }
            public string PaymentReference { get; set; }
            public bool IsUrgentPayment { get; set; }
            public string PaymentId { get; set; }
            public string CustomerName { get; set; }
        }

        private const string BasePattern = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Document xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.001.001.03\"></Document>";

        private T[] SkipNulls<T>(params T[] args) where T : class
        {
            return args?.Where(x => x != null).ToArray();
        }

        private static string GenerateId(string prefix)
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[10];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[random.Value.Next(Chars.Length)];
            }

            return prefix + new string(stringChars);
        }

        public void PopulateIds(PaymentFile f)
        {
            if (string.IsNullOrWhiteSpace(f.PaymentFileId))
                f.PaymentFileId = GenerateId(isProduction ? "P-" : "T-");

            foreach (var g in f.Groups.Select((x, i) => new { group = x, groupIndex = i }))
            {
                if (string.IsNullOrWhiteSpace(g.group.PaymentGroupId))
                    g.group.PaymentGroupId = $"{f.PaymentFileId}-{g.groupIndex}";
                foreach (var p in g.group.Payments.Select((x, i) => new { payment = x, paymentIndex = i }))
                {
                    if (string.IsNullOrWhiteSpace(p.payment.PaymentId))
                        p.payment.PaymentId = $"{f.PaymentFileId}-{g.groupIndex}-{p.paymentIndex}";
                }
            }
        }

        public XDocument CreateFile(PaymentFile paymentFile, DateTimeOffset now)
        {
            var d = XDocuments.Parse(BasePattern);
            string context = $"File {paymentFile.PaymentFileId}";

            XElement Req(string name, string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception($"{context}: Missing required value {name}");
                }
                return new XElement(name, value);
            }

            XElement Opt(Lazy<XElement> element, bool condition)
            {
                return condition ? element.Value : null;
                // Returning null will yield no element in the XML being generated. 
            }

            var root = d.Descendants().Single();

            var header = new XElement("CstmrCdtTrfInitn");

            header.Add(new XElement("GrpHdr", SkipNulls(
                Req("MsgId", paymentFile.PaymentFileId),
                Req("CreDtTm", now.ToString("o")),
                isProduction ? null : new XElement("Authstn", new XElement("Prtry", "TEST")),
                Req("NbOfTxs", paymentFile.Groups.SelectMany(x => x.Payments).Count().ToString()),
                new XElement("InitgPty",
                    Req("Nm", paymentFile.SenderCompanyName),
                    new XElement("Id",
                        new XElement("OrgId",
                            new XElement("Othr",
                                Req("Id", paymentFile.SenderCompanyId), new XElement("SchmeNm", new XElement("Cd", "BANK"))))))
                )));
            root.Add(header);

            var translator = new IBANToBICTranslator();

            foreach (var g in paymentFile.Groups)
            {
                context = $"File {paymentFile.PaymentFileId}, Group {g.PaymentGroupId}";

                var pmt = new XElement("PmtInf");
                pmt.Add(Req("PmtInfId", g.PaymentGroupId));
                pmt.Add(new XElement("PmtMtd", "TRF"));
                pmt.Add(Req("NbOfTxs", g.Payments.Count().ToString()));
                pmt.Add(Req("ReqdExctnDt", paymentFile.ExecutionDate.ToString("yyyy-MM-dd")));
                pmt.Add(new XElement("Dbtr", Req("Nm", paymentFile.SenderCompanyName)));
                pmt.Add(new XElement("DbtrAcct",
                    new XElement("Id", Req("IBAN", g.FromIban.NormalizedValue)),
                    Req("Ccy", paymentFile.CurrencyCode),
                    Req("Nm", paymentFile.SendingBankName)));
                pmt.Add(new XElement("DbtrAgt",
                    new XElement("FinInstnId",
                        Req("BIC", paymentFile.SendingBankBic))));
                foreach (var p in g.Payments)
                {
                    string supplierPayment = "SUPP", structuredCreditorReference = "SCOR";
                    context = $"File {paymentFile.PaymentFileId}, Group {g.PaymentGroupId}, Payment {p.PaymentId}";

                    pmt.Add(new XElement("CdtTrfTxInf",
                        new XElement("PmtId",
                            Req("InstrId", p.PaymentId),
                            Req("EndToEndId", p.PaymentId)),
                        new XElement("PmtTpInf",
                            new XElement("SvcLvl", new XElement("Cd", p.IsUrgentPayment ? "URGP" : "SEPA")),
                            new XElement("CtgyPurp", new XElement("Cd", supplierPayment))),
                        new XElement("Amt",
                            new XElement("InstdAmt",
                                new XAttribute("Ccy", paymentFile.CurrencyCode),
                                p.Amount.ToString(CultureInfo.InvariantCulture))),
                        new XElement("ChrgBr", "SHAR"),
                        Opt(new Lazy<XElement>(() => new XElement("CdtrAgt", 
                                new XElement("FinInstnId", 
                                    new XElement("BIC", GetFinnishBic(p.ToIban, translator))))),
                            p.ToIban.NormalizedValue.StartsWith("FI")),
                        new XElement("Cdtr", Req("Nm", p.CustomerName)),
                        new XElement("CdtrAcct", new XElement("Id", Req("IBAN", p.ToIban.NormalizedValue))),
                        new XElement("RmtInf", p.PaymentReference != null 
                                ? new XElement("Strd", 
                                    new XElement("CdtrRefInf", 
                                        new XElement("Tp", 
                                            new XElement("CdOrPrtry", 
                                                new XElement("Cd", structuredCreditorReference))),
                                        Req("Ref", p.PaymentReference))) 
                                : Req("Ustrd", p.Message ?? "Payment"))
                        ));
                }
                header.Add(pmt);
            }

            //NOTE: The parse and replace cycle is to make this <CstmrCdtTrfInitn xmlns=""> into this <CstmrCdtTrfInitn>. It likely because we are parsing the header from a string and not reusing the namespaces properly but this works fine.
            return XDocuments.Parse(XDocumentToString(d).Replace("xmlns=\"\"", ""));
        }

        private string GetFinnishBic(IBAN toIban, IBANToBICTranslator translator)
        {
            var ibanFi = IBANFi.Parse(toIban.NormalizedValue);

            return translator.InferBic(ibanFi);
        }

        public byte[] CreateFileAsBytes(PaymentFile f, DateTimeOffset now)
        {
            return Encoding.UTF8.GetBytes(XDocumentToString(CreateFile(f, now)));
        }

        private static string XDocumentToString(XDocument d)
        {
            using (var w = new Utf8StringWriter())
            {
                d.Save(w);
                w.Flush();
                return w.ToString();
            }
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }
    }
}