import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'other-loans-amount',
  templateUrl: './other-loans-amount.component.html',
  styleUrls: []
})
export class OtherLoansAmountComponent  extends ApplicationStep<OtherLoansAmountFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            loansAmount: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: OtherLoansAmountFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): OtherLoansAmountFormDataModel {
        return {
            loansAmount: this.application.hasOtherLoans() && this.application.otherLoans.loansAmount ? this.validationService.formatInteger(this.application.otherLoans.loansAmount): ''
        }
    }
    protected updateApplicationFromForm(formData: OtherLoansAmountFormDataModel) {
        this.application.setOtherLoansAmount(this.validationService.parseInteger(formData.loansAmount))
    }
    protected getStepName(): string {
        return 'other-loans-amount'
    }    
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('consent')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('has-other-loans')
    }
    
}

export class OtherLoansAmountFormDataModel {
    loansAmount: string
}
