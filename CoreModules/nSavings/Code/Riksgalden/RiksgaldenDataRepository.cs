using nSavings.DbModel.BusinessEvents;
using NTech;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.Code.Riksgalden
{
    public class RiksgaldenDataRepository
    {
        private readonly string clientCurrency;
        private readonly CivicRegNumberParser civicRegNumberParser;
        private readonly string clientBaseCountry;
        private readonly IClock clock;

        public RiksgaldenDataRepository(string clientCurrency, CivicRegNumberParser civicRegNumberParser, string clientBaseCountry, IClock clock)
        {
            this.clientCurrency = clientCurrency;
            this.civicRegNumberParser = civicRegNumberParser;
            this.clientBaseCountry = clientBaseCountry;
            this.clock = clock;
        }

        public class FirstFileDataSet
        {
            public int MaxBusinessEventId { get; set; }
            public IList<RiksgaldenFileExporter.Customer> Customers { get; set; }
            public IList<RiksgaldenFileExporter.Account> Accounts { get; set; }
            public IList<RiksgaldenFileExporter.AccountDistribution> AccountDistributions { get; set; }
            public IList<RiksgaldenFileExporter.Transaction> Transactions { get; set; }

            //Only used for the 'filial.txt-file' when the 'filial' is finnish
            public IList<RiksgaldenFileExporter.FiFilialCustomer> FiFilialCustomers { get; set; }
            public IList<RiksgaldenFileExporter.FiFilialAccount> FiFilialAccounts { get; set; }
        }

        public class SecondFileDataSet
        {
            public int StartAfterBusinessEventId { get; set; }
            public int? MaxBusinessEventId { get; set; }
            public IList<RiksgaldenFileExporter.Transaction> Transactions { get; set; }
        }

        private IDictionary<string, decimal> GetAccumulatedInterest(ISet<string> accountNrs, int maxBusinessEventId)
        {
            using (var context = new SavingsContext())
            {
                IDictionary<string, YearlyInterestCapitalizationBusinessEventManager.ResultModel> interestCapResult;
                string failedMessage;
                if (!YearlyInterestCapitalizationBusinessEventManager.TryComputeAccumulatedInterestAssumingAccountIsClosedToday(context, clock, accountNrs.ToList(), false, out interestCapResult, out failedMessage, maxBusinessEventId: maxBusinessEventId))
                {
                    throw new Exception(failedMessage);
                }
                return interestCapResult.ToDictionary(x => x.Key, x => x.Value.TotalInterestAmount);
            }
        }

        private IList<RiksgaldenFileExporter.Customer> GetCustomers(ISet<int> customerIds, out IList<RiksgaldenFileExporter.FiFilialCustomer> fiFilialCustomers)
        {
            var client = new CustomerClient();
            var customers = new List<RiksgaldenFileExporter.Customer>(customerIds.Count);

            var isFi = this.clientBaseCountry == "FI";
            fiFilialCustomers = isFi ? new List<RiksgaldenFileExporter.FiFilialCustomer>() : null;

            foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(500))
            {
                var result = client.BulkFetchPropertiesByCustomerIds(new HashSet<int>(customerIdGroup), "civicRegNr", "tin", "firstName", "lastName", "addressCity", "addressCountry", "addressZipcode", "addressStreet");

                Func<string, IDictionary<int, CustomerClient.GetPropertyCustomer>, int, string> getValue = (n, r, cid) =>
                   r[cid].Properties.SingleOrDefault(x => x.Name.Equals(n, StringComparison.OrdinalIgnoreCase))?.Value;

                foreach (var c in customerIdGroup)
                {
                    var civicRegNr = civicRegNumberParser.Parse(getValue("civicRegNr", result, c));
                    var firstName = getValue("firstName", result, c);
                    var lastName = getValue("lastName", result, c);
                    customers.Add(new RiksgaldenFileExporter.Customer
                    {
                        PersonOrgNummer = civicRegNr,
                        TIN = getValue("tin", result, c),
                        CONamn = null,
                        Kundnummer = c.ToString(),
                        Landskod = civicRegNr.Country,
                        Namn = $"{firstName} {lastName}".Trim(),
                        Ort = getValue("addressCity", result, c),
                        Postland = getValue("addressCountry", result, c) ?? clientBaseCountry,
                        Postnummer = getValue("addressZipcode", result, c),
                        Utdelningsadress = getValue("addressStreet", result, c)
                    });
                    if (isFi)
                    {
                        fiFilialCustomers.Add(new RiksgaldenFileExporter.FiFilialCustomer
                        {
                            Kundnummer = c.ToString(),
                            Fornamn = firstName,
                            Efternamn = lastName,
                            Titel = null
                        });
                    }
                }
            }
            return customers;
        }

        private class CapitalTransactionModel
        {
            public long Id { get; set; }
            public int CreatedByBusinessEventId { get; set; }
            public decimal Amount { get; set; }
            public string SavingsAccountNr { get; set; }
            public DateTime TransactionDate { get; set; }
            public int? CommitedBusinessEventId { get; set; }
            public DateTime? CommitedTransactionDate { get; set; }
        }

        private IQueryable<CapitalTransactionModel> GetTransactionModel(SavingsContext context)
        {
            return context
                .LedgerAccountTransactions
                .Where(x => x.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                .Select(x => new CapitalTransactionModel
                {
                    Id = x.Id,
                    SavingsAccountNr = x.SavingsAccountNr,
                    CreatedByBusinessEventId = x.BusinessEventId,
                    Amount = x.Amount,
                    CommitedBusinessEventId = x.OutgoingPaymentId.HasValue
                        ? x.OutgoingPayment.OutgoingPaymentFile.CreatedByBusinessEventId
                        : x.BusinessEventId,
                    CommitedTransactionDate = x.OutgoingPaymentId.HasValue
                        ? x.OutgoingPayment.OutgoingPaymentFile.CreatedByEvent.TransactionDate
                        : x.TransactionDate,
                    TransactionDate = x.TransactionDate
                });
        }

        public SecondFileDataSet GetSecondFileDataSet(int maxBusinessEventIdFromFirstFile)
        {
            var d = new SecondFileDataSet();
            using (var context = new SavingsContext())
            {
                var maxBusinessEventId = context.BusinessEvents.Max(x => x.Id);
                d.MaxBusinessEventId = maxBusinessEventId;

                d.StartAfterBusinessEventId = maxBusinessEventIdFromFirstFile;

                var trs = GetTransactionModel(context);

                //Accounts
                var accounts = context
                    .SavingsAccountHeaders
                    .Where(x => x.CreatedByBusinessEventId <= maxBusinessEventIdFromFirstFile)
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.MainCustomerId,
                        NewCapitalTransactions = trs
                            .Where(y =>
                                y.SavingsAccountNr == x.SavingsAccountNr
                                && y.CreatedByBusinessEventId > maxBusinessEventIdFromFirstFile
                                && y.CreatedByBusinessEventId <= maxBusinessEventId
                                && y.CommitedBusinessEventId.HasValue),
                        CompletedPreviousPendingTransactions = trs
                            .Where(y =>
                                y.SavingsAccountNr == x.SavingsAccountNr

                                //Queued before the first export
                                && y.CreatedByBusinessEventId <= maxBusinessEventIdFromFirstFile

                                //And completed after
                                && y.CommitedBusinessEventId.HasValue
                                && y.CommitedBusinessEventId > maxBusinessEventIdFromFirstFile)
                    })
                    .Select(x =>
                        new
                        {
                            x.SavingsAccountNr,
                            x.NewCapitalTransactions,
                            x.CompletedPreviousPendingTransactions
                        })
                    .ToList();

                //Transactions
                d.Transactions = new List<RiksgaldenFileExporter.Transaction>();
                foreach (var a in accounts)
                {
                    foreach (var t in a.NewCapitalTransactions)
                    {
                        d.Transactions.Add(new RiksgaldenFileExporter.Transaction
                        {
                            Belopp = t.Amount,
                            Bokforingsdatum = t.CommitedTransactionDate,
                            Kontonummer = a.SavingsAccountNr,
                            Transaktionsdatum = t.TransactionDate,
                            Referens = $"Id {t.Id}"
                        });
                    }
                    foreach (var t in a.CompletedPreviousPendingTransactions)
                    {
                        d.Transactions.Add(new RiksgaldenFileExporter.Transaction
                        {
                            Belopp = t.Amount,
                            Bokforingsdatum = t.CommitedTransactionDate,
                            Kontonummer = a.SavingsAccountNr,
                            Transaktionsdatum = t.TransactionDate,
                            Referens = $"Id {t.Id}"
                        });
                    }
                }
            }

            return d;
        }

        public FirstFileDataSet GetFirstFileDataSet()
        {
            var d = new FirstFileDataSet();

            using (var context = new SavingsContext())
            {
                var maxBusinessEventId = context.BusinessEvents.Max(x => x.Id);
                d.MaxBusinessEventId = maxBusinessEventId;

                d.MaxBusinessEventId = context.BusinessEvents.Max(x => x.Id);

                var trs = GetTransactionModel(context);

                //Accounts
                var accounts = context
                    .SavingsAccountHeaders
                    .Where(x => x.CreatedByBusinessEventId <= maxBusinessEventId && x.Status != SavingsAccountStatusCode.Closed.ToString())
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.MainCustomerId,
                        CapitalBalance = (trs
                            .Where(y =>
                                y.SavingsAccountNr == x.SavingsAccountNr
                                && y.CreatedByBusinessEventId <= maxBusinessEventId)
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                        PendingTransactions = trs
                            .Where(y =>
                                y.SavingsAccountNr == x.SavingsAccountNr
                                && y.CreatedByBusinessEventId <= maxBusinessEventId
                                && !y.CommitedBusinessEventId.HasValue)
                    })
                    .Select(x =>
                        new
                        {
                            MainCustomerId = x.MainCustomerId,
                            x.PendingTransactions,
                            Account = new RiksgaldenFileExporter.Account
                            {
                                Valuta = clientCurrency,
                                Kontonummer = x.SavingsAccountNr,
                                Pantsatt = false,
                                Sparrat = false,
                                Kapital = x.CapitalBalance - (x.PendingTransactions.Sum(y => (decimal?)y.Amount) ?? 0m),
                                UpplupenRanta = 0m //Added below
                            }
                        })
                    .ToList();
                var accountNrs = new HashSet<string>(accounts.Select(x => x.Account.Kontonummer));
                var accumulatedInterestByAccountNr = GetAccumulatedInterest(accountNrs, maxBusinessEventId);
                foreach (var a in accounts)
                {
                    a.Account.UpplupenRanta = accumulatedInterestByAccountNr.ContainsKey(a.Account.Kontonummer) ? accumulatedInterestByAccountNr[a.Account.Kontonummer] : 0m;
                }
                d.Accounts = accounts.Select(x => x.Account).ToList();
                if (this.clientBaseCountry == "FI")
                {
                    d.FiFilialAccounts = d.Accounts.Select(x => new RiksgaldenFileExporter.FiFilialAccount
                    {
                        Kontonummer = x.Kontonummer,
                        Landskod = this.clientBaseCountry
                    }).ToList();
                }

                //Customers
                IList<RiksgaldenFileExporter.FiFilialCustomer> fiFilialCustomers;
                var customerIds = new HashSet<int>(accounts.Select(x => x.MainCustomerId));
                d.Customers = GetCustomers(customerIds, out fiFilialCustomers);
                d.FiFilialCustomers = fiFilialCustomers;

                //AccountDistributions 
                d.AccountDistributions = new List<RiksgaldenFileExporter.AccountDistribution>();
                foreach (var a in accounts)
                {
                    d.AccountDistributions.Add(RiksgaldenFileExporter.AccountDistribution.CreateEqualAmongAll(a.Account.Kontonummer, a.MainCustomerId.ToString(), 1));
                }

                //Transactions
                d.Transactions = new List<RiksgaldenFileExporter.Transaction>();
                foreach (var a in accounts)
                {
                    foreach (var t in a.PendingTransactions)
                    {
                        d.Transactions.Add(new RiksgaldenFileExporter.Transaction
                        {
                            Belopp = t.Amount,
                            Bokforingsdatum = null,
                            Kontonummer = a.Account.Kontonummer,
                            Transaktionsdatum = t.TransactionDate,
                            Referens = $"Id {t.Id}"
                        });
                    }
                }
            }

            return d;
        }
    }
}