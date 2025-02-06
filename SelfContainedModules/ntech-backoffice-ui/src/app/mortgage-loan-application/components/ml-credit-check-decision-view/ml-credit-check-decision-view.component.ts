import { Component, Input } from '@angular/core';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';

@Component({
    selector: 'ml-credit-check-decision-view',
    templateUrl: './ml-credit-check-decision-view.component.html',
    styles: [],
})
export class MlCreditCheckDecisionViewComponent {
    constructor(private validationService: NTechValidationService) {}

    @Input()
    public application: StandardMortgageLoanApplicationModel;

    @Input()
    public isFinal: boolean;

    getUniqueCreditDecisionItem(itemName: string) {
        return this.application?.getUniqueCurrentCreditDecisionItem(this.isFinal, itemName);
    }

    getRepeatableCreditDecisionItem(itemName: string) {
        return this.application?.getRepeatableCurrentCreditDecisionItem(this.isFinal, itemName);
    }

    hasSettledMortgageLoans() {
        let mortgageLoansToSettleAmount = this.validationService.parseIntegerOrNull(
            this.getUniqueCreditDecisionItem('mortgageLoansToSettleAmount')
        );
        return mortgageLoansToSettleAmount !== null && mortgageLoansToSettleAmount > 0;
    }

    isPurchase() {
        return this.getUniqueCreditDecisionItem('isPurchase') === 'true';
    }

    getLtl() {
        return this.validationService.parseIntegerOrNull(this.getUniqueCreditDecisionItem('leftToLiveOnAmount'));
    }

    getLtvPercent() {
        let ltv = this.validationService.parseDecimalOrNull(this.getUniqueCreditDecisionItem('loanToValue'), false);
        if (!ltv) {
            return null;
        }

        return ltv * 100;
    }
}
