import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { NullableNumber } from '../../backend/common.types';

@Component({
  selector: 'cost-of-living',
  templateUrl: './cost-of-living.component.html',
  styleUrls: []
})
export class CostOfLivingComponent extends ApplicationApplicantStep<CostOfLivingFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            costOfLiving: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: CostOfLivingFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): CostOfLivingFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.costOfLiving ? { costOfLiving: this.validationService.formatInteger(a.costOfLiving.value) } : null
    }

    protected updateApplicationFromForm(formData: CostOfLivingFormDataModel) {
        this.application.setDataCostOfLiving(new NullableNumber(this.validationService.parseInteger(formData.costOfLiving)), this.applicantNr)
    }

    protected getStepName(): string {
        return 'cost-of-living'
    }

    protected getNextStep(): StepRouteModel {
        if(this.applicantNr === 1) {
            return new StepRouteModel('marriage', 1)
        } else {
            return new StepRouteModel('employment', 2)
        }
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.applicantNr === 1) {
            return new StepRouteModel('housing', 1)
        } else {
            return new StepRouteModel('phone', 2)
        }
    }
}

class CostOfLivingFormDataModel {
    costOfLiving: string
}
