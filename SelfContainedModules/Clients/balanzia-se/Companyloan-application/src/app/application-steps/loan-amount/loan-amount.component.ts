import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'loan-amount',
  templateUrl: './loan-amount.component.html',
  styleUrls: []
})
export class LoanAmountComponent  extends ApplicationStep<LoanAmountFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            loanAmount: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: LoanAmountFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): LoanAmountFormDataModel {
        return {
            loanAmount: this.application.loanAmount ? this.validationService.formatInteger(this.application.loanAmount) : ''
        }
    }
    protected updateApplicationFromForm(formData: LoanAmountFormDataModel) {
        this.application.setLoanAmount(this.validationService.parseInteger(formData.loanAmount))
    }
    protected getStepName(): string {
        return 'loan-amount'
    }    
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('repayment-time')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('orgnr')
    }
    
}

export class LoanAmountFormDataModel {
    loanAmount: string
}
