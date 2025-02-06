import { Injectable } from "@angular/core";
import { NtechApiService } from "src/app/common-services/ntech-api.service";
import { TestPortalApiServiceService } from "src/app/test-portal/services/TestPortalApiService.service";

@Injectable({
    providedIn: 'root',
})
export class MortgageLoanSeService {
    constructor(private testApiService: TestPortalApiServiceService, private apiService: NtechApiService) {

    }

    public testPortal() : TestPortalApiServiceService {
        return this.testApiService;
    }

    public shared() {
        return this.testApiService.apiService.shared;
    }

    public createSwedishMortgageLoan(request: any) {
        return this.apiService.post<{ creditNrs: string[], collateralId: number }>('NTechHost', 'Api/Credit/SeMortgageLoans/Create', request);
    }

    public getAmortizationBasisForExistingCredit(creditNr: string, useUpdatedBalance: boolean) {
        return this.apiService.post<{ amortizationBasis: SeAmortizationBasis }>('NTechHost', 'Api/Credit/SeMortgageLoans/Get-AmortizationBasis', 
        { creditNr, useUpdatedBalance });
    }
    
    public calculateSwedishAmortizationBasis(request: {
        newLoans:{
            creditNr: string,
            currentBalanceAmount: number
          }[],
        existingLoans: {
            maxBalanceAmount: number,
            ruleCode: string,
            isUsingAlternateRule: boolean,
            monthlyAmortizationAmount: number,
            creditNr: string,
            currentBalanceAmount: number
          }[],
        objectValueAmount: number,
        objectValueDate: string,
        isNewObjectValueAmount: boolean,
        combinedYearlyIncomeAmount: number,
        otherMortageLoansBalanceAmount: number,
        ignoreAlternateRule: boolean,
        forceAlternateRuleEvenIfWorse: boolean,
        exception: {
          untilDate: string,
          maxMonthlyAmount: number,
          reason: string
        }
      }) {
        return this.apiService.post<SeAmortizationBasis>('NTechHost', 'Api/Credit/SeMortgageLoans/Calculate-AmortizationBasis', request);
    }

    public appendAlternateRuleLoansToAmortizationBasis(basis: SeAmortizationBasis, 
        newLoans: { creditNr: string, loanAmount: number }[], exception ?: {
            untilDate: string,
            maxMonthlyAmount: number,
            reason: string
          }) {
        return this.calculateSwedishAmortizationBasis({
            newLoans: newLoans.map(x => ({
                creditNr: x.creditNr,
                currentBalanceAmount: x.loanAmount
            })),
            existingLoans: basis.loans.map(x => ({
                creditNr: x.creditNr,
                maxBalanceAmount: x.maxCapitalBalanceAmount,
                currentBalanceAmount: x.currentCapitalBalanceAmount,
                isUsingAlternateRule: x.isUsingAlternateRule,
                ruleCode: x.ruleCode,
                monthlyAmortizationAmount: x.monthlyAmortizationAmount
            })),
            exception: exception,
            objectValueAmount: basis.objectValue,
            objectValueDate: basis.objectValueDate,
            otherMortageLoansBalanceAmount: basis.otherMortageLoansAmount,
            isNewObjectValueAmount: false,
            combinedYearlyIncomeAmount: basis.currentCombinedYearlyIncomeAmount,
            forceAlternateRuleEvenIfWorse: true,
            ignoreAlternateRule: false
        })
    }
}

export interface SeAmortizationBasis {
    objectValueDate: string
    objectValue: number
    ltiFraction: number
    ltvFraction: number
    currentCombinedYearlyIncomeAmount: number
    otherMortageLoansAmount: number
    loans: {
        creditNr: string
        currentCapitalBalanceAmount: number
        maxCapitalBalanceAmount: number
        ruleCode: string,
        isUsingAlternateRule: boolean
        monthlyAmortizationAmount: number
        monthlyExceptionAmortizationAmount: number
        amortizationExceptionReason: string
        amortizationExceptionUntilDate: string
    }[]
}