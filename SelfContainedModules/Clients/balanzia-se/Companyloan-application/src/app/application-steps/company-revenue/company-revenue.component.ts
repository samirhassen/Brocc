import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'company-revenue',
  templateUrl: './company-revenue.component.html',
  styleUrls: []
})
export class CompanyRevenueComponent  extends ApplicationStep<CompanyRevenueFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            companyRevenue: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: CompanyRevenueFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): CompanyRevenueFormDataModel {
        return {
            companyRevenue: this.application.companyRevenue ? this.validationService.formatInteger(this.application.companyRevenue) : ''
        }
    }
    protected updateApplicationFromForm(formData: CompanyRevenueFormDataModel) {
        this.application.setCompanyRevenue(this.validationService.parseInteger(formData.companyRevenue))
    }
    protected getStepName(): string {
        return 'company-revenue'
    }    
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('has-other-loans')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('company-result')
    }
    
}

export class CompanyRevenueFormDataModel {
    companyRevenue: string
}
