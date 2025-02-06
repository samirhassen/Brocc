import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'has-other-loans',
  templateUrl: './has-other-loans.component.html',
  styleUrls: []
})
export class HasOtherLoansComponent  extends ApplicationStep<HasOtherLoansFormDataModel>  {
    
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            hasLoans: ['', [Validators.required] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: HasOtherLoansFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): HasOtherLoansFormDataModel {
        return {
            hasLoans: this.application.otherLoans ? (this.application.otherLoans.hasLoans ? 'yes' : 'no') : ''
        }
    }
    protected updateApplicationFromForm(formData: HasOtherLoansFormDataModel) {
        this.application.setHasOtherLoans(formData.hasLoans === 'yes')
    }
    protected getStepName(): string {
        return 'has-other-loans'
    }
    protected getNextStep(): StepRouteModel {
        return new StepRouteModel(this.application.hasOtherLoans() ? 'other-loans-amount' : 'consent') 
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('company-revenue')
    }
}

export class HasOtherLoansFormDataModel {
    hasLoans: string
}
