import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-cashhandling-option',
  templateUrl: './q-cashhandling-option.component.html',
  styleUrls: []
})
export class QCashHandlingOptionComponent  extends QuestionsStep<CashHandlingOptionFormDataModel>  {
    protected createForm(formData: CashHandlingOptionFormDataModel): FormGroup {        
        return this.fb.group({
            cashHandlingOption: ['', [Validators.required] ]
        })
    }    

    protected getIsForwardAllowed(formData: CashHandlingOptionFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CashHandlingOptionFormDataModel {
        return {
            cashHandlingOption: this.questions.cashHandling ? (this.questions.cashHandling.hasCashHandling ? 'yes' : 'no') :  ''
        }
    }    

    protected updateQuestionsFromForm(formData: CashHandlingOptionFormDataModel) {
        let prev = this.questions.cashHandling
        let hasCashHandling = formData.cashHandlingOption === 'yes'
        this.questions.cashHandling = {
            hasCashHandling: hasCashHandling,
            volume: hasCashHandling && prev ? prev.volume : null,
            description: hasCashHandling && prev ? prev.description : null
        }
    }

    protected getStepName(): string {
        return 'q-cashhandling-option'
    }

    protected getNextStep(): StepRouteModel {
        if(this.questions.cashHandling.hasCashHandling) {
            return new StepRouteModel('cashhandling')     
        } else {
            return new StepRouteModel('currency-exchange-option')
        }
        
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('psp-option')
    }
}

export class CashHandlingOptionFormDataModel {
    cashHandlingOption: string
}
