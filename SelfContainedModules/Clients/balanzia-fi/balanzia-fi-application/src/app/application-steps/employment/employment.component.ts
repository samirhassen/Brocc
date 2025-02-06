import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'employment',
  templateUrl: './employment.component.html',
  styleUrls: []
})
export class EmploymentComponent  extends ApplicationApplicantStep<EmploymentFormDataModel> {
    employmentModels: EmploymentUiModel[] = [
        new EmploymentUiModel('employment_fastanstalld'), 
        new EmploymentUiModel('employment_visstidsanstalld'), 
        new EmploymentUiModel('employment_foretagare'),
        new EmploymentUiModel('employment_pensionar'), 
        new EmploymentUiModel('employment_sjukpensionar'),
        new EmploymentUiModel('employment_studerande'),
        new EmploymentUiModel('employment_arbetslos')
    ]

    protected createForm(): FormGroup {
        return this.fb.group({
            employment: ['', Validators.required]
        })
    }

    protected getIsForwardAllowed(formData: EmploymentFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): EmploymentFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.employment ? { employment: a.employment} : null
    }

    protected updateApplicationFromForm(formData: EmploymentFormDataModel) {
        this.application.setDataEmployment(formData.employment, this.applicantNr)
    }

    protected getStepName(): string {
        return 'employment'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('employment-details', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.applicantNr === 1) {
            return new StepRouteModel('nr-of-children', this.applicantNr)
        } else {
            return new StepRouteModel('cost-of-living', this.applicantNr)
        }        
    }
}

export class EmploymentFormDataModel {
    employment: string
}

export class EmploymentUiModel {
    public translationCode
    constructor(public code: string) {
        this.translationCode = `employment.${code}`
    }    
}