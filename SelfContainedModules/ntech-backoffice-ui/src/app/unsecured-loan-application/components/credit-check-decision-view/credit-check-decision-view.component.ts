import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { PaymentOrderService } from 'src/app/common-services/payment-order-service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

@Component({
    selector: 'credit-check-decision-view',
    templateUrl: './credit-check-decision-view.component.html',
    styles: [],
})
export class CreditCheckDecisionViewComponent {
    constructor(public config: ConfigService, private paymentOrderService: PaymentOrderService, private validationService: NTechValidationService) {}

    @Input()
    public application: StandardCreditApplicationModel;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if(!this.application) {
            return;
        }
        let m: Model = {
            application: this.application
        };

        let firstNotificationCostDecisionItems = this.application.getFirstNotificationCostCreditDecisionItems();
        if(firstNotificationCostDecisionItems.length > 0) {
            let customCosts = await this.paymentOrderService.getCustomCosts();
            m.firstNotificationCost = {
                costs: firstNotificationCostDecisionItems.map(x => ({                    
                    text: customCosts.find(y => y.code === x.costCode)?.text ?? x.costCode,
                    amount: this.validationService.parseDecimalOrNull(x.value, false),
                }))
            }
        }

        this.m = m;
    }
}

interface Model {
    application: StandardCreditApplicationModel
    firstNotificationCost ?: {
        costs: { text: string, amount: number }[]
    }
}
