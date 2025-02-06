using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.Code.EInvoiceFi
{
    public class EInvoiceFiIncomingMessageFileFormat
    {
        public class Message
        {
            public string MessageId { get; set; }
            public string MessageType { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string CustomerName { get; set; }
            public string CustomerAddressStreet { get; set; }
            public string CustomerAddressZipcode { get; set; }
            public string CustomerAddressArea { get; set; }
            public string CustomerLanguageCode { get; set; }
            public string LastInvoicePaidOcr { get; set; }
            public string CustomerIdentification1 { get; set; }
            public string CustomerIdentification2 { get; set; }
            public string EInvoiceAddress { get; set; }
            public string EInvoiceBankCode { get; set; }
        }

        public bool TryParseFile(Stream s, out IList<Message> messages, out string failedMessage)
        {
            XDocument d;
            try
            {
                d = XDocuments.Load(s);
            }
            catch
            {
                failedMessage = "Invalid xml-file";
                messages = null;
                return false;
            }
            return TryParseFile(d, out messages, out failedMessage);
        }

        public bool TryParseFile(XDocument document, out IList<Message> messages, out string failedMessage)
        {
            Func<XElement, string, string> getValue = (e, n) =>
                e.Descendants().SingleOrDefault(y => y.Name.LocalName == n)?.Value;

            if (!(document.Descendants().Any(x => x.Name.LocalName == "EInvoiceMessages") || document.Descendants().Any(x => x.Name.LocalName == "EInvoiceMessage")))
            {
                messages = null;
                failedMessage = "Invalid message file. Structure should be EInvoiceMessages(EInvoiceMessage*)";
                return false;
            }

            string firstInvalidTimestamp = null;
            bool hasInvalidData = false;
            Func<string, DateTimeOffset> parseDateWithFlagError = s =>
            {
                DateTimeOffset d;
                if (!DateTimeOffset.TryParseExact(s, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                {
                    hasInvalidData = true;
                    if (firstInvalidTimestamp == null)
                        firstInvalidTimestamp = s;
                    return default(DateTimeOffset);
                }
                else
                    return d;
            };

            HashSet<string> missingRequired = new HashSet<string>();
            Func<string, string, string> requiredWithFlagError = (value, name) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingRequired.Add(name);
                    hasInvalidData = true;
                }
                return value?.Trim();
            };

            var localMessages = document
                .Descendants()
                .Where(x => x.Name.LocalName == "EInvoiceMessage")
                .Select(x =>
                {
                    return new Message
                    {
                        MessageId = requiredWithFlagError(getValue(x, "MessageId"), "MessageId"),
                        MessageType = requiredWithFlagError(getValue(x, "MessageType"), "MessageType"),
                        Timestamp = parseDateWithFlagError(getValue(x, "Timestamp")),
                        CustomerName = getValue(x, "CustomerName"),
                        CustomerAddressStreet = getValue(x, "CustomerAddressStreet"),
                        CustomerAddressZipcode = getValue(x, "CustomerAddressZipcode"),
                        CustomerAddressArea = getValue(x, "CustomerAddressArea"),
                        CustomerLanguageCode = getValue(x, "CustomerLanguageCode"),
                        LastInvoicePaidOcr = getValue(x, "LastInvoicePaidOcr"),
                        CustomerIdentification1 = getValue(x, "CustomerIdentification1"),
                        CustomerIdentification2 = getValue(x, "CustomerIdentification2"),
                        EInvoiceAddress = getValue(x, "EInvoiceAddress"),
                        EInvoiceBankCode = getValue(x, "EInvoiceBankCode"),
                    };
                })
                .ToList();
            if (hasInvalidData)
            {
                messages = null;
                failedMessage = "Invalid message file";
                if (firstInvalidTimestamp != null)
                    failedMessage += $". There are invalid timestamps, the first of which is '{firstInvalidTimestamp}'. The format should be yyyyMMddHHmmss";
                if (missingRequired.Any())
                    failedMessage += $". Missing required elements: {string.Join(", ", missingRequired)}";

                return false;
            }
            else
            {
                messages = localMessages;
                failedMessage = null;
                return true;
            }
        }
    }
}