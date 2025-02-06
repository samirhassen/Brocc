import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary } from 'src/app/common.types';
import { PolicyFilterCreditRecommendationModel } from 'src/app/loan-policyfilters/services/policy-filters-apiservice';
import { SharedApplicationApiService } from 'src/app/shared-application-components/services/shared-loan-application-api.service';
import {
    StandardApplicationInitialDataModelBase,
    StandardApplicationModelBase,
} from 'src/app/shared-application-components/services/standard-application-base';
import { PropertyValuation } from '../components/ucbv-valuation-buyer/ucbv-valuation-process';
import {
    MortgageLoanApplicationLoanModel,
    StandardMortgageLoanApplicationModel,
} from './mortgage-loan-application-model';

export const MortgageLoanApplicationKycQuestionSourceType = 'MortgageLoanApplication';

@Injectable({
    providedIn: 'root',
})
export class MortgageLoanApplicationApiService extends SharedApplicationApiService {
    constructor(apiService: NtechApiService) {
        super(apiService, true);
    }

    public fetchApplicationInitialDataShared(
        applicationNr: string
    ): Promise<'noSuchApplicationExists' | StandardApplicationModelBase> {
        return this.fetchApplicationInitialData(applicationNr);
    }

    fetchApplicationInitialData(
        applicationNr: string
    ): Promise<StandardMortgageLoanApplicationModel | 'noSuchApplicationExists'> {
        return this.fetchApplicationInitialDataInternal(applicationNr).then((x) => {
            if (x === 'noSuchApplicationExists') return x;
            else return new StandardMortgageLoanApplicationModel(x);
        });
    }

    private fetchApplicationInitialDataInternal(
        applicationNr: string
    ): Promise<MortgageLoanApplicationInitialDataModel | 'noSuchApplicationExists'> {
        return this.apiService.post(
            'nPreCredit',
            'api/MortgageLoanStandard/FetchApplicationInitialData',
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

    public editProperty(request: MortgageApplicationEditPropertyRequestModel): Promise<{}> {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/Edit-Property', request);
    }

    public editGeneralData(request: MortgageApplicationEditGeneralData): Promise<{}> {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/Edit-Application-General-Data', request);
    }

    public newCreditCheck(applicationNr: string): Promise<MlNewCreditCheckResponse> {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/New-CreditCheck', { applicationNr });
    }

    public addTestPolicyFilterRuleSet(
        skipIfExists: boolean,
        overrideSlotName?: string
    ): Promise<{ WasCreated: boolean }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/Create-TestSet', {
            skipIfExists,
            overrideSlotName,
        });
    }

    setCurrentCreditDecision(request: MlSetCurrentCreditDecisionRequest): Promise<{ CreditDecisionId: number }> {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/Set-Current-CreditDecision', request);
    }

    addPropertyValuation(request: PropertyValuation): Promise<{ RowNumber: number }> {
        return this.apiService.post('nPreCredit', 'Api/MortgageLoanStandard/Add-Property-Valuation', request);
    }

    removeApplicationDocument(applicationNr: string, documentId: number) {
        return this.apiService.post('nPreCredit', 'api/ApplicationDocuments/Remove', {
            applicationNr: applicationNr,
            documentId: documentId,
        });
    }

    setApplicationDocumentVerified(
        applicationNr: string,
        documentId: number,
        isVerified: boolean
    ): Promise<ServerDocumentModel> {
        return this.apiService.post<ServerDocumentModel>('nPreCredit', 'api/ApplicationDocuments/SetVerified', {
            applicationNr: applicationNr,
            documentId: documentId,
            isVerified: isVerified,
        });
    }

    addApplicationDocument(
        applicationNr: string,
        documentType: string,
        documentSubType: string,
        dataUrl: string,
        filename: string
    ) {
        return this.apiService.post<ServerDocumentModel>('nPreCredit', 'api/ApplicationDocuments/AddAndRemove', {
            applicationNr: applicationNr,
            applicantNr: null,
            customerId: null,
            documentType: documentType,
            documentSubType: documentSubType,
            dataUrl: dataUrl,
            filename: filename,
        });
    }

    fetchApplicationDocuments(applicationNr: string, documentTypes: string[]): Promise<ServerDocumentModel[]> {
        return this.apiService.post('nPreCredit', 'api/ApplicationDocuments/FetchForApplication', {
            applicationNr: applicationNr,
            documentTypes: documentTypes,
        });
    }

    setIsApprovedWaitingForAdditionalInfoStep(applicationNr: string, isApproved: boolean) {
        return this.apiService.post(
            'nPreCredit',
            'api/MortgageLoanStandard/WaitingForAdditionalInfo/Set-Approved-Step',
            { applicationNr: applicationNr, isApproved: isApproved }
        );
    }

    setIsApprovedCollateralStep(applicationNr: string, isApproved: boolean) {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/Collateral/Set-Approved-Step', {
            applicationNr: applicationNr,
            isApproved: isApproved,
        });
    }

    public fetchMortageLoanCollaterals(filter: { customerIds?: number[]; collateralIds?: number[] }): Promise<{
        Collaterals: {
            CollateralId: number;
            CreditNrs: string[];
            CollateralItems: Dictionary<{ StringValue: string }>;
        }[];
        Credits: {
            CollateralId: number;
            CreditNr: string;
            CurrentCapitalBalance: number;
            Customers: {
                CustomerId: number;
                ApplicantNr: number;
                ListNames: string[];
            }[];
        }[];
    }> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/Fetch-Collaterals', { ...filter });
    }
}

