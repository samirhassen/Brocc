import { Component } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { StepRouteModel, ApplicationStep } from '../../backend/application-step';
import { NullableNumber } from '../../backend/common.types';

@Component({
  selector: 'consolidation-amount',
  templateUrl: './consolidation-amount.component.html',
  styleUrls: []
})
export class ConsolidationAmountComponent  extends ApplicationStep<ConsolidationAmountFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            consolidationAmount: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }

    protected getIsForwardAllowed(formData: ConsolidationAmountFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): ConsolidationAmountFormDataModel {
        let a = this.application
        return (a.hasConsolidation === true && a.consolidationAmount) ? {consolidationAmount: this.validationService.formatInteger(a.consolidationAmount.value) } : null
    }

    protected updateApplicationFromForm(formData: ConsolidationAmountFormDataModel) {
        this.application.setDataConsolidationAmount(new NullableNumber(this.validationService.parseInteger(formData.consolidationAmount)))
    }

    protected getStepName(): string {
        return 'consolidation-amount'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('consent')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('consolidation-option')
    }
}

export class ConsolidationAmountFormDataModel {
    consolidationAmount: string
}
