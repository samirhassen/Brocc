import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'ssn',
  templateUrl: './ssn.component.html',
  styleUrls: []
})
export class SsnComponent extends ApplicationApplicantStep<SsnFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            ssn: ['', [Validators.required, this.validationService.getCivicNrValidator(), this.getOtherApplicantSsnBannedValidator()] ]
        })
    }

    private getOtherApplicantSsnBannedValidator() {
        let otherApplicantSsn : string = null
        if(this.application.nrOfApplicants && this.application.nrOfApplicants.value > 1) {
            let otherApplicant = this.application.getApplicant(this.applicantNr === 1 ? 2 : 1)
            if(otherApplicant.ssn) {
                otherApplicantSsn = otherApplicant.ssn
            }
        }
        return this.validationService.getBannedValueValidator('duplicateSsn', otherApplicantSsn)
    }

    public isDuplicateSsn() {
        return this.form && this.form.controls && this.form.controls['ssn'] && !!this.form.controls['ssn'].getError('duplicateSsn') 
    }
    
    protected getIsForwardAllowed(formData: SsnFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): SsnFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.ssn ? { ssn: a.ssn } : null
    }

    protected updateApplicationFromForm(formData: SsnFormDataModel) {
        this.application.setDataSsn(formData.ssn, this.applicantNr)
    }

    protected getStepName(): string {
        return 'ssn'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('email', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.applicantNr === 1) {
            if (this.application.skipCampaignStep){
                return new StepRouteModel('nr-of-applicants')
            }
            else if(!this.application.campaignCodeOrChannel) {
                return new StepRouteModel('campaign-code')
            } else if(this.application.campaignCodeOrChannel.isChannel) {
                return new StepRouteModel('campaign-code-channel')
            } else {
                return new StepRouteModel('campaign-code-code')
            }            
        } else {
            let a = this.application.getApplicant(1)
            if(a.hasOtherLoans === true && a.otherLoansOptions) {
                return new StepRouteModel('other-loans-amount', a.applicantNr, a.otherLoansOptions[a.otherLoansOptions.length-1])
            } else {
                return new StepRouteModel('has-other-loans', a.applicantNr)
            }
        }
    }
}

class SsnFormDataModel {
    ssn: string
}