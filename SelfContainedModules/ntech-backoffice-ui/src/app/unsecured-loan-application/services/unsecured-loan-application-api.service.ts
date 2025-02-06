import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, NumberDictionary, StringDictionary } from 'src/app/common.types';
import { PolicyFilterCreditRecommendationModel, PolicyFilterDetailsDisplayItem, PolicyFilterEngineResult } from 'src/app/loan-policyfilters/services/policy-filters-apiservice';
import { SharedApplicationApiService } from 'src/app/shared-application-components/services/shared-loan-application-api.service';
import {
    StandardApplicationInitialDataModelBase,
    StandardApplicationModelBase,
    StandardLoanApplicationEnumsModel,
} from 'src/app/shared-application-components/services/standard-application-base';
import { StandardCreditApplicationModel } from './standard-credit-application-model';

export const UnsecuredLoanApplicationKycQuestionSourceType = 'UnsecuredLoanApplication';

@Injectable({
    providedIn: 'root',
})
export class UnsecuredLoanApplicationApiService extends SharedApplicationApiService {
    constructor(apiService: NtechApiService) {
        super(apiService, false);
    }

    fetchApplicationInitialData(
        applicationNr: string
    ): Promise<StandardCreditApplicationModel | 'noSuchApplicationExists'> {
        return this.fetchApplicationInitialDataInternal(applicationNr).then((x) => {
            if (x === 'noSuchApplicationExists') return x;
            else return new StandardCreditApplicationModel(x);
        });
    }

    private fetchApplicationInitialDataInternal(
        applicationNr: string
    ): Promise<UnsecuredLoanApplicationInitialDataModel | 'noSuchApplicationExists'> {
        return this.apiService.post(
            'nPreCredit',
            'api/UnsecuredLoanStandard/FetchApplicationInitialData',
            { applicationNr },
            {
                handleNTechError: (err) => {
                    if (err.errorCode === 'noSuchApplicationExists') {
                        return 'noSuchApplicationExists';
                    } else {
                        throw err;
                    }
                },
            }
        );
    }

    public fetchApplicationInitialDataShared(
        applicationNr: string
    ): Promise<StandardApplicationModelBase | 'noSuchApplicationExists'> {
        return this.fetchApplicationInitialData(applicationNr);
    }

    setCurrentCreditDecision(request: SetCurrentCreditDecisionRequest): Promise<{ CreditDecisionId: number }> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Set-Current-CreditDecision', request);
    }

    editApplication(request: {
        applicationNr: string;
        requestedLoanAmount: number;
        requestedRepaymentTimeInMonths ?: number;
        requestedRepaymentTimeInDays ?: number;
        paidToCustomerBankAccountNr: string;
        paidToCustomerBankAccountNrType: string;
        loanObjective: string;
    }) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Edit-Application', request);
    }

    setCustomerCreditDecisionCode(applicationNr: string, customerDecisionCode: string) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Set-Customer-CreditDecisionCode', {
            applicationNr,
            customerDecisionCode,
        });
    }

    setIsApprovedCustomerOfferDecisionStep(applicationNr: string, isApproved: boolean) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/CustomerOfferDecision/Set-Approved-Step', {
            applicationNr: applicationNr,
            isApproved: isApproved,
        });
    }

    approveFraudStep(applicationNr: string, isApproved: boolean) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Fraud/Approve-Step', {
            applicationNr: applicationNr,
            isApproved: isApproved,
        });
    }

    setIsApprovedAgreementStep(applicationNr: string, isApproved: boolean) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Agreement/Set-Approved-Step', {
            applicationNr: applicationNr,
            isApproved: isApproved,
        });
    }

    createAgreementSignatureSessionWithDataUrl(
        applicationNr: string,
        unsignedDocumentDataUrl: string,
        unsignedDocumentFileName: string
    ): Promise<{
        SignatureUrlByApplicantNr: NumberDictionary<string>;
    }> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Create-Agreement-Signature-Session', {
            applicationNr,
            DataUrlFile: {
                DataUrl: unsignedDocumentDataUrl,
                FileName: unsignedDocumentFileName,
            },
        });
    }

    createAgreementSignatureSessionWithArchiveKey(
        applicationNr: string,
        unsignedAgreementPdfArchiveKey: string
    ): Promise<{
        SignatureUrlByApplicantNr: NumberDictionary<string>;
    }> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Create-Agreement-Signature-Session', {
            applicationNr,
            unsignedAgreementPdfArchiveKey,
        });
    }

    cancelAgreementSignatureSession(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Cancel-Agreement-Signature-Session', {
            applicationNr,
        });
    }

    attachSignedAgreementManually(applicationNr: string, dataUrl: string, filename: string) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Agreement/Set-SignedAgreement-Manually', {
            applicationNr,
            filename,
            dataUrl,
        });
    }

    removeSignedAgreementManually(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Agreement/Set-SignedAgreement-Manually', {
            applicationNr,
            isRemove: true,
        });
    }

    createLoan(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'Api/UnsecuredLoanStandard/Create-Loan', { applicationNr });
    }

    newCreditCheck(applicationNr: string): Promise<NewCreditCheckResponse> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/New-CreditCheck', { applicationNr });
    }

    runFraudControls(applicationNr: string): Promise<RunFraudControlResult> {
        return this.apiService.post('nPreCredit', 'api/FraudControl/RunFraudControls', {
            applicationNr: applicationNr,
        });
    }

    getFraudControlResults(applicationNr: string): Promise<RunFraudControlResult> {
        return this.apiService.post('nPreCredit', 'api/FraudControl/GetFraudControls', {
            applicationNr: applicationNr,
        });
    }

    setFraudControlItemApproved(applicationNr: string, fraudControlName: string): Promise<void> {
        return this.apiService.post('nPreCredit', 'api/FraudControl/SetFraudControlItemApproved', {
            applicationNr: applicationNr,
            fraudControlName: fraudControlName,
        });
    }

    setFraudControlItemInitial(applicationNr: string, fraudControlName: string): Promise<void> {
        return this.apiService.post('nPreCredit', 'api/FraudControl/SetFraudControlItemInitial', {
            applicationNr: applicationNr,
            fraudControlName: fraudControlName,
        });
    }

    addTestPolicyFilterRuleSet(skipIfExists: boolean, overrideSlotName?: string): Promise<{ WasCreated: boolean }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/Create-TestSet', {
            skipIfExists,
            overrideSlotName,
        });
    }

    toggleBankAccountConfirmationCodeStatus(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/CustomerPages/Confirm-Bank-Accounts', {
            applicationNr,
            canToggle: true,
        });
    }

    //TODO: Think about if this should really be any or we should include all the fields here.
    createApplication(request: any): Promise<{ ApplicationNr: string }> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Create-Application', request);
    }

    fetchRegisterApplicationInitialData(): Promise<{
        ProviderDisplayNameByName: Dictionary<string>;
        Enums: StandardLoanApplicationEnumsModel;
    }> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/FetchRegisterApplicationInitialData', {});
    }

    cancelDirectDebitSignatureSession(applicationNr: string): Promise<void> {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/Cancel-DirectDebit-Signature-Session', {
            applicationNr,
        });
    }

    setDirectDebitLoanTerms(
        applicationNr: string,
        isPending: boolean,
        activeAccount?: {
            ensureCreditNrExists: boolean;
            bankAccountNr: string;
            bankAccountNrType: string;
            bankAccountNrOwnerApplicantNr: number;
            directDebitConsentArchiveKey: string;
        }
    ) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/DirectDebit/Set-LoanTerms', {
            applicationNr,
            isCancel: false,
            newTerms: {
                isActive: !!activeAccount,
                isPending,
                ...activeAccount,
            },
        });
    }

    cancelDirectDebitLoanTerms(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'api/UnsecuredLoanStandard/DirectDebit/Set-LoanTerms', {
            applicationNr,
            isCancel: true,
        });
    }

    async getAllLoanObjectives() {
        return await this.apiService.post<string[]>('NTechHost', 'Api/PreCredit/LoanObjectives/All', {});
    }
}

