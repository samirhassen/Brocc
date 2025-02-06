import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-cashhandling',
  templateUrl: './q-cashhandling.component.html',
  styleUrls: []
})
export class QCashHandlingComponent  extends QuestionsStep<CashHandlingFormDataModel>  {
    protected createForm(formData: CashHandlingFormDataModel): FormGroup {        
        return this.fb.group({
            volume: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ],
            description: ['', [Validators.required]]
        })
    }    

    protected getIsForwardAllowed(formData: CashHandlingFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CashHandlingFormDataModel {
        let p = this.questions.cashHandling
        return {
            volume: p.hasCashHandling ? this.validationService.formatInteger(p.volume) :  '',
            description: p.hasCashHandling ? p.description : ''
        }
    }    

    protected updateQuestionsFromForm(formData: CashHandlingFormDataModel) {
        this.questions.cashHandling = {
            hasCashHandling: true,
            volume: this.validationService.parseInteger(formData.volume) ,
            description: formData.description
        }
    }

    protected getStepName(): string {
        return 'q-cashhandling'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('currency-exchange-option')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('cashhandling-option')
    }
}

export class CashHandlingFormDataModel {
    volume: string
    description: string
}
