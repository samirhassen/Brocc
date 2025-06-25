namespace nCustomerPages.Models;

public enum CustomerSavingsApplicationStatus
{
    NoActiveApplication,
    WaitingForClient,
    CustomerIsAMinor,
    CustomerHasAnActiveAccount
}

public class SavingsAccountApplicationViewModel
{
    public CustomerSavingsApplicationStatus Status { get; set; }
}