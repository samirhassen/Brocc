import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { LoadAmortizationBasisSeResult, MlAmortizationSeService, MortgageLoanSeAmortizationBasisModel, SwedishAmorteringsunderlag } from '../../ml-amortization-se.service';
import { MlSeAmortizationBasisLoanInitialDataModel } from '../ml-se-amortization-basis-loan/ml-se-amortization-basis-loan.component';
import { ToggleBlockInitialData } from 'src/app/common-components/toggle-block/toggle-block.component';

@Component({
    selector: 'ml-se-amortization-basis',
    templateUrl: './ml-se-amortization-basis.component.html',
    styles: [
    ]
})
export class MlSeAmortizationBasisComponent implements OnInit {
    constructor(private apiService: MlAmortizationSeService) { }

    @Input()
    public creditNr: string;

    @Input()
    public preloadedBasis: LoadAmortizationBasisSeResult

    @Input()
    public reloadWithUpdatedBalance: boolean

    public m: Model;

    ngOnInit(): void {
    }

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if(!(this.creditNr || this.preloadedBasis)) {
            return;
        }
        
        let result = this.preloadedBasis 
            ? this.preloadedBasis 
            : (await this.apiService.loadAmortizationBasisSe(this.creditNr, !!this.reloadWithUpdatedBalance, true));
        
        this.m =  {
            creditNr: this.creditNr,
            balanceDate: result.balanceDate,
            propertyIdWithLabel: result.propertyIdWithLabel,
            currentBasis: {
                basis: result.amortizationBasis,
                underlag: result.amorteringsunderlag
            },
            historyInitialData: {
                headerText: 'Historik',
                useTransparentBackground: true,
                onExpandedToggled: async isExpanded => {
                    if(!isExpanded) {
                        return;
                    }
                    let historyItems = await this.apiService.raw().post<{
                        transactionDate: string,
                        amortizationBasis: MortgageLoanSeAmortizationBasisModel,
                        amorteringsunderlag: SwedishAmorteringsunderlag                        
                    }[]>('NTechHost', 'Api/Credit/SeMortgageLoans/Get-AmortizationBasisHistory', { collateralId: result.collateralId });
                    this.m.historicalBasis = historyItems.map(x => ({
                        transactionDate: x.transactionDate,
                        basisData: {
                            basis: x.amortizationBasis,
                            underlag: x.amorteringsunderlag
                        }
                    }));
                }
            },
            historicalBasis: null
        }
    }
}


interface Model {
    creditNr: string
    balanceDate: string
    propertyIdWithLabel: string
    currentBasis: MlSeAmortizationBasisLoanInitialDataModel
    historyInitialData: ToggleBlockInitialData
    historicalBasis: {
        basisData: MlSeAmortizationBasisLoanInitialDataModel
        transactionDate: string
    }[]
}