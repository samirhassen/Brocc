import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-currency-exchange',
  templateUrl: './q-currency-exchange.component.html',
  styleUrls: []
})
export class QCurrencyExchangeComponent  extends QuestionsStep<CurrencyExchangeFormDataModel>  {
    protected createForm(formData: CurrencyExchangeFormDataModel): FormGroup {        
        return this.fb.group({
            description: ['', [Validators.required] ]
        })
    }    

    protected getIsForwardAllowed(formData: CurrencyExchangeFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CurrencyExchangeFormDataModel {
        let p = this.questions.currencyExchange
        return {
            description: p ? (p.hasCurrencyExchange ? p.description : '') :  ''
        }
    }    

    protected updateQuestionsFromForm(formData: CurrencyExchangeFormDataModel) {
        this.questions.currencyExchange = {
            hasCurrencyExchange: true,
            description: formData.description
        }
    }

    protected getStepName(): string {
        return 'q-currency-exchange'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('employee-count')     
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('currency-exchange-option')
    }
}

export class CurrencyExchangeFormDataModel {
    description: string
}
