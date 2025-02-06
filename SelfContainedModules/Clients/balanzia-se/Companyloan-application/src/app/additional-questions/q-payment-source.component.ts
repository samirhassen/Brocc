import { Component } from '@angular/core';
import { QTemplateRadioStepComponent, TemplateRadioStepOptionModel } from './q-template-radio-step/q-template-radio-step.component';
import { StepRouteModel } from 'src/app/backend/application-step';
import { QuestionsStepRouteModel } from 'src/app/backend/questions-step';

@Component({
    selector: 'q-extra-payments',
    templateUrl: './q-template-radio-step/q-template-radio-step.component.html',
    styleUrls: []
  })
export class QPaymentSourceComponent  extends QTemplateRadioStepComponent  {
    public options: TemplateRadioStepOptionModel[] = [
        new TemplateRadioStepOptionModel('extracapital', 'Överskott av likviditet/kapital'),
        new TemplateRadioStepOptionModel('sale', 'Försäljning av tillgångar/verksamhet'),
        new TemplateRadioStepOptionModel('savings', 'Sparande från annat institut'),
        new TemplateRadioStepOptionModel('investments', 'Externa investeringar')
    ]

    public pText: string = null
    
    public labelText: string = 'Varifrån kommer de pengar som amorteras månatligen på lånet?'

    protected getCurrentModelValue(): string {
        return this.questions.paymentSource ? this.questions.paymentSource.paymentSourceCode : ''
    }

    protected setCurrentModelValue(v: string) {
        let opt = this.options.find(x => x.value === v)
        this.questions.paymentSource = {
            paymentSourceCode: opt.value,
            paymentSourceText: opt.displayText
        }
    }

    protected getStepName(): string {
        return 'q-payment-source'
    }

    protected getNextStep(): QuestionsStepRouteModel {
        return new StepRouteModel('bankaccount')
    }

    protected getPreviousStep(): QuestionsStepRouteModel {
        return new StepRouteModel('extra-payments-option')
    }
}