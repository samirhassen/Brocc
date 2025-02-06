import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { NullableNumber } from '../../backend/common.types';

@Component({
  selector: 'income',
  templateUrl: './income.component.html',
  styleUrls: []
})
export class IncomeComponent extends ApplicationApplicantStep<IncomeFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            income: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: IncomeFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): IncomeFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)        
        return a.income ? { income: this.validationService.formatInteger(a.income.value) } : null
    }

    protected updateApplicationFromForm(formData: IncomeFormDataModel) {
        this.application.setDataIncome(new NullableNumber(this.validationService.parseInteger(formData.income)), this.applicantNr)
    }

    protected getStepName(): string {
        return 'income'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('has-other-loans', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('employment-details', this.applicantNr)
    }
}

class IncomeFormDataModel {
    income: string
}