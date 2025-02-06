using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace nTest.RandomDataSource
{
    public class TestPaymentFileCreator
    {
        private const string Camt05400102_Pattern_Resource = @"PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjxEb2N1bWVudCB4bWxucz0idXJuOmlzbzpzdGQ6aXNvOjIwMDIyOnRlY2g6eHNkOmNhbXQuMDU0LjAwMS4wMiIgeG1sbnM6eHNpPSJodHRwOi8vd3d3LnczLm9yZy8yMDAxL1hNTFNjaGVtYS1pbnN0YW5jZSIgeHNpOnNjaGVtYUxvY2F0aW9uPSJ1cm46aXNvOnN0ZDppc286MjAwMjI6dGVjaDp4c2Q6Y2FtdC4wNTQuMDAxLjAyIGNhbXQuMDU0LjAwMS4wMi54c2QiPg0KCTxCa1RvQ3N0bXJEYnRDZHROdGZjdG4+DQoJCTxHcnBIZHI+DQoJCQk8TXNnSWQ+e3tNZXNzYWdlSWR9fTwvTXNnSWQ+DQoJCQk8Q3JlRHRUbT4yMDE2LTExLTI4VDIzOjI0OjQzPC9DcmVEdFRtPg0KCQk8L0dycEhkcj4NCgkJPE50ZmN0bj4NCgkJCTxJZD5oZWo2PC9JZD4NCgkJCTxFbGN0cm5jU2VxTmI+MTQ2PC9FbGN0cm5jU2VxTmI+DQoJCQk8Q3JlRHRUbT4yMDE2LTExLTI4VDIzOjI0OjQzPC9DcmVEdFRtPg0KCQkJPEZyVG9EdD4NCgkJCQk8RnJEdFRtPjIwMTYtMTEtMjhUMDA6MDA6MDA8L0ZyRHRUbT4NCgkJCQk8VG9EdFRtPjIwMTYtMTEtMjhUMjM6NTk6NTk8L1RvRHRUbT4NCgkJCTwvRnJUb0R0Pg0KCQkJPEFjY3Q+DQoJCQkJPElkPg0KCQkJCQk8SUJBTj5GSTYwODQwMDA3MTAzNTk3Nzg8L0lCQU4+DQoJCQkJPC9JZD4NCgkJCQk8Q2N5PkVVUjwvQ2N5Pg0KCQkJCTxObT5EYW5za2UgQnVzaW5lc3M8L05tPg0KCQkJCTxPd25yPg0KCQkJCQk8Tm0+CUJBTEFOWklBIE9ZPC9ObT4NCgkJCQkJPFBzdGxBZHI+DQoJCQkJCQk8U3RydE5tPk1ZTlRHQVRBTjwvU3RydE5tPg0KCQkJCQkJPEJsZGdOYj4xMjwvQmxkZ05iPg0KCQkJCQkJPFBzdENkPjEyMzQ1PC9Qc3RDZD4NCgkJCQkJCTxUd25ObT5TVEFEPC9Ud25ObT4NCgkJCQkJCTxDdHJ5PkZJPC9DdHJ5Pg0KCQkJCQk8L1BzdGxBZHI+DQoJCQkJCTxJZD4NCgkJCQkJCTxPcmdJZD4NCgkJCQkJCQk8T3Rocj4NCgkJCQkJCQkJPElkPjEyMzQ1Njc4PC9JZD4NCgkJCQkJCQkJPFNjaG1lTm0+DQoJCQkJCQkJCQk8Q2Q+Q1VTVDwvQ2Q+DQoJCQkJCQkJCTwvU2NobWVObT4NCgkJCQkJCQk8L090aHI+DQoJCQkJCQk8L09yZ0lkPg0KCQkJCQk8L0lkPg0KCQkJCTwvT3ducj4NCgkJCQk8U3Zjcj4NCgkJCQkJPEZpbkluc3RuSWQ+DQoJCQkJCQk8QklDPkRBQkFGSUhIPC9CSUM+DQoJCQkJCTwvRmluSW5zdG5JZD4NCgkJCQk8L1N2Y3I+DQoJCQk8L0FjY3Q+DQoJCQk8VHhzU3VtbXJ5Pg0KCQkJCTxUdGxOdHJpZXM+DQoJCQkJCTxOYk9mTnRyaWVzPjE8L05iT2ZOdHJpZXM+DQoJCQkJCTxUdGxOZXROdHJ5QW10Pnt7QW1vdW50fX08L1R0bE5ldE50cnlBbXQ+DQoJCQkJCTxDZHREYnRJbmQ+Q1JEVDwvQ2R0RGJ0SW5kPg0KCQkJCTwvVHRsTnRyaWVzPg0KCQkJCTxUdGxDZHROdHJpZXM+DQoJCQkJCTxOYk9mTnRyaWVzPjE8L05iT2ZOdHJpZXM+DQoJCQkJCTxTdW0+e3tBbW91bnR9fTwvU3VtPg0KCQkJCTwvVHRsQ2R0TnRyaWVzPg0KCQkJCTxUdGxEYnROdHJpZXM+DQoJCQkJCTxOYk9mTnRyaWVzPjA8L05iT2ZOdHJpZXM+DQoJCQkJCTxTdW0+MC4wMDwvU3VtPg0KCQkJCTwvVHRsRGJ0TnRyaWVzPg0KCQkJPC9UeHNTdW1tcnk+DQoJCQk8TnRyeT4NCgkJCQk8TnRyeVJlZj4xPC9OdHJ5UmVmPg0KCQkJCTxBbXQgQ2N5PSJFVVIiPnt7QW1vdW50fX08L0FtdD4NCgkJCQk8Q2R0RGJ0SW5kPkNSRFQ8L0NkdERidEluZD4NCgkJCQk8U3RzPkJPT0s8L1N0cz4NCgkJCQk8Qm9va2dEdD4NCgkJCQkJPER0PjIwMTYtMTEtMjg8L0R0Pg0KCQkJCTwvQm9va2dEdD4NCgkJCQk8VmFsRHQ+DQoJCQkJCTxEdD4yMDE2LTExLTI4PC9EdD4NCgkJCQk8L1ZhbER0Pg0KCQkJCTxBY2N0U3ZjclJlZj5SRUZQMTYwMDE4ODwvQWNjdFN2Y3JSZWY+DQoJCQkJPEJrVHhDZD4NCgkJCQkJPERvbW4+DQoJCQkJCQk8Q2Q+UE1OVDwvQ2Q+DQoJCQkJCQk8Rm1seT4NCgkJCQkJCQk8Q2Q+UkNEVDwvQ2Q+DQoJCQkJCQkJPFN1YkZtbHlDZD5FU0NUPC9TdWJGbWx5Q2Q+DQoJCQkJCQk8L0ZtbHk+DQoJCQkJCTwvRG9tbj4NCgkJCQkJPFBydHJ5Pg0KCQkJCQkJPENkPjcwNSBWSUlURVNJSVJST1Q8L0NkPg0KCQkJCQkJPElzc3I+RkZGUzwvSXNzcj4NCgkJCQkJPC9QcnRyeT4NCgkJCQk8L0JrVHhDZD4NCgkJCQk8TnRyeUR0bHM+DQoJCQkJCTxCdGNoPg0KCQkJCQkJPE5iT2ZUeHM+MTwvTmJPZlR4cz4NCgkJCQkJPC9CdGNoPg0KCQkJCQk8VHhEdGxzPg0KCQkJCQkJPFJlZnM+DQoJCQkJCQkJPEFjY3RTdmNyUmVmPjE2MDkyNjU5MzQ5N1VPMzEyMzwvQWNjdFN2Y3JSZWY+DQoJCQkJCQkJPEVuZFRvRW5kSWQ+Tk9UUFJPVklERUQ8L0VuZFRvRW5kSWQ+DQoJCQkJCQkJPFR4SWQ+MjAxNjA5MjZNUEFPS0ktMjAxNjA5MjY1OTM0OTdVTzMxMjM8L1R4SWQ+DQoJCQkJCQk8L1JlZnM+DQoJCQkJCQk8QW10RHRscz4NCgkJCQkJCQk8SW5zdGRBbXQ+DQoJCQkJCQkJCTxBbXQgQ2N5PSJFVVIiPnt7QW1vdW50fX08L0FtdD4NCgkJCQkJCQk8L0luc3RkQW10Pg0KCQkJCQkJCTxUeEFtdD4NCgkJCQkJCQkJPEFtdCBDY3k9IkVVUiI+e3tBbW91bnR9fTwvQW10Pg0KCQkJCQkJCTwvVHhBbXQ+DQoJCQkJCQk8L0FtdER0bHM+DQoJCQkJCQk8QmtUeENkPg0KCQkJCQkJCTxEb21uPg0KCQkJCQkJCQk8Q2Q+UE1OVDwvQ2Q+DQoJCQkJCQkJCTxGbWx5Pg0KCQkJCQkJCQkJPENkPlJDRFQ8L0NkPg0KCQkJCQkJCQkJPFN1YkZtbHlDZD5FU0NUPC9TdWJGbWx5Q2Q+DQoJCQkJCQkJCTwvRm1seT4NCgkJCQkJCQk8L0RvbW4+DQoJCQkJCQkJPFBydHJ5Pg0KCQkJCQkJCQk8Q2Q+NzEwIFBBTktLSVNJSVJUTzwvQ2Q+DQoJCQkJCQkJCTxJc3NyPkZGRlM8L0lzc3I+DQoJCQkJCQkJPC9QcnRyeT4NCgkJCQkJCTwvQmtUeENkPg0KCQkJCQkJPFJsdGRQdGllcz4NCgkJCQkJCQk8RGJ0cj4NCgkJCQkJCQkJPE5tPkpVU1RJTiBCSUVCRVI8L05tPg0KCQkJCQkJCQk8UHN0bEFkcj4NCgkJCQkJCQkJCTxDdHJ5PkZJPC9DdHJ5Pg0KCQkJCQkJCQk8L1BzdGxBZHI+DQoJCQkJCQkJPC9EYnRyPg0KCQkJCQkJCTxDZHRyPg0KCQkJCQkJCQk8Tm0+QkFMQU5aSUEgT1k8L05tPg0KCQkJCQkJCTwvQ2R0cj4NCgkJCQkJCTwvUmx0ZFB0aWVzPg0KCQkJCQkJPFJtdEluZj4NCgkJCQkJCQk8U3RyZD4NCgkJCQkJCQkJPENkdHJSZWZJbmY+DQoJCQkJCQkJCQk8VHA+DQoJCQkJCQkJCQkJPENkT3JQcnRyeT4NCgkJCQkJCQkJCQkJPENkPlNDT1I8L0NkPg0KCQkJCQkJCQkJCTwvQ2RPclBydHJ5Pg0KCQkJCQkJCQkJPC9UcD4NCgkJCQkJCQkJCTxSZWY+e3tPY3JSZWZlcmVuY2V9fTwvUmVmPg0KCQkJCQkJCQk8L0NkdHJSZWZJbmY+DQoJCQkJCQkJPC9TdHJkPg0KCQkJCQkJPC9SbXRJbmY+DQoJCQkJCQk8Umx0ZER0cz4NCgkJCQkJCQk8QWNjcHRuY0R0VG0+MjAxNi0xMS0yNlQwMDowMDowMDwvQWNjcHRuY0R0VG0+DQoJCQkJCQk8L1JsdGREdHM+DQoJCQkJCTwvVHhEdGxzPg0KCQkJCQkNCgkJCQkJDQoJCQkJPC9OdHJ5RHRscz4NCgkJCTwvTnRyeT4NCgkJPC9OdGZjdG4+DQoJPC9Ca1RvQ3N0bXJEYnRDZHROdGZjdG4+DQo8L0RvY3VtZW50Pg==";
        private Lazy<string> Camt05400102_Pattern = new Lazy<string>(() => Encoding.UTF8.GetString(Convert.FromBase64String(Camt05400102_Pattern_Resource)));

        private static DateTime DefaultBookkeepingDate = new DateTime(2016, 11, 28);
        private static string DefaultClientIban = "FI6084000710359778";

        public XDocument CreateSinglePayment_Camt_054_001_02File(decimal amount, string ocrReference)
        {
            var f = Camt05400102_Pattern.Value.Replace("{{MessageId}}", Guid.NewGuid().ToString());
            f = f.Replace("{{Amount}}", amount.ToString("F2", CultureInfo.InvariantCulture));
            f = f.Replace("{{OcrReference}}", ocrReference);
            return XDocuments.Parse(f);
        }

        public XDocument Create_Camt_054_001_02File(List<Tuple<decimal, string>> amountsAndOcrs,
            string payerNameDefaultValue = "Payer Name",
            DateTime? bookkeepingDate = null,
            string clientIban = null)
        {
            var payments = amountsAndOcrs.Select(x => new Payment
            {
                Amount = x.Item1,
                OcrReference = x.Item2,
                BookkeepingDate = bookkeepingDate ?? DateTime.Today,
                PayerName = payerNameDefaultValue
            }).ToList();
            return Create_Camt_054_001_02File(payments, clientIban: clientIban);
        }

        public class Payment
        {
            public decimal Amount { get; set; }
            public string OcrReference { get; set; }
            public string PayerName { get; set; }
            public DateTime BookkeepingDate { get; set; }
        }

        public XDocument Create_Camt_054_001_02File(
            List<Payment> payments,
            string clientIban = null)
        {
            var dates = payments
                .GroupBy(x => x.BookkeepingDate)
                .ToList();

            var count = dates.Count;
            var sum = payments.Sum(x => x.Amount);

            var pattern = EmbeddedResources.LoadFileAsString("FinnishCamt54TestFile.xml");

            var f = pattern.Replace("{{MessageId}}", Guid.NewGuid().ToString());
            f = f.Replace("{{FileId}}", Guid.NewGuid().ToString());
            f = f.Replace("{{TotalAmount}}", sum.ToString("F2", CultureInfo.InvariantCulture));
            f = f.Replace("{{TotalCount}}", count.ToString(CultureInfo.InvariantCulture));
            f = f.Replace("{{ClientIban}}", (clientIban ?? DefaultClientIban));

            var doc = XDocuments.Parse(f);

            Action<XElement, string, string> setValue = (e, p, value) =>
            {
                foreach (var ee in e.Descendants().Where(x => !x.Elements().Any() && x.Value == p).ToList())
                {
                    ee.Value = value;
                }
            };

            Action<XElement, decimal, string, string> setPaymentInfo = (e, amount, ocr, payerName) =>
            {
                setValue(e, "{{Amount}}", amount.ToString("F2", CultureInfo.InvariantCulture));
                setValue(e, "{{OcrReference}}", ocr);
                setValue(e, "{{PayerName}}", payerName);
            };

            Action<XElement, DateTime, List<Payment>> setEntryInfo = (e, bookKeepingDate, pms) =>
            {
                var paymentNode = e.Descendants().Where(x => x.Name.LocalName == "TxDtls").Single();

                setValue(e, "{{NtryTotalAmount}}", pms.Sum(x => x.Amount).ToString("F2", CultureInfo.InvariantCulture));
                setValue(e, "{{NtryBookkeepingDate}}", bookKeepingDate.ToString("yyyy-MM-dd"));
                setValue(e, "{{NtryTotalCount}}", pms.Count().ToString());

                //For all but one payment copy from the paymentNode
                foreach (var payment in pms.Skip(1))
                {
                    var newPayment = new XElement(paymentNode);
                    setPaymentInfo(newPayment, payment.Amount, payment.OcrReference, payment.PayerName);
                    paymentNode.AddAfterSelf(newPayment);
                }

                //For the first payment, use the template node
                setPaymentInfo(paymentNode, pms[0].Amount, pms[0].OcrReference, pms[0].PayerName);
            };

            var entryNode = doc.Descendants().Where(x => x.Name.LocalName == "Ntry").Single();
            foreach (var date in dates.Skip(1))
            {
                var newEntry = new XElement(entryNode);
                setEntryInfo(newEntry, date.Key, date.Select(x => x).ToList());
                entryNode.AddAfterSelf(newEntry);
            }

            setEntryInfo(entryNode, dates[0].Key, dates[0].Select(x => x).ToList());

            return doc;
        }

        public byte[] Create_BgMax_File(
            DateTime now,
            List<Payment> payments,
            string clientBankGiroNr = null)
        {
            const string FileHeaderLinePattern = "01BGMAX               0120160714173035010331T                                   "; //!this is a different length in the actual sample file from bankgirot
            const string SectionHeaderPattern = "050009912346          SEK                                                       ";
            const string PaymentPattern = "20000000000008221231                 00000000000002000024                       ";
            const string SectionFooterPattern = "15000000000000000000058410000010098232016071400036000000000000070000SEK00000004 ";
            const string FileFooterLinePattern = "7000000004000000000000000000000001                                              ";

            //from and to are one based to match the bg max file spec
            Func<string, int, int, string, string> withValue = (pattern, fromPos, toPos, value) =>
            {
                if (value.Length != (toPos - fromPos + 1))
                    throw new Exception($"Invalid replacement for {fromPos} -> {toPos}: '{value}'");

                var p = new List<char>(pattern);
                for (var pos = fromPos; pos <= toPos; pos++)
                {
                    p[pos - 1] = value[pos - fromPos];
                }
                return new string(p.ToArray());
            };
            var lines = new List<string>();

            var header = withValue(FileHeaderLinePattern, 25, 44, now.ToString("yyyyMMddHHmmss") + "000000");
            lines.Add(header);

            foreach (var section in payments.GroupBy(x => x.BookkeepingDate))
            {
                lines.Add(withValue(SectionHeaderPattern, 3, 12, clientBankGiroNr?.PadLeft(10, '0')));
                foreach (var payment in section)
                {
                    var p = PaymentPattern;
                    p = withValue(p, 13, 37, payment.OcrReference.PadRight(25, ' '));
                    p = withValue(p, 38, 55, payment.Amount.ToString("f2", CultureInfo.InvariantCulture).Replace(".", "").PadLeft(18, '0'));
                    lines.Add(p);
                }
                var f = SectionFooterPattern;
                f = withValue(f, 38, 45, section.Key.ToString("yyyyMMdd"));
                f = withValue(f, 51, 68, section.Sum(x => x.Amount).ToString("f2", CultureInfo.InvariantCulture).Replace(".", "").PadLeft(18, '0'));
                f = withValue(f, 72, 79, section.Count().ToString().PadLeft(8, '0'));
                lines.Add(f);
            }

            lines.Add(withValue(FileFooterLinePattern, 3, 10, payments.Count.ToString().PadLeft(8, '0')));

            return Encoding.GetEncoding("iso-8859-1").GetBytes(string.Join(Environment.NewLine, lines));
        }

        public string ToDataUrl(XDocument d)
        {
            return $"data:application/xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(d.ToString()))}";
        }

        public string ToDataUrl(byte[] d)
        {
            return $"data:application/xml;base64,{Convert.ToBase64String(d)}";
        }
    }
}