export interface ServerDocumentModel {
    DocumentId: number;
    DocumentType: string;
    DocumentSubType: string;
    DocumentArchiveKey: string;
    Filename: string;
    VerifiedDate: string;
}

export class MortgageApplicationEditGeneralData {
    applicationNr: string;
    isPurchase: boolean;
    objectPriceAmount?: number;
    ownSavingsAmount?: number;
    additionalLoanAmount?: number;
    objectValueAmount?: number;
}

export class MortgageApplicationEditPropertyRequestModel {
    applicationNr: string;
    objectTypeCode: string;
    seBrfName: string;
    seBrfOrgNr: string;
    seBrfApartmentNr: string;
    objectLivingArea: number;
    objectMonthlyFeeAmount: number;
    objectOtherMonthlyCostsAmount: number;
    objectAddressStreet: string;
    objectAddressZipcode: string;
    objectAddressCity: string;
    objectAddressMunicipality: string;
    mortgageLoansToSettle: MortgageLoanApplicationLoanModel[];
}

export class MortgageApplicationsListAssignedHandlerModel {
    AssignedHandlerUserId: string;
    ExcludeAssignedApplications: boolean;
    ExcludeUnassignedApplications: boolean;
}

export interface MortgageLoanApplicationInitialDataModel extends StandardApplicationInitialDataModelBase {
    CurrentInitialCreditDecision: MlCreditDecision;
    CurrentFinalCreditDecision: MlCreditDecision;
    Settings: {
        IsPropertyValuationActive: boolean;
    };
}

export interface MlCreditDecision {
    IsFinal: boolean;
    IsCurrent: boolean;
    CreditDecisionItems: { ItemName: string; Value: string; IsRepeatable: boolean }[];
    Recommendation: MlCreditRecommendationModel;
}

export interface MlNewCreditCheckResponse {
    Recommendation: MlCreditRecommendationModel;
    RecommendationTemporaryStorageKey: string;
}

export interface MlCreditRecommendationModel extends PolicyFilterCreditRecommendationModel {
    Version: number;
    LoanToIncome: number;
    LoanToIncomeMissingReason: string;
    LoanToValue: number;
    LoanToValueMissingReason: string;
}

export class MlSetCurrentCreditDecisionRequest {
    ApplicationNr: string;
    WasAutomated: boolean;
    SupressUserNotification: boolean;
    InitialOffer?: {
        IsPurchase: boolean;
        ObjectPriceAmount: number;
        PaidToCustomerAmount: number;
        OwnSavingsAmount: number;
        SettlementAmount: number;
    };
    Rejection?: {
        OtherText: string;
        RejectionReasons: { Code: string; DisplayName: string }[];
    };
    FinalOffer?: {
        /* Use this for the final credit check */
    };
    RecommendationTemporaryStorageKey: string;
}
