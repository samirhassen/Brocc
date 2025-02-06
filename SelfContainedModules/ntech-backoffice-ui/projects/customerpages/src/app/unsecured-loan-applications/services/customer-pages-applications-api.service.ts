import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { SizeAndTimeLimitedCache } from 'src/app/common-services/size-and-time-limited-cache';
import { Dictionary, NumberDictionary, StringDictionary } from 'src/app/common.types';
import { CustomerPagesApiService } from '../../common-services/customer-pages-api.service';
import { SharedApplicationBasicInfoModel } from '../../shared-components/applications-list/applications-list.component';
import { CustomerPagesKycQuestionAnswerModel } from '../../shared-components/task-kyc-questions/task-kyc-questions.component';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesApplicationsApiService {
    constructor(private apiService: CustomerPagesApiService) {}

    private nPreCredit: string = 'nPreCredit';

    public fetchApplications(): Promise<{
        Applications: ApplicationBasicInfoModel[];
    }> {
        return this.apiService.post(this.nPreCredit, 'api/UnsecuredLoanStandard/CustomerPages/Fetch-Applications', {});
    }

    public fetchApplication(applicationNr: string): Promise<{ Application: ApplicationExtendedModel }> {
        return this.apiService.post(this.nPreCredit, 'api/UnsecuredLoanStandard/CustomerPages/Fetch-Application', {
            applicationNr,
        });
    }

    public createKycQuestionSession(applicationNr: string): Promise<{ sessionId: string }> {
        return this.apiService.post('NTechHost', 'Api/PreCredit/UnsecuredLoanStandard/Create-Application-KycQuestionSession', {
            applicationNr
        });        
    }

    setCustomerCreditDecisionCode(applicationNr: string, customerDecisionCode: string) {
        return this.apiService.post(this.nPreCredit, 'api/UnsecuredLoanStandard/Set-Customer-CreditDecisionCode', {
            applicationNr,
            customerDecisionCode,
        });
    }

    answerKycQuestions(
        applicationNr: string,
        answersByApplicantNr: NumberDictionary<CustomerPagesKycQuestionAnswerModel[]>,
        isConfirmedKycQuestionAnswers: boolean
    ) {
        return this.apiService.post(this.nPreCredit, 'api/UnsecuredLoanStandard/CustomerPages/Answer-Kyc-Questions', {
            applicationNr,
            answersByApplicantNr,
            isConfirmedKycQuestionAnswers,
        });
    }

    confirmBankAccounts(applicationNr: string) {
        return this.apiService.post(this.nPreCredit, 'api/UnsecuredLoanStandard/CustomerPages/Confirm-Bank-Accounts', {
            applicationNr,
        });
    }

    getArchiveDocumentUrl(archiveKey: string, skipFilename?: boolean) {
        return this.apiService.getArchiveDocumentUrl(archiveKey, skipFilename);
    }

    saveDirectDebitAccountInfo(applicationNr: string, bankAccountNr: string, applicantNr: number): Promise<void> {
        return this.apiService.post(
            this.nPreCredit,
            'api/UnsecuredLoanStandard/CustomerPages/Save-DirectDebit-Account',
            {
                ApplicationNr: applicationNr,
                DirectDebitBankAccountNr: bankAccountNr,
                DirectDebitAccountOwnerApplicantNr: applicantNr,
            }
        );
    }

    confirmDirectDebitAccountInfo(applicationNr: string): Promise<void> {
        return this.apiService.post(
            this.nPreCredit,
            'api/UnsecuredLoanStandard/CustomerPages/Confirm-DirectDebit-Account',
            { applicationNr }
        );
    }

    private getValidateBankAccountNrCacheKey(request: BankAccountNrValidationRequest): string {
        return `${request.bankAccountNr}#${request.bankAccountNrType}`;
    }
    private validateBankAccountNrsCache: SizeAndTimeLimitedCache = new SizeAndTimeLimitedCache(200, 30);
    private validateBankAccountNrsBatchRaw(
        request: Dictionary<BankAccountNrValidationRequest>,
        skipLoadingIndicator: boolean
    ): Promise<{
        ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
    }> {
        let accounts = Object.keys(request).map((x) => {
            return {
                requestKey: x,
                bankAccountNr: request[x].bankAccountNr,
                bankAccountNrType: request[x].bankAccountNrType,
            };
        });
        return this.apiService.post(
            this.nPreCredit,
            'api/bankaccount/validate-nr-batch',
            { accounts },
            { skipLoadingIndicator: skipLoadingIndicator }
        );
    }
    validateBankAccountNrsBatch(
        request: Dictionary<BankAccountNrValidationRequest>,
        skipLoadingIndicator: boolean
    ): Promise<{
        ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
    }> {
        let fromCacheResult: {
            ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
        } = { ValidatedAccountsByKey: {} };

        if (!request || Object.keys(request).length === 0) {
            return of({ ValidatedAccountsByKey: {} }).toPromise();
        }

        let anyNotInCache = false;

        for (let key of Object.keys(request)) {
            let account = request[key];
            let cacheHit = this.validateBankAccountNrsCache.get<BankAccountNrValidationResult>(
                this.getValidateBankAccountNrCacheKey(account)
            );
            if (!cacheHit) {
                anyNotInCache = true;
                break;
            }
            fromCacheResult.ValidatedAccountsByKey[key] = cacheHit;
        }

        if (anyNotInCache) {
            return this.validateBankAccountNrsBatchRaw(request, skipLoadingIndicator).then((x) => {
                for (let key of Object.keys(x.ValidatedAccountsByKey)) {
                    this.validateBankAccountNrsCache.set(
                        this.getValidateBankAccountNrCacheKey(request[key]),
                        x.ValidatedAccountsByKey[key]
                    );
                }
                return x;
            });
        } else {
            return of(fromCacheResult).toPromise();
        }
    }

    editBankAccounts(request: EditBankAccountsRequest) {
        return this.apiService.post(
            this.nPreCredit,
            'api/UnsecuredLoanStandard/CustomerPages/Edit-BankAccounts',
            request
        );
    }
}

