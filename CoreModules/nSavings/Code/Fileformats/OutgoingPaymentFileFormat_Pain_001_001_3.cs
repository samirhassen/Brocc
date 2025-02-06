using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace nSavings.Code
{
    public class OutgoingPaymentFileFormat_Pain_001_001_3
    {
        private static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

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
            public IBANFi ToIban { get; set; }
            public string Message { get; set; }
            public decimal Amount { get; set; }
            public string PaymentId { get; set; }
            public string CustomerName { get; set; }
        }

        private const string BasePattern = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Document xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.001.001.03\"></Document>";

        private T[] SkipNulls<T>(params T[] args) where T : class
        {
            if (args == null) return null;
            return args.Where(x => x != null).ToArray();
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
                f.PaymentFileId = GenerateId(NEnv.IsProduction ? "P-" : "T-");

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

        public XDocument CreateFile(PaymentFile f, DateTimeOffset now)
        {
            var d = XDocuments.Parse(BasePattern);
            string context = $"File {f.PaymentFileId}";

            Func<string, string, XElement> req = (name, value) =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new Exception($"{context}: Missing required value {name}");
                    }
                    return new XElement(name, value);
                };

            var root = d.Descendants().Single();

            var header = new XElement("CstmrCdtTrfInitn");

            header.Add(new XElement("GrpHdr", SkipNulls(
                req("MsgId", f.PaymentFileId),
                req("CreDtTm", now.ToString("o")),
                NEnv.IsProduction ? null : new XElement("Authstn", new XElement("Prtry", "TEST")),
                req("NbOfTxs", f.Groups.SelectMany(x => x.Payments).Count().ToString()),
                new XElement("InitgPty",
                    req("Nm", f.SenderCompanyName),
                    new XElement("Id",
                        new XElement("OrgId",
                            new XElement("Othr",
                                req("Id", f.SenderCompanyId), new XElement("SchmeNm", new XElement("Cd", "BANK"))))))
                )));
            root.Add(header);
            var translator = new IBANToBICTranslator();
            foreach (var g in f.Groups)
            {
                context = $"File {f.PaymentFileId}, Group {g.PaymentGroupId}";

                var pmt = new XElement("PmtInf");
                pmt.Add(req("PmtInfId", g.PaymentGroupId));
                pmt.Add(new XElement("PmtMtd", "TRF"));
                pmt.Add(req("NbOfTxs", g.Payments.Count().ToString()));
                pmt.Add(req("ReqdExctnDt", f.ExecutionDate.ToString("yyyy-MM-dd")));
                pmt.Add(new XElement("Dbtr", req("Nm", f.SenderCompanyName)));
                pmt.Add(new XElement("DbtrAcct",
                    new XElement("Id", req("IBAN", g.FromIban.NormalizedValue)),
                    req("Ccy", f.CurrencyCode),
                    req("Nm", f.SendingBankName)));
                pmt.Add(new XElement("DbtrAgt",
                    new XElement("FinInstnId",
                        req("BIC", f.SendingBankBic))));
                foreach (var p in g.Payments)
                {
                    context = $"File {f.PaymentFileId}, Group {g.PaymentGroupId}, Payment {p.PaymentId}";
                    pmt.Add(new XElement("CdtTrfTxInf",
                        new XElement("PmtId",
                            req("InstrId", p.PaymentId),
                            req("EndToEndId", p.PaymentId)),
                        new XElement("PmtTpInf",
                            new XElement("SvcLvl", new XElement("Cd", "SEPA")),
                            new XElement("CtgyPurp", new XElement("Cd", "SUPP"))),
                        new XElement("Amt",
                            new XElement("InstdAmt",
                                new XAttribute("Ccy", f.CurrencyCode),
                                p.Amount.ToString(CultureInfo.InvariantCulture))),
                        new XElement("ChrgBr", "SHAR"),
                        new XElement("CdtrAgt", new XElement("FinInstnId", new XElement("BIC", translator.InferBic(p.ToIban)))),
                        new XElement("Cdtr", req("Nm", p.CustomerName)),
                        new XElement("CdtrAcct", new XElement("Id", req("IBAN", p.ToIban.NormalizedValue))),
                        new XElement("RmtInf", req("Ustrd", ClipRight(p.Message, 140) ?? "Payment"))));
                }
                header.Add(pmt);
            }

            //NOTE: The parse and replace cycle is to make this <CstmrCdtTrfInitn xmlns=""> into this <CstmrCdtTrfInitn>. It likely because we are parsing the header from a string and not reusing the namespaces properly but this works fine.
            return XDocuments.Parse(XDocumentToString(d).Replace("xmlns=\"\"", ""));
        }

        private string ClipRight(string s, int maxLength)
        {
            if (s == null)
                return null;
            else if (s.Length <= maxLength)
                return s;
            else
                return s.Substring(0, maxLength);
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