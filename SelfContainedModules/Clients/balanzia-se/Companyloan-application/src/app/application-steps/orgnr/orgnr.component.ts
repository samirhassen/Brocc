import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'orgnr',
  templateUrl: './orgnr.component.html',
  styleUrls: []
})
export class OrgnrComponent  extends ApplicationStep<OrgnrFormDataModel>  {
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            orgnr: ['', [Validators.required, this.validationService.getOrgnrValidator()] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: OrgnrFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): OrgnrFormDataModel {
        return {
            orgnr: this.application.companyOrgnr
        }
    }
    protected updateApplicationFromForm(formData: OrgnrFormDataModel) {
        this.application.setOrgnr(formData.orgnr)
    }
    protected getStepName(): string {
        return 'orgnr'
    }
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('loan-amount')
    }
    protected getPreviousStep(): StepRouteModel {
        return null
    }
    
}

export class OrgnrFormDataModel {
    orgnr: string
}
