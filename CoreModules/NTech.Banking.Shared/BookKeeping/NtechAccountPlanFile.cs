using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace NTech.Banking.BookKeeping
{
    public class NtechAccountPlanFile
    {
        public List<Account> Accounts { get; set; }

        /// <summary>
        /// Example:
        ///  <AccountPlan>
        ///     <Accounts>
        ///        <Account name="CapitalDebt" initialAccountNr="1600"></Account>
        ///        <Account  name="BankGiro" initialAccountNr="2400"></Account>
        ///     </Accounts>
        ///   </AccountPlan>            
        /// </summary>
        public static NtechAccountPlanFile Parse(XDocument d)
        {
            var planElement = XDocuments.GetSingleDescendant(d.Root, "Accounts", true);            
            var plan = new NtechAccountPlanFile { Accounts = new List<Account>() };
            foreach(var accountElement in XDocuments.GetDescendants(planElement, "Account"))
            {
                var name = accountElement.Attribute("name")?.Value?.Trim();
                if(!IsValidAccountName(name))
                    throw new Exception($"Invalid Account.name: {name}");

                var initialAccountNr = accountElement.Attribute("initialAccountNr")?.Value?.Trim();
                if(!IsValidAccountNr(initialAccountNr))
                    throw new Exception($"Invalid Account.initialAccountNr: {initialAccountNr}");

                plan.Accounts.Add(new Account
                {
                    Name = name,
                    InitialAccountNr = initialAccountNr
                });
            }
            return plan;
        }

        public class Account
        {
            public string Name { get; set; }
            public string InitialAccountNr { get; set; }
        }

        public const string AccountNameExpressionRaw = @"[^\d_<\>\[\]\&][^_<\>\[\]\&]*";
        private static ThreadLocal<Regex> AccountNameExpression = new ThreadLocal<Regex>(() => new Regex($"^{AccountNameExpressionRaw}$"));
        public static bool IsValidAccountName(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                return false;
            return AccountNameExpression.Value.IsMatch(value);
        }

        public const string AccountNrExpressionRaw = @"\d+";
        private static ThreadLocal<Regex> AccountNrExpression = new ThreadLocal<Regex>(() => new Regex($"^{AccountNrExpressionRaw}$"));
        public static bool IsValidAccountNr(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                return false;
            return AccountNrExpression.Value.IsMatch(value);
        }

    }
}