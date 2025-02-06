using nCredit;
using NTech;
using System;
using System.Collections.Generic;

namespace TestsnPreCredit.Credit
{

    public class MockMessage : IEInvoiceFiMessageHeader
    {
        private Dictionary<EInvoiceFiItemCode, string> items = new Dictionary<EInvoiceFiItemCode, string>();

        private MockMessage()
        {

        }

        public static MockMessage Create(string externalMessageType, string externalMessageId, IClock clock, TimeSpan age, params Tuple<EInvoiceFiItemCode, string>[] items)
        {
            var m = new MockMessage();
            m.ExternalMessageId = externalMessageId;
            m.ExternalMessageType = externalMessageType;
            m.ImportDate = clock.Now.Subtract(age).DateTime;
            m.items = new Dictionary<EInvoiceFiItemCode, string>();
            foreach (var i in items)
                m.items[i.Item1] = i.Item2;

            return m;
        }

        public string ExternalMessageType { get; private set; }

        public string ExternalMessageId { get; private set; }

        public DateTime ImportDate { get; private set; }

        public string GetItemValue(EInvoiceFiItemCode itemCode)
        {
            return items.Opt(itemCode);
        }
    }
}
