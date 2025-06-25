using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace nSavings.Code.Fileformats;

public class OutgoingPaymentFileFormat_Pain_001_001_3
{
    private static readonly ThreadLocal<Random> Random = new(() => new Random());

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

    private const string BasePattern =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Document xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.001.001.03\"></Document>";

    private static T[] SkipNulls<T>(params T[] args) where T : class
    {
        return args?.Where(x => x != null).ToArray();
    }

    private static string GenerateId(string prefix)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[10];

        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[Random.Value.Next(chars.Length)];
        }

        return prefix + new string(stringChars);
    }

    public static void PopulateIds(PaymentFile f)
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
        var context = $"File {f.PaymentFileId}";

        var root = d.Descendants().Single();

        var header = new XElement("CstmrCdtTrfInitn");

        header.Add(new XElement("GrpHdr", SkipNulls(
            Req("MsgId", f.PaymentFileId),
            Req("CreDtTm", now.ToString("o")),
            NEnv.IsProduction ? null : new XElement("Authstn", new XElement("Prtry", "TEST")),
            Req("NbOfTxs", f.Groups.SelectMany(x => x.Payments).Count().ToString()),
            new XElement("InitgPty",
                Req("Nm", f.SenderCompanyName),
                new XElement("Id",
                    new XElement("OrgId",
                        new XElement("Othr",
                            Req("Id", f.SenderCompanyId), new XElement("SchmeNm", new XElement("Cd", "BANK"))))))
        )));
        root.Add(header);
        var translator = new IBANToBICTranslator();
        foreach (var g in f.Groups)
        {
            context = $"File {f.PaymentFileId}, Group {g.PaymentGroupId}";

            var pmt = new XElement("PmtInf");
            pmt.Add(Req("PmtInfId", g.PaymentGroupId));
            pmt.Add(new XElement("PmtMtd", "TRF"));
            pmt.Add(Req("NbOfTxs", g.Payments.Count().ToString()));
            pmt.Add(Req("ReqdExctnDt", f.ExecutionDate.ToString("yyyy-MM-dd")));
            pmt.Add(new XElement("Dbtr", Req("Nm", f.SenderCompanyName)));
            pmt.Add(new XElement("DbtrAcct",
                new XElement("Id", Req("IBAN", g.FromIban.NormalizedValue)),
                Req("Ccy", f.CurrencyCode),
                Req("Nm", f.SendingBankName)));
            pmt.Add(new XElement("DbtrAgt",
                new XElement("FinInstnId",
                    Req("BIC", f.SendingBankBic))));
            foreach (var p in g.Payments)
            {
                context = $"File {f.PaymentFileId}, Group {g.PaymentGroupId}, Payment {p.PaymentId}";
                pmt.Add(new XElement("CdtTrfTxInf",
                    new XElement("PmtId",
                        Req("InstrId", p.PaymentId),
                        Req("EndToEndId", p.PaymentId)),
                    new XElement("PmtTpInf",
                        new XElement("SvcLvl", new XElement("Cd", "SEPA")),
                        new XElement("CtgyPurp", new XElement("Cd", "SUPP"))),
                    new XElement("Amt",
                        new XElement("InstdAmt",
                            new XAttribute("Ccy", f.CurrencyCode),
                            p.Amount.ToString(CultureInfo.InvariantCulture))),
                    new XElement("ChrgBr", "SHAR"),
                    new XElement("CdtrAgt",
                        new XElement("FinInstnId", new XElement("BIC", translator.InferBic(p.ToIban)))),
                    new XElement("Cdtr", Req("Nm", p.CustomerName)),
                    new XElement("CdtrAcct", new XElement("Id", Req("IBAN", p.ToIban.NormalizedValue))),
                    new XElement("RmtInf", Req("Ustrd", ClipRight(p.Message, 140) ?? "Payment"))));
            }

            header.Add(pmt);
        }

        //NOTE: The parse and replace cycle is to make this <CstmrCdtTrfInitn xmlns=""> into this <CstmrCdtTrfInitn>. It likely because we are parsing the header from a string and not reusing the namespaces properly but this works fine.
        return XDocuments.Parse(XDocumentToString(d).Replace("xmlns=\"\"", ""));

        XElement Req(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"{context}: Missing required value {name}");
            }

            return new XElement(name, value);
        }
    }

    private static string ClipRight(string s, int maxLength)
    {
        if (s == null)
            return null;
        return s.Length <= maxLength ? s : s.Substring(0, maxLength);
    }

    public byte[] CreateFileAsBytes(PaymentFile f, DateTimeOffset now)
    {
        return Encoding.UTF8.GetBytes(XDocumentToString(CreateFile(f, now)));
    }

    private static string XDocumentToString(XDocument d)
    {
        using var w = new Utf8StringWriter();
        d.Save(w);
        w.Flush();
        return w.ToString();
    }

    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}