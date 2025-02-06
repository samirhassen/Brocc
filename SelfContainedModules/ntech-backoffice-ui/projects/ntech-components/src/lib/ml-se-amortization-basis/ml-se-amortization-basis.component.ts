import { Component, Input, OnInit, SimpleChanges } from '@angular/core';

@Component({
    selector: 'ml-se-amortization-basis',
    templateUrl: './ml-se-amortization-basis.component.html',
    styles: [],
})
export class MlSeAmortizationBasisComponent implements OnInit {
    constructor() {}

    @Input()
    public initialData: MlSeAmortizationBasisComponentInitialData;

    public m: Model;

    ngOnInit(): void {}

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let basis = this.initialData.basis;

        this.m = {
            highlightCreditNr: this.initialData.highlightCreditNr,
            balanceDate: basis.balanceDate,
            propertyIdWithLabel: basis.propertyIdWithLabel,
            basis: basis.amortizationBasis,
            underlag: basis.amorteringsunderlag,
        };
    }

    public getRuleDescription(loan: MortgageLoanSeAmortizationBasisLoan) {
        let d = '';
        d += (() => {
            switch (loan.ruleCode) {
                case 'r201616':
                    return 'Amorteringskrav';
                case 'r201723':
                    return 'Skärpt amorteringskrav';
                case 'none':
                    return 'Inget amorteringskrav';
                default:
                    return loan.ruleCode;
            }
        })();

        if (loan.isUsingAlternateRule) {
            d += ' + Alternativregeln';
        }

        return d;
    }

    public isSecondLoanRowNeeded(loan: MortgageLoanSeAmortizationBasisLoan) {
        return loan.currentCapitalBalanceAmount !== loan.maxCapitalBalanceAmount;
    }
}

export interface MlSeAmortizationBasisModel {
    propertyId: string;
    propertyIdWithLabel: string;
    amortizationBasis: MortgageLoanSeAmortizationBasisModel;
    amorteringsunderlag: SwedishAmorteringsunderlag;
    balanceDate: string;
}

export interface MlSeAmortizationBasisComponentInitialData {
    highlightCreditNr?: string;
    basis: MlSeAmortizationBasisModel;
}

interface Model {
    highlightCreditNr?: string;
    balanceDate: string;
    propertyIdWithLabel: string;
    basis: MortgageLoanSeAmortizationBasisModel;
    underlag: SwedishAmorteringsunderlag;
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

interface MortgageLoanSeAmortizationBasisLoan {
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
