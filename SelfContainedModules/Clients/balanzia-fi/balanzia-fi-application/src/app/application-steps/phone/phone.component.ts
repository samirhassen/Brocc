import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'phone',
  templateUrl: './phone.component.html',
  styleUrls: []
})
export class PhoneComponent extends ApplicationApplicantStep<PhoneFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            phone: ['', [Validators.required, this.validationService.getPhoneValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: PhoneFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): PhoneFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.phone ? {phone: a.phone} : null
    }

    protected updateApplicationFromForm(formData: PhoneFormDataModel) {
        this.application.setDataPhone(formData.phone, this.applicantNr)
    }

    protected getStepName(): string {
        return 'phone'
    }

    protected getNextStep(): StepRouteModel {
        if(this.applicantNr === 1) {
            return new StepRouteModel('housing', this.applicantNr)
        } else {
            return new StepRouteModel('cost-of-living', this.applicantNr)
        }
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('email', this.applicantNr)
    }
}

export class PhoneFormDataModel {
    phone: string
}
