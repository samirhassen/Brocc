using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace nCredit.Code
{
    public class Pain_001_001_3_XmlGenerator
    {
        public string Context { get; set; }

        private XElement Req(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"{Context}: Missing required value {name}");
            }
            return new XElement(name, value);
        }

        public XElement CreateDbtrAcct(IBankAccountNumber nr, string currencyCode)
        {
            //Clients source account
            switch (nr.AccountType)
            {
                case BankAccountNumberTypeCode.BankAccountSe:
                    {
                        var a = nr as BankAccountNumberSe;
                        return new XElement("DbtrAcct",
                                            new XElement("Id",
                                                new XElement("Othr",
                                                    new XElement("Id", a.AccountNr),
                                                    new XElement("SchmeNm", new XElement("Cd", "BBAN")))),
                                            Req("Ccy", currencyCode));
                    }
                default:
                    throw new Exception($"{Context}: Unsupported account type {nr.AccountType}");
            }
        }

        public XElement CreateCdtrAgt(IBankAccountNumber nr)
        {
            if (nr.AccountType != BankAccountNumberTypeCode.BankAccountSe)
                throw new NotImplementedException();
            BankAccountNumberSe b = (BankAccountNumberSe)nr;
            return new XElement("CdtrAgt", new XElement("FinInstnId", new XElement("ClrSysMmbId", new XElement("MmbId", b.ClearingNr))));
        }

        public XElement CreateCdtrAcct(IBankAccountNumber nr, string currencyCode)
        {
            //Customers target account
            switch (nr.AccountType)
            {
                case BankAccountNumberTypeCode.BankAccountSe:
                    return new XElement("CdtrAcct",
                            new XElement("Id",
                                new XElement("Othr",
                                    Req("Id", nr.FormatFor("pain.001.001.3")),
                                    new XElement("SchmeNm",
                                        new XElement("Cd", "BBAN")))));

                case BankAccountNumberTypeCode.BankGiroSe:
                    return new XElement("DbtrAcct",
                        new XElement("Id", new XElement("Othr",
                            new XElement("Id", nr.FormatFor("pain.001.001.3")),
                            new XElement("SchmeNm",
                                new XElement("Prtry", "BGNR")))));

                default:
                    throw new Exception($"{Context}: Unsupported account type {nr.AccountType}");
            }
        }
    }

    public class OutgoingPaymentFileFormat_Pain_001_001_3_SE
    {
        public OutgoingPaymentFileFormat_Pain_001_001_3_SE(bool isProduction)
        {
            this.isProduction = isProduction;
        }

        public OutgoingPaymentFileFormat_Pain_001_001_3_SE(ICreditEnvSettings envSettings) : this(envSettings.IsProduction)
        {
        }

        protected bool isProduction;

        private static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

        //ISO Standard: ISO 20022

        public class PaymentFile
        {
            public string CurrencyCode { get; set; }
            public string PaymentFileId { get; set; }
            public string SenderCompanyName { get; set; }
            public string SenderCompanyId { get; set; }
            public string SendingBankBic { get; set; }
            public string SendingBankName { get; set; }
            public DateTime ExecutionDate { get; set; }
            public List<PaymentGroup> Groups { get; set; }

            public class PaymentGroup
            {
                public string PaymentGroupId { get; set; }
                public IBankAccountNumber FromAccount { get; set; }
                public List<Payment> Payments { get; set; }
            }
        }

        public class Payment
        {
            public IBankAccountNumber ToAccount { get; set; }
            public string Message { get; set; }
            public decimal Amount { get; set; }
            public string PaymentId { get; set; }
            public string CustomerName { get; set; }
        }

        private const string BasePattern = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Document xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.001.001.03\"></Document>";

        protected T[] SkipNulls<T>(params T[] args) where T : class
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

        public XDocument CreateFile(PaymentFile f, DateTime now, OrganisationNumberSe clientOrgnr, string clientBankClearingNr)
        {
            var d = XDocuments.Parse(BasePattern);

            var xmlGenerator = new Pain_001_001_3_XmlGenerator();
            Action<string> setContext = x => { xmlGenerator.Context = x; };
            setContext($"File {f.PaymentFileId}");

            Func<string, string, XElement> req = (name, value) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception($"{xmlGenerator.Context}: Missing required value {name}");
                }
                return new XElement(name, value);
            };

            var root = d.Descendants().Single();

            var header = new XElement("CstmrCdtTrfInitn");

            header.Add(new XElement("GrpHdr", SkipNulls(
                req("MsgId", f.PaymentFileId),
                req("CreDtTm", now.ToString("yyyy-MM-ddTHH:mm:ss")),
                isProduction ? null : new XElement("Authstn", new XElement("Prtry", "TEST")),
                req("NbOfTxs", f.Groups.SelectMany(x => x.Payments).Count().ToString()),
                new XElement("InitgPty", req("Nm", f.SenderCompanyName))
                )));
            root.Add(header);

            foreach (var g in f.Groups)
            {
                setContext($"File {f.PaymentFileId}, Group {g.PaymentGroupId}");

                var pmt = new XElement("PmtInf");
                pmt.Add(req("PmtInfId", g.PaymentGroupId));
                pmt.Add(new XElement("PmtMtd", "TRF"));
                pmt.Add(new XElement("PmtTpInf",
                        new XElement("InstrPrty", "NORM"),
                        new XElement("SvcLvl", new XElement("Cd", "NURG")),
                        new XElement("CtgyPurp", new XElement("Cd", "SUPP"))));

                pmt.Add(req("ReqdExctnDt", f.ExecutionDate.ToString("yyyy-MM-dd")));
                pmt.Add(new XElement("Dbtr",
                    req("Nm", f.SenderCompanyName),
                    new XElement("PstlAdr", new XElement("Ctry", "SE")),
                    new XElement("Id",
                        new XElement("OrgId",
                            new XElement("Othr",
                                new XElement("Id", clientOrgnr.NormalizedValue),
                                new XElement("SchmeNm",
                                    new XElement("Cd", "BANK")))))
                    ));

                pmt.Add(xmlGenerator.CreateDbtrAcct(g.FromAccount, f.CurrencyCode));
                pmt.Add(new XElement("DbtrAgt",
                    new XElement("FinInstnId",
                        new XElement("ClrSysMmbId",
                            new XElement("MmbId", clientBankClearingNr)),
                        new XElement("PstlAdr",
                            new XElement("Ctry", "SE")
                        ))));

                foreach (var p in g.Payments)
                {
                    setContext($"File {f.PaymentFileId}, Group {g.PaymentGroupId}, Payment {p.PaymentId}");

                    pmt.Add(new XElement("CdtTrfTxInf",
                        new XElement("PmtId",
                            req("InstrId", p.PaymentId),
                            req("EndToEndId", p.PaymentId)),
                        new XElement("Amt",
                            new XElement("InstdAmt",
                                new XAttribute("Ccy", f.CurrencyCode),
                                p.Amount.ToString(CultureInfo.InvariantCulture))),
                        xmlGenerator.CreateCdtrAgt(p.ToAccount),
                        new XElement("Cdtr",
                            req("Nm", p.CustomerName),
                            new XElement("PstlAdr", new XElement("Ctry", "SE"))),
                        xmlGenerator.CreateCdtrAcct(p.ToAccount, f.CurrencyCode),
                        new XElement("RmtInf", req("Ustrd", p.Message ?? "Payment"))));
                }
                header.Add(pmt);
            }

            //NOTE: The parse and replace cycle is to make this <CstmrCdtTrfInitn xmlns=""> into this <CstmrCdtTrfInitn>. It likely because we are parsing the header from a string and not reusing the namespaces properly but this works fine.
            return XDocuments.Parse(XDocumentToString(d).Replace("xmlns=\"\"", ""));
        }

        public byte[] CreateFileAsBytes(PaymentFile f, DateTime now, OrganisationNumberSe clientOrgnr, string clientBankClearingNr)
        {
            return Encoding.UTF8.GetBytes(XDocumentToString(CreateFile(f, now, clientOrgnr, clientBankClearingNr)));
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