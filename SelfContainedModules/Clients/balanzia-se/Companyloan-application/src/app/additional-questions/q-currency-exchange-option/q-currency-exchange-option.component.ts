import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-currency-exchange-option',
  templateUrl: './q-currency-exchange-option.component.html',
  styleUrls: []
})
export class QCurrencyExchangeOptionComponent  extends QuestionsStep<CurrencyExchangeFormDataModel>  {
    protected createForm(formData: CurrencyExchangeFormDataModel): FormGroup {        
        return this.fb.group({
            currencyExchangeOption: ['', [Validators.required] ]
        })
    }    

    protected getIsForwardAllowed(formData: CurrencyExchangeFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CurrencyExchangeFormDataModel {
        return {
            currencyExchangeOption: this.questions.currencyExchange ? (this.questions.currencyExchange.hasCurrencyExchange ? 'yes' : 'no') :  ''
        }
    }    

    protected updateQuestionsFromForm(formData: CurrencyExchangeFormDataModel) {
        let prev = this.questions.currencyExchange
        let hasCurrencyExchange = formData.currencyExchangeOption === 'yes'
        this.questions.currencyExchange = {
            hasCurrencyExchange: hasCurrencyExchange,
            description: hasCurrencyExchange && prev ? prev.description : null
        }
    }

    protected getStepName(): string {
        return 'q-currency-exchange-option'
    }

    protected getNextStep(): StepRouteModel {
        if(this.questions.currencyExchange.hasCurrencyExchange) {
            return new StepRouteModel('currency-exchange')
        } else {
            return new StepRouteModel('employee-count')
        }        
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.questions.cashHandling && this.questions.cashHandling.hasCashHandling) {
            return new StepRouteModel('cashhandling')
        } else {
            return new StepRouteModel('cashhandling-option')
        }        
    }
}

export class CurrencyExchangeFormDataModel {
    currencyExchangeOption: string
}
