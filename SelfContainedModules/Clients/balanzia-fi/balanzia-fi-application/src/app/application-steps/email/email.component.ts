import { Component } from '@angular/core';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';
import { Validators, FormGroup } from '@angular/forms';

@Component({
  selector: 'email',
  templateUrl: './email.component.html',
  styleUrls: []
})
export class EmailComponent extends ApplicationApplicantStep<EmailFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            email: ['', [Validators.required, this.validationService.getEmailValidator()] ]
        })
    }

    protected getIsForwardAllowed(formData: EmailFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): EmailFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.email ? { email: a.email } : null        
    }

    protected updateApplicationFromForm(formData: EmailFormDataModel) {
        this.application.setDataEmail(formData.email, this.applicantNr)
    }

    protected getStepName(): string {
        return 'email'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('phone', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('ssn', this.applicantNr)
    }
}

class EmailFormDataModel {
    email: string
}