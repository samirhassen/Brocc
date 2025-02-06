import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
  selector: 'purpose',
  templateUrl: './purpose.component.html',
  styleUrls: []
})
export class PurposeComponent  extends ApplicationStep<PurposeFormDataModel>  {
    purposes: string[] = ['Anställa personal', 'Finansiera skuld', 'Förvärv', 
        'Generell likviditet/kassaflöde', 'Hemsida/marknadsföring', 
        'Inköp av lager', 'Oväntade utgifter', 'Renovering', 
        'Säsongsinvestering', 'Annat']
    
    protected createForm(): FormGroup {
        return this.form = this.fb.group({
            purpose: ['', [Validators.required] ]
        })
    }    
    
    protected getIsForwardAllowed(formData: PurposeFormDataModel): boolean {
        return this.form.valid
    }
    protected getFormUpdateFromApplication(): PurposeFormDataModel {
        return {
            purpose: this.application.purpose
        }
    }
    protected updateApplicationFromForm(formData: PurposeFormDataModel) {
        this.application.setPurpose(formData.purpose)
    }
    protected getStepName(): string {
        return 'purpose'
    }
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('applicant-email')
    }
    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('repayment-time')
    }    
}

export class PurposeFormDataModel {
    purpose: string
}
