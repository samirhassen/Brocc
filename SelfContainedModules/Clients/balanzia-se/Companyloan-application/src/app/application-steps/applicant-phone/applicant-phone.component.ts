import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'applicant-phone',
  templateUrl: './applicant-phone.component.html',
  styleUrls: []
})
export class ApplicantPhoneComponent  extends ApplicationStep<ApplicantPhoneFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            phone: ['', [Validators.required, this.validationService.getPhoneValidator()] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: ApplicantPhoneFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): ApplicantPhoneFormDataModel {
        return {
            phone: this.application.applicantPhone
        }
    }
    protected updateApplicationFromForm(formData: ApplicantPhoneFormDataModel) {
        this.application.setApplicantPhone(formData.phone)
    }
    protected getStepName(): string {
        return 'applicant-phone'
    }
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('company-age')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('applicant-email')
    }
    
}

export class ApplicantPhoneFormDataModel {
    phone: string
}
