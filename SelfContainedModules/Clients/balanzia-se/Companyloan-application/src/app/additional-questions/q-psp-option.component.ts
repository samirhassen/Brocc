import { Component } from '@angular/core';
import { QTemplateRadioStepComponent, TemplateRadioStepOptionModel } from './q-template-radio-step/q-template-radio-step.component';
import { StepRouteModel } from 'src/app/backend/application-step';
import { QuestionsStepRouteModel } from 'src/app/backend/questions-step';

@Component({
    selector: 'q-psp-option',
    templateUrl: './q-template-radio-step/q-template-radio-step.component.html',
    styleUrls: []
  })
export class QPspOptionComponent  extends QTemplateRadioStepComponent  {
    public options: TemplateRadioStepOptionModel[] = TemplateRadioStepOptionModel.createYesNoOptions()

    public pText: string = null
    
    public labelText: string = 'Bedriver ni verksamhet som betaltjänstleverantör?'

    protected getCurrentModelValue(): string {
        return this.questions.psp ? (this.questions.psp.isPaymentServiceProvider ? 'yes' : 'no') : ''
    }

    protected setCurrentModelValue(v: string) {
        this.questions.psp = {
            isPaymentServiceProvider : v === 'yes'
        }
    }

    protected getStepName(): string {
        return 'q-psp-option'
    }

    protected getNextStep(): QuestionsStepRouteModel {
        return new StepRouteModel('cashhandling-option')
    }

    protected getPreviousStep(): QuestionsStepRouteModel {
        return new StepRouteModel('company-sector')
    }
}