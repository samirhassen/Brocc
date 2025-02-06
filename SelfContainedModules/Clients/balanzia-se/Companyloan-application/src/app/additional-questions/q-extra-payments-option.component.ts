import { Component } from '@angular/core';
import { QTemplateRadioStepComponent, TemplateRadioStepOptionModel } from './q-template-radio-step/q-template-radio-step.component';
import { StepRouteModel } from 'src/app/backend/application-step';
import { QuestionsStepRouteModel } from 'src/app/backend/questions-step';

@Component({
    selector: 'q-extra-payments',
    templateUrl: './q-template-radio-step/q-template-radio-step.component.html',
    styleUrls: []
  })
export class QExtraPaymentsComponent  extends QTemplateRadioStepComponent  {
    public options: TemplateRadioStepOptionModel[] = TemplateRadioStepOptionModel.createYesNoOptions()

    public pText: string = null
    
    public labelText: string = 'Kan vi förvänta oss att ni kommer amortera/betala snabbare än vad som krävs?'

    protected getCurrentModelValue(): string {
        return this.questions.extraPayments ? (this.questions.extraPayments.areExtraPaymentsExpected ? 'yes' : 'no') : ''
    }

    protected setCurrentModelValue(v: string) {
        this.questions.extraPayments = {
            areExtraPaymentsExpected : v === 'yes'
        }
    }

    protected getStepName(): string {
        return 'q-extra-payments-option'
    }

    protected getNextStep(): QuestionsStepRouteModel {
        return new StepRouteModel('payment-source')
    }

    protected getPreviousStep(): QuestionsStepRouteModel {
        return new StepRouteModel('employee-count')
    }
}