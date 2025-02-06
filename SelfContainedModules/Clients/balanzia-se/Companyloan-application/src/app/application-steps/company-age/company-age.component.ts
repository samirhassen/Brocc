import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'company-age',
  templateUrl: './company-age.component.html',
  styleUrls: []
})
export class CompanyAgeComponent  extends ApplicationStep<CompanyAgeFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            companyAgeInMonths: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: CompanyAgeFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): CompanyAgeFormDataModel {
        return {
            companyAgeInMonths: this.application.companyAgeInMonths ? this.validationService.formatInteger(this.application.companyAgeInMonths) : ''
        }
    }
    protected updateApplicationFromForm(formData: CompanyAgeFormDataModel) {
        this.application.setCompanyAgeInMonths(this.validationService.parseInteger(formData.companyAgeInMonths))
    }
    protected getStepName(): string {
        return 'company-age'
    }    
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('company-result')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('applicant-phone')
    }
    
}

export class CompanyAgeFormDataModel {
    companyAgeInMonths: string
}
