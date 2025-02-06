import { BehaviorSubject, Subscription } from 'rxjs';
import { FormGroup, FormBuilder } from '@angular/forms';
import { ActivatedRoute, Data } from '@angular/router';
import { Inject, OnInit } from '@angular/core';
import { API_SERVICE, ApiService } from './api-service';
import { NTechValidationService } from './ntech-validation.service';
import { ApplicationForwardBackService } from './application-forward-back.service';
import { QuestionsModel } from './questions-model';
import { environment } from 'src/environments/environment.embeddeddev';

export abstract class QuestionsStep<TFormDataModel> implements OnInit {
    questions: QuestionsModel
    form: FormGroup

    constructor(private route: ActivatedRoute,
        @Inject(API_SERVICE) protected apiService: ApiService,
        protected fb: FormBuilder,
        protected validationService: NTechValidationService,
        protected forwardBackService: ApplicationForwardBackService) {
           this.log('new()')
    }

    protected usesCustomFormPopulation() {
        return false
    }

    ngOnInit() {
        this.log('ngOnInit')
        this.subs.push(this.route.data.subscribe(x => {
            this.questions = x.questions
            this.onDataChanged(x)

            let d = this.getFormUpdateFromQuestions()
            this.form = this.createForm(d)
            if(!this.usesCustomFormPopulation()) {
                this.form.patchValue(d)
            }

            this.forwardBackService.isBackAllowed.next(!!this.getPreviousStep())
            this.forwardBackService.isForwardAllowed.next(this.getIsForwardAllowed(this.form.value))

            let onValueChange = () => {
                let f : TFormDataModel = this.form.value
                this.forwardBackService.isForwardAllowed.next(this.getIsForwardAllowed(f))
            }

            this.subs.push(this.form.statusChanges.subscribe(x => {
                if(x === 'PENDING') {
                    this.forwardBackService.isForwardAllowed.next(false)
                } else {
                    onValueChange()
                }
            }))
            
            this.subs.push(this.form.valueChanges.subscribe(x => {
                onValueChange()
            }))            
        }))

        this.subs.push(this.forwardBackService.onForward.subscribe(x => {
            this.forward()
        }))
        this.subs.push(this.forwardBackService.onBack.subscribe(x => {
            this.back()
        }))

        this.forwardBackService.isFinalStep.next(this.isFinalStep())
        this.afterNgOnInit()
    }

    ngOnDestroy() {
        this.log('ngOnDestroy')
        this.unsub()
    }

    subs: Subscription[] = []

    unsub() {
        if(this.subs) {
            for(let s of this.subs) {
                s.unsubscribe()
            }
            this.subs = []
        }
    }

    protected log(t: string) {
        if(environment.production) {
            return
        }
        console.log(this.getStepName() + ': ' + t)
    }

    protected abstract createForm(formData: TFormDataModel) : FormGroup    
    protected abstract getIsForwardAllowed(formData: TFormDataModel) : boolean
    protected abstract getFormUpdateFromQuestions() : TFormDataModel
    protected abstract updateQuestionsFromForm(formData: TFormDataModel)
    protected abstract getStepName(): string
    protected abstract getNextStep(): QuestionsStepRouteModel
    protected abstract getPreviousStep(): QuestionsStepRouteModel
    protected onDataChanged(x: Data) { }
    protected isFinalStep(): boolean { return false }
    protected afterNgOnInit() {

    }

    protected customForward(): boolean {
        return false
    }

    protected onForward() {
        this.log('onForward')

        let f : TFormDataModel = this.form.value
        this.updateQuestionsFromForm(f)
        this.apiService.saveQuestions(this.questions)
        if(this.customForward()) {
            return
        }
        let n = this.getNextStep()
        this.apiService.navigateToQuestionsRoute(n.stepName, this.questions.id)
    }

    forward() {
        this.onForward()
    }

    back() {
        let n = this.getPreviousStep()
        this.apiService.navigateToQuestionsRoute(n.stepName, this.questions.id)
    }
}

export class QuestionsStepRouteModel {
    constructor(public stepName: string)  {}
}