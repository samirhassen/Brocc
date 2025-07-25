﻿using System;
using System.Collections.Generic;
using System.Linq;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.Code.Riksgalden;

public class RiksgaldenDataRepository(
    in string clientCurrency,
    in CivicRegNumberParser civicRegNumberParser,
    in string clientBaseCountry,
    in IClock clock)
{
    private readonly string clientCurrency = clientCurrency;
    private readonly CivicRegNumberParser civicRegNumberParser = civicRegNumberParser;
    private readonly string clientBaseCountry = clientBaseCountry;
    private readonly IClock clock = clock;

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
        using var context = new SavingsContext();
        if (!YearlyInterestCapitalizationBusinessEventManager
                .TryComputeAccumulatedInterestAssumingAccountIsClosedToday(context, clock, accountNrs.ToList(),
                    false, out var interestCapResult, out var failedMessage,
                    maxBusinessEventId: maxBusinessEventId))
        {
            throw new Exception(failedMessage);
        }

        return interestCapResult.ToDictionary(x => x.Key, x => x.Value.TotalInterestAmount);
    }

    private IList<RiksgaldenFileExporter.Customer> GetCustomers(ISet<int> customerIds,
        out IList<RiksgaldenFileExporter.FiFilialCustomer> fiFilialCustomers)
    {
        var client = new CustomerClient();
        var customers = new List<RiksgaldenFileExporter.Customer>(customerIds.Count);

        var isFi = clientBaseCountry == "FI";
        fiFilialCustomers = isFi ? new List<RiksgaldenFileExporter.FiFilialCustomer>() : null;

        foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(500))
        {
            var result = client.BulkFetchPropertiesByCustomerIds(new HashSet<int>(customerIdGroup), "civicRegNr",
                "tin", "firstName", "lastName", "addressCity", "addressCountry", "addressZipcode", "addressStreet");

            foreach (var c in customerIdGroup)
            {
                var civicRegNr = civicRegNumberParser.Parse(GetValue("civicRegNr", result, c));
                var firstName = GetValue("firstName", result, c);
                var lastName = GetValue("lastName", result, c);
                customers.Add(new RiksgaldenFileExporter.Customer
                {
                    PersonOrgNummer = civicRegNr,
                    TIN = GetValue("tin", result, c),
                    CONamn = null,
                    Kundnummer = c.ToString(),
                    Landskod = civicRegNr.Country,
                    Namn = $"{firstName} {lastName}".Trim(),
                    Ort = GetValue("addressCity", result, c),
                    Postland = GetValue("addressCountry", result, c) ?? clientBaseCountry,
                    Postnummer = GetValue("addressZipcode", result, c),
                    Utdelningsadress = GetValue("addressStreet", result, c)
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

            continue;

            string GetValue(string n, IDictionary<int, CustomerClient.GetPropertyCustomer> r, int cid) =>
                r[cid]
                    .Properties.SingleOrDefault(x => x.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
                    ?.Value;
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
            .Where(x => x.AccountCode == nameof(LedgerAccountTypeCode.Capital))
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
        using var context = new SavingsContext();
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

        return d;
    }

    public FirstFileDataSet GetFirstFileDataSet()
    {
        var d = new FirstFileDataSet();

        using var context = new SavingsContext();
        var maxBusinessEventId = context.BusinessEvents.Max(x => x.Id);
        d.MaxBusinessEventId = maxBusinessEventId;

        d.MaxBusinessEventId = context.BusinessEvents.Max(x => x.Id);

        var trs = GetTransactionModel(context);

        //Accounts
        var accounts = context
            .SavingsAccountHeaders
            .Where(x => x.CreatedByBusinessEventId <= maxBusinessEventId &&
                        x.Status != nameof(SavingsAccountStatusCode.Closed))
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
            a.Account.UpplupenRanta =
                accumulatedInterestByAccountNr.TryGetValue(a.Account.Kontonummer, out var value)
                    ? value
                    : 0m;
        }

        d.Accounts = accounts.Select(x => x.Account).ToList();
        if (clientBaseCountry == "FI")
        {
            d.FiFilialAccounts = d.Accounts.Select(x => new RiksgaldenFileExporter.FiFilialAccount
            {
                Kontonummer = x.Kontonummer,
                Landskod = clientBaseCountry
            }).ToList();
        }

        //Customers
        var customerIds = new HashSet<int>(accounts.Select(x => x.MainCustomerId));
        d.Customers = GetCustomers(customerIds, out var fiFilialCustomers);
        d.FiFilialCustomers = fiFilialCustomers;

        //AccountDistributions 
        d.AccountDistributions = new List<RiksgaldenFileExporter.AccountDistribution>();
        foreach (var a in accounts)
        {
            d.AccountDistributions.Add(
                RiksgaldenFileExporter.AccountDistribution.CreateEqualAmongAll(a.Account.Kontonummer,
                    a.MainCustomerId.ToString(), 1));
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

        return d;
    }
}