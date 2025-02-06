import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Injectable({
    providedIn: 'root',
})
export class MlAmortizationSeService {
    constructor(private apiService: NtechApiService) {}

    public raw(): NtechApiService {
        return this.apiService;
    }

    public async loadAmortizationBasisSe(creditNr: string, useUpdatedBalance: boolean, includeHistory: boolean) {
        return this.apiService.post<LoadAmortizationBasisSeResult>(
            'NTechHost',
            'Api/Credit/SeMortgageLoans/Get-AmortizationBasis',
            { creditNr: creditNr, useUpdatedBalance: useUpdatedBalance, includeHistory: includeHistory }
        );
    }

    public commitRevaluate(newBasis: MortgageLoanSeAmortizationBasisModel) {
        return this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/Commit-Revaluate', {
            newBasis: newBasis,
        });
    }

    public computeRevaluate(request: {
        creditNr: string;
        currentCombinedYearlyIncomeAmount: number;
        otherMortageLoansAmount: number;
        newValuationAmount: number | null;
        newValuationDate: string | null;
    }) {
        return this.apiService.post<{
            mlStandardSeRevaluationCalculateResult: {
                newBasis: MortgageLoanSeAmortizationBasisModel;
                newAmorteringsunderlag: SwedishAmorteringsunderlag;
            };
            mlStandardSeRevaluationKeepExistingRuleCodeResult: {
                newBasis: MortgageLoanSeAmortizationBasisModel;
                newAmorteringsunderlag: SwedishAmorteringsunderlag;
            };
            isKeepExistingRuleCodeAllowed: boolean;
        }>('NTechHost', 'Api/Credit/SeMortgageLoans/Compute-Revaluate', request);
    }

    public async getCurrentLoans(creditNr: string) {
        return this.apiService.post<{ loans: MortgageLoanSeCurrentLoanModel[] }>(
            'NTechHost',
            'Api/Credit/SeMortgageLoans/Current-Collateral-Loans',
            { creditNr: creditNr }
        );
    }

    public setAmortizationExceptions(request: MortageLoanSeSetExceptionsRequest) {
        return this.apiService.post<{ businessEventId: number }>(
            'NTechHost',
            'Api/Credit/SeMortgageLoans/Set-AmortizationExceptions',
            request
        );
    }
}

export function getAmortizationRuleCodeDisplayName(ruleCode: string, isUsingAlternateRule?: boolean) {
    let baseCode = (() => {
        if (ruleCode === 'none') {
            return 'Inget amorteringskrav';
        } else if (ruleCode === 'r201616') {
            return 'Amorteringskrav';
        } else if (ruleCode === 'r201723') {
            return 'Skärpt amorteringskrav';
        } else {
            return ruleCode;
        }
    })();
    return isUsingAlternateRule ? `${baseCode}, enligt alternativregeln` : baseCode;
}

export interface MortgageLoanSeAmortizationBasisLoan {
    creditNr: string;
    currentCapitalBalanceAmount: number;
    maxCapitalBalanceAmount: number;
    ruleCode: string;
    isUsingAlternateRule: boolean;
    monthlyAmortizationAmount: number;
    interestBindMonthCount: number;
}

export interface SwedishAmorteringsunderlag {
    amorteringsgrundandeVarde: number;
    datumAmorteringsgrundandeVarde: string;
    amorteringsgrundandeSkuld: number;
    skuldEjOmfattasAmorteringskrav: number;
    skuldOmfattasAmorteringskrav: number;
    skuldOmfattasSkarptAmorteringskrav: number;
    varavOvanAlternativRegeln: number;
    totalAmorteringObjektet: number;
    totalAmorteringKravObjektetBankenHuvud: number;
    totalAmorteringKravObjektetBankenAlternativ: number;
}

export interface MortgageLoanSeAmortizationBasisModel {
    objectValueDate: string;
    objectValue: number;
    ltiFraction: number;
    ltvFraction: number;
    currentCombinedYearlyIncomeAmount: number;
    otherMortageLoansAmount: number;
    loans: MortgageLoanSeAmortizationBasisLoan[];
}

export interface LoadAmortizationBasisSeResult {
    propertyId: string;
    propertyIdWithLabel: string;
    amortizationBasis: MortgageLoanSeAmortizationBasisModel;
    amorteringsunderlag: SwedishAmorteringsunderlag;
    balanceDate: string;
    collateralId: number
}

export interface MortgageLoanSeCurrentLoanModel {
    creditNr: string;
    currentCapitalBalanceAmount: number;
    actualFixedMonthlyPayment: number;
    isActive: boolean;
    amortizationException?: MortgageLoanSeAmortizationExceptionModel;
}

export interface MortgageLoanSeAmortizationExceptionModel {
    amortizationAmount: number;
    untilDate: string;
    reasons: string[];
}

export interface MortageLoanSeSetExceptionsRequest {
    credits: {
        creditNr: string;
        hasException: boolean;
        exception?: MortgageLoanSeAmortizationExceptionModel;
    }[];
}