export class RunFraudControlResult {
    FraudControls: {
        CheckName: string;
        Values: string[];
        HasMatch: boolean;
        IsApproved: boolean;
    }[];
}

export interface PreScoreRecommendationModel {
    Version: number
    IsAcceptRecommended: boolean
    IsSlotNameMissing: boolean
    PolicyFilterResult: PolicyFilterEngineResult
    PolicyFilterDetailsDisplayItems: PolicyFilterDetailsDisplayItem[];
}

export interface NewCreditCheckResponse {
    Recommendation: CreditRecommendationModel;
    RecommendationTemporaryStorageKey: string;
}

export interface CreditRecommendationModel extends PolicyFilterCreditRecommendationModel {
    Version: number;
    DebtBurdenRatio: number;
    DebtBurdenRatioMissingReasonMessage: string;
    ProbabilityOfDefaultPercent: number;
    ProbabilityOfDefaultMissingReasonMessage: string;
}

export class SetCurrentCreditDecisionRequest {
    ApplicationNr: string;
    WasAutomated: boolean;
    SupressUserNotification: boolean;
    Offer: {
        SinglePaymentLoanRepaymentTimeInDays: number;
        PaidToCustomerAmount: number;
        SettleOtherLoansAmount: number;
        AnnuityAmount: number;
        RepaymentTimeInMonths: number;
        NominalInterestRatePercent: number;
        ReferenceInterestRatePercent: number;
        NotificationFeeAmount: number;
        InitialFeeCapitalizedAmount: number;
        InitialFeeDrawnFromLoanAmount: number;
        FirstNotificationCosts: { Code: string, Value: number }[]
    };
    Rejection: {
        OtherText: string;
        RejectionReasons: { Code: string; DisplayName: string }[];
    };
    RecommendationTemporaryStorageKey: string;
}

export interface UnsecuredLoanApplicationInitialDataModel extends StandardApplicationInitialDataModelBase {
    CurrentCreditDecisionItems: { ItemName: string; Value: string; IsRepeatable: string }[];
    CurrentReferenceInterestRatePercent: number;
    CurrentCreditDecisionRecommendation: CreditRecommendationModel;
    PreScoreRecommendation: PreScoreRecommendationModel;
}

export interface BuyCreditReportRequest {
    providerName: string;
    customerId: number;
    returningItemNames: string[];
    additionalParameters?: StringDictionary;
    reasonType: string;
    reasonData: string;
}

export class CreditReport {
    AgeInDays: number;
    CanFetchTabledValues: boolean;
    CreditReportId: string;
    RequestDate: Date;
}
