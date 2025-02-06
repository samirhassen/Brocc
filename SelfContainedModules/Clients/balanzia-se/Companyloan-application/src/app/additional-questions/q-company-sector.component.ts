import { Component } from '@angular/core';
import { QTemplateRadioStepComponent, TemplateRadioStepOptionModel } from './q-template-radio-step/q-template-radio-step.component';
import { StepRouteModel } from 'src/app/backend/application-step';
import { QuestionsStepRouteModel } from 'src/app/backend/questions-step';

@Component({
    selector: 'q-company-sector',
    templateUrl: './q-template-radio-step/q-template-radio-step.component.html',
    styleUrls: []
  })
export class QSectorNameComponent  extends QTemplateRadioStepComponent  {
    public pText: string = null
    
    public labelText: string = 'I vilken bransch är ni verksamma?'

    protected getCurrentModelValue(): string {
        return this.questions.sector ? this.questions.sector.sectorName : null
    }

    protected setCurrentModelValue(v: string) {
        this.questions.sector = {
            sectorName: v
        }
    }

    protected getStepName(): string {
        return 'q-sector-name'
    }

    protected getNextStep(): QuestionsStepRouteModel {
        return new StepRouteModel('psp-option')
    }

    protected getPreviousStep(): QuestionsStepRouteModel {
        return new StepRouteModel('pep')
    }

    public useDropdown() {
        return true
    }

    public options: TemplateRadioStepOptionModel[] = TemplateRadioStepOptionModel.fromSimpleArray([
        'Bygg / Anläggning / Infrastruktur',
        'Data / Teknik / IT',
        'Ekonomi / Finans',
        'Fastigheter',
        'Finansiell verksamhet',
        'Hotell / Restaurang / Turism',
        'HR / Personal',
        'Import / Export',
        'Juridik',
        'Kundsupport / Service',
        'Lantbruk / skogsbruk',
        'Ledning / Management',
        'Logistik / Transport',
        'Marknad / Reklam',
        'Tillverkning / Produktion',
        'Utbildning',
        'Kommun / Landsting / Stat',
        'Förbund',
        'Stiftelse',
        'Börsbolag',
        'Annat'
    ])
}