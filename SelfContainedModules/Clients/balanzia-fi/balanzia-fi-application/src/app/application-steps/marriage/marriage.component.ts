import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'marriage',
  templateUrl: './marriage.component.html',
  styleUrls: []
})
export class MarriageComponent extends ApplicationApplicantStep<MarriageFormDataModel> {
    marriageModels: MarriageUiModel[] = [
        new MarriageUiModel('marriage_gift'), 
        new MarriageUiModel('marriage_ogift'), 
        new MarriageUiModel('marriage_sambo')
    ]

    protected createForm(): FormGroup {
        return this.fb.group({
            marriage: ['', Validators.required]
        })
    }

    protected getIsForwardAllowed(formData: MarriageFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): MarriageFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.marriage ? { marriage: a.marriage} : null
    }

    protected updateApplicationFromForm(formData: MarriageFormDataModel) {
        this.application.setDataMarriage(formData.marriage, this.applicantNr)
    }

    protected getStepName(): string {
        return 'marriage'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('nr-of-children', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('phone', this.applicantNr)
    }
}

export class MarriageFormDataModel {
    marriage: string
}

export class MarriageUiModel {
    public translationCode
    constructor(public code: string) {
        this.translationCode = `marriage.${code}`
    }    
}