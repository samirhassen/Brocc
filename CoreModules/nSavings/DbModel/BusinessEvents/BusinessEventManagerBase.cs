using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using nSavings.Code;
using NTech;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace nSavings.DbModel.BusinessEvents;

public class BusinessEventManagerBase(in int userId, in string informationMetadata, IClock clock)
{
    public int UserId { get; } = userId;
    public string InformationMetadata { get; } = informationMetadata;

    public DateTimeOffset Now => Clock.Now;

    public IClock Clock { get; set; } = clock;
    protected bool IsProduction => NEnv.IsProduction;

    public BusinessEventManagerBase(int userId, string informationMetadata) : this(userId, informationMetadata,
        ClockFactory.SharedInstance)
    {
    }

    public static string GenerateUniqueOperationKey()
    {
        return OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(length: 20);
    }

    protected static bool TryGetCustomerNameFromCustomerCard(int customerId, out string customerName,
        out string failedMessage)
    {
        var customerClient = new CustomerClient();
        var customerCard = customerClient.GetCustomerCardItems(customerId, "firstName", "lastName");
        if (!customerCard.ContainsKey("firstName") || !customerCard.ContainsKey("lastName"))
        {
            failedMessage = "First name and or last name missing from customer card on customer";
            customerName = null;
            return false;
        }

        failedMessage = null;
        customerName = $"{customerCard["firstName"]} {customerCard["lastName"]}";
        return true;
    }

    protected void SetStatus(SavingsAccountHeader savingsAccount, SavingsAccountStatusCode status, BusinessEvent e,
        SavingsContext context)
    {
        savingsAccount.Status = status.ToString();
        AddDatedSavingsAccountString(nameof(DatedSavingsAccountStringCode.SavingsAccountStatus),
            status.ToString(), savingsAccount, e, context);
    }

    protected LedgerAccountTransaction AddTransaction(ISavingsContext context, LedgerAccountTypeCode accountType,
        decimal amount, BusinessEvent e, DateTime bookKeepingDate,
        SavingsAccountHeader savingsAccount = null, string savingsAccountNr = null,
        IncomingPaymentHeader incomingPayment = null, int? incomingPaymentId = null,
        int? outgoingPaymentId = null, OutgoingPaymentHeader outgoingPayment = null,
        DateTime? interestFromDate = null, //Defaults to transaction date            
        string businessEventRoleCode = null)
    {
        var transactionDate = Now.ToLocalTime().Date;
        var tr = new LedgerAccountTransaction
        {
            AccountCode = accountType.ToString(),
            Amount = amount,
            BookKeepingDate = bookKeepingDate,
            InterestFromDate = interestFromDate ?? transactionDate,
            TransactionDate = transactionDate,
            BusinessEvent = e,
            SavingsAccount = savingsAccount,
            SavingsAccountNr = savingsAccountNr,
            IncomingPayment = incomingPayment,
            IncomingPaymentId = incomingPaymentId,
            OutgoingPayment = outgoingPayment,
            OutgoingPaymentId = outgoingPaymentId,
            BusinessEventRoleCode = businessEventRoleCode
        };
        FillInInfrastructureFields(tr);
        context.AddLedgerAccountTransactions(tr);
        return tr;
    }

    protected BusinessEvent AddBusinessEvent(BusinessEventType t, ISavingsContext context)
    {
        var evt = new BusinessEvent
        {
            EventDate = Now,
            EventType = t.ToString(),
            TransactionDate = Now.ToLocalTime().Date,
        };
        FillInInfrastructureFields(evt);
        context.AddBusinessEvents(evt);
        return evt;
    }

