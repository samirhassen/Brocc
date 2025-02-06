import { Component } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';

@Component({
  selector: 'other-loans-options',
  templateUrl: './other-loans-options.component.html',
  styleUrls: []
})
export class OtherLoansOptionsComponent extends ApplicationApplicantStep<OtherLoanOptionsFormDataModel> {
    loanTypes: string[] = ['mortgageLoan', 'carOrBoatLoan', 'creditCard', 'otherLoan']

    protected createForm(): FormGroup {
        return this.fb.group({
            mortgageLoan: new FormControl(false),
            carOrBoatLoan: new FormControl(false),
            creditCard: new FormControl(false),
            otherLoan: new FormControl(false)
          });
    }

    protected getIsForwardAllowed(formData: OtherLoanOptionsFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): OtherLoanOptionsFormDataModel {        
        let a = this.getApplicant()
        if(a.hasOtherLoans === true && a.otherLoansOptions) {
            let f: any = {}
            for(let t of a.otherLoansOptions) {
                f[t] = true
            }
            return f
        } else {
            return null
        }
    }

    protected updateApplicationFromForm(formData: OtherLoanOptionsFormDataModel) {
        let f: any = formData
        let r : string[] = []
        for(let loanType of this.loanTypes) {
            if(formData[loanType] === true) {
                r.push(loanType)
            }
        }
        this.application.setDataOtherLoansOptions(r, this.applicantNr)
    }

    protected getStepName(): string {
        return 'other-loans-options'
    }

    protected getNextStep(): StepRouteModel {
        let a = this.getApplicant()
        return new StepRouteModel('other-loans-amount', this.applicantNr, a.otherLoansOptions[0])
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('has-other-loans', this.applicantNr)
    }
}

export class OtherLoanOptionsFormDataModel {
    mortgageLoan: boolean
    carOrBoat: boolean
    student: boolean
    creditCard: boolean
    other: boolean
}