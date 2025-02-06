import { Component, OnInit, Inject } from '@angular/core';
import { Validators, FormGroup, FormBuilder } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';
import { QuestionsCollateralModel } from 'src/app/backend/questions-model';
import { ActivatedRoute } from '@angular/router';
import { API_SERVICE, ApiService } from 'src/app/backend/api-service';
import { NTechValidationService } from 'src/app/backend/ntech-validation.service';
import { ApplicationForwardBackService } from 'src/app/backend/application-forward-back.service';

export abstract class QTemplateRadioStepComponent  extends QuestionsStep<TemplateRadioStepFormDataModel>  {
    constructor(route: ActivatedRoute,
        @Inject(API_SERVICE) apiService: ApiService,
        fb: FormBuilder,
        validationService: NTechValidationService,
        forwardBackService: ApplicationForwardBackService) {
            super(route, apiService, fb, validationService, forwardBackService)
    }
    public abstract options : TemplateRadioStepOptionModel[]
    public abstract pText: string
    public abstract labelText: string
    public getSelectText() {
        return 'Välj'
    }
    protected abstract getCurrentModelValue(): string
    protected abstract setCurrentModelValue(v: string)

    public useDropdown() : boolean {
        return false
    }

    protected createForm(formData: TemplateRadioStepFormDataModel): FormGroup {        
        return this.fb.group({
            selectedOption: ['', [Validators.required] ]
        })
    }

    protected getFormUpdateFromQuestions(): TemplateRadioStepFormDataModel {
        let v = this.getCurrentModelValue()
        return {
            selectedOption: v ? v : ''
        }
    }    

    protected updateQuestionsFromForm(formData: TemplateRadioStepFormDataModel) {
        this.setCurrentModelValue(formData.selectedOption)
    }    

    protected getIsForwardAllowed(formData: TemplateRadioStepFormDataModel): boolean {
        return this.form.valid
    }
}

export class TemplateRadioStepFormDataModel {
    selectedOption: string
}

export class TemplateRadioStepOptionModel {
    constructor(public value: string, public displayText: string) {

    }

    public static fromSimpleArray(values: string[]) : TemplateRadioStepOptionModel[] {
        let a : TemplateRadioStepOptionModel[] = []
        for(let v of values) {
            a.push({
                value: v,
                displayText: v
            })
        }
        return a
    }

    public static createYesNoOptions() : TemplateRadioStepOptionModel[] {
        return [ new TemplateRadioStepOptionModel('yes', 'Ja'), new  TemplateRadioStepOptionModel('no', 'Nej')]
    }
}
