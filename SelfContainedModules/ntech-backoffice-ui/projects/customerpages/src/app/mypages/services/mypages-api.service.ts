import { Injectable } from '@angular/core';
import { MlSeAmortizationBasisModel } from 'projects/ntech-components/src/public-api';
import { Dictionary, toCamelCase } from 'src/app/common.types';
import { CustomerPagesApiService } from '../../common-services/customer-pages-api.service';

@Injectable({
    providedIn: 'root',
})
export class MyPagesApiService {
    constructor(public apiService: CustomerPagesApiService) {}

    public shared(): CustomerPagesApiService {
        return this.apiService;
    }

    public fetchCustomerInfo(): Promise<CustomerInfo> {
        return this.apiService.postLocal<CustomerInfo>('Api/FetchCustomerInfo', {});
    }

    public updateCustomerValues(properties: { Name: string; Value: string }[]): Promise<any> {
        return this.apiService.post('nCustomer', 'Api/ContactInfo/UpdateMultiple', { Properties: properties });
    }

    public fetchCredits(includeCustomerPersonalData?: boolean, includeInactiveLoans?: boolean) {
        return this.apiService.post<CreditsModel>('nCredit', 'api/LoanStandard/CustomerPages/Fetch-Loans', {
            includeCustomerPersonalData,
            includeInactiveLoans,
        });
    }

    public async fetchLoanAmortizationPlan(creditNr: string) {
        let result = await this.apiService.post<any>(
            'nCredit',
            'api/LoanStandard/CustomerPages/Fetch-Loan-AmortizationPlan',
            { creditNr }
        );
        result.AmortizationBasis = toCamelCase(result.AmortizationBasis);
        return result as LoanAmortizationPlan;
    }

    public fetchUnsecuredLoansInterestHistory(creditNr: string) {
        return this.apiService.post<InterestHistory>(
            'nCredit',
            'api/LoanStandard/CustomerPages/Fetch-Interest-History',
            { creditNr }
        );
    }

    public fetchCapitalTransactionHistory(creditNr: string) {
        return this.apiService.post<CapitalTransactionHistory>(
            'nCredit',
            'api/LoanStandard/CustomerPages/Fetch-Capital-Transactions',
            { creditNr }
        );
    }

    public fetchCreditDocuments() {
        return this.apiService.post<{ Documents: FetchCreditDocumentsResponseDocument[] }>(
            'nCredit',
            'api/LoanStandard/CustomerPages/Fetch-Documents',
            {}
        );
    }
}

export class Address {
    Street?: string;
    Zipcode?: string;
    City?: string;
    Country?: any;
}

// Any data that does not exist in the database will be returned, but as null, even the Address.
export class CustomerInfo {
    Address?: Address;
    Email?: string;
    Phone?: string;
    FirstName?: string;
    LastName?: string;
}

export interface CreditsModel {
    ActiveCredits: CreditModel[];
    InactiveCredits: CreditModel[];
}

export interface CreditModel {
    CreditNr: string;
    StartDate: string;
    Status?: string;
    EndDate?: string;
    CapitalBalance: number;
    CurrentInterestRatePercent: number;
    IsDirectDebitActive: boolean;
    Notifications: NotificationModel[];
    NextNotificationDate: string;
    SinglePaymentLoanRepaymentDays ?: number;
    ApplicantsPersonalData?: {
        ApplicantNr: number;
        CivicRegNr: string;
        FirstName: string;
        LastName: string;
    }[];
    MortgageLoan?: {
        MortgageLoanInterestRebindMonthCount: number;
        MortgageLoanNextInterestRebindDate: string;
        CollateralId: number;
        CollateralStringItems: Dictionary<string>;
    };
    UnsecuredLoan?: {
        MonthlyPaymentExcludingFee: number;
        MonthlyPaymentIncludingFee: number;
    };
}

export interface ReminderModel {
    ReminderNumber: number;
    ReminderDate: string;
    ArchiveKey: string;
}

export interface NotificationModel {
    NotificationId: number;
    NotificationDate: string;
    IsOpen: boolean;
    ClosedDate?: string;
    DueDate: string;
    IsOverdue: boolean;
    InitialAmount: number;
    BalanceAmount: number;
    PaymentBankGiroNr: string;
    PaymentBankGiroNrDisplay: string;
    OcrPaymentReference: string;
    OcrPaymentReferenceDisplay: string;
    PdfArchiveKey: string;
    Reminders: ReminderModel[];
}

export interface LoanAmortizationPlan {
    NrOfRemainingPayments: number;
    AmortizationPlanItems: LoanAmortizationPlanItem[];
    TotalInterestAmount: number;
    UsesAnnuities: boolean;
    AnnuityAmount: number;
    FixedMonthlyPaymentAmount: number;
    AmortizationBasis: MlSeAmortizationBasisModel;
    SinglePaymentLoanRepaymentDays ?: number;
}

export interface LoanAmortizationPlanItem {
    EventTransactionDate: string;
    CapitalBefore: number;
    EventTypeCode: string;
    CapitalTransaction: number;
    NotificationFeeTransaction: number;
    InitialFeeTransaction: number;
    InterestTransaction: number;
    TotalTransaction: number;
    IsWriteOff: boolean;
    IsFutureItem: boolean;
    BusinessEventRoleCode: string;
    FutureItemDueDate: string;
}

export interface InterestHistory {
    InterestChanges: { TransactionDate: string; InterestRatePercent: number }[];
}

export interface CapitalTransactionHistory {
    Transactions: {
        TransactionDate: string;
        Amount: number;
        BusinessEventType: string;
        BusinessEventRoleCode: string;
        SubAccountCode: string;
        TotalAmountAfter: number;
    }[];
}

export interface FetchCreditDocumentsResponseDocument {
    DocumentDate: string;
    CreditNr: string;
    DocumentTypeCode: string;
    DocumentContext: string;
    DocumentArchiveKey: string;
}
