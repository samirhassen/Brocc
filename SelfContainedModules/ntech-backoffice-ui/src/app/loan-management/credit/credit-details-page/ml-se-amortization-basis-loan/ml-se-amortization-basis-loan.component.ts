import { Component, Input, OnInit } from '@angular/core';
import { MortgageLoanSeAmortizationBasisLoan, MortgageLoanSeAmortizationBasisModel, SwedishAmorteringsunderlag } from '../../ml-amortization-se.service';

@Component({
    selector: 'ml-se-amortization-basis-loan',
    templateUrl: './ml-se-amortization-basis-loan.component.html',
    styles: [
    ]
})
export class MlSeAmortizationBasisLoanComponent implements OnInit {
    constructor() { }

    @Input()
    public initialData: MlSeAmortizationBasisLoanInitialDataModel

    ngOnInit(): void {
    }
    
    public getRuleDescription(loan: MortgageLoanSeAmortizationBasisLoan) {
        let d = '';
        d += (() => {
            switch(loan.ruleCode) {
                case 'r201616': return 'Amorteringskrav';
                case 'r201723': return 'Skärpt amorteringskrav';
                case 'none': return 'Inget amorteringskrav';
                default: return loan.ruleCode;
            }
        })();

        if(loan.isUsingAlternateRule) {
            d += ' + Alternativregeln';
        }
        
        return d;
    }

    public isSecondLoanRowNeeded(loan: MortgageLoanSeAmortizationBasisLoan) {        
        return loan.currentCapitalBalanceAmount !== loan.maxCapitalBalanceAmount;
    }
}

export interface MlSeAmortizationBasisLoanInitialDataModel {
    basis: MortgageLoanSeAmortizationBasisModel
    underlag: SwedishAmorteringsunderlag
}