    protected SavingsAccountComment AddComment(string commentText, BusinessEventType eventType,
        ISavingsContext context, SavingsAccountHeader savingsAccount = null, string savingsAccountNr = null,
        List<string> attachmentArchiveKeys = null)
    {
        if (savingsAccount == null && savingsAccountNr == null)
            throw new Exception("One of savingsAccount or savingsAccountNr must be set");
        var c = new SavingsAccountComment
        {
            CommentById = UserId,
            CommentDate = Now,
            CommentText = commentText,
            Attachment = attachmentArchiveKeys == null
                ? null
                : JsonConvert.SerializeObject(new { archiveKeys = attachmentArchiveKeys }),
            SavingsAccount = savingsAccount,
            SavingsAccountNr = savingsAccountNr,
            EventType = $"BusinessEvent_{eventType}",
        };
        FillInInfrastructureFields(c);
        context.AddSavingsAccountComments(c);
        return c;
    }

    protected DatedSavingsAccountValue AddDatedSavingsAccountValue(string name, decimal amount,
        SavingsAccountHeader savingsAccount, BusinessEvent e, SavingsContext context)
    {
        var r = new DatedSavingsAccountValue
        {
            SavingsAccount = savingsAccount,
            BusinessEvent = e,
            TransactionDate = Now.ToLocalTime().Date,
            Name = name,
            Value = amount
        };
        FillInInfrastructureFields(r);
        context.DatedSavingsAccountValues.Add(r);
        return r;
    }

    protected DatedSavingsAccountString AddDatedSavingsAccountString(string name, string value,
        SavingsAccountHeader savingsAccount, BusinessEvent e, SavingsContext context)
    {
        return AddSavingsAccountStringI(name, value, savingsAccount, null, e, context);
    }

    protected DatedSavingsAccountString AddDatedSavingsAccountString(string name, string value,
        string savingsAccountNr, BusinessEvent e, SavingsContext context)
    {
        return AddSavingsAccountStringI(name, value, null, savingsAccountNr, e, context);
    }

    protected SavingsAccountDocument AddSavingsAccountDocument(SavingsAccountDocumentTypeCode code,
        string archiveKey, BusinessEvent e, SavingsContext context, string savingsAccountNr = null,
        SavingsAccountHeader savingsAccount = null, string documentData = null)
    {
        var d = new SavingsAccountDocument
        {
            CreatedByEvent = e,
            DocumentArchiveKey = archiveKey,
            DocumentType = code.ToString(),
            SavingsAccount = savingsAccount,
            SavingsAccountNr = savingsAccountNr,
            DocumentData = documentData,
            DocumentDate = Clock.Now
        };
        FillInInfrastructureFields(d);
        context.SavingsAccountDocuments.Add(d);
        return d;
    }

    private DatedSavingsAccountString AddSavingsAccountStringI(string name, string value,
        SavingsAccountHeader savingsAccount, string savingsAccountNr, BusinessEvent e, SavingsContext context)
    {
        var r = new DatedSavingsAccountString
        {
            SavingsAccount = savingsAccount,
            SavingsAccountNr = savingsAccountNr,
            BusinessEvent = e,
            TransactionDate = Now.ToLocalTime().Date,
            Name = name,
            Value = value
        };
        FillInInfrastructureFields(r);
        context.DatedSavingsAccountStrings.Add(r);
        return r;
    }

    private CultureInfo commentFormattingCulture;

    protected CultureInfo CommentFormattingCulture =>
        commentFormattingCulture ??= CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);

    private CultureInfo printFormattingCulture;

    protected CultureInfo PrintFormattingCulture =>
        printFormattingCulture ??= CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);

    /// <summary>
    /// YYYY-MM but culture aware
    /// </summary>
    /// <param name="d">The datetime to format</param>
    /// <returns></returns>
    protected string FormatMonthCultureAware(DateTime d)
    {
        var c = (NEnv.ClientCfg.Country.BaseCountry ?? "FI").ToUpperInvariant();
        return d.ToString(c == "FI" ? "yyyy.MM" : "yyyy-MM");
    }

    protected T FillInInfrastructureFields<T>(T item) where T : InfrastructureBaseItem
    {
        item.ChangedById = UserId;
        item.ChangedDate = Clock.Now;
        item.InformationMetaData = InformationMetadata;
        return item;
    }
}