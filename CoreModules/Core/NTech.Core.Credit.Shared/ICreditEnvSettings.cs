
using nCredit.Code.EInvoiceFi;
using nCredit.Code.Fileformats;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module;
using NTech.Core.Module.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace nCredit
{
    public interface ICreditEnvSettings : ISharedEnvSettings
    {
        bool IsMortgageLoansEnabled { get; }
        bool IsStandardMortgageLoansEnabled { get; }
        bool IsUnsecuredLoansEnabled { get; }
        bool IsStandardUnsecuredLoansEnabled { get; }
        bool IsCompanyLoansEnabled { get; }
        bool CreditsUse360DayInterestYear { get; }
        string OutgoingPaymentFileCustomerMessagePattern { get; }
        IBankAccountNumber OutgoingPaymentBankAccountNr { get; }
        IBankAccountNumber IncomingPaymentBankAccountNr { get; }
        decimal CreditSettlementBalanceLimit { get; }
        InterestModelCode ClientInterestModel { get; }
        bool HasPerLoanDueDay { get; }
        int? MortgageLoanInterestBindingMonths { get; }
        AutogiroSettingsModel AutogiroSettings { get; }
        PositiveCreditRegisterSettingsModel PositiveCreditRegisterSettings { get; }
        bool IsDirectDebitPaymentsEnabled { get; }
        bool IsPromiseToPayAmortizationFreedomEnabled { get; }
        int? CreditSettlementOfferGraceDays { get; }
        OutgoingPaymentFileFormat_SUS_SE.SwedbankSeSettings OutgoingPaymentFilesSwedbankSeSettings { get; }        
        string TemplatePrintContextLogFolder { get; }
        int NrOfDaysUntilCreditTermsChangeOfferExpires { get; }
        DirectoryInfo OutgoingCreditNotificationDeliveryFolder { get; }
        string CreditRemindersExportProfileName { get; }
        CreditType ClientCreditType { get; }
        string DebtCollectionPartnerName { get; }
        int LindorffFileDebtCollectionClientNumber { get; }
        bool ShouldRecalculateAnnuityOnInterestChange { get; }
        Tuple<decimal?, decimal?> MinAndMaxAllowedMarginInterestRate { get; }
        decimal? LegalInterestCeilingPercent { get; }
        string BookKeepingRuleFileName { get; }
        string SieFileEnding { get; }
        List<AffiliateModel> GetAffiliateModels();
        string OutgoingPaymentFilesBankName { get; }
        DanskeBankFiSettings OutgoingPaymentFilesDanskeBankSettings { get; }
        HandelsbankenSeSettings OutgoingPaymentFilesHandelsbankenSeSettings { get; }
        EInvoiceFiSettingsFile EInvoiceFiSettingsFile { get; }
        NTech.Banking.BookKeeping.NtechAccountPlanFile BookKeepingAccountPlan { get; }
        FileInfo BookKeepingReconciliationReportFormatFile { get; }
        SignatureProviderCode EidSignatureProviderCode { get; }
    }

    public class AutogiroSettingsModel
    {
        public bool IsHmacFileSealEnabled { get; set; }
        public string HmacFileSealKey { get; set; }
        public string CustomerNr { get; set; }
        public BankGiroNumberSe BankGiroNr { get; set; }
        public string OutgoingStatusFileExportProfileName { get; set; }
        public string OutgoingPaymentFileExportProfileName { get; set; }
        public string IncomingStatusFileImportFolder { get; set; }
        public string GetRequiredCustomerNr()
        {
            if (string.IsNullOrWhiteSpace(CustomerNr))
                throw new Exception("Missing direct debit customerNr");
            return CustomerNr;
        }
    }


    public class HandelsbankenSeSettings
    {
        public OrganisationNumberSe ClientOrgnr { get; set; }
        public string BankMmbId { get; set; }
        public string FileFormat { get; set; }
        public string SenderCompanyName { get; set; }
    }


    public class DanskeBankFiSettings
    {
        public string FileFormat { get; set; }
        public string SendingCompanyId { get; set; }
        public string SendingCompanyName { get; set; }
        public string SendingBankBic { get; set; }
        public string SendingBankName { get; set; }
    }

    public enum SignatureProviderCode
    {
        signicat,
        signicat2, //New Signicat, use this instead
        mock
    }
}