export interface ApplicationBasicInfoModel extends SharedApplicationBasicInfoModel {
    Enums: {
        CivilStatuses: { Code: string; DisplayName: string }[];
        EmploymentStatuses: { Code: string; DisplayName: string }[];
        HousingTypes: { Code: string; DisplayName: string }[];
        OtherLoanTypes: { Code: string; DisplayName: string }[];
    };
}

export interface ApplicationExtendedModel extends ApplicationBasicInfoModel {
    IsFutureOfferPossible?: boolean;
    CurrentOffer?: {
        OfferItems: StringDictionary;
        IsPossibleToDecide: boolean;
    };
    KycTask: KycTaskModel;
    AgreementTask: AgreementTaskModel;
    BankAccountsTask: BankAccountsTaskModel;
    DirectDebitTask: DirectDebitTaskModel;
}

export interface ApplicationTask {
    IsActive: boolean;
    IsAccepted?: boolean;
}

export interface KycTaskModel extends ApplicationTask {
    IsPossibleToAnswer: boolean;
    IsAnswersApproved: boolean;
}

export interface AgreementTaskModel extends ApplicationTask {
    UnsignedAgreementArchiveKey: string;
    SignedAgreementArchiveKey: string;
    ApplicantStatusByApplicantNr: NumberDictionary<{
        CustomerBirthDate: string;
        CustomerShortName: string;
        HasSigned: boolean;
        SignatureUrl: string;
        IsPossibleToSign: boolean;
    }>;
}

export interface DirectDebitTaskModel extends ApplicationTask {
    CustomerInfoByApplicantNr: NumberDictionary<{
        CustomerId: number;
        BirthDate: string;
        FirstName: string;
    }>;
    HasConfirmedAccountInfo: boolean;
    AccountOwnerApplicantNr?: number;
    DirectDebitBankAccountNr?: string;
    UnsignedDirectDebitConsentFileArchiveKey?: string;
    SignedDirectDebitConsentFileArchiveKey?: string;
    SignatureSessionUrl?: string;
    PaidToCustomerBankAccountNr?: string;
}

export interface BankAccountsTaskModel extends ApplicationTask {
    IsPossibleToEditPaidToCustomerBankAccount: boolean;
    IsPossibleToEditLoansToSettleBankAccounts: boolean;
    PaidToCustomer: {
        Amount: number;
        BankAccountNrType: string;
        BankAccountNr: string;
    };
    LoansToSettle: {
        Nr: number;
        CurrentDebtAmount: number;
        MonthlyCostAmount: number;
        LoanType: string;
        BankAccountNrType: string;
        BankAccountNr: string;
        SettlementPaymentReference: string;
        SettlementPaymentMessage: string;
    }[];
}

export interface BankAccountNrValidationRequest {
    bankAccountNr: string;
    bankAccountNrType: string;
}

export interface BankAccountNrValidationResult {
    RawNr: string;
    IsValid: boolean;
    ValidAccount?: {
        BankName: string;
        ClearingNr: string;
        AccountNr: string;
        NormalizedNr: string;
        Bic: string;
        DisplayNr: string;
        BankAccountNrType: string;
    };
}

export interface EditBankAccountsRequest {
    applicationNr: string;
    paidToCustomer?: {
        bankAccountNr: string;
        bankAccountNrType?: string;
    };
    loansToSettle?: {
        Accounts: {
            nr: number;
            bankAccountNr: string;
            bankAccountNrType?: string;
            settlementPaymentReference?: string;
        }[];
    };
}
