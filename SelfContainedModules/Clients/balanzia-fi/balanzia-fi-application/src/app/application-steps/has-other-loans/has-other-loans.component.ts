import { Component } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';

@Component({
  selector: 'has-other-loans',
  templateUrl: './has-other-loans.component.html',
  styleUrls: []
})
export class HasOtherLoansComponent extends ApplicationApplicantStep<HasOtherLoansFormDataModel> {
    protected createForm(): FormGroup {
        return new FormGroup({
            yesNo: new FormControl(),
          })
    }

    protected getIsForwardAllowed(formData: HasOtherLoansFormDataModel): boolean {
        return formData && formData.yesNo && formData.yesNo.length > 0
    }

    protected getFormUpdateFromApplication(): HasOtherLoansFormDataModel {
        let a = this.getApplicant()
        return (a.hasOtherLoans === true || a.hasOtherLoans === false) ? {yesNo: a.hasOtherLoans ? 'yes' : 'no'} : null
    }

    protected updateApplicationFromForm(formData: HasOtherLoansFormDataModel) {
        this.application.setDataHasOtherLoans(formData.yesNo === 'yes', this.applicantNr)
    }

    protected getStepName(): string {
        return 'has-other-loans'
    }

    protected getNextStep(): StepRouteModel {
        let a = this.getApplicant()
        if(a.hasOtherLoans === true) {
            return new StepRouteModel('other-loans-options', this.applicantNr)
        } else if(this.applicantNr == 1 && this.application.nrOfApplicants.value === 2) {
            return new StepRouteModel('ssn', 2)
        } else {
            return new StepRouteModel('consolidation-option')
        }
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('income', 1)
    }
}

export class HasOtherLoansFormDataModel {
    yesNo: string
}
