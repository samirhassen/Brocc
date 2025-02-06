import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'repayment-time',
  templateUrl: './repayment-time.component.html',
  styleUrls: []
})
export class RepaymentTimeComponent  extends ApplicationStep<RepaymentTimeFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            repaymentTime: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: RepaymentTimeFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): RepaymentTimeFormDataModel {
        return {
            repaymentTime: this.application.repaymentTimeInMonths ? this.validationService.formatInteger(this.application.repaymentTimeInMonths) : ''
        }
    }
    protected updateApplicationFromForm(formData: RepaymentTimeFormDataModel) {
        this.application.setRepaymentTime(this.validationService.parseInteger(formData.repaymentTime))
    }
    protected getStepName(): string {
        return 'repayment-time'
    }
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('purpose')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('loan-amount')
    }
    
}

export class RepaymentTimeFormDataModel {
    repaymentTime: string
}
