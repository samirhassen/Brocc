import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'company-result',
  templateUrl: './company-result.component.html',
  styleUrls: []
})
export class CompanyResultComponent  extends ApplicationStep<CompanyResultFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            companyResult: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: CompanyResultFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): CompanyResultFormDataModel {
        return {
            companyResult: this.application.companyResult ? this.validationService.formatInteger(this.application.companyResult) : ''
        }
    }
    protected updateApplicationFromForm(formData: CompanyResultFormDataModel) {
        this.application.setCompanyResult(this.validationService.parseInteger(formData.companyResult))
    }
    protected getStepName(): string {
        return 'company-result'
    }    
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('company-revenue')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('company-age')
    }
    
}

export class CompanyResultFormDataModel {
    companyResult: string
}
