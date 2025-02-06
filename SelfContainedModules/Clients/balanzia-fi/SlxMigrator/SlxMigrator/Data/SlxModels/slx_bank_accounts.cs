using Dapper;
using Newtonsoft.Json.Linq;
using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_bank_accounts
    {
		public static string GetKey(string savingsAccountNr, int customerId) => $"{savingsAccountNr}#{customerId}";

		public static Dictionary<string, List<JObject>> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, bool isLoan)
		{
			if (isLoan)
				throw new Exception("Only for savings");

			var query = @"select a.SavingsAccountNr,
		h.MainCustomerId,
		a.TransactionDate,
		a.[Value] as IbanRaw,
		(select min(aNext.TransactionDate) from DatedSavingsAccountString aNext where a.SavingsAccountNr = aNext.SavingsAccountNr and aNext.BusinessEventId > a.BusinessEventId and aNext.[Name] = a.[Name]) as NextIbanTransactionDate,
		(select MAX(s.TransactionDate) from DatedSavingsAccountString s where s.SavingsAccountNr = a.SavingsAccountNr and s.[Name] = 'SavingsAccountStatus' and s.[Value] = 'Closed') as AccountClosedDate
from	DatedSavingsAccountString a
join	SavingsAccountHeader h on h.SavingsAccountNr = a.SavingsAccountNr
where	a.[Name] = 'WithdrawalIban'
and		h.MainCustomerId in @customerIds";
			using (var savingsConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Savings))
			{
				return savingsConnection
					.Query<DbAccount>(query, param: new { customerIds }, commandTimeout: 60000)
					.ToList()
					.GroupBy(x => new { x.MainCustomerId, x.SavingsAccountNr })
					.ToDictionary(
						x => GetKey(x.Key.SavingsAccountNr, x.Key.MainCustomerId), 
						x => x.Select(TransformAccount).ToList());					
			}
		}

		private static JObject TransformAccount(DbAccount account)
        {
			string bankName = "";
			if (IBANFi.TryParse(account.IbanRaw, out var parsedAccount))
				bankName = IBANToBICTranslator.Value.InferBankName(parsedAccount);
			var endDate = account.NextIbanTransactionDate ?? account.AccountClosedDate;
			
			var result = new
			{
				bank_name = bankName,
				bank_clearing_number = 0,
				bank_account_number = account.IbanRaw,
				start_datetime = account.TransactionDate.ToString("yyyy-MM-dd"),
				end_datetime = endDate?.ToString("yyyy-MM-dd")
			};

			return JObject.FromObject(result);
        }

		private class DbAccount
        {
			public string SavingsAccountNr { get; set; }
			public int MainCustomerId { get; set; }
			public DateTime TransactionDate { get; set; }
			public string IbanRaw { get; set; }
			public DateTime? NextIbanTransactionDate { get; set; }
			public DateTime? AccountClosedDate { get; set; }
		}

		private static Lazy<IBANToBICTranslator> IBANToBICTranslator = new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());
	}
}
