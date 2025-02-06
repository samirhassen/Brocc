import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'housing',
  templateUrl: './housing.component.html',
  styleUrls: []
})
export class HousingComponent extends ApplicationApplicantStep<HousingFormDataModel> {
    housingModels: HousingUiModel[] = [
        new HousingUiModel('housing_egenbostad'), 
        new HousingUiModel('housing_bostadsratt'), 
        new HousingUiModel('housing_hyresbostad'), 
        new HousingUiModel('housing_hosforaldrar'), 
        new HousingUiModel('housing_tjanstebostad')
    ]

    protected createForm(): FormGroup {
        return this.fb.group({
            housing: ['', Validators.required]
        })
    }

    protected getIsForwardAllowed(formData: HousingFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): HousingFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.housing ? { housing: a.housing} : null
    }

    protected updateApplicationFromForm(formData: HousingFormDataModel) {
        this.application.setDataHousing(formData.housing, this.applicantNr)
    }

    protected getStepName(): string {
        return 'housing'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('cost-of-living', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('phone', this.applicantNr)
    }
}

export class HousingFormDataModel {
    housing: string
}

export class HousingUiModel {
    public translationCode
    constructor(public code: string) {
        this.translationCode = `housing.${code}`
    }    
}