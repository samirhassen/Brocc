using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace NTech.Banking.BookKeeping
{
    public class NtechBookKeepingRuleFile
    {
        public static NtechBookKeepingRuleFile Parse(XDocument d)
        {
            Func<XElement, string, XElement> singleD = (x,y) => XDocuments.GetSingleDescendant(x, y, true);
            var f = new NtechBookKeepingRuleFile();
            f.BusinessEventRules = new List<BusinessEventRule>();
            f.CompanyName = singleD(d.Root, "CompanyName").Value;

            var commonCostPlaceElement = XDocuments.GetSingleDescendant(d.Root, "CommonCostPlace", false);
            var customDimensionsElement = XDocuments.GetSingleDescendant(d.Root, "CustomDimensions", false);
            if (customDimensionsElement != null && commonCostPlaceElement != null)
                throw new Exception("CommonCostPlace and CustomDimensions cannot be combined");

            if (commonCostPlaceElement != null)
            {
                f.CommonCostPlace = Tuple.Create(
                    singleD(commonCostPlaceElement, "ObjectNr").Value,
                    singleD(commonCostPlaceElement, "ObjectName").Value);
            }
            else if (customDimensionsElement != null)
            {
                var c = new CustomDimensionSet();
                c.CustomDimensionDeclaration = singleD(customDimensionsElement, "Declarations").Value;
                c.CustomDimensionTextByCaseName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tdElement in XDocuments.GetDescendants(customDimensionsElement, "TransactionDimension"))
                {
                    c.CustomDimensionTextByCaseName[tdElement.Attribute("case").Value] = tdElement.Value;
                }
                f.CustomDimensions = c;
            }

            var bis = XDocuments.GetDescendants(d.Root, "BusinessEvent");
            foreach (var biElement in bis)
            {
                var names = singleD(biElement, "BusinessEventName").Value.Split('|').Select(x => x.Trim());
                var ruleElements = XDocuments.GetDescendants(biElement, "Booking");
                foreach (var name in names)
                {
                    var bi = new BusinessEventRule
                    {
                        BusinessEventName = name,
                        Bookings = new List<BookingRule>()
                    };
                    foreach (var ruleElement in ruleElements)
                    {
                        var accountNrs = singleD(ruleElement, "Accounts").Value.Split(',').Select(x => x.Trim()).ToList();
                        if (accountNrs.Count != 2)
                            throw new Exception("Each Accounts-element must have the format <debetaccount>,<creditaccount>");
                        
                        bi.Bookings.Add(new BookingRule
                        {
                            LedgerAccount = singleD(ruleElement, "LedgerAccount").Value,
                            OnlySubAccountCode = XDocuments.GetSingleDescendant(ruleElement, "OnlySubAccountCode", false)?.Value,
                            BookKeepingAccounts = Tuple.Create(
                                ParseAccountNrOrAccountNameMacro(accountNrs[0]), 
                                ParseAccountNrOrAccountNameMacro(accountNrs[1])),
                            Connections = new HashSet<string>(singleD(ruleElement, "Connections").Value.Split(',').Select(x => x.Trim()).ToList())
                        });
                    }
                    f.BusinessEventRules.Add(bi);
                }
            }
            return f;
        }

        /// <summary>
        /// Either an integer like 1603 or something like __A_SomeName__
        /// SomeName cannot contain: _ < > [ ] &   (anything that could cause issues with xml and _ to prevent parsing issues)
        /// </summary>
        private static ThreadLocal<Regex> AccountNrOrAccountNameMacroExpression = new ThreadLocal<Regex>(() => new Regex(
                $"^(?: (?<accountNr>{NtechAccountPlanFile.AccountNrExpressionRaw})   |   (__A_(?<accountName>{NtechAccountPlanFile.AccountNameExpressionRaw})__)  )$", RegexOptions.IgnorePatternWhitespace));
        public static bool TryParseAccountNrOrAccountNameMacro(string nrOrMacro, out BookKeepingAccount account)
        {
            if(string.IsNullOrWhiteSpace(nrOrMacro))
            {
                account = null;
                return false;
            }

            var match = AccountNrOrAccountNameMacroExpression.Value.Match(nrOrMacro);
            if(!match.Success)
            {
                account = null;
                return false;
            }
            
            var nrGroup = match.Groups["accountNr"];
            if(nrGroup.Success)
                account = new BookKeepingAccount(nrGroup.Value, false);
            else
            {
                var name = match.Groups["accountName"].Value;
                account = new BookKeepingAccount(name, true);
            }                

            return true;
        }        

        private static BookKeepingAccount ParseAccountNrOrAccountNameMacro(string nrOrMacro)
        {
            if(!TryParseAccountNrOrAccountNameMacro(nrOrMacro, out var bookKeepingAccount))
                throw new Exception("Invalid account or macro: " + nrOrMacro);
            return bookKeepingAccount;
        }

        public class BusinessEventRule
        {
            public string BusinessEventName { get; set; }

            public List<BookingRule> Bookings { get; set; }
        }

        public class BookingRule
        {
            public string LedgerAccount { get; set; }
            public string OnlySubAccountCode { get; set; }
            public HashSet<string> Connections { get; set; }

            public Tuple<BookKeepingAccount, BookKeepingAccount> BookKeepingAccounts { get; set; }
        }

        public class BookKeepingAccount
        {
            private readonly string accountNrOrAccountName;
            private readonly bool isLedgerAccountName;

            public BookKeepingAccount(string accountNrOrAccountName, bool isLedgerAccountName)
            {
                this.accountNrOrAccountName = accountNrOrAccountName;
                this.isLedgerAccountName = isLedgerAccountName;
            }

            public string GetLedgerAccountNr(Func<string, string> translateAccountName)
            {
                return isLedgerAccountName ? translateAccountName(accountNrOrAccountName) : accountNrOrAccountName;
            }
        }

        public class CustomDimensionSet
        {
            public Dictionary<string, string> CustomDimensionTextByCaseName { get; set; }
            public string CustomDimensionDeclaration { get; set; }
        }

        public CustomDimensionSet CustomDimensions { get; set; }
        public string CompanyName { get; set; }
        public Tuple<string, string> CommonCostPlace { get; set; }
        public IList<BusinessEventRule> BusinessEventRules { get; set; }
    }
}