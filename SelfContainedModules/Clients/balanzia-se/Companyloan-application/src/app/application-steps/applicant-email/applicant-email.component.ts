import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'applicant-email',
  templateUrl: './applicant-email.component.html',
  styleUrls: []
})
export class ApplicantEmailComponent  extends ApplicationStep<ApplicantEmailFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            email: ['', [Validators.required, this.validationService.getEmailValidator()] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: ApplicantEmailFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): ApplicantEmailFormDataModel {
        return {
            email: this.application.applicantEmail
        }
    }
    protected updateApplicationFromForm(formData: ApplicantEmailFormDataModel) {
        this.application.setApplicantEmail(formData.email)
    }
    protected getStepName(): string {
        return 'applicant-email'
    }
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('applicant-phone')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('purpose')
    }
    
}

export class ApplicantEmailFormDataModel {
    email: string
